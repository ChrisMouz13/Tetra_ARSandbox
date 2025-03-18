using System.Collections;
using Orbbec;
using UnityEngine;
using UnityEngine.Events;

namespace OrbbecUnity
{
    [System.Serializable]
    public class PipelineInitEvent : UnityEvent { }

    public class OrbbecPipeline : MonoBehaviour
    {
        public OrbbecDevice orbbecDevice;
        public OrbbecProfile[] orbbecProfiles;
        public PipelineInitEvent onPipelineInit;

        private bool hasInit;
        private Pipeline pipeline;
        private Config config;
        private FramesetCallback framesetCallback;

        public bool HasInit
        {
            get
            {
                return hasInit;
            }
        }

        public Pipeline Pipeline
        {
            get
            {
                return pipeline;
            }
        }

        public Config Config
        {
            get
            {
                return config;
            }
        }

        void Start()
        {
            orbbecDevice.onDeviceFound.AddListener(InitPipeline);
        }

        void OnDestroy()
        {
            if (hasInit)
            {
                config.Dispose();
                pipeline.Dispose();
            }
        }

        private void InitPipeline(Device device)
        {
            pipeline = new Pipeline(device);
            InitConfig();
            hasInit = true;
            onPipelineInit?.Invoke();
        }

        private void InitConfig()
        {
            config = new Config();
            Debug.Log($"🔵 Starting InitConfig() with {orbbecProfiles.Length} profiles.");

            // 1️⃣ Φόρτωσε πρώτα το Depth Profile
            foreach (var profile in orbbecProfiles)
            {
                if (profile.sensorType == SensorType.OB_SENSOR_DEPTH)
                {
                    Debug.Log($"🔍 Checking Depth Profile: {profile.sensorType}, {profile.width}x{profile.height} {profile.format} @ {profile.fps}");
                    var streamProfile = FindProfile(profile, StreamType.OB_STREAM_DEPTH);
                    if (streamProfile != null)
                    {
                        Debug.Log($"✅ Found Depth Stream Profile: {streamProfile.GetWidth()}x{streamProfile.GetHeight()}@{streamProfile.GetFPS()}");
                        config.EnableStream(streamProfile);
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ No matching depth profile found!");
                    }
                }
            }

            // 2️⃣ Μετά φόρτωσε το IR Profile
            foreach (var profile in orbbecProfiles)
            {
                if (profile.sensorType == SensorType.OB_SENSOR_IR)
                {
                    Debug.Log($"🔍 Checking IR Profile: {profile.sensorType}, {profile.width}x{profile.height} {profile.format} @ {profile.fps}");
                    var irProfile = FindProfile(profile, StreamType.OB_STREAM_IR);
                    if (irProfile != null)
                    {
                        Debug.Log($"✅ Found IR Stream Profile: {irProfile.GetWidth()}x{irProfile.GetHeight()}@{irProfile.GetFPS()}");
                        config.EnableStream(irProfile);
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ No matching IR profile found!");
                    }
                }
            }
        }

        /*private void InitConfig()
        {
            config = new Config();
            for (int i = 0; i < orbbecProfiles.Length - 1; i++)
            {
                var streamProfile = FindProfile(orbbecProfiles[i], StreamType.OB_STREAM_COLOR);
                if (streamProfile != null)
                {
                    config.EnableStream(streamProfile);
                    break;
                }
            }
            for (int i = 0; i < orbbecProfiles.Length - 1; i++)
            {
                var streamProfile = FindProfile(orbbecProfiles[i], StreamType.OB_STREAM_DEPTH);
                if (streamProfile != null)
                {
                    config.EnableStream(streamProfile);
                    break;
                }
            }
            for (int i = 0; i < orbbecProfiles.Length - 1; i++)
            {
                var streamProfile = FindProfile(orbbecProfiles[i], StreamType.OB_STREAM_IR);
                if (streamProfile != null)
                {
                    config.EnableStream(streamProfile);
                    break;
                }
            }
            for (int i = 0; i < orbbecProfiles.Length - 1; i++)
            {
                var streamProfile = FindProfile(orbbecProfiles[i], StreamType.OB_STREAM_IR_LEFT);
                if (streamProfile != null)
                {
                    config.EnableStream(streamProfile);
                    break;
                }
            }
            for (int i = 0; i < orbbecProfiles.Length - 1; i++)
            {
                var streamProfile = FindProfile(orbbecProfiles[i], StreamType.OB_STREAM_IR_RIGHT);
                if (streamProfile != null)
                {
                    config.EnableStream(streamProfile);
                    break;
                }
            }
        }*/

        private VideoStreamProfile FindProfile(OrbbecProfile obProfile, StreamType streamType)
        {
            try
            {
                Debug.Log($"🔍 Trying to find profile: {obProfile.sensorType}, {obProfile.width}x{obProfile.height} {obProfile.format} @ {obProfile.fps}");

                var profileList = pipeline.GetStreamProfileList(obProfile.sensorType);
                VideoStreamProfile streamProfile = profileList.GetVideoStreamProfile(obProfile.width, obProfile.height, obProfile.format, obProfile.fps);

                if (streamProfile != null && streamProfile.GetStreamType() == streamType)
                {
                    Debug.LogFormat("✅ Profile found: {0}x{1}@{2} {3}",
                            streamProfile.GetWidth(),
                            streamProfile.GetHeight(),
                            streamProfile.GetFPS(),
                            streamProfile.GetFormat());
                    return streamProfile;
                }
                else
                {
                    Debug.LogWarning("⚠️ Profile not found!");
                }
            }
            catch (NativeException e)
            {
                Debug.LogError($"❌ Exception while finding profile: {e.Message}");
            }

            return null;
        }

        public void SetFramesetCallback(FramesetCallback callback)
        {
            framesetCallback = callback;
        }

        public void StartPipeline()
        {
            pipeline.Start(config, framesetCallback);
        }

        public void StopPipeline()
        {
            pipeline.Stop();
        }
    }
}