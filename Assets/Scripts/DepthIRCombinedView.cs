using System;
using UnityEngine;
using UnityEngine.UI;
using OrbbecUnity;

public class DepthIRCombinedView : MonoBehaviour
{
    public OrbbecPipelineFrameSource frameSource;
    public RawImage outputImage;
    public Material displayMaterial; // ✅ FishEyeUnwarp Shader

    private Texture2D combinedTexture;

    public float FarestDepth = 0.6f;  // Κατώτερο σημείο (βάθος άμμου)
    public float NearestDepth = 0.8f;  // Επιφάνεια της άμμου

    private float gamma = 0.8f; // ✅ Αντίθεση
    private const int KernelSize = 3; // ✅ Μέγεθος πυρήνα για τον φιλτράρισμα θορύβου
    private const float IRInfluence = 0.4f; // ✅ Πόση επίδραση θα έχει το IR στο βάθος

    void Start()
    {
        if (frameSource == null || outputImage == null)
        {
            Debug.LogError("❌ Frame Source ή Output Image δεν έχει οριστεί!");
            return;
        }

        int width = 1024;
        int height = 1024;
        combinedTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        outputImage.texture = combinedTexture;
    }

    void Update()
    {
        OrbbecFrame depthFrame = frameSource.GetDepthFrame();
        OrbbecFrame irFrame = frameSource.GetIrFrame();

        if (depthFrame == null || depthFrame.data == null || irFrame == null || irFrame.data == null)
        {
            Debug.LogWarning("⚠️ Depth ή IR Frame είναι NULL ή άδειο!");
            return;
        }

        ApplyCombinedProcessing(depthFrame, irFrame);
    }

    private void ApplyCombinedProcessing(OrbbecFrame depthFrame, OrbbecFrame irFrame)
    {
        int width = combinedTexture.width;
        int height = combinedTexture.height;
        Color[] colors = new Color[width * height];

        ushort[] depthData = new ushort[depthFrame.data.Length / 2];
        Buffer.BlockCopy(depthFrame.data, 0, depthData, 0, depthFrame.data.Length);

        byte[] irData = irFrame.data;

        ushort[] filteredDepth = ApplyNoiseReduction(depthData, width, height);

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int index = y * width + x;

                float depthInMeters = filteredDepth[index] * 0.001f;
                float irValue = irData[index] / 255.0f;

                if (depthInMeters == 0)
                {
                    colors[index] = Color.gray;
                    continue;
                }

                float confidenceFactor = Mathf.Lerp(1.0f - IRInfluence, 1.0f, irValue);
                float adjustedDepth = depthInMeters * confidenceFactor;

                float normalizedDepth = Mathf.InverseLerp(NearestDepth, FarestDepth, adjustedDepth);
                normalizedDepth = Mathf.Pow(normalizedDepth, gamma);
                colors[index] = GetColorFromDepth(normalizedDepth);
            }
        }

        combinedTexture.SetPixels(colors);
        combinedTexture.Apply();
        outputImage.material = displayMaterial;
    }

    private ushort[] ApplyNoiseReduction(ushort[] depthData, int width, int height)
    {
        ushort[] filtered = new ushort[depthData.Length];

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int index = y * width + x;
                ushort depth = depthData[index];

                if (depth == 0 || Math.Abs(depth - depthData[index - 1]) > 200)
                {
                    filtered[index] = GetNeighborhoodAverage(depthData, x, y, width);
                }
                else
                {
                    filtered[index] = depth;
                }
            }
        }

        return filtered;
    }

    private ushort GetNeighborhoodAverage(ushort[] depthData, int x, int y, int width)
    {
        int sum = 0;
        int count = 0;

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int neighborIndex = (y + dy) * width + (x + dx);
                ushort neighborDepth = depthData[neighborIndex];

                if (neighborDepth > 0)
                {
                    sum += neighborDepth;
                    count++;
                }
            }
        }

        return (ushort)(count > 0 ? sum / count : 0);
    }

    private Color GetColorFromDepth(float normalizedDepth)
    {
        float contrastMin = 0.1f;
        float contrastMax = 0.9f;
        normalizedDepth = Mathf.Clamp((normalizedDepth - contrastMin) / (contrastMax - contrastMin), 0f, 1f);

        if (normalizedDepth < 0.2f) return Color.Lerp(Color.blue, Color.cyan, normalizedDepth * 5);
        if (normalizedDepth < 0.4f) return Color.Lerp(Color.cyan, Color.yellow, (normalizedDepth - 0.2f) * 5);
        if (normalizedDepth < 0.6f) return Color.Lerp(Color.yellow, Color.green, (normalizedDepth - 0.4f) * 5);
        if (normalizedDepth < 0.8f) return Color.Lerp(Color.green, new Color(0.5f, 0.25f, 0), (normalizedDepth - 0.6f) * 5);
        return Color.Lerp(new Color(0.5f, 0.25f, 0), Color.gray, (normalizedDepth - 0.8f) * 5);
    }
}