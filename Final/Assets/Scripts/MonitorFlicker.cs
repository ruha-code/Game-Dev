using UnityEngine;

[DisallowMultipleComponent]
public sealed class MonitorFlicker : MonoBehaviour
{
    [SerializeField] private Renderer screenRenderer;
    [SerializeField] private Light spillLight;
    [SerializeField] private Color baseEmissionColor = new(0.35f, 0.8f, 1f, 1f);
    [SerializeField] private float emissionIntensity = 2.4f;
    [SerializeField] private float flickerAmount = 0.12f;
    [SerializeField] private float flickerSpeed = 2.2f;
    [SerializeField] private float lightBaseIntensity = 2.3f;

    private Material runtimeMaterial;
    private BootScreenController screenController;

    private void Awake()
    {
        if (screenRenderer == null)
        {
            screenRenderer = GetComponent<Renderer>();
        }

        if (spillLight == null)
        {
            GameObject lightObject = GameObject.Find("MonitorLight");
            if (lightObject != null)
            {
                spillLight = lightObject.GetComponent<Light>();
            }
        }

        if (screenRenderer != null)
        {
            runtimeMaterial = screenRenderer.material;
            runtimeMaterial.EnableKeyword("_EMISSION");
        }

        screenController = GetComponentInParent<BootScreenController>();
    }

    private void LateUpdate()
    {
        if (screenController != null && screenController.CurrentMode != BootScreenController.ScreenMode.Idle)
        {
            return;
        }

        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0.37f);
        float intensityScale = 1f - flickerAmount + noise * flickerAmount;

        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetColor("_EmissionColor", baseEmissionColor * emissionIntensity * intensityScale);
        }

        if (spillLight != null)
        {
            spillLight.intensity = lightBaseIntensity * intensityScale;
        }
    }
}
