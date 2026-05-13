using UnityEngine;

public class BootScreenController : MonoBehaviour
{
    public enum ScreenMode { Idle, Hallucination, Pull, GlitchText }

    public Renderer screenSurface;
    public TextMesh titleText;
    public TextMesh statusText;
    public Renderer[] scanLines;
    public Renderer[] vortexLines;
    public Light monitorLight;
    public bool anchorEffectsToScreen = true;

    public Color idleColor = new Color(0.0f, 0.08f, 0.12f, 1f);
    public Color activeColor = new Color(0.02f, 0.25f, 0.35f, 1f);
    public Color panicColor = new Color(0.3f, 0.9f, 1f, 1f);
    public float baseLight = 1.4f;
    public float glitchStrength = 1f;
    [Header("Screen Clarity")]
    public float screenBrightness = 0.8f;
    public float contrastBoost = 1.1f;
    public float scanlineOpacity = 0.08f;

    private Material screenMaterial;
    private Vector3 screenBaseScale;
    private Vector3[] scanBasePositions;
    private Vector3[] vortexBaseScales;
    private ScreenMode mode;
    private float phaseTime;
    private float phaseProgress;
    private Vector3 anchoredPlaneOrigin;
    private Vector3 anchoredPlaneRight;
    private Vector3 anchoredPlaneUp;
    private Vector3 anchoredPlaneFacing;
    private Quaternion anchoredPlaneRotation;
    private float anchoredPlaneWidth;
    private float anchoredPlaneHeight;
    private Color currentScreenColor;
    private Color currentEmissionColor;
    private float currentLightIntensity;
    private float chromaticOffset;
    private float screenTearOffset;
    private float lastGlitchTime;
    private float glitchTextTimer;
    private int glitchTextIndex;

    public ScreenMode CurrentMode => mode;
    public float CurrentProgress => phaseProgress;

    private readonly string[] glitchTexts =
    {
        "H̷E̷ ̷S̷E̷E̷S̷ ̷Y̷O̷U̷",
        "D̸O̸ ̸N̸O̸T̸ ̸L̸O̸O̸K̸",
        "S̷Y̷S̷T̷E̷M̷ ̷F̷A̷I̷L̷U̷R̷E̷",
        "W̸A̸K̸E̸ ̸U̸P̸",
        "R̷U̷N̷",
        "01001000 01000101 01001100 01010000",
        "c̸o̸n̸n̸e̸c̸t̸i̸o̸n̸ ̸l̸o̸s̸t̸",
        "e̷n̷t̷i̷t̷y̷ ̷a̷p̷p̷r̷o̷a̷c̷h̷i̷n̷g̷",
        "m̸e̸m̸o̸r̸y̸ ̸c̸o̸r̸r̸u̸p̸t̸e̸d̸",
        "d̷o̷ ̷n̷o̷t̷ ̷t̷r̷u̷s̷t̷ ̷t̷h̷e̷ ̷s̷c̷r̷e̷e̷n̷",
        "I̸T̸ ̸K̸N̸O̸W̸S̸",
        "Y̷O̷U̷ ̷C̷A̷N̷T̷ ̷E̷S̷C̷A̷P̷E̷",
        "████████ ERROR ████",
        "F̸A̸T̸A̸L̸ ̸E̸R̸R̸O̸R̸",
        "H̷E̷ ̷I̷S̷ ̷H̷E̷R̷E̷",
        "... ... ...",
        "L̸O̸O̸K̸ ̸B̸E̸H̸I̸N̸D̸ ̸Y̸O̸U̸",
        "S̷I̷G̷N̷A̷L̷ ̷L̷O̷S̷T̷",
        "0̸1̸0̸0̸ ̸D̸E̸A̸T̸H̸",
        "W̷H̷Y̷ ̷D̷I̷D̷ ̷Y̷O̷U̷ ̷C̷O̷M̷E̷ ̷B̷A̷C̷K̷"
    };

    private void Awake()
    {
        EnsureTextMeshes();

        if (screenSurface != null)
        {
            screenMaterial = screenSurface.material;
            screenBaseScale = screenSurface.transform.localScale;
            currentScreenColor = screenMaterial.color;
            currentEmissionColor = screenMaterial.HasProperty("_EmissionColor") ? screenMaterial.GetColor("_EmissionColor") : Color.black;
        }

        currentLightIntensity = monitorLight != null ? monitorLight.intensity : 0f;

        scanBasePositions = CachePositions(scanLines);
        vortexBaseScales = CacheScales(vortexLines);
        SetMode(ScreenMode.Idle, 0f);
    }

