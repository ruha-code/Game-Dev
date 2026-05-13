using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [Header("Cursor Settings")]
    public Texture2D cursorTexture;
    public Vector2 hotspot = Vector2.zero;
    
    [Tooltip("The size of the cursor in pixels.")]
    [Range(16, 128)]
    public int cursorSize = 128;

    private Texture2D resizedCursor;
    private int lastSize;
    private Texture2D lastTexture;

    private void Start()
    {
        UpdateCursor();
    }

    private void Update()
    {
        // Simple check to update if values change in inspector during play
        if (cursorSize != lastSize || cursorTexture != lastTexture)
        {
            UpdateCursor();
        }
    }

    private void OnValidate()
    {
        // This ensures the cursor updates immediately when changing values in the Inspector
        if (Application.isPlaying)
        {
            UpdateCursor();
        }
    }

    public void UpdateCursor()
    {
        if (cursorTexture == null)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        lastSize = cursorSize;
        lastTexture = cursorTexture;

        // Clean up old temporary texture to avoid memory leaks
        if (resizedCursor != null)
        {
            Destroy(resizedCursor);
        }

        // Resize the texture to the desired size
        resizedCursor = ResizeTexture(cursorTexture, cursorSize, cursorSize);
        
        // Apply the cursor. Hotspot is scaled relative to the new size.
        Vector2 scaledHotspot = hotspot * ((float)cursorSize / cursorTexture.width);
        Cursor.SetCursor(resizedCursor, scaledHotspot, CursorMode.Auto);
    }

    private Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        rt.filterMode = FilterMode.Bilinear;
        
        RenderTexture active = RenderTexture.active;
        RenderTexture.active = rt;
        
        Graphics.Blit(source, rt);
        
        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();
        
        RenderTexture.active = active;
        RenderTexture.ReleaseTemporary(rt);
        
        return result;
    }

    private void OnDestroy()
    {
        if (resizedCursor != null)
        {
            Destroy(resizedCursor);
        }
    }
}
