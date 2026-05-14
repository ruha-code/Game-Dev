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
    public float initialDelay = 2.5f; // Longer start
    public float characterDelay = 0.11f; 
    public float lineDelay = 1.8f; // More time to read
    public string nextScene = "BootScene";
    
    [Header("Audio Clips")]
    public AudioClip droneClip;
    public AudioClip typewriterClip;
    public AudioClip glitchClip;

    [Header("Sync Settings")]
    public float typewriterSilenceOffset = 0.142f; // Offset to skip leading silence in clip
    
    private AudioSource ambientSource;
    private AudioSource sfxSource;

    private string[] lines = {
        "AETHER DYNAMICS",
        "INTERNAL INCIDENT REPORT",
        "Year: 2026",
        "Project: AeroOS",
        "Lead Engineers Missing: 7",
        "Final Active Employee:",
        "YOU"
    };

    private IEnumerator Start()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument not found on " + gameObject.name);
            yield break;
        }

        // Wait for UI to be ready
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
        
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            bool isLastLine = (i == lines.Length - 1);
            
            yield return StartCoroutine(TypewriteLine(line, fullText, isLastLine));

            if (!isLastLine)
            {
                fullText.Append("\n");
                if (storyLabel != null) storyLabel.text = fullText.ToString();
                yield return new WaitForSeconds(lineDelay);
            }
        }
    }

    private IEnumerator TypewriteLine(string line, StringBuilder fullText, bool isGlitchLine)
    {
        for (int i = 0; i < line.Length; i++)
        {
            if (!char.IsWhiteSpace(line[i]) && sfxSource != null && typewriterClip != null)
            {
                sfxSource.Stop();
                sfxSource.clip = typewriterClip;
                sfxSource.time = typewriterSilenceOffset; // Skip leading silence
                sfxSource.pitch = Random.Range(0.9f, 1.1f);
                sfxSource.Play();
            }

            fullText.Append(line[i]);
            if (storyLabel != null) storyLabel.text = fullText.ToString();
            
            yield return new WaitForSeconds(characterDelay);
        }

        if (isGlitchLine)
        {
            yield return StartCoroutine(TriggerGlitch());
        }
    }

    private IEnumerator TriggerGlitch()
    {
        if (sfxSource != null && glitchClip != null)
        {
            sfxSource.Stop();
            sfxSource.clip = glitchClip;
            sfxSource.time = 0;
            sfxSource.pitch = 1f;
            sfxSource.Play();
        }

        float elapsed = 0f;
        float duration = 0.5f;

        if (glitchLabelR != null)
        {
            glitchLabelR.style.display = DisplayStyle.Flex;
            glitchLabelR.text = storyLabel.text;
        }
        if (glitchLabelB != null)
        {
            glitchLabelB.style.display = DisplayStyle.Flex;
            glitchLabelB.text = storyLabel.text;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float intensity = 12f;
            Vector2 offsetR = new Vector2(Random.Range(-intensity, intensity), Random.Range(-intensity, intensity));
            Vector2 offsetB = new Vector2(Random.Range(-intensity, intensity), Random.Range(-intensity, intensity));
            
            if (glitchLabelR != null) glitchLabelR.style.translate = new Translate(offsetR.x, offsetR.y, 0);
            if (glitchLabelB != null) glitchLabelB.style.translate = new Translate(offsetB.x, offsetB.y, 0);
            
            if (Random.value > 0.7f && storyLabel != null)
                storyLabel.style.opacity = Random.Range(0.3f, 1f);

            yield return null;
        }

        if (storyLabel != null) storyLabel.style.opacity = 1f;
        if (glitchLabelR != null) glitchLabelR.style.display = DisplayStyle.None;
        if (glitchLabelB != null) glitchLabelB.style.display = DisplayStyle.None;
        
        yield return new WaitForSeconds(2.5f);
        SceneManager.LoadScene(nextScene);
    }
}
