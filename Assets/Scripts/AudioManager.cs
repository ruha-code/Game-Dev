using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;

    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AudioManager");
                _instance = go.AddComponent<AudioManager>();
                DontDestroyOnLoad(go);
            }

            return _instance;
        }
    }

    public static bool HasInstance => _instance != null;

    [Header("Mixer")]
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup uiGroup;

    private AudioSource _musicSource;
    private AudioSource _ambientSource;
    private AudioSource _uiSource;
    private AudioSource _sfxSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        _ = Instance;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSources();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void PlayUISFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            return;
        }

        EnsureSources();
        _uiSource.PlayOneShot(clip, volume);
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            return;
        }

        EnsureSources();
        _sfxSource.PlayOneShot(clip, volume);
    }

    public void PlayAmbient(AudioClip clip, bool loop = true, float volume = 1f)
    {
        EnsureSources();

        if (clip == null)
        {
            _ambientSource.Stop();
            _ambientSource.clip = null;
            return;
        }

        if (_ambientSource.clip == clip && _ambientSource.isPlaying)
        {
            _ambientSource.volume = volume;
            _ambientSource.loop = loop;
            return;
        }

        _ambientSource.clip = clip;
        _ambientSource.loop = loop;
        _ambientSource.volume = volume;
        _ambientSource.Play();
    }

    public void StopAmbient()
    {
        if (_ambientSource == null)
        {
            return;
        }

        _ambientSource.Stop();
        _ambientSource.clip = null;
    }

    public void SetAmbientVolume(float volume)
    {
        EnsureSources();
        _ambientSource.volume = volume;
    }

    public void PlayMusic(AudioClip clip, bool loop = true, float volume = 1f)
    {
        EnsureSources();

        if (clip == null)
        {
            _musicSource.Stop();
            _musicSource.clip = null;
            return;
        }

        if (_musicSource.clip == clip && _musicSource.isPlaying)
        {
            _musicSource.volume = volume;
            _musicSource.loop = loop;
            return;
        }

        _musicSource.clip = clip;
        _musicSource.loop = loop;
        _musicSource.volume = volume;
        _musicSource.Play();
    }

    public bool IsAmbientPlaying(AudioClip clip)
    {
        if (_ambientSource == null || clip == null)
        {
            return false;
        }

        return _ambientSource.isPlaying && _ambientSource.clip == clip;
    }

    private void EnsureSources()
    {
        if (_musicSource == null)
        {
            _musicSource = CreateChildSource("MusicSource", musicGroup, loop: true);
        }

        if (_ambientSource == null)
        {
            _ambientSource = CreateChildSource("AmbientSource", musicGroup, loop: true);
        }

        if (_uiSource == null)
        {
            _uiSource = CreateChildSource("UISource", uiGroup, loop: false);
        }

        if (_sfxSource == null)
        {
            _sfxSource = CreateChildSource("SFXSource", sfxGroup, loop: false);
        }
    }

    private AudioSource CreateChildSource(string childName, AudioMixerGroup mixerGroup, bool loop)
    {
        Transform child = transform.Find(childName);
        GameObject sourceObject;

        if (child == null)
        {
            sourceObject = new GameObject(childName);
            sourceObject.transform.SetParent(transform, false);
        }
        else
        {
            sourceObject = child.gameObject;
        }

        AudioSource source = sourceObject.GetComponent<AudioSource>();
        if (source == null)
        {
            source = sourceObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = mixerGroup;
        return source;
    }
}
