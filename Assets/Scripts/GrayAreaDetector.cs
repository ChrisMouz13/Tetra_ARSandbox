using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrayAreaDetector : MonoBehaviour
{
    public NewDepthImageView depthImageView;
    public GameObject boundingBoxPrefab;
    public RectTransform rawImageTransform;

    public int downsampleFactor = 2;
    public float grayThreshold = 0.1f;
    public int maxDetectedRegions = 5; // ✅ Ανώτατο όριο ηφαιστείων
    public float positionThreshold = 50f;
    public float despawnTime = 3f;

    private List<Rect> detectedGrayRegions = new List<Rect>();
    List<GameObject> activePrefabs = new List<GameObject>(); // ✅ Διατηρούμε χειροκίνητα λίστα με τα ενεργά Prefabs

    private const int MinRegionSize = 200;
    private const float minYLimit = 224f;  // ✅ Κάτω όριο προβολής
    private const float maxYLimit = 800f;  // ✅ Πάνω όριο προβολής

    void Update()
    {
        if (depthImageView == null || depthImageView.depthImage.texture == null)
            return;

        Texture2D depthTexture = depthImageView.depthImage.texture as Texture2D;
        if (depthTexture == null)
            return;

        DetectGrayRegions(depthTexture);
        UpdateBoundingBoxes();
    }

    void DetectGrayRegions(Texture2D texture)
    {
        detectedGrayRegions.Clear();
        Color[] pixels = texture.GetPixels();
        int width = texture.width;
        int height = texture.height;
        bool[,] visited = new bool[width, height];

        for (int y = 0; y < height; y += downsampleFactor)
        {
            for (int x = 0; x < width; x += downsampleFactor)
            {
                if (visited[x, y]) continue;

                if (IsGray(pixels[y * width + x]))
                {
                    Rect boundingBox = GetRegionBounds(x, y, pixels, visited, width, height);

                    if (boundingBox.width > 50 && boundingBox.height > 50)
                    {
                        detectedGrayRegions.Add(boundingBox);

                        // ✅ Αν φτάσαμε το όριο των 5, σταματάμε
                        if (detectedGrayRegions.Count >= maxDetectedRegions)
                            return;
                    }
                }
            }
        }
    }

    bool IsGray(Color pixel)
    {
        float diffRG = Mathf.Abs(pixel.r - pixel.g);
        float diffGB = Mathf.Abs(pixel.g - pixel.b);
        float diffBR = Mathf.Abs(pixel.b - pixel.r);

        return diffRG < grayThreshold && diffGB < grayThreshold && diffBR < grayThreshold;
    }

    Rect GetRegionBounds(int startX, int startY, Color[] pixels, bool[,] visited, int width, int height)
    {
        int maxX = startX, maxY = startY;
        int minX = startX, minY = startY;

        for (int y = startY; y < height; y++)
        {
            if (!IsGray(pixels[y * width + startX])) break;

            for (int x = startX; x < width; x++)
            {
                if (!IsGray(pixels[y * width + x]) || visited[x, y]) break;
                visited[x, y] = true;

                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    void UpdateBoundingBoxes()
    {
        List<GameObject> newActivePrefabs = new List<GameObject>();

        foreach (var region in detectedGrayRegions)
        {
            float centerX = region.x + region.width / 2;
            float centerY = region.y + region.height / 2;

            float rawWidth = rawImageTransform.rect.width;
            float rawHeight = rawImageTransform.rect.height;

            float uiX = (centerX / 1024f) * rawWidth - rawWidth / 2;
            float uiY = (centerY / 1024f) * rawHeight - rawHeight / 2;

            // ✅ Φιλτράρουμε τις περιοχές που είναι εκτός της ωφέλιμης προβολής
            if (centerY < minYLimit || centerY > maxYLimit)
                continue;

            bool existingFound = false;

            foreach (var prefab in activePrefabs)
            {
                Vector2 existingPos = prefab.GetComponent<RectTransform>().anchoredPosition;
                Vector2 newPos = new Vector2(uiX, uiY);

                if (Vector2.Distance(existingPos, newPos) < positionThreshold)
                {
                    newActivePrefabs.Add(prefab);
                    existingFound = true;
                    break;
                }
            }

            if (!existingFound && newActivePrefabs.Count < maxDetectedRegions)
            {
                SpawnVolcano(new Vector2(uiX, uiY)); // ✅ Διορθωμένη κλήση!
            }
        }

        // ✅ Καθαρίζουμε τα Prefabs που είναι ανενεργά
        foreach (var prefab in activePrefabs)
        {
            if (!newActivePrefabs.Contains(prefab))
            {
                Destroy(prefab);
            }
        }

        activePrefabs = newActivePrefabs;
    }

    void SpawnVolcano(Vector2 position)
    {
        if (activePrefabs.Count >= maxDetectedRegions)
        {
            Debug.Log("[LIMIT] Max number of volcanoes reached.");
            return;
        }

        GameObject box = Instantiate(boundingBoxPrefab, rawImageTransform);
        RectTransform rect = box.GetComponent<RectTransform>();

        rect.anchoredPosition = position;
        rect.sizeDelta = boundingBoxPrefab.GetComponent<RectTransform>().sizeDelta;

        activePrefabs.Add(box);

        Debug.Log($"[SPAWN] New volcano spawned at {position}. Total now: {activePrefabs.Count}");
    }

}