    private void EnsureTextMeshes()
    {
        titleText = EnsureTextMesh(titleText, "Boot Screen Title", TextAnchor.MiddleCenter, 64);
        statusText = EnsureTextMesh(statusText, "Boot Screen Status", TextAnchor.MiddleCenter, 40);
    }

    private TextMesh EnsureTextMesh(TextMesh existing, string objectName, TextAnchor anchor, int fontSize)
    {
        if (existing != null) return existing;

        GameObject textObject = new GameObject(objectName);
        Transform parent = screenSurface != null ? screenSurface.transform.parent : transform;
        textObject.transform.SetParent(parent, false);

        TextMesh created = textObject.AddComponent<TextMesh>();
        created.anchor = anchor;
        created.alignment = TextAlignment.Center;
        created.fontSize = fontSize;
        created.characterSize = 0.02f;
        created.text = string.Empty;
        created.color = new Color(0.85f, 1f, 1f, 1f);
        return created;
    }

    private void Update()
    {
        phaseTime += Time.deltaTime;
        RenderScreen();
    }

    public void SetMode(ScreenMode nextMode, float progress)
    {
        if (mode != nextMode)
        {
            mode = nextMode;
            phaseTime = 0f;
            glitchTextTimer = 0f;
            glitchTextIndex = 0;
        }
        phaseProgress = Mathf.Clamp01(progress);
    }

    private void RenderScreen()
    {
        AnchorEffectsToScreen();

        float flicker = Mathf.PerlinNoise(Time.time * 10f, 2.5f);
        float pulse = 0.5f + Mathf.Sin(Time.time * 6.5f) * 0.5f;
        float burstNoise = Mathf.PerlinNoise(Time.time * 16f, 9.2f);
        float burstDuration = 0.08f;
        float burstTime = Mathf.Repeat(Time.time * 2.3f, burstDuration * 10f);
        bool burstActive = burstTime < burstDuration;
        float burstStrength = burstActive ? Mathf.Sin((burstTime / burstDuration) * Mathf.PI) : 0f;
        float glitchBurst = mode == ScreenMode.Idle ? 0f : Mathf.Clamp01(0.55f + phaseProgress * 0.7f + (burstNoise - 0.5f) * 0.55f + burstStrength * 0.3f);
        bool hardGlitch = mode != ScreenMode.Idle && (glitchBurst > 0.48f || burstActive && mode != ScreenMode.Idle);
        
        chromaticOffset = mode == ScreenMode.Idle ? 0f : Mathf.Lerp(0f, 0.03f, glitchBurst) * (Mathf.PerlinNoise(Time.time * 20f, 5f) - 0.5f);
        screenTearOffset = mode == ScreenMode.Idle ? 0f : Mathf.Lerp(0f, 0.08f, glitchBurst * glitchBurst) * (Mathf.Sin(Time.time * 35f) > 0.7f ? 1f : 0f);
        
        if (hardGlitch && Time.time - lastGlitchTime > 0.15f)
            lastGlitchTime = Time.time;

        Color targetColor = idleColor;
        if (mode == ScreenMode.Hallucination || mode == ScreenMode.GlitchText)
            targetColor = Color.Lerp(activeColor, panicColor, 0.35f + glitchBurst * 0.4f);
        else if (mode == ScreenMode.Pull)
            targetColor = Color.Lerp(activeColor, panicColor, Mathf.Clamp01(phaseProgress + pulse * 0.25f));

        if (screenMaterial != null)
        {
            Color targetLitColor = targetColor * Mathf.Lerp(1f, 1.35f, glitchBurst) * screenBrightness;
            currentScreenColor = Color.Lerp(currentScreenColor, targetLitColor, Time.deltaTime * 7f);
            
            Color aberratedColor = currentScreenColor;
            aberratedColor.r = Mathf.Clamp01(aberratedColor.r + chromaticOffset);
            aberratedColor.b = Mathf.Clamp01(aberratedColor.b - chromaticOffset);
            
            screenMaterial.color = aberratedColor;
            if (screenMaterial.HasProperty("_EmissionColor"))
            {
                screenMaterial.EnableKeyword("_EMISSION");
                Color contrastedColor = new Color(
                    Mathf.Pow(aberratedColor.r * contrastBoost, 1.1f),
                    Mathf.Pow(aberratedColor.g * contrastBoost, 1.1f),
                    Mathf.Pow(aberratedColor.b * contrastBoost, 1.1f),
                    aberratedColor.a
                );
                Color targetEmission = contrastedColor * Mathf.Lerp(0.55f, 1.75f, glitchBurst);
                currentEmissionColor = Color.Lerp(currentEmissionColor, targetEmission, Time.deltaTime * 8f);
                screenMaterial.SetColor("_EmissionColor", currentEmissionColor);
            }
        }

        if (screenSurface != null)
        {
            Vector3 scale = screenBaseScale;
            scale.x += (Mathf.PerlinNoise(Time.time * 18f, 1.1f) - 0.5f) * 0.028f * glitchStrength * Mathf.Max(0.18f, glitchBurst);
            scale.y += (Mathf.PerlinNoise(4.4f, Time.time * 20f) - 0.5f) * 0.018f * glitchStrength * Mathf.Max(0.12f, glitchBurst);
            scale.y += mode == ScreenMode.Pull ? Mathf.Sin(Time.time * 24f) * 0.028f * Mathf.Max(0.2f, phaseProgress) : 0f;
            scale.x += screenTearOffset;
            
            if (hardGlitch && Mathf.PerlinNoise(Time.time * 50f, 10f) > 0.85f)
                scale.y *= Mathf.Lerp(1f, 0.7f, Mathf.PerlinNoise(Time.time * 50f, 11f));
            
            screenSurface.transform.localScale = scale;
        }

        UpdateText(hardGlitch);
        UpdateScanLines(hardGlitch);
        UpdateVortex();
        UpdateLight(hardGlitch, flicker);
    }

