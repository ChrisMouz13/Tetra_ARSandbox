using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlueRegionVisualizer : MonoBehaviour
{
    public BlueAreaDetector detector; // σύνδεση με το άλλο script
    public RectTransform rawImageTransform;
    public GameObject outlinePrefab;

    private List<GameObject> activeOutlines = new List<GameObject>();

    void Update()
    {
        Debug.Log("[Visualizer] Running Update()");

        ClearOldOutlines();

        foreach (Rect region in detector.GetDetectedBlueRegions())
        {
            GameObject outline = Instantiate(outlinePrefab, rawImageTransform);
            RectTransform rect = outline.GetComponent<RectTransform>();

            float rawWidth = rawImageTransform.rect.width;
            float rawHeight = rawImageTransform.rect.height;

            float regionCenterX = (region.x + region.width / 2f) / 1024f * rawWidth - rawWidth / 2f;
            float regionCenterY = (region.y + region.height / 2f) / 1024f * rawHeight - rawHeight / 2f;

            rect.anchoredPosition = new Vector2(regionCenterX, regionCenterY);
            rect.sizeDelta = new Vector2(region.width / 1024f * rawWidth, region.height / 1024f * rawHeight);

            activeOutlines.Add(outline);
        }

        var regions = detector.GetDetectedBlueRegions();
        Debug.Log($"[Visualizer] Found {regions.Count} blue regions");

    }

    void ClearOldOutlines()
    {
        foreach (GameObject obj in activeOutlines)
        {
            Destroy(obj);
        }
        activeOutlines.Clear();
    }
}
