using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text;
using System.Linq;

public class StoryIntroController : MonoBehaviour
{
    private const string SystemEndingFlagKey = "SystemEnding.Active";

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
    private bool _isSystemEnding;

    private string[] initialLines = {
    "AETHER DYNAMICS",
    "INTERNAL INCIDENT REPORT",
    "Year: 2026",
    "Project: AeroOS",
    "Lead Engineers Missing: 4",
    "Final Active Employee:",
    "YOU"
    };

    private string[] systemEndingInitialLines = {
    "AETHER DYNAMICS",
    "INTERNAL INCIDENT REPORT",
    "Year: 2026",
    "Project: AeroOS",
    "Lead Engineers Missing: 5",
    "Final Active Employee:",
    "???"
    };


    private string[] confirmedLines = {
    "USER CONFIRMED",
    "{TIME}\nLAB 7\nNIGHT SHIFT",
    "Please investigate the test environment."
    };

    private IEnumerator Start()
    {
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;

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

        _isSystemEnding = PlayerPrefs.GetInt(SystemEndingFlagKey, 0) == 1;

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
        string[] activeInitialLines = _isSystemEnding ? systemEndingInitialLines : initialLines;
        
        for (int i = 0; i < activeInitialLines.Length; i++)
        {
            string line = activeInitialLines[i];
            bool isYOU = (i == activeInitialLines.Length - 1);
            
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

        if (_isSystemEnding)
        {
            yield return new WaitForSeconds(0.8f);
            yield return StartCoroutine(TriggerSystemEndingMutation(fullText));
            yield return StartCoroutine(ShowSystemEndingResult());
            yield break;
        }

        fullText.Append("\n\n");
        foreach (var originalLine in confirmedLines)
        {
            string line = originalLine;
            if (line.Contains("{TIME}"))
            {
                line = line.Replace("{TIME}", System.DateTime.Now.ToString("HH:mm"));
            }
            
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

    private IEnumerator ShowSystemEndingResult()
    {
        if (ambientSource != null)
        {
            ambientSource.Stop();
        }

        if (sfxSource != null && staticCrackClip != null)
        {
            sfxSource.PlayOneShot(staticCrackClip, 0.85f);
        }

        if (root != null)
        {
            root.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 1f));
        }

        if (glitchLabelR != null) glitchLabelR.style.display = DisplayStyle.None;
        if (glitchLabelB != null) glitchLabelB.style.display = DisplayStyle.None;

        if (storyLabel != null)
        {
            storyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            storyLabel.style.fontSize = 34;
            storyLabel.style.whiteSpace = WhiteSpace.Normal;
            storyLabel.style.position = Position.Absolute;
            storyLabel.style.left = 180;
            storyLabel.style.right = 180;
            storyLabel.style.top = 0;
            storyLabel.style.bottom = 0;
            storyLabel.style.color = new StyleColor(new Color(0.94f, 0.96f, 1f, 1f));
            storyLabel.text = string.Empty;
        }

        string endingText = "Ты теперь стал частью системы.\n\nТы узнал правду о системе.\nНо какой ценой?";
        StringBuilder endingBuilder = new StringBuilder();
        for (int i = 0; i < endingText.Length; i++)
        {
            endingBuilder.Append(endingText[i]);
            if (storyLabel != null)
            {
                storyLabel.text = endingBuilder.ToString();
            }

            if (!char.IsWhiteSpace(endingText[i]) && sfxSource != null && glitchClip != null && i % 3 == 0)
            {
                sfxSource.PlayOneShot(glitchClip, 0.25f);
            }

            yield return new WaitForSeconds(0.06f);
        }

        yield return new WaitForSeconds(2.8f);
        PlayerPrefs.DeleteKey(SystemEndingFlagKey);
        PlayerPrefs.Save();
        SceneManager.LoadScene("MainMenuScene");
    }

    private IEnumerator TriggerSystemEndingMutation(StringBuilder fullText)
    {
        if (storyLabel == null)
        {
            yield break;
        }

        string baseText = fullText.ToString();
        int youIndex = baseText.LastIndexOf("YOU");
        if (youIndex < 0)
        {
            yield break;
        }

        if (sfxSource != null && staticCrackClip != null)
        {
            sfxSource.PlayOneShot(staticCrackClip, 0.95f);
        }

        if (glitchLabelR != null)
        {
            glitchLabelR.style.display = DisplayStyle.Flex;
        }

        if (glitchLabelB != null)
        {
            glitchLabelB.style.display = DisplayStyle.Flex;
        }

        string[] mutationFrames =
        {
            baseText,
            baseText.Substring(0, youIndex) + "Y0U",
            baseText.Substring(0, youIndex) + "FOU",
            baseText.Substring(0, youIndex) + "FIVE",
            baseText.Substring(0, youIndex) + "FIVE",
            baseText
        };

        for (int i = 0; i < mutationFrames.Length; i++)
        {
            string frame = mutationFrames[i];
            storyLabel.text = frame;

            if (glitchLabelR != null)
            {
                glitchLabelR.text = frame;
                glitchLabelR.style.translate = new Translate(Random.Range(-18f, 18f), Random.Range(-8f, 8f), 0);
            }

            if (glitchLabelB != null)
            {
                glitchLabelB.text = frame;
                glitchLabelB.style.translate = new Translate(Random.Range(-18f, 18f), Random.Range(-8f, 8f), 0);
            }

            storyLabel.style.opacity = Random.Range(0.65f, 1f);

            if (sfxSource != null && glitchClip != null && i > 0 && i < mutationFrames.Length - 1)
            {
                sfxSource.PlayOneShot(glitchClip, 0.35f);
            }

            yield return new WaitForSeconds(i == 3 ? 0.18f : 0.08f);
        }

        storyLabel.style.opacity = 1f;
        storyLabel.text = baseText;

        if (glitchLabelR != null) glitchLabelR.style.display = DisplayStyle.None;
        if (glitchLabelB != null) glitchLabelB.style.display = DisplayStyle.None;

        yield return new WaitForSeconds(0.45f);
    }
}