    private void AnchorEffectsToScreen()
    {
        if (!anchorEffectsToScreen || screenSurface == null) return;

        Vector3 center = screenSurface.bounds.center;
        Vector3 size = screenSurface.bounds.size;
        float width = Mathf.Max(0.1f, size.x);
        float height = Mathf.Max(0.1f, size.y);
        Transform screenTransform = screenSurface.transform;
        Camera referenceCamera = Camera.main;
        Vector3 up = screenTransform.up;
        Vector3 right = screenTransform.right;
        Vector3 facing = screenTransform.forward;

        if (referenceCamera != null)
        {
            Vector3 toCamera = (referenceCamera.transform.position - center).normalized;
            if (Vector3.Dot(facing, toCamera) < 0f) { facing = -facing; right = -right; }
            Vector3 rightToCamera = new Vector3(toCamera.x, 0f, toCamera.z).normalized;
            if (Vector3.Dot(right, rightToCamera) > 0.5f) right = -right;
        }

        Vector3 planeOrigin = center + facing * Mathf.Max(0.004f, size.z * 0.12f + 0.0015f);
        Quaternion planeRotation = Quaternion.LookRotation(facing, up);
        anchoredPlaneOrigin = planeOrigin;
        anchoredPlaneRight = right;
        anchoredPlaneUp = up;
        anchoredPlaneFacing = facing;
        anchoredPlaneRotation = planeRotation;
        anchoredPlaneWidth = width;
        anchoredPlaneHeight = height;

        if (titleText != null)
        {
            titleText.transform.position = planeOrigin + up * (height * 0.18f);
            Camera refCam = Camera.main;
            if (refCam != null) titleText.transform.LookAt(refCam.transform.position, up);
            else titleText.transform.rotation = planeRotation;
            titleText.transform.localScale = new Vector3(-0.08f, 0.08f, 0.08f);
        }

        if (statusText != null)
        {
            statusText.transform.position = planeOrigin - up * (height * 0.18f);
            Camera refCam = Camera.main;
            if (refCam != null) statusText.transform.LookAt(refCam.transform.position, up);
            else statusText.transform.rotation = planeRotation;
            statusText.transform.localScale = new Vector3(-1f, 1f, 1f);
        }

        if (scanLines != null)
        {
            for (int i = 0; i < scanLines.Length; i++)
            {
                if (scanLines[i] == null) continue;
                float normalizedY = scanLines.Length <= 1 ? 0.5f : i / (float)(scanLines.Length - 1);
                Vector3 anchoredPosition = planeOrigin - up * (height * 0.34f) + up * (height * 0.68f * normalizedY) + facing * (0.0008f * i);
                scanLines[i].transform.SetPositionAndRotation(anchoredPosition, planeRotation);
                Vector3 scale = scanLines[i].transform.localScale;
                scale.y = Mathf.Max(0.003f, height * 0.015f);
                scale.z = 0.004f;
                scanLines[i].transform.localScale = scale;
            }
        }

        if (vortexLines != null)
        {
            for (int i = 0; i < vortexLines.Length; i++)
            {
                if (vortexLines[i] == null) continue;
                vortexLines[i].transform.SetPositionAndRotation(planeOrigin + facing * (0.0012f * i), planeRotation);
                Vector3 scale = vortexLines[i].transform.localScale;
                scale.y = Mathf.Max(0.003f, height * 0.013f);
                scale.z = 0.004f;
                vortexLines[i].transform.localScale = scale;
            }
        }
    }

