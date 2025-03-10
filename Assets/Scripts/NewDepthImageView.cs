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

    // ✅ Μετράμε από 1.0m (επιφάνεια) έως 0.8m (βάθος άμμου)
    public float minDepth = 0.8f;  // Κατώτερο σημείο (βάθος άμμου)
    public float maxDepth = 1.0f;  // Επιφάνεια της άμμου

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

        Debug.Log("✅ OrbbecPipelineFrameSource initialized.");
    }

    void Update()
    {
        OrbbecFrame depthFrame = frameSource.GetDepthFrame();

        if (depthFrame != null && depthFrame.data != null && depthFrame.data.Length > 0)
        {
            ApplyColorMap(depthFrame);
        }
        else
        {
            Debug.LogWarning("⚠ No depth frame received or data is empty.");
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
                int index = y * width + x;               // Αρχικός δείκτης
                int flippedIndex = flippedY * width + x; // Δείκτης με ανεστραμμένο Y

                float depthInMeters = depthData[index] * 0.001f; // mm → meters

                // **Αντιμετωπίζουμε τις τιμές 0 ως μη έγκυρες**
                if (depthInMeters == 0)
                {
                    colors[flippedIndex] = Color.black; // Αντί για μαύρο, γκρι για no data
                    continue;
                }

                // **Κανονικοποίηση βάθους σε σχέση με την άμμο (1.0m → 0.8m)**
                float normalizedDepth = Mathf.InverseLerp(maxDepth, minDepth, depthInMeters);
                colors[flippedIndex] = GetColorFromDepth(normalizedDepth);
            }
        }

        depthTexture.SetPixels(colors);
        depthTexture.Apply();
    }

    // Μερικώς πετυχημένο πείραμα για οριοθετηση της επιφανειας προβολης
    /*private void ApplyColorMap(OrbbecFrame depthFrame)
    {
        int width = depthTexture.width;
        int height = depthTexture.height;
        Color[] colors = new Color[width * height];

        if (depthFrame == null || depthFrame.data == null || depthFrame.data.Length == 0)
        {
            Debug.LogError("❌ Depth frame is NULL or empty.");
            return;
        }

        ushort[] depthData = new ushort[depthFrame.data.Length / 2];
        Buffer.BlockCopy(depthFrame.data, 0, depthData, 0, depthFrame.data.Length);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float depthInMeters = depthData[index] * 0.001f;

                // 🛑 **Αν το βάθος είναι 0 (δηλ. μη έγκυρο), κάνε το μαύρο**
                if (depthInMeters == 0)
                {
                    colors[index] = Color.black;
                    continue;
                }

                // 🛑 **Αν το pixel είναι εκτός του ορατού πεδίου (δηλ. στον ρόμβο), κάνε το μαύρο**
                if (IsOutsideVisibleRegion(x, y, width, height))
                {
                    colors[index] = Color.black;
                    continue;
                }

                // ✅ **Κανονικός χρωματισμός για έγκυρα pixels**
                float normalizedDepth = Mathf.InverseLerp(minDepth, maxDepth, depthInMeters);
                colors[index] = GetColorFromDepth(normalizedDepth);
            }
        }

        depthTexture.SetPixels(colors);
        depthTexture.Apply();
    }*/

    private bool IsOutsideVisibleRegion(int x, int y, int width, int height)
    {
        // Ορίζουμε μια απλή ρομβοειδή περιοχή, με την προϋπόθεση ότι το κέντρο είναι το μέσο της εικόνας.
        int centerX = width / 2;
        int centerY = height / 2;

        // Υπολογισμός απόστασης από το κέντρο (δημιουργώντας έναν ρόμβο)
        int dx = Mathf.Abs(x - centerX);
        int dy = Mathf.Abs(y - centerY);

        // Αν είναι εκτός του νοητού ρόμβου, επιστρέφουμε TRUE για να γίνει μαύρο
        return (dx + dy > centerX);
    }

    private Color GetColorFromDepth(float normalizedDepth)
    {
        if (normalizedDepth < 0.2f) return Color.Lerp(Color.blue, Color.cyan, normalizedDepth * 5); // Μπλε → Γαλάζιο
        if (normalizedDepth < 0.4f) return Color.Lerp(Color.cyan, Color.yellow, (normalizedDepth - 0.2f) * 5); // Γαλάζιο → Κίτρινο
        if (normalizedDepth < 0.6f) return Color.Lerp(Color.yellow, Color.green, (normalizedDepth - 0.4f) * 5); // Κίτρινο → Πράσινο
        if (normalizedDepth < 0.8f) return Color.Lerp(Color.green, new Color(0.5f, 0.25f, 0), (normalizedDepth - 0.6f) * 5); // Πράσινο → Καφέ
        return Color.Lerp(new Color(0.5f, 0.25f, 0), Color.gray, (normalizedDepth - 0.8f) * 5); // Καφέ → Γκρι
    }
}