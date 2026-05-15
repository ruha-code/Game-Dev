using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text;
using System.Linq;

public class StoryIntroController : MonoBehaviour
{
    private VisualElement root;
    private Label storyLabel;
    private Label glitchLabelR;
    private Label glitchLabelB;
    
    [Header("Sequence Settings")]
    public float initialDelay = 2.5f;
    public float characterDelay = 0.11f; 
    public float lineDelay = 1.8f;
    public string nextScene = "BootScene";
    
    [Header("Audio Clips")]
    public AudioClip droneClip;
    public AudioClip typewriterClip;
    public AudioClip glitchClip;
    public AudioClip staticCrackClip;
    public AudioClip pcHumClip;
    public AudioClip ventilationClip;

    [Header("Sync Settings")]
    public float typewriterSilenceOffset = 0.142f;
    
    private AudioSource ambientSource;
    private AudioSource sfxSource;

    private string[] initialLines = {
        "AETHER DYNAMICS",
        "INTERNAL INCIDENT REPORT",
        "Year: 2026",
        "Project: AeroOS",
        "Lead Engineers Missing: 7",
        "Final Active Employee:",
        "YOU"
    };

    private string[] confirmedLines = {
        "USER CONFIRMED",
        "22:47 PM\nLAB 7\nNIGHT SHIFT",
        "Please investigate the test environment."
    };

