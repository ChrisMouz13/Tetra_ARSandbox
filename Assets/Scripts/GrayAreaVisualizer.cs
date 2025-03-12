using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrayAreaVisualizer : MonoBehaviour
{
    public ComputeShader detectGrayAreasShader;
    public RawImage depthImage; // Το RawImage όπου προβάλλεται ο depth χάρτης
    public RectTransform canvasTransform; // Το Canvas που περιέχει το RawImage
    public GameObject boundingBoxPrefab; // Ένα απλό UI Image prefab για bounding boxes

    private int maxBoundingBoxes = 100;
    private ComputeBuffer boundingBoxBuffer;
    private ComputeBuffer grayRegionCountBuffer;
    private List<GameObject> activeBoundingBoxes = new List<GameObject>();

    void Start()
    {
        if (depthImage.texture == null)
        {
            Debug.LogError("❌ depthImage.texture is NULL at Start!");
            return;
        }

        Debug.Log($"✅ depthImage.texture is valid: {depthImage.texture.width}x{depthImage.texture.height}");

        // Αρχικοποίηση του Compute Buffer για τα bounding boxes
        boundingBoxBuffer = new ComputeBuffer(maxBoundingBoxes, sizeof(int) * 4);
        detectGrayAreasShader.SetBuffer(0, "_GrayBoundingBoxes", boundingBoxBuffer);

        // Αρχικοποίηση του Compute Buffer για τον αριθμό των γκρι περιοχών
        grayRegionCountBuffer = new ComputeBuffer(1, sizeof(int));
        detectGrayAreasShader.SetBuffer(0, "_GrayRegionCount", grayRegionCountBuffer);

        // Ορισμός του Depth Texture στον Compute Shader
        detectGrayAreasShader.SetTexture(0, "_DepthTex", depthImage.texture);
    }

    void Update()
    {
        if (depthImage.texture == null)
        {
            Debug.LogError("❌ depthImage.texture is NULL in Update!");
            return;
        }

        Debug.Log($"⚡ Running Compute Shader Dispatch on texture: {depthImage.texture.width}x{depthImage.texture.height}");

        // Μηδενίζουμε το GrayRegionCount για να μην κρατάει παλιά δεδομένα
        int[] zeroArray = new int[1] { 0 };
        grayRegionCountBuffer.SetData(zeroArray);

        // Τρέχουμε τον Compute Shader
        detectGrayAreasShader.Dispatch(0, 16, 16, 1);

        // Παίρνουμε τα δεδομένα από τη GPU
        int[] boundingBoxData = new int[maxBoundingBoxes * 4];
        boundingBoxBuffer.GetData(boundingBoxData);

        int[] grayRegionCount = new int[1];
        grayRegionCountBuffer.GetData(grayRegionCount);

        Debug.Log($"✅ Compute Shader Dispatch finished. Detected {grayRegionCount[0]} gray regions.");

        // Ενημερώνουμε τα bounding boxes στο UI
        UpdateBoundingBoxes(boundingBoxData, grayRegionCount[0]);
    }

    void UpdateBoundingBoxes(int[] boundingBoxData, int detectedRegions)
    {
        // Καθαρίζουμε τα παλιά bounding boxes
        foreach (GameObject box in activeBoundingBoxes)
        {
            Destroy(box);
        }
        activeBoundingBoxes.Clear();

        // Παίρνουμε τις διαστάσεις του RawImage
        RectTransform imageRect = depthImage.rectTransform;
        float imageWidth = imageRect.rect.width;
        float imageHeight = imageRect.rect.height;

        for (int i = 0; i < detectedRegions; i++)
        {
            int minX = boundingBoxData[i * 4 + 0];
            int minY = boundingBoxData[i * 4 + 1];
            int maxX = boundingBoxData[i * 4 + 2];
            int maxY = boundingBoxData[i * 4 + 3];

            if (minX == maxX || minY == maxY) continue; // Αγνοούμε άκυρα bounding boxes

            Debug.Log($"🟦 Bounding Box [{i}] -> minX: {minX}, minY: {minY}, maxX: {maxX}, maxY: {maxY}");

            // Δημιουργούμε νέο bounding box και το προσθέτουμε στο RawImage
            GameObject box = Instantiate(boundingBoxPrefab, depthImage.transform);
            RectTransform rect = box.GetComponent<RectTransform>();

            // **Μετατροπή από 1024x1024 pixels σε UI space**
            float uiMinX = Mathf.Lerp(0, imageWidth, minX / 1024f);
            float uiMaxX = Mathf.Lerp(0, imageWidth, maxX / 1024f);
            float uiMinY = Mathf.Lerp(imageHeight, 0, minY / 1024f); // Αντιστροφή Y
            float uiMaxY = Mathf.Lerp(imageHeight, 0, maxY / 1024f); // Αντιστροφή Y

            // Υπολογισμός διαστάσεων
            float uiWidth = uiMaxX - uiMinX;
            float uiHeight = uiMaxY - uiMinY;
            float centerX = (uiMinX + uiMaxX) / 2;
            float centerY = (uiMinY + uiMaxY) / 2;

            // Εφαρμογή θέσης και μεγέθους
            rect.anchoredPosition = new Vector2(centerX - imageWidth / 2, centerY - imageHeight / 2);
            rect.sizeDelta = new Vector2(uiWidth, uiHeight);

            activeBoundingBoxes.Add(box);
        }
    }

    void OnDestroy()
    {
        if (boundingBoxBuffer != null)
        {
            boundingBoxBuffer.Release();
            boundingBoxBuffer = null;
        }

        if (grayRegionCountBuffer != null)
        {
            grayRegionCountBuffer.Release();
            grayRegionCountBuffer = null;
        }
    }
}