using UnityEngine;

/// <summary>
/// Простой контроллер для галлюциногенного существа.
/// Делает существо видимым/невидимым и заставляет смотреть на камеру.
/// </summary>
public class HallucinationPresence : MonoBehaviour
{
    [Header("Target")]
    public Camera targetCamera;
    
    [Header("Visual")]
    public Renderer[] presenceRenderers;
    public float flickerSpeed = 2f;
    public float flickerIntensity = 0.5f;
    
    [Header("Movement")]
    public float jitterAmount = 0.02f;
    public float jitterSpeed = 15f;
    
    private bool isVisible = true;
    private Vector3 basePosition;
    private Material[] baseMaterials;
    private Color[] baseColors;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        
        basePosition = transform.position;
        
        if (presenceRenderers == null || presenceRenderers.Length == 0)
        {
            presenceRenderers = GetComponentsInChildren<Renderer>();
        }
        
        // Сохраняем материалы
        if (presenceRenderers != null)
        {
            baseMaterials = new Material[presenceRenderers.Length];
            baseColors = new Color[presenceRenderers.Length];
            
            for (int i = 0; i < presenceRenderers.Length; i++)
            {
                if (presenceRenderers[i] != null)
                {
                    baseMaterials[i] = presenceRenderers[i].sharedMaterial;
                    baseColors[i] = presenceRenderers[i].material.color;
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (!isVisible) return;
        
        // Смотрим на камеру
        FaceCamera();
        
        // Лёгкий джиттер
        ApplyJitter();
        
        // Мерцание
        ApplyFlicker();
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        UpdateRendererVisibility();
    }

    public void SetPosition(Vector3 position)
    {
        basePosition = position;
        transform.position = position;
    }

    private void FaceCamera()
    {
        if (targetCamera == null) return;
        
        Vector3 direction = targetCamera.transform.position - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private void ApplyJitter()
    {
        float jitterX = Mathf.Sin(Time.time * jitterSpeed) * jitterAmount;
        float jitterY = Mathf.Cos(Time.time * jitterSpeed * 0.7f) * jitterAmount;
        float jitterZ = Mathf.Sin(Time.time * jitterSpeed * 1.3f) * jitterAmount;
        
        transform.position = basePosition + new Vector3(jitterX, jitterY, jitterZ);
    }

    private void ApplyFlicker()
    {
        if (presenceRenderers == null) return;
        
        float flicker = (Mathf.Sin(Time.time * flickerSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(1f - flickerIntensity, 1f, flicker);
        
        for (int i = 0; i < presenceRenderers.Length; i++)
        {
            if (presenceRenderers[i] != null && baseColors[i] != null)
            {
                Color c = baseColors[i];
                c.a = alpha;
                presenceRenderers[i].material.color = c;
            }
        }
    }

    private void UpdateRendererVisibility()
    {
        if (presenceRenderers == null) return;
        
        for (int i = 0; i < presenceRenderers.Length; i++)
        {
            if (presenceRenderers[i] != null)
            {
                presenceRenderers[i].enabled = isVisible;
            }
        }
    }

    private void OnValidate()
    {
        if (presenceRenderers == null || presenceRenderers.Length == 0)
        {
            presenceRenderers = GetComponentsInChildren<Renderer>();
        }
    }
}
