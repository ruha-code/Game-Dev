using UnityEngine;

public class WallpaperController : MonoBehaviour
{
    public Renderer wallpaperRenderer;
    public Texture2D wallpaperTexture;
    
    private Texture2D generatedWallpaper;
    private Material wallpaperMaterial;
    
    void Start()
    {
        if (wallpaperRenderer == null)
            wallpaperRenderer = GetComponent<Renderer>();
        
        if (wallpaperRenderer != null)
        {
            wallpaperMaterial = new Material(Shader.Find("Standard"));
            wallpaperMaterial.EnableKeyword("_EMISSION");
            wallpaperRenderer.material = wallpaperMaterial;
            
            if (wallpaperTexture == null)
            {
                generatedWallpaper = GenerateWallpaper();
                wallpaperMaterial.SetTexture("_MainTex", generatedWallpaper);
                wallpaperMaterial.SetTexture("_EmissionMap", generatedWallpaper);
                wallpaperMaterial.SetFloat("_EmissionScaleUI", 1.5f);
            }
            else
            {
                wallpaperMaterial.SetTexture("_MainTex", wallpaperTexture);
                wallpaperMaterial.SetTexture("_EmissionMap", wallpaperTexture);
            }
        }
    }
    
    Texture2D GenerateWallpaper()
    {
        int w = 1024, h = 1024;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = (float)x / w, ny = 1f - (float)y / h;
                Color c;
                
                // Sky gradient (top 65%)
                if (ny > 0.35f)
                {
                    float skyT = (ny - 0.35f) / 0.65f;
                    c = Color.Lerp(new Color(0.5f, 0.8f, 1f), new Color(0.05f, 0.25f, 0.85f), skyT);
                    
                    // Sun with lens flare
                    float sunDist = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.12f, 0.88f));
                    if (sunDist < 0.04f)
                        c = Color.Lerp(Color.white, c, sunDist / 0.04f);
                    else if (sunDist < 0.08f)
                        c = Color.Lerp(new Color(1f, 0.98f, 0.8f), c, (sunDist - 0.04f) / 0.04f);
                    
                    // Lens flare streaks
                    float flareAngle = Mathf.Atan2(ny - 0.88f, nx - 0.12f);
                    float flareDist = sunDist;
                    if (Mathf.Abs(flareAngle) < 0.1f && flareDist < 0.3f && flareDist > 0.08f)
                    {
                        float flareIntensity = Mathf.Pow(1f - (flareDist - 0.08f) / 0.22f, 2f);
                        c = Color.Lerp(c, new Color(0.8f, 0.9f, 1f), flareIntensity * 0.3f);
                    }
                    
                    // Large bubble
                    float b1Dist = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.5f, 0.78f));
                    if (b1Dist < 0.15f)
                    {
                        float ba = 1f - b1Dist / 0.15f;
                        float rim = Mathf.Pow(1f - Mathf.Abs(b1Dist - 0.12f) / 0.03f, 0.5f);
                        rim = Mathf.Clamp01(rim);
                        Color bubbleColor = new Color(0.7f, 0.85f, 1f, 0.4f);
                        Color rimColor = new Color(0.9f, 0.95f, 1f, 0.8f);
                        c = Color.Lerp(c, rim > 0.5f ? rimColor : bubbleColor, ba * 0.6f);
                        
                        // Bubble highlight
                        float hlDist = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.47f, 0.82f));
                        if (hlDist < 0.03f)
                            c = Color.Lerp(c, Color.white, (1f - hlDist / 0.03f) * 0.7f);
                    }
                    
                    // Small bubble
                    float b2Dist = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.18f, 0.62f));
                    if (b2Dist < 0.1f)
                    {
                        float ba = 1f - b2Dist / 0.1f;
                        Color bubbleColor = new Color(0.7f, 0.85f, 1f, 0.4f);
                        c = Color.Lerp(c, bubbleColor, ba * 0.5f);
                        float hlDist = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.16f, 0.65f));
                        if (hlDist < 0.02f)
                            c = Color.Lerp(c, Color.white, (1f - hlDist / 0.02f) * 0.6f);
                    }
                    
                    // Hot air balloon
                    float balloonX = 0.62f, balloonY = 0.48f;
                    float balloonDist = Vector2.Distance(new Vector2(nx, ny), new Vector2(balloonX, balloonY));
                    if (balloonDist < 0.04f)
                    {
                        float bt = 1f - balloonDist / 0.04f;
                        Color balloonColor = new Color(0.9f, 0.3f, 0.2f);
                        c = Color.Lerp(c, balloonColor, bt);
                    }
                    // Balloon basket
                    if (nx > balloonX - 0.01f && nx < balloonX + 0.01f && ny > balloonY - 0.06f && ny < balloonY - 0.03f)
                        c = new Color(0.5f, 0.3f, 0.1f);
                    
                    // City skyline (right side)
                    if (nx > 0.58f && ny < 0.65f && ny > 0.35f)
                    {
                        float buildingSeed = Mathf.Sin(nx * 100f) * 0.5f + 0.5f;
                        float buildingHeight = 0.38f + buildingSeed * 0.25f;
                        if (ny < buildingHeight)
                        {
                            float windowPattern = (Mathf.Sin(nx * 200f) > 0.7f && Mathf.Sin(ny * 150f) > 0.7f) ? 1f : 0f;
                            Color buildingColor = Color.Lerp(new Color(0.6f, 0.65f, 0.7f), new Color(0.4f, 0.45f, 0.5f), buildingSeed);
                            if (windowPattern > 0.5f)
                                buildingColor = Color.Lerp(buildingColor, new Color(0.9f, 0.95f, 1f), 0.5f);
                            c = buildingColor;
                        }
                    }
                }
                // Tree line
                else if (ny > 0.3f && ny <= 0.35f)
                {
                    float treeNoise = Mathf.Sin(nx * 40f + Mathf.Sin(nx * 15f) * 3f) * 0.5f + 0.5f;
                    c = Color.Lerp(new Color(0.15f, 0.45f, 0.1f), new Color(0.25f, 0.55f, 0.15f), treeNoise);
                }
                // Grass field (bottom 30%)
                else
                {
                    float grassNoise = Mathf.PerlinNoise(nx * 30f, ny * 30f) * 0.2f;
                    float grassDetail = Mathf.Sin(nx * 100f + ny * 50f) * 0.05f;
                    c = new Color(0.18f + grassNoise + grassDetail, 0.55f + grassNoise, 0.08f);
                    
                    // Single tree on right
                    float treeDist = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.82f, 0.15f));
                    if (treeDist < 0.06f && ny < 0.28f)
                    {
                        float treeAlpha = 1f - treeDist / 0.06f;
                        c = Color.Lerp(c, new Color(0.12f, 0.35f, 0.08f), treeAlpha);
                    }
                    // Tree trunk
                    if (nx > 0.815f && nx < 0.825f && ny > 0.05f && ny < 0.15f)
                        c = new Color(0.3f, 0.2f, 0.1f);
                    
                    // Bench under tree
                    if (nx > 0.8f && nx < 0.84f && ny > 0.07f && ny < 0.09f)
                        c = new Color(0.4f, 0.25f, 0.15f);
                }
                
                tex.SetPixel(x, y, c);
            }
        }
        
        tex.Apply();
        return tex;
    }
    
    void OnDestroy()
    {
        if (generatedWallpaper != null) Destroy(generatedWallpaper);
        if (wallpaperMaterial != null) Destroy(wallpaperMaterial);
    }
}
