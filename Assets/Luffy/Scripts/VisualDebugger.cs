using UnityEngine;

public class VisualDebugger : MonoBehaviour
{
    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        
        if (sr == null)
        {
            Debug.LogError("VISUAL FAIL: No SpriteRenderer found!");
            return;
        }

        string spriteName = (sr.sprite != null) ? sr.sprite.name : "NULL (No Sprite Assigned!)";
        string colorAlpha = sr.color.a.ToString(); // Should be 1
        string layerName = sr.sortingLayerName;
        int layerOrder = sr.sortingOrder;
        Vector3 scale = transform.localScale;
        
        Debug.Log($"<color=cyan>VISUAL REPORT for {gameObject.name}:</color>\n" +
                  $"Sprite: {spriteName}\n" +
                  $"Color Alpha: {colorAlpha} (Must be 1)\n" +
                  $"Sorting Layer: {layerName}\n" +
                  $"Order In Layer: {layerOrder} (Should be 100+)\n" +
                  $"Scale: {scale} (Must not be 0)\n" + 
                  $"Position: {transform.position}");
    }
}