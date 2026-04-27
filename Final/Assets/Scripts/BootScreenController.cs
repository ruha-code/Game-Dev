using UnityEngine;

public class BootScreenController : MonoBehaviour
{
    public enum ScreenMode
    {
        Idle,
        Hallucination,
        Pull
    }

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

    public ScreenMode CurrentMode => mode;
    public float CurrentProgress => phaseProgress;

    private readonly string[] corruptTitles =
    {
        "Welcome back...",
        "Welc_me back...",
        "Welcome b_ck...",
        "W E L C O M E",
        "DO NOT LOOK AWAY"
    };

    private readonly string[] corruptStatuses =
    {
        "system online",
        "signal unstable",
        "unknown user detected",
        "pull vector locked",
        "wake sequence failed"
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
        if (existing != null)
        {
            return existing;
        }

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

        Color targetColor = idleColor;
        if (mode == ScreenMode.Hallucination)
        {
            targetColor = Color.Lerp(activeColor, panicColor, 0.35f + glitchBurst * 0.4f);
        }
        else if (mode == ScreenMode.Pull)
        {
            targetColor = Color.Lerp(activeColor, panicColor, Mathf.Clamp01(phaseProgress + pulse * 0.25f));
        }

        if (screenMaterial != null)
        {
            Color targetLitColor = targetColor * Mathf.Lerp(1f, 1.35f, glitchBurst) * screenBrightness;
            currentScreenColor = Color.Lerp(currentScreenColor, targetLitColor, Time.deltaTime * 7f);
            screenMaterial.color = currentScreenColor;
            if (screenMaterial.HasProperty("_EmissionColor"))
            {
                screenMaterial.EnableKeyword("_EMISSION");
                Color contrastedColor = new Color(
                    Mathf.Pow(currentScreenColor.r * contrastBoost, 1.1f),
                    Mathf.Pow(currentScreenColor.g * contrastBoost, 1.1f),
                    Mathf.Pow(currentScreenColor.b * contrastBoost, 1.1f),
                    currentScreenColor.a
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
            screenSurface.transform.localScale = scale;
        }

        UpdateText(hardGlitch);
        UpdateScanLines(hardGlitch);
        UpdateVortex();
        UpdateLight(hardGlitch, flicker);
    }

    private void AnchorEffectsToScreen()
    {
        if (!anchorEffectsToScreen || screenSurface == null)
        {
            return;
        }

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
            if (Vector3.Dot(facing, toCamera) < 0f)
            {
                facing = -facing;
            }
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
            titleText.transform.SetPositionAndRotation(planeOrigin + up * (height * 0.18f), planeRotation);
        }

        if (statusText != null)
        {
            statusText.transform.SetPositionAndRotation(planeOrigin - up * (height * 0.18f), planeRotation);
        }

        if (scanLines != null)
        {
            for (int i = 0; i < scanLines.Length; i++)
            {
                if (scanLines[i] == null)
                {
                    continue;
                }

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
                if (vortexLines[i] == null)
                {
                    continue;
                }

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
        if (titleText != null)
        {
            int titleIndex = 0;
            if (mode != ScreenMode.Idle && phaseProgress > 0.82f)
            {
                titleIndex = Mathf.Abs(Mathf.FloorToInt(phaseTime * 14f)) % corruptTitles.Length;
            }
            titleText.text = corruptTitles[titleIndex];
            titleText.color = hardGlitch ? panicColor : new Color(0.88f, 1f, 1f, 1f);
            Vector3 titleScale = Vector3.one * (mode == ScreenMode.Pull ? Mathf.Lerp(0.08f, 0.12f, phaseProgress) : 0.08f);
            titleText.transform.localScale = titleScale;
        }

        if (statusText != null)
        {
            int statusIndex = 0;
            if (mode == ScreenMode.Pull)
            {
                statusIndex = Mathf.Abs(Mathf.FloorToInt(phaseTime * 18f)) % corruptStatuses.Length;
            }
            else if (hardGlitch && phaseProgress > 0.88f)
            {
                statusIndex = 2 + (Mathf.Abs(Mathf.FloorToInt(phaseTime * 10f)) % 3);
            }
            statusText.text = corruptStatuses[statusIndex];
            statusText.color = mode == ScreenMode.Idle ? new Color(0.45f, 0.95f, 1f, 1f) : panicColor;
        }
    }

    private void UpdateScanLines(bool hardGlitch)
    {
        if (scanLines == null)
        {
            return;
        }

        for (int i = 0; i < scanLines.Length; i++)
        {
            if (scanLines[i] == null)
            {
                continue;
            }

            float wave = Mathf.PerlinNoise(Time.time * (7f + i), i * 0.77f);
            bool visible = mode == ScreenMode.Pull || (mode == ScreenMode.Hallucination && wave > 0.32f);
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
            float modeBoost = mode == ScreenMode.Pull ? 1.0f + phaseProgress * 0.8f : mode == ScreenMode.Hallucination ? 1.15f : 0.65f;
            scale.x = anchorEffectsToScreen && screenSurface != null
                ? Mathf.Lerp(0.02f, 0.085f, wave) * modeBoost
                : Mathf.Lerp(0.08f, 0.38f, wave) * modeBoost;
            scanLines[i].transform.localScale = scale;
        }
    }

    private void UpdateVortex()
    {
        if (vortexLines == null)
        {
            return;
        }

        for (int i = 0; i < vortexLines.Length; i++)
        {
            if (vortexLines[i] == null)
            {
                continue;
            }

            bool visible = mode == ScreenMode.Pull;
            vortexLines[i].enabled = visible;
            if (!visible)
            {
                continue;
            }

            float offset = i * 0.13f;
            float spiral = Mathf.Repeat(Mathf.Max(phaseProgress, 0.2f) + offset + Time.time * 0.22f, 1f);
            Quaternion spin = Quaternion.Euler(0f, 0f, Time.time * (mode == ScreenMode.Pull ? 180f + i * 28f : 65f + i * 12f));
            if (anchorEffectsToScreen && screenSurface != null)
            {
                vortexLines[i].transform.SetPositionAndRotation(anchoredPlaneOrigin + anchoredPlaneFacing * (0.0012f * i), anchoredPlaneRotation * spin);
            }
            else
            {
                vortexLines[i].transform.localRotation = spin;
            }

            float shrink = Mathf.Lerp(mode == ScreenMode.Pull ? 2.35f : 1.2f, mode == ScreenMode.Pull ? 0.08f : 0.45f, spiral);
            if (anchorEffectsToScreen && screenSurface != null)
            {
                vortexLines[i].transform.localScale = new Vector3(
                    Mathf.Lerp(0.025f, 0.075f, shrink),
                    Mathf.Max(0.0025f, anchoredPlaneHeight * 0.01f),
                    0.004f);
            }
            else
            {
                Vector3 baseScale = i < vortexBaseScales.Length ? vortexBaseScales[i] : Vector3.one;
                vortexLines[i].transform.localScale = baseScale * shrink;
            }
        }
    }

    private void UpdateLight(bool hardGlitch, float flicker)
    {
        if (monitorLight == null)
        {
            return;
        }

        if (mode == ScreenMode.Idle)
        {
            currentLightIntensity = Mathf.Lerp(currentLightIntensity, Mathf.Lerp(baseLight * 0.75f, baseLight, flicker), Time.deltaTime * 6f);
            monitorLight.intensity = currentLightIntensity;
            monitorLight.color = Color.Lerp(activeColor, panicColor, 0.1f);
        }
        else if (mode == ScreenMode.Hallucination)
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
        if (renderers == null)
        {
            return new Vector3[0];
        }

        Vector3[] positions = new Vector3[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            positions[i] = renderers[i] != null ? renderers[i].transform.localPosition : Vector3.zero;
        }

        return positions;
    }

    private Vector3[] CacheScales(Renderer[] renderers)
    {
        if (renderers == null)
        {
            return new Vector3[0];
        }

        Vector3[] scales = new Vector3[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            scales[i] = renderers[i] != null ? renderers[i].transform.localScale : Vector3.one;
        }

        return scales;
    }
}
