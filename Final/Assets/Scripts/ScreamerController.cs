using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ScreamerController : MonoBehaviour
{
    [Header("Screamer Texture")]
    public Texture2D screamerTexture;
    
    [Header("Timing")]
    public float flashDuration = 0.15f;
    public float totalDuration = 0.4f;
    
    [Header("Flash Intensity")]
    public float maxIntensity = 2.5f;
    public AnimationCurve flashCurve;
    
    [Header("Camera Shake")]
    public float shakeIntensity = 2.5f;
    public float shakeSpeed = 15f;
    
    [Header("FOV Animation")]
    public float startFOV = 98f;
    public float endFOV = 60f;
    
    private Camera targetCamera;
    private Material screamerMaterial;
    private bool isActive = false;
    private float timer = 0f;
    private Vector3 originalPosition;

    public bool IsActive => isActive;
    public float Progress => timer / totalDuration;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        originalPosition = targetCamera.transform.position;

        if (screamerTexture != null)
        {
            screamerMaterial = new Material(Shader.Find("GUI/Text Shader"));
            screamerMaterial.mainTexture = screamerTexture;
        }

        if (flashCurve == null)
        {
            flashCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.3f, 1f),
                new Keyframe(1f, 0f)
            );
        }
    }

    public void Trigger()
    {
        isActive = true;
        timer = 0f;
        targetCamera = targetCamera ?? Camera.main;
        originalPosition = targetCamera.transform.position;
    }

    private void LateUpdate()
    {
        if (!isActive)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer <= flashDuration)
        {
            float flashProgress = timer / flashDuration;
            float flashValue = flashCurve.Evaluate(flashProgress);
            RenderOverlay(flashValue * maxIntensity);
            ApplyShake(flashValue);
        }
        else if (timer <= totalDuration)
        {
            float fadeProgress = (timer - flashDuration) / (totalDuration - flashDuration);
            float intensity = Mathf.Lerp(maxIntensity, 0f, fadeProgress);
            RenderOverlay(intensity);
            ApplyShake(1f - fadeProgress);
        }
        else
        {
            isActive = false;
            RenderOverlay(0f);
            targetCamera.transform.position = originalPosition;
            targetCamera.fieldOfView = endFOV;
        }
    }

    private void ApplyShake(float intensityMultiplier)
    {
        float currentShake = shakeIntensity * intensityMultiplier;
        Vector3 shake = new Vector3(
            Random.Range(-currentShake, currentShake) * 0.01f,
            Random.Range(-currentShake, currentShake) * 0.01f,
            Random.Range(-currentShake, currentShake) * 0.01f
        );
        targetCamera.transform.position = originalPosition + shake;
        targetCamera.fieldOfView = Mathf.Lerp(startFOV, endFOV, timer / totalDuration);
    }

    private void RenderOverlay(float intensity)
    {
        if (screamerMaterial == null || screamerTexture == null || intensity <= 0.01f)
        {
            return;
        }

        GL.PushMatrix();
        GL.LoadOrtho();
        screamerMaterial.SetPass(0);
        GL.Color(new Color(1f, 1f, 1f, intensity));
        GL.Begin(GL.QUADS);
        GL.TexCoord2(0f, 0f);
        GL.Vertex3(0f, 0f, 0f);
        GL.TexCoord2(1f, 0f);
        GL.Vertex3(1f, 0f, 0f);
        GL.TexCoord2(1f, 1f);
        GL.Vertex3(1f, 1f, 0f);
        GL.TexCoord2(0f, 1f);
        GL.Vertex3(0f, 1f, 0f);
        GL.End();
        GL.PopMatrix();
    }

    private void OnGUI()
    {
        if (!isActive || screamerTexture == null)
        {
            return;
        }

        float intensity = 1f;
        if (timer <= flashDuration)
        {
            float flashProgress = timer / flashDuration;
            intensity = flashCurve.Evaluate(flashProgress) * maxIntensity;
        }
        else if (timer <= totalDuration)
        {
            float fadeProgress = (timer - flashDuration) / (totalDuration - flashDuration);
            intensity = Mathf.Lerp(maxIntensity, 0f, fadeProgress);
        }

        if (intensity > 0.01f)
        {
            Color originalColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(intensity));
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), screamerTexture, ScaleMode.StretchToFill);
            GUI.color = originalColor;
        }
    }

    private void OnDestroy()
    {
        if (screamerMaterial != null)
        {
            Destroy(screamerMaterial);
        }
    }
}
