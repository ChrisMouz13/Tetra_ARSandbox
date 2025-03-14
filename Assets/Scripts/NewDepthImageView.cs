using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using OrbbecUnity;

public class NewDepthImageView : MonoBehaviour
{
    public OrbbecPipelineFrameSource frameSource;
    public RawImage depthImage;
    private Texture2D depthTexture;
    public Material blurMaterial; // ✅ Υλικό για τον GPU-Based Shader
    private RenderTexture tempRT1; // Πρώτο RenderTexture
    private RenderTexture tempRT2; // Δεύτερο RenderTexture

    // ✅ Μετράμε από 1.0m (επιφάνεια) έως 0.8m (βάθος άμμου). 
    public float FarestDepth = 0.6f;  // Κατώτερο σημείο (βάθος άμμου)
    public float NearestDepth = 0.8f;  // Επιφάνεια της άμμου
    //Αν η καμερα μπει 1.5μ πανω απο την επιφανεια της αμμου τοτε λογικά το FarestDepth = 1.7f και το NearestDepth = 1.5f.

    private float gamma = 0.8f; // ✅ Τιμή gamma για Contrast Enhancement

    void Start()
    { 
        if (frameSource == null || depthImage == null)
        {
            Debug.LogError("Frame Source or Depth Image not assigned!");
            return;
        }

        int width = 1024;
        int height = 1024;
        depthTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        depthImage.texture = depthTexture;
    }

    void Update()
    {
        OrbbecFrame depthFrame = frameSource.GetDepthFrame();

        if (depthFrame != null && depthFrame.data != null && depthFrame.data.Length > 0)
        {
            ApplyColorMap(depthFrame);
        }
    }

    private void ApplyColorMap(OrbbecFrame depthFrame)
    {
        int width = depthTexture.width;
        int height = depthTexture.height;
        Color[] colors = new Color[width * height];

        if (depthFrame == null || depthFrame.data == null || depthFrame.data.Length == 0)
        {
            Debug.LogError("❌ Depth frame is NULL or empty.");
            return;
        }

        // **Μετατροπή των raw byte δεδομένων σε ushort (16-bit depth)**
        ushort[] depthData = new ushort[depthFrame.data.Length / 2];
        Buffer.BlockCopy(depthFrame.data, 0, depthData, 0, depthFrame.data.Length);

        // **Χρήση των minDepth και maxDepth**
        for (int y = 0; y < height; y++)
        {
            int flippedY = height - 1 - y; // Αντιστροφή του Y άξονα

            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                int flippedIndex = flippedY * width + x;

                float depthInMeters = depthData[index] * 0.001f;

                if (depthInMeters == 0)
                {
                    colors[flippedIndex] = Color.gray;
                    continue;
                }

                float normalizedDepth = Mathf.InverseLerp(NearestDepth, FarestDepth, depthInMeters);
                normalizedDepth = Mathf.Pow(normalizedDepth, gamma);
                colors[flippedIndex] = GetColorFromDepth(normalizedDepth);
            }
        }

        depthTexture.SetPixels(colors);
        depthTexture.Apply();
        ApplyGPUBlur(); // ✅ Αντί για ApplyBoxBlur, καλούμε τον Shader
    }

    private void ApplyGPUBlur()
    {
        if (blurMaterial != null)
        {
            // ✅ Αν τα RenderTextures δεν έχουν δημιουργηθεί, τα αρχικοποιούμε
            if (tempRT1 == null || tempRT2 == null)
            {
                tempRT1 = new RenderTexture(depthTexture.width, depthTexture.height, 0);
                tempRT2 = new RenderTexture(depthTexture.width, depthTexture.height, 0);
            }

            // ✅ Μεταφέρουμε το Texture2D στο tempRT1
            Graphics.Blit(depthTexture, tempRT1);

            // ✅ Εφαρμόζουμε τον Shader στο tempRT2
            Graphics.Blit(tempRT1, tempRT2, blurMaterial);

            // ✅ Αντιγράφουμε το αποτέλεσμα πίσω στο Texture2D
            RenderTexture.active = tempRT2;
            depthTexture.ReadPixels(new Rect(0, 0, depthTexture.width, depthTexture.height), 0, 0);
            depthTexture.Apply();
            RenderTexture.active = null;
        }
    }

    private bool IsOutsideVisibleRegion(int x, int y, int width, int height)
    {
        int centerX = width / 2;
        int centerY = height / 2;
        int dx = Mathf.Abs(x - centerX);
        int dy = Mathf.Abs(y - centerY);
        return (dx + dy > centerX);
    }

    private Color GetColorFromDepth(float normalizedDepth)
    {
        // ✅ Contrast Stretching: "Τεντώνουμε" τις τιμές για πιο έντονα χρώματα
        float contrastMin = 0.1f; // Το χαμηλότερο επίπεδο βάθους
        float contrastMax = 0.9f; // Το υψηλότερο επίπεδο βάθους
        normalizedDepth = Mathf.Clamp((normalizedDepth - contrastMin) / (contrastMax - contrastMin), 0f, 1f);

        if (normalizedDepth < 0.2f) return Color.Lerp(Color.blue, Color.cyan, normalizedDepth * 5);
        if (normalizedDepth < 0.4f) return Color.Lerp(Color.cyan, Color.yellow, (normalizedDepth - 0.2f) * 5);
        if (normalizedDepth < 0.6f) return Color.Lerp(Color.yellow, Color.green, (normalizedDepth - 0.4f) * 5);
        if (normalizedDepth < 0.8f) return Color.Lerp(Color.green, new Color(0.5f, 0.25f, 0), (normalizedDepth - 0.6f) * 5);
        return Color.Lerp(new Color(0.5f, 0.25f, 0), Color.gray, (normalizedDepth - 0.8f) * 5);
    }
}