    private void UpdateText(bool hardGlitch)
    {
        if (mode == ScreenMode.GlitchText)
{
            // Rapid glitch text cycling
            glitchTextTimer += Time.deltaTime;
            float textSpeed = 3f + phaseProgress * 10f;
            
            if (titleText != null)
            {
                glitchTextIndex = Mathf.Abs(Mathf.FloorToInt(glitchTextTimer * textSpeed)) % glitchTexts.Length;
                titleText.text = glitchTexts[glitchTextIndex];
                
                float textFlicker = Mathf.PerlinNoise(Time.time * 30f, 3f);
                float alpha = textFlicker > 0.2f ? 1f : 0.2f;
                titleText.color = new Color(panicColor.r, panicColor.g, panicColor.b, alpha);
                
                Vector3 titleScale = Vector3.one * 0.08f;
                if (hardGlitch)
                {
                    titleScale.x += (Mathf.PerlinNoise(Time.time * 50f, 0f) - 0.5f) * 0.03f;
                    titleScale.y += (Mathf.PerlinNoise(Time.time * 50f, 1f) - 0.5f) * 0.03f;
                }
                titleText.transform.localScale = titleScale;
                titleText.transform.localPosition += new Vector3(chromaticOffset * 8f, 0f, 0f);
            }
            
            if (statusText != null)
            {
                int statusIdx = (glitchTextIndex + 5) % glitchTexts.Length;
                statusText.text = glitchTexts[statusIdx];
                
                float textFlicker = Mathf.PerlinNoise(Time.time * 30f, 4f);
                float alpha = textFlicker > 0.3f ? 1f : 0.1f;
                statusText.color = new Color(panicColor.r, panicColor.g, panicColor.b, alpha);
                statusText.transform.localPosition += new Vector3(-chromaticOffset * 8f, 0f, 0f);
            }
        }
        else if (mode == ScreenMode.Hallucination || mode == ScreenMode.Pull)
        {
            if (titleText != null)
            {
                titleText.text = string.Empty;
                titleText.color = Color.clear;
            }
            if (statusText != null)
            {
                statusText.text = string.Empty;
                statusText.color = Color.clear;
            }
        }
        else
        {
            // Idle - no text
            if (titleText != null) { titleText.text = string.Empty; titleText.color = Color.clear; }
            if (statusText != null) { statusText.text = string.Empty; statusText.color = Color.clear; }
        }
    }

    private void UpdateScanLines(bool hardGlitch)
    {
        if (scanLines == null) return;

        for (int i = 0; i < scanLines.Length; i++)
        {
            if (scanLines[i] == null) continue;
            float wave = Mathf.PerlinNoise(Time.time * (7f + i), i * 0.77f);
            bool visible = mode == ScreenMode.Pull || mode == ScreenMode.GlitchText || (mode == ScreenMode.Hallucination && wave > 0.32f);
            scanLines[i].enabled = visible;

            if (scanBasePositions != null && i < scanBasePositions.Length)
            {
                if (anchorEffectsToScreen && screenSurface != null)
                {
                    float normalizedY = scanLines.Length <= 1 ? 0.5f : i / (float)(scanLines.Length - 1);
                    float horizontalSweep = Mathf.Sin(Time.time * (16f + i * 1.7f) + i * 0.65f) * 0.045f;
                    float xOffset = (wave - 0.5f) * (mode == ScreenMode.Idle ? 0.02f : 0.07f * glitchStrength) + horizontalSweep * Mathf.Max(0f, phaseProgress);
                    float yOffset = hardGlitch ? Mathf.Sin(Time.time * (34f + i * 2.7f)) * 0.018f : 0f;
                    Vector3 anchoredPosition = anchoredPlaneOrigin - anchoredPlaneUp * (anchoredPlaneHeight * 0.34f)
                        + anchoredPlaneUp * (anchoredPlaneHeight * 0.68f * normalizedY + yOffset)
                        + anchoredPlaneRight * xOffset
                        + anchoredPlaneFacing * (0.0008f * i);
                    scanLines[i].transform.SetPositionAndRotation(anchoredPosition, anchoredPlaneRotation);
                }
                else
                {
                    Vector3 position = scanBasePositions[i];
                    float horizontalSweep = Mathf.Sin(Time.time * (16f + i * 1.7f) + i * 0.65f) * 0.045f;
                    position.x += (wave - 0.5f) * (mode == ScreenMode.Idle ? 0.02f : 0.07f * glitchStrength) + horizontalSweep * Mathf.Max(0f, phaseProgress);
                    position.y += hardGlitch ? Mathf.Sin(Time.time * (34f + i * 2.7f)) * 0.018f : 0f;
                    scanLines[i].transform.localPosition = position;
                }
            }

            Vector3 scale = scanLines[i].transform.localScale;
            float modeBoost = mode == ScreenMode.Pull || mode == ScreenMode.GlitchText ? 1.0f + phaseProgress * 0.8f : mode == ScreenMode.Hallucination ? 1.15f : 0.65f;
            scale.x = anchorEffectsToScreen && screenSurface != null
                ? Mathf.Lerp(0.02f, 0.085f, wave) * modeBoost
                : Mathf.Lerp(0.08f, 0.38f, wave) * modeBoost;
            scanLines[i].transform.localScale = scale;
        }
    }

