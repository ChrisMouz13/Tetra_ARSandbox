using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using OrbbecUnity;

public class NewIRImageView : MonoBehaviour
{
    public OrbbecPipelineFrameSource frameSource;
    public RawImage irImage;
    private Texture2D irTexture;

    void Start()
    {
        if (frameSource == null || irImage == null)
        {
            Debug.LogError("⚠️ Frame Source or IR Image is not assigned!");
            return;
        }

        int width = 1024;
        int height = 1024;
        irTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        irImage.texture = irTexture;

        Debug.LogWarning("🛠 Available methods in frameSource: " + frameSource.GetType().FullName);
        foreach (var method in frameSource.GetType().GetMethods())
        {
            Debug.LogWarning($"🔹 Method: {method.Name}");
        }
    }

    void Update()
    {
        OrbbecFrame irFrame = frameSource.GetIrFrame();

        if (irFrame == null || irFrame.data == null || irFrame.data.Length == 0)
        {
            Debug.LogWarning("⚠️ IR Frame is NULL or empty!");
            return;
        }

        Debug.Log($"✅ IR Frame received with {irFrame.data.Length} bytes.");
        ApplyColorMap(irFrame);
    }

    private void ApplyColorMap(OrbbecFrame irFrame)
    {
        int width = irTexture.width;
        int height = irTexture.height;
        Color[] colors = new Color[width * height];

        ushort[] irData = new ushort[irFrame.data.Length / 2];
        Buffer.BlockCopy(irFrame.data, 0, irData, 0, irFrame.data.Length);

        for (int y = 0; y < height; y++)
        {
            int flippedY = height - 1 - y;

            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                int flippedIndex = flippedY * width + x;

                float intensity = irData[index] / 65535f; // Normalize to [0,1]
                colors[flippedIndex] = new Color(intensity, intensity, intensity);
            }
        }

        irTexture.SetPixels(colors);
        irTexture.Apply();
    }
}