    private IEnumerator Start()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument not found on " + gameObject.name);
            yield break;
        }

        float timeout = 2f;
        while (uiDocument.rootVisualElement == null && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        root = uiDocument.rootVisualElement;
        yield return new WaitForEndOfFrame();

        storyLabel = root.Q<Label>("story-text");
        glitchLabelR = root.Q<Label>("glitch-text-r");
        glitchLabelB = root.Q<Label>("glitch-text-b");

        if (storyLabel == null)
        {
            storyLabel = root.Query<Label>().First();
        }

        SetupAudio();
        StartCoroutine(RunSequence());
    }

    private void SetupAudio()
    {
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.loop = true;
        ambientSource.clip = droneClip;
        ambientSource.volume = 0.25f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.volume = 0.8f;
    }

    private IEnumerator RunSequence()
    {
        yield return new WaitForSeconds(initialDelay);

        if (ambientSource != null && ambientSource.clip != null)
            ambientSource.Play();

        StringBuilder fullText = new StringBuilder();
        
        for (int i = 0; i < initialLines.Length; i++)
        {
            string line = initialLines[i];
            bool isYOU = (i == initialLines.Length - 1);
            
            yield return StartCoroutine(TypewriteLine(line, fullText));

            if (isYOU)
            {
                yield return StartCoroutine(TriggerGlitchOnYOU());
            }
            else
            {
                fullText.Append("\n");
                if (storyLabel != null) storyLabel.text = fullText.ToString();
                yield return new WaitForSeconds(lineDelay);
            }
        }

        fullText.Append("\n\n");
        foreach (var line in confirmedLines)
        {
            yield return StartCoroutine(TypewriteLine(line, fullText));
            fullText.Append("\n");
            if (storyLabel != null) storyLabel.text = fullText.ToString();
            
            if (line.Contains("investigate"))
            {
                StartCoroutine(RevealBootScene());
            }
            yield return new WaitForSeconds(lineDelay);
        }
    }

    private IEnumerator TypewriteLine(string line, StringBuilder fullText)
    {
        for (int i = 0; i < line.Length; i++)
        {
            if (!char.IsWhiteSpace(line[i]) && sfxSource != null && typewriterClip != null)
            {
                sfxSource.Stop();
                sfxSource.clip = typewriterClip;
                sfxSource.time = typewriterSilenceOffset;
                sfxSource.pitch = Random.Range(0.9f, 1.1f);
                sfxSource.Play();
            }

            fullText.Append(line[i]);
            if (storyLabel != null) storyLabel.text = fullText.ToString();
            
            yield return new WaitForSeconds(characterDelay);
        }
    }

    private IEnumerator TriggerGlitchOnYOU()
    {
        if (sfxSource != null && staticCrackClip != null)
        {
            sfxSource.PlayOneShot(staticCrackClip);
        }

        float elapsed = 0f;
        float duration = 0.5f;

        if (glitchLabelR != null) { glitchLabelR.style.display = DisplayStyle.Flex; glitchLabelR.text = storyLabel.text; }
        if (glitchLabelB != null) { glitchLabelB.style.display = DisplayStyle.Flex; glitchLabelB.text = storyLabel.text; }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float intensity = 15f;
            Vector2 offsetR = new Vector2(Random.Range(-intensity, intensity), Random.Range(-intensity, intensity));
            Vector2 offsetB = new Vector2(Random.Range(-intensity, intensity), Random.Range(-intensity, intensity));
            
            if (glitchLabelR != null) glitchLabelR.style.translate = new Translate(offsetR.x, offsetR.y, 0);
            if (glitchLabelB != null) glitchLabelB.style.translate = new Translate(offsetB.x, offsetB.y, 0);
            
            if (storyLabel != null)
            {
                storyLabel.style.opacity = Random.Range(0.5f, 1f);
            }

            yield return null;
        }

        if (storyLabel != null)
        {
            storyLabel.style.opacity = 1f;
        }
        if (glitchLabelR != null) glitchLabelR.style.display = DisplayStyle.None;
        if (glitchLabelB != null) glitchLabelB.style.display = DisplayStyle.None;
    }

    private IEnumerator RevealBootScene()
    {
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Additive);
        while (!loadOp.isDone) yield return null;

        Scene bootScene = SceneManager.GetSceneByName(nextScene);
        SceneManager.SetActiveScene(bootScene);

        Light[] allLights = FindObjectsByType<Light>(FindObjectsInactive.Include);
        foreach (var l in allLights)
        {
            if (l.gameObject.scene == bootScene) l.intensity = 0f;
        }

        GameObject audioObj = new GameObject("IntroEnvironmentAudio");
        AudioSource humSource = audioObj.AddComponent<AudioSource>();
        humSource.clip = pcHumClip;
        humSource.loop = true;
        humSource.volume = 0f;
        humSource.Play();

        AudioSource ventSource = audioObj.AddComponent<AudioSource>();
        ventSource.clip = ventilationClip;
        ventSource.loop = true;
        ventSource.volume = 0f;
        ventSource.Play();

        float revealDuration = 3f;
        float elapsed = 0f;
        while (elapsed < revealDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / revealDuration;
            RenderSettings.ambientIntensity = Mathf.Lerp(0f, 0.2f, t);
            yield return null;
        }

        Light monitorLight = null;
        foreach (var l in allLights)
        {
            if (l.gameObject.scene == bootScene && l.name.ToLower().Contains("monitor"))
            {
                monitorLight = l;
                break;
            }
        }

        if (monitorLight != null)
        {
            elapsed = 0f;
            float monitorDuration = 2f;
            while (elapsed < monitorDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / monitorDuration;
                monitorLight.intensity = Mathf.Lerp(0f, 2f, t);
                monitorLight.color = new Color(0.5f, 0.7f, 1f);
                
                humSource.volume = Mathf.Lerp(0f, 0.4f, t);
                ventSource.volume = Mathf.Lerp(0f, 0.3f, t);
                yield return null;
            }
        }

        yield return StartCoroutine(DissolveText());

        Camera introCam = Camera.main;
        GameObject doorway = GameObject.Find("Entrance Top Door Frame");
        if (doorway != null)
        {
            Vector3 doorPos = doorway.transform.position;
            Vector3 targetPos = doorPos + Vector3.forward * 0.5f;
            targetPos.y = 1.6f;

            elapsed = 0f;
            float camDuration = 2f;
            Vector3 startPos = introCam.transform.position;
            Quaternion startRot = introCam.transform.rotation;
            
            Vector3 monitorPos = Vector3.zero;
            GameObject monitor = GameObject.Find("Monitor Facing Player");
            if (monitor != null) monitorPos = monitor.transform.position;
            else monitorPos = new Vector3(0, 1.3f, 2.2f);

            Quaternion targetRot = Quaternion.LookRotation(monitorPos - targetPos);

            while (elapsed < camDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / camDuration;
                float smoothT = t * t * (3f - 2f * t);
                introCam.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
                introCam.transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
                yield return null;
            }
        }

        yield return new WaitForSeconds(2f);

        GameObject ghost = GameObject.Find("Glitching Hallucination Presence");
        if (ghost != null) ghost.SetActive(true);

        CinematicIntroController cinematic = FindAnyObjectByType<CinematicIntroController>();
        if (cinematic != null) cinematic.enabled = true;
        
        SceneManager.UnloadSceneAsync("StoryIntroScene");
    }

    private IEnumerator DissolveText()
    {
        float elapsed = 0f;
        float duration = 1.5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            root.style.opacity = 1f - (elapsed / duration);
            yield return null;
        }
        root.style.display = DisplayStyle.None;
    }
}
