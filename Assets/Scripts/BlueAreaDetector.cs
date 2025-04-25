using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlueAreaDetector : MonoBehaviour
{
    public NewDepthImageView depthImageView;
    public RectTransform rawImageTransform;

    [Header("Fish Settings")]
    public GameObject fishPrefab;
    public Vector2 fishSize = new Vector2(100f, 100f);

    [Header("Detection Settings")]
    public int downsampleFactor = 2;
    public float blueThreshold = 0.1f;

    private List<Rect> detectedBlueRegions = new List<Rect>();
    private List<RegionFishGroup> activeRegions = new List<RegionFishGroup>();
    private List<Rect> currentDetectedRegions = new List<Rect>();

    private const float minYLimit = 224f;
    private const float maxYLimit = 800f;

    private class RegionFishGroup
    {
        public Vector2 center;
        public List<GameObject> fishList;
    }

    void Update()
    {
        if (depthImageView == null || depthImageView.depthImage.texture == null)
            return;

        Texture2D depthTexture = depthImageView.depthImage.texture as Texture2D;
        if (depthTexture == null)
            return;

        DetectBlueRegions(depthTexture);
        UpdateFishSpawns();
    }

    void DetectBlueRegions(Texture2D texture)
    {
        detectedBlueRegions.Clear();
        Color[] pixels = texture.GetPixels();
        int width = texture.width;
        int height = texture.height;
        bool[,] visited = new bool[width, height];

        for (int y = 0; y < height; y += downsampleFactor)
        {
            for (int x = 0; x < width; x += downsampleFactor)
            {
                if (visited[x, y]) continue;

                if (IsBlue(pixels[y * width + x]))
                {
                    Rect boundingBox = GetRegionBounds(x, y, pixels, visited, width, height);

                    if (boundingBox.width > 30 && boundingBox.height > 30)
                    {
                        detectedBlueRegions.Add(boundingBox);
                    }
                }
            }
        }

        currentDetectedRegions = new List<Rect>(detectedBlueRegions);
    }

    bool IsBlue(Color pixel)
    {
        float minDiff = blueThreshold;

        return (pixel.b > pixel.r + minDiff) &&
               (pixel.b > pixel.g + minDiff) &&
               (pixel.r < 0.3f) &&
               (pixel.g < 0.3f);
    }

    Rect GetRegionBounds(int startX, int startY, Color[] pixels, bool[,] visited, int width, int height)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));

        int minX = startX, maxX = startX;
        int minY = startY, maxY = startY;

        int maxPixels = 2000;
        int processed = 0;

        while (queue.Count > 0 && processed < maxPixels)
        {
            Vector2Int current = queue.Dequeue();
            int x = current.x;
            int y = current.y;

            if (x < 0 || x >= width || y < 0 || y >= height) continue;
            if (visited[x, y]) continue;

            Color pixel = pixels[y * width + x];
            if (!IsBlue(pixel)) continue;

            visited[x, y] = true;
            processed++;

            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;

            queue.Enqueue(new Vector2Int(x + 1, y));
            queue.Enqueue(new Vector2Int(x - 1, y));
            queue.Enqueue(new Vector2Int(x, y + 1));
            queue.Enqueue(new Vector2Int(x, y - 1));
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    void UpdateFishSpawns()
    {
        List<RegionFishGroup> newActiveRegions = new List<RegionFishGroup>();

        foreach (var region in detectedBlueRegions)
        {
            float centerY = region.y + region.height / 2f;
            if (centerY < minYLimit || centerY > maxYLimit)
                continue;

            Vector2 currentCenter = new Vector2(region.x + region.width / 2f, region.y + region.height / 2f);

            // Ψάξε αν υπάρχει ήδη περιοχή κοντά
            RegionFishGroup existing = null;
            foreach (var r in activeRegions)
            {
                if (Vector2.Distance(currentCenter, r.center) < 40f) // ← μπορείς να το ρυθμίσεις
                {
                    existing = r;
                    break;
                }
            }

            if (existing != null)
            {
                newActiveRegions.Add(existing); // διατήρησέ την
            }
            else
            {
                // νέα περιοχή = spawn ψαράκια
                var fishes = SpawnFishInRegion(region);
                newActiveRegions.Add(new RegionFishGroup { center = currentCenter, fishList = fishes });
            }
        }

        // Καθάρισε ψάρια από περιοχές που χάθηκαν
        foreach (var r in activeRegions)
        {
            if (!newActiveRegions.Contains(r))
            {
                foreach (var f in r.fishList)
                    Destroy(f);
            }
        }

        activeRegions = newActiveRegions;
    }

    List<GameObject> SpawnFishInRegion(Rect region)
    {
        List<GameObject> fishList = new List<GameObject>();

        float rawWidth = rawImageTransform.rect.width;
        float rawHeight = rawImageTransform.rect.height;

        float uiFishWidth = fishSize.x * 1024f / rawWidth;
        float uiFishHeight = fishSize.y * 1024f / rawHeight;

        int cols = Mathf.FloorToInt(region.width / uiFishWidth);
        int rows = Mathf.FloorToInt(region.height / uiFishHeight);

        // ✅ τουλάχιστον 1 prefab
        cols = Mathf.Max(1, cols);
        rows = Mathf.Max(1, rows);

        for (int i = 0; i < cols; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                float localX = region.x + (i + 0.5f) * region.width / cols;
                float localY = region.y + (j + 0.5f) * region.height / rows;

                float uiX = (localX / 1024f) * rawWidth - rawWidth / 2f;
                float uiY = (localY / 1024f) * rawHeight - rawHeight / 2f;

                GameObject fish = Instantiate(fishPrefab, rawImageTransform);
                RectTransform rect = fish.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(uiX, uiY);
                rect.sizeDelta = fishSize;

                fishList.Add(fish);
            }
        }

        Debug.Log($"[FISH] Spawned {fishList.Count} fish in region: {region}");

        return fishList;
    }

    bool RectsApproximatelyEqual(Rect a, Rect b, float tolerance = 20f)
    {
        return Mathf.Abs(a.x - b.x) < tolerance &&
               Mathf.Abs(a.y - b.y) < tolerance &&
               Mathf.Abs(a.width - b.width) < tolerance &&
               Mathf.Abs(a.height - b.height) < tolerance;
    }

    public List<Rect> GetDetectedBlueRegions()
    {
        return detectedBlueRegions;
    }
}