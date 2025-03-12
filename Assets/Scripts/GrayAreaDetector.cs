using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GrayAreaDetector : MonoBehaviour
{
    public NewDepthImageView depthImageView; // Αναφορά στο NewDepthImageView
    public GameObject boundingBoxPrefab; // Το prefab που θα εμφανίζεται
    public RectTransform rawImageTransform; // Το RawImage στο οποίο εμφανίζεται ο depth χάρτης

    public int downsampleFactor = 2; // Εξετάζουμε κάθε 2ο pixel για απόδοση
    public float grayThreshold = 0.1f;
    public List<Rect> detectedGrayRegions = new List<Rect>();

    private Dictionary<int, GameObject> activeBoundingBoxes = new Dictionary<int, GameObject>();

    private const int MinRegionSize = 100; // Ελάχιστο μέγεθος περιοχής 100x100 pixels

    void Update()
    {
        if (depthImageView == null || depthImageView.depthImage.texture == null)
        {
            Debug.LogWarning("GrayAreaDetector: Depth texture not ready.");
            return;
        }

        Texture2D depthTexture = depthImageView.depthImage.texture as Texture2D;
        if (depthTexture == null)
        {
            Debug.LogWarning("GrayAreaDetector: Depth texture is not a Texture2D.");
            return;
        }

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

                Color pixel = pixels[y * width + x];
                if (IsGray(pixel))
                {
                    List<Vector2Int> cluster = new List<Vector2Int>();
                    FloodFill(x, y, pixels, visited, width, height, cluster);

                    Rect boundingBox = CalculateBoundingBox(cluster);

                    if (boundingBox.width >= MinRegionSize && boundingBox.height >= MinRegionSize)
                    {
                        detectedGrayRegions.Add(boundingBox);
                    }
                }
            }
        }
    }

    void UpdateBoundingBoxes()
    {
        // Δημιουργούμε ή ενημερώνουμε τα bounding boxes, χωρίς να αλλάζουμε το μέγεθός τους
        for (int i = 0; i < detectedGrayRegions.Count; i++)
        {
            Rect region = detectedGrayRegions[i];

            // Υπολογίζουμε το κέντρο της γκρι περιοχής
            float centerX = region.x + region.width / 2;
            float centerY = region.y + region.height / 2;

            // Αν υπάρχει ήδη το prefab, απλά ενημερώνουμε τη θέση του
            if (activeBoundingBoxes.ContainsKey(i))
            {
                RectTransform existingRect = activeBoundingBoxes[i].GetComponent<RectTransform>();

                // Υπολογισμός θέσης στο RawImage
                float rawWidth = rawImageTransform.rect.width;
                float rawHeight = rawImageTransform.rect.height;

                float uiX = Mathf.Lerp(0, rawWidth, centerX / 1024f);
                float uiY = Mathf.Lerp(rawHeight, 0, centerY / 1024f);

                // Ενημερώνουμε μόνο τη θέση
                existingRect.anchoredPosition = new Vector2(uiX - rawWidth / 2, uiY - rawHeight / 2);
            }
            else
            {
                // Δημιουργούμε νέο prefab αν δεν υπάρχει ήδη
                GameObject box = Instantiate(boundingBoxPrefab, rawImageTransform);
                RectTransform rect = box.GetComponent<RectTransform>();

                float rawWidth = rawImageTransform.rect.width;
                float rawHeight = rawImageTransform.rect.height;

                float uiX = Mathf.Lerp(0, rawWidth, centerX / 1024f);
                float uiY = Mathf.Lerp(rawHeight, 0, centerY / 1024f);

                rect.anchoredPosition = new Vector2(uiX - rawWidth / 2, uiY - rawHeight / 2);

                // **Κρατάμε το αρχικό μέγεθος του prefab!**
                rect.sizeDelta = boundingBoxPrefab.GetComponent<RectTransform>().sizeDelta;

                activeBoundingBoxes[i] = box;
            }
        }

        // **Αφαιρούμε τα παραπανίσια prefabs αν μειωθεί ο αριθμός των γκρι περιοχών**
        List<int> keysToRemove = new List<int>();
        foreach (var key in activeBoundingBoxes.Keys)
        {
            if (key >= detectedGrayRegions.Count)
            {
                Destroy(activeBoundingBoxes[key]);
                keysToRemove.Add(key);
            }
        }
        foreach (var key in keysToRemove)
        {
            activeBoundingBoxes.Remove(key);
        }
    }

    bool IsGray(Color pixel)
    {
        float diffRG = Mathf.Abs(pixel.r - pixel.g);
        float diffGB = Mathf.Abs(pixel.g - pixel.b);
        float diffBR = Mathf.Abs(pixel.b - pixel.r);
        return diffRG < grayThreshold && diffGB < grayThreshold && diffBR < grayThreshold;
    }

    void FloodFill(int x, int y, Color[] pixels, bool[,] visited, int width, int height, List<Vector2Int> cluster)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(x, y));

        while (queue.Count > 0)
        {
            Vector2Int pixel = queue.Dequeue();
            int px = pixel.x;
            int py = pixel.y;

            if (px < 0 || py < 0 || px >= width || py >= height || visited[px, py]) continue;
            if (!IsGray(pixels[py * width + px])) continue;

            visited[px, py] = true;
            cluster.Add(new Vector2Int(px, py));

            queue.Enqueue(new Vector2Int(px + 1, py));
            queue.Enqueue(new Vector2Int(px - 1, py));
            queue.Enqueue(new Vector2Int(px, py + 1));
            queue.Enqueue(new Vector2Int(px, py - 1));
        }
    }

    Rect CalculateBoundingBox(List<Vector2Int> cluster)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var point in cluster)
        {
            if (point.x < minX) minX = point.x;
            if (point.y < minY) minY = point.y;
            if (point.x > maxX) maxX = point.x;
            if (point.y > maxY) maxY = point.y;
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }
}