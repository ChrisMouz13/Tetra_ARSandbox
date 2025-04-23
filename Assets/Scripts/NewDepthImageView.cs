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

    // ✅ Μετράμε από 1.1m (πάτωμα - πιο βαθύ σημείο) έως 0.44m (πιο ρηχό σημείο)
    public float FarestDepth = 1.1f;  // ✅ Κατώτερο σημείο (βάθος - πάτωμα)
    public float NearestDepth = 0.9f;  // ✅ Επιφάνεια (πιο κοντά στην κάμερα)

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

        // ✅ **Διόρθωση Προοπτικής με βάση το FOV της κάμερας**
        float fovY = 69f; // Τυπικό FOV για Orbbec Femto Bolt
        float aspectRatio = (float)width / height;
        float fovX = 2f * Mathf.Atan(Mathf.Tan(fovY * Mathf.Deg2Rad / 2f) * aspectRatio) * Mathf.Rad2Deg;

        for (int y = 0; y < height; y++)
        {
            int flippedY = height - 1 - y; // ✅ Αντιστροφή του Y άξονα

            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                int flippedIndex = flippedY * width + x;

                float depthInMeters = depthData[index] * 0.001f; // ✅ Μετατροπή από mm σε μέτρα
                if (depthInMeters == 0)
                {
                    colors[flippedIndex] = Color.gray; // ✅ Αντιμετώπιση 0-depth values
                    continue;
                }

                // ✅ **Υπολογισμός γωνίας pixel (προοπτική διόρθωση)**
                float normalizedX = (x - width / 2f) / width;   // Απόσταση από το κέντρο [-0.5, 0.5]
                float normalizedY = (y - height / 2f) / height; // Απόσταση από το κέντρο [-0.5, 0.5]

                float angleX = normalizedX * fovX * Mathf.Deg2Rad;
                float angleY = normalizedY * fovY * Mathf.Deg2Rad;

                // ✅ **Εφαρμογή διόρθωσης προοπτικής**
                float correctedDepth = depthInMeters / (Mathf.Cos(angleX) * Mathf.Cos(angleY));

                // ✅ **Κανονικοποίηση βάθους βάσει του διορθωμένου βάθους**
                float normalizedDepth = Mathf.InverseLerp(FarestDepth, NearestDepth, correctedDepth); // ισως να πρεπει να παει (NearestDepth, FarestDepth, correctedDepth) 
                normalizedDepth = Mathf.Pow(normalizedDepth, gamma);
                colors[flippedIndex] = GetColorFromDepth(normalizedDepth);
            }
        }

        depthTexture.SetPixels(colors);
        depthTexture.Apply();
        ApplyGPUBlur();
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