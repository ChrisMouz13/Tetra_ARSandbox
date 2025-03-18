using System;
using UnityEngine;
using UnityEngine.UI;
using OrbbecUnity;

public class IRDepthDebugger : MonoBehaviour
{
    public OrbbecPipelineFrameSource frameSource;
    public RawImage debugImage; // Για να δούμε τις αποκλίσεις
    private Texture2D debugTexture;

    void Start()
    {
        if (frameSource == null || debugImage == null)
        {
            Debug.LogError("⚠️ Frame Source ή Debug Image δεν έχει αντιστοιχιστεί!");
            return;
        }

        int width = 1024;
        int height = 1024;
        debugTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        debugImage.texture = debugTexture;
    }

    void Update()
    {
        OrbbecFrame depthFrame = frameSource.GetDepthFrame();
        OrbbecFrame irFrame = frameSource.GetIrFrame();

        if (depthFrame == null || irFrame == null || depthFrame.data == null || irFrame.data == null)
        {
            Debug.LogWarning("⚠️ Depth ή IR Frame είναι NULL ή άδειο!");
            return;
        }

        ApplyDebugMap(depthFrame, irFrame);
    }

    private void ApplyDebugMap(OrbbecFrame depthFrame, OrbbecFrame irFrame)
    {
        int width = debugTexture.width;
        int height = debugTexture.height;
        Color[] colors = new Color[width * height];

        ushort[] depthData = new ushort[depthFrame.data.Length / 2];
        Buffer.BlockCopy(depthFrame.data, 0, depthData, 0, depthFrame.data.Length);

        ushort[] irData = new ushort[irFrame.data.Length / 2];
        Buffer.BlockCopy(irFrame.data, 0, irData, 0, irFrame.data.Length);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float depthValue = depthData[index] * 0.001f; // Μετατροπή σε μέτρα
                float irValue = irData[index] / 65535f; // Κανονικοποίηση IR

                float diff = Mathf.Abs(depthValue - irValue); // Απόκλιση βάθους-IR

                // **Αν είναι πολύ μεγάλη η απόκλιση, το πιθανότερο είναι να έχουμε reflection**
                colors[index] = GetColorFromDifference(diff);
            }
        }

        debugTexture.SetPixels(colors);
        debugTexture.Apply();
    }

    private Color GetColorFromDifference(float diff)
    {
        if (diff < 0.01f) return Color.green; // Μικρή απόκλιση = Αξιόπιστο βάθος
        if (diff < 0.05f) return Color.yellow; // Ίσως μικρό λάθος
        if (diff < 0.1f) return Color.red; // Μεγάλη απόκλιση = πιθανό πρόβλημα
        return Color.magenta; // Τεράστια απόκλιση = Αντανάκλαση ή σφάλμα
    }
}