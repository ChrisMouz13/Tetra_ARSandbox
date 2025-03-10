using System;
using UnityEngine;
using UnityEngine.UI;
using OrbbecUnity;

public class RawDepthVisualizer : MonoBehaviour
{
    public OrbbecFrameSource frameSource;
    public RawImage depthDisplay;
    public Text debugText;

    private Texture2D depthTexture;
    private float minDepth = float.MaxValue;
    private float maxDepth = float.MinValue;

    void Start()
    {
        if (frameSource == null)
        {
            Debug.LogError("FrameSource is not assigned!");
            return;
        }
    }

    void Update()
    {
        var depthFrame = frameSource.GetDepthFrame();
        if (depthFrame == null || depthFrame.data == null || depthFrame.data.Length == 0)
        {
            return;
        }

        int width = depthFrame.width;
        int height = depthFrame.height;

        if (depthTexture == null || depthTexture.width != width || depthTexture.height != height)
        {
            depthTexture = new Texture2D(width, height, TextureFormat.R16, false);
        }

        ushort[] depthData = new ushort[depthFrame.data.Length / 2];
        Buffer.BlockCopy(depthFrame.data, 0, depthData, 0, depthFrame.data.Length);

        Color[] colors = new Color[depthData.Length];
        minDepth = float.MaxValue;
        maxDepth = float.MinValue;

        for (int i = 0; i < depthData.Length; i++)
        {
            float depthValue = depthData[i] * 0.001f; // Μετατροπή σε μέτρα
            minDepth = Mathf.Min(minDepth, depthValue);
            maxDepth = Mathf.Max(maxDepth, depthValue);
            float normalizedDepth = Mathf.InverseLerp(0.2f, 10f, depthValue);
            colors[i] = new Color(normalizedDepth, normalizedDepth, normalizedDepth);
        }

        depthTexture.SetPixels(colors);
        depthTexture.Apply();
        depthDisplay.texture = depthTexture;

        if (debugText != null)
        {
            debugText.text = $"Min Depth: {minDepth:F2}m\nMax Depth: {maxDepth:F2}m";
        }
    }
}