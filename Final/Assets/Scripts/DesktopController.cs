using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DesktopController : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public Transform transform;
        public float mouseInfluence = 0.05f;
        public Vector2 movementOffset;
        [HideInInspector] public Vector3 startPos;
    }

    [Header("Parallax")]
    public List<ParallaxLayer> layers = new List<ParallaxLayer>();
    public float smoothTime = 0.1f;

    [Header("Animated Elements")]
    public List<Transform> clouds = new List<Transform>();
    public List<Transform> bubbles = new List<Transform>();
    public Light sunLight;
    public float cloudSpeed = 0.5f;
    public float bubbleFloatSpeed = 0.3f;
    public float bubbleFloatAmount = 0.1f;

    [Header("Anomalies")]
    public float minAnomalyInterval = 30f;
    public float maxAnomalyInterval = 60f;
    public AudioClip glitchCrack;
    private AudioSource audioSource;

    private void Start()
    {
        foreach (var layer in layers)
        {
            if (layer.transform) layer.startPos = layer.transform.localPosition;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        StartCoroutine(AnomalyRoutine());
    }

    private void Update()
    {
        HandleParallax();
        HandleCloudMovement();
        HandleBubbleFloating();
        HandleSunPulse();
    }

    private void HandleParallax()
    {
        if (UnityEngine.InputSystem.Mouse.current == null) return;

        Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        float screenW = Screen.width;
        float screenH = Screen.height;
        
        Vector2 normalizedMouse = new Vector2(
            (mousePos.x / screenW) - 0.5f,
            (mousePos.y / screenH) - 0.5f
        );

        foreach (var layer in layers)
        {
            if (!layer.transform) continue;

            Vector3 targetPos = layer.startPos + new Vector3(
                normalizedMouse.x * layer.mouseInfluence,
                normalizedMouse.y * layer.mouseInfluence,
                0
            );

            layer.transform.localPosition = Vector3.Lerp(layer.transform.localPosition, targetPos, Time.deltaTime / smoothTime);
        }
    }

    private void HandleCloudMovement()
    {
        foreach (var cloud in clouds)
        {
            if (!cloud) continue;
            cloud.Translate(Vector3.right * cloudSpeed * Time.deltaTime, Space.World);
            
            // Loop clouds
            if (cloud.position.x > 15f) {
                cloud.position = new Vector3(-15f, cloud.position.y, cloud.position.z);
            }
        }
    }

    private void HandleBubbleFloating()
    {
        foreach (var bubble in bubbles)
        {
            if (!bubble) continue;
            float yOffset = Mathf.Sin(Time.time * bubbleFloatSpeed + bubble.position.x) * bubbleFloatAmount;
            bubble.Translate(Vector3.up * yOffset * Time.deltaTime, Space.Self);
        }
    }

    private void HandleSunPulse()
    {
        if (sunLight)
        {
            sunLight.intensity = 1.0f + Mathf.PingPong(Time.time * 0.5f, 0.2f);
        }
    }

    private IEnumerator AnomalyRoutine()
    {
        while (true)
        {
            float wait = Random.Range(minAnomalyInterval, maxAnomalyInterval);
            yield return new WaitForSeconds(wait);
            
            TriggerRandomAnomaly();
        }
    }

    private void TriggerRandomAnomaly()
    {
        int type = Random.Range(0, 4);
        switch (type)
        {
            case 0: StartCoroutine(BubbleDisappearAnomaly()); break;
            case 1: StartCoroutine(CityGlitchAnomaly()); break;
            case 2: StartCoroutine(TreeAnomaly()); break;
            case 3: StartCoroutine(ReflectionAnomaly()); break;
        }
    }

    private IEnumerator BubbleDisappearAnomaly()
    {
        if (bubbles.Count == 0) yield break;
        var b = bubbles[Random.Range(0, bubbles.Count)];
        if (!b) yield break;
        
        b.gameObject.SetActive(false);
        if (glitchCrack) audioSource.PlayOneShot(glitchCrack);
        yield return new WaitForSeconds(0.8f);
        b.gameObject.SetActive(true);
    }

    private IEnumerator CityGlitchAnomaly()
    {
        // Find city layer
        Transform city = layers.Find(l => l.transform && l.transform.name.Contains("City"))?.transform;
        if (!city) yield break;

        Vector3 originalScale = city.localScale;
        city.localScale = new Vector3(originalScale.x * 1.1f, originalScale.y * 0.9f, originalScale.z);
        if (glitchCrack) audioSource.PlayOneShot(glitchCrack);
        yield return new WaitForSeconds(0.3f);
        city.localScale = originalScale;
    }

    private IEnumerator TreeAnomaly()
    {
        Transform nature = layers.Find(l => l.transform && l.transform.name.Contains("Nature"))?.transform;
        if (!nature) yield break;

        Quaternion originalRot = nature.localRotation;
        nature.localRotation = Quaternion.Euler(0, 0, 5f);
        yield return new WaitForSeconds(1.0f);
        nature.localRotation = originalRot;
    }

    private IEnumerator ReflectionAnomaly()
    {
        // Just a subtle flicker or color shift
        if (glitchCrack) audioSource.PlayOneShot(glitchCrack);
        yield return null;
    }
}