    private void UpdateVortex()
    {
        if (vortexLines == null) return;

        for (int i = 0; i < vortexLines.Length; i++)
        {
            if (vortexLines[i] == null) continue;
            bool visible = mode == ScreenMode.Pull || mode == ScreenMode.GlitchText;
            vortexLines[i].enabled = visible;
            if (!visible) continue;

            float offset = i * 0.13f;
            float spiral = Mathf.Repeat(Mathf.Max(phaseProgress, 0.2f) + offset + Time.time * 0.22f, 1f);
            Quaternion spin = Quaternion.Euler(0f, 0f, Time.time * (mode == ScreenMode.Pull ? 180f + i * 28f : 65f + i * 12f));
            if (anchorEffectsToScreen && screenSurface != null)
                vortexLines[i].transform.SetPositionAndRotation(anchoredPlaneOrigin + anchoredPlaneFacing * (0.0012f * i), anchoredPlaneRotation * spin);
            else
                vortexLines[i].transform.localRotation = spin;

            float shrink = Mathf.Lerp(mode == ScreenMode.Pull ? 2.35f : 1.2f, mode == ScreenMode.Pull ? 0.08f : 0.45f, spiral);
            if (anchorEffectsToScreen && screenSurface != null)
                vortexLines[i].transform.localScale = new Vector3(Mathf.Lerp(0.025f, 0.075f, shrink), Mathf.Max(0.0025f, anchoredPlaneHeight * 0.01f), 0.004f);
            else
            {
                Vector3 baseScale = i < vortexBaseScales.Length ? vortexBaseScales[i] : Vector3.one;
                vortexLines[i].transform.localScale = baseScale * shrink;
            }
        }
    }

    private void UpdateLight(bool hardGlitch, float flicker)
    {
        if (monitorLight == null) return;

        if (mode == ScreenMode.Idle)
        {
            currentLightIntensity = Mathf.Lerp(currentLightIntensity, Mathf.Lerp(baseLight * 0.75f, baseLight, flicker), Time.deltaTime * 6f);
            monitorLight.intensity = currentLightIntensity;
            monitorLight.color = Color.Lerp(activeColor, panicColor, 0.1f);
        }
        else if (mode == ScreenMode.Hallucination || mode == ScreenMode.GlitchText)
        {
            float targetIntensity = Mathf.Lerp(baseLight * 1.4f, baseLight * 2.1f, phaseProgress) + (hardGlitch ? baseLight * 0.25f : 0f);
            currentLightIntensity = Mathf.Lerp(currentLightIntensity, targetIntensity, Time.deltaTime * 8f);
            monitorLight.intensity = currentLightIntensity;
            monitorLight.color = Color.Lerp(activeColor, panicColor, 0.55f + phaseProgress * 0.25f);
        }
        else
        {
            currentLightIntensity = Mathf.Lerp(currentLightIntensity, Mathf.Lerp(baseLight * 1.5f, baseLight * 3.1f, phaseProgress), Time.deltaTime * 10f);
            monitorLight.intensity = currentLightIntensity;
            monitorLight.color = Color.Lerp(activeColor, panicColor, 0.85f);
        }
    }

    private Vector3[] CachePositions(Renderer[] renderers)
    {
        if (renderers == null) return new Vector3[0];
        Vector3[] positions = new Vector3[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            positions[i] = renderers[i] != null ? renderers[i].transform.localPosition : Vector3.zero;
        return positions;
    }

    private Vector3[] CacheScales(Renderer[] renderers)
    {
        if (renderers == null) return new Vector3[0];
        Vector3[] scales = new Vector3[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            scales[i] = renderers[i] != null ? renderers[i].transform.localScale : Vector3.one;
        return scales;
    }
}
