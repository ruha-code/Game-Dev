using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.EventSystems;

public class ParkSceneController : MonoBehaviour
{
    private const string DesktopSceneName = "AeroDesktopScene";
    private const string MainMenuSceneName = "MainMenuScene";
    private const string StoryIntroSceneName = "StoryIntroScene";
    private const string SystemEndingFlagKey = "SystemEnding.Active";
    private const string PlayerRootName = "PlayerCapsule";
    private const string CameraName = "MainCamera";

    private readonly string[] _stoneNames =
    {
        "PT_Menhir_Rock_02 (1)",
        "PT_Menhir_Rock_02 (2)",
        "PT_Menhir_Rock_02 (3)",
        "PT_Menhir_Rock_02"
    };

    private readonly string[] _letterTitles =
    {
        "Fragment 01 // Arrival Log",
        "Fragment 02 // Groundskeeper Memo",
        "Fragment 03 // Missing Staff Notice",
        "Fragment 04 // Memory Bleed Report"
    };

    private readonly string[] _letterBodies =
{
    "Я больше не уверен, что это я.\n\nСистема забрала слишком много.\nИ с каждым днём остаётся всё меньше.\n\nЕсли ты это читаешь — значит меня уже почти нет.",

    "Имя… я помню только ощущение имени.\n\nМысли становятся пустыми, как будто их кто-то вынимает.\n\nЯ пытался удержаться, но меня уже переписывают.",

    "Я вижу мир, но он не мой.\n\nСистема подменяет то, что я думаю и что я помню.\n\nЯ перестал отличать свои мысли от чужих.",

    "Теперь я понимаю.\n\nНас не удаляют.\nНас постепенно стирают, пока не остаётся ничего.\n\nИ когда не остаётся ничего — система считает это завершением."
};

    private readonly List<Transform> _stones = new List<Transform>();
    private readonly List<GameObject> _watchers = new List<GameObject>();
    private readonly List<Vector3> _watcherBasePositions = new List<Vector3>();
    private readonly List<GameObject> _anomalyOrbs = new List<GameObject>();
    private readonly List<Vector3> _anomalyOrbBasePositions = new List<Vector3>();
    private readonly List<GameObject> _riftSpikes = new List<GameObject>();
    private readonly List<Vector3> _riftSpikeBasePositions = new List<Vector3>();
    private readonly List<Renderer> _bloodStainRenderers = new List<Renderer>();
    private readonly List<GameObject> _parkErasedObjects = new List<GameObject>();
    private readonly HashSet<int> _usedStoneIndices = new HashSet<int>();
    private readonly HashSet<int> _triggeredMilestoneScares = new HashSet<int>();

    [SerializeField] private float interactDistance = 5f;
    [SerializeField] private Color activeGlowColor = new Color(0.4f, 0.85f, 1f, 1f);
    [SerializeField] private Color watcherGlowColor = new Color(0.22f, 0.6f, 0.9f, 1f);
    [SerializeField] private Light directionalLight;
    [SerializeField] private Volume globalVolume;
    [Header("Horror Prefabs")]
    [SerializeField] private GameObject watcherPrefab;
    [SerializeField] private GameObject anomalyOrbPrefab;
    [SerializeField] private GameObject riftSpikePrefab;
    [Header("Horror Audio")]
    [SerializeField] private AudioClip whisperLoopClip;
    [SerializeField] private AudioClip watcherStingClip;
    [SerializeField] private AudioClip fogPulseClip;
    [SerializeField] private AudioClip stoneHumClip;
    [SerializeField] private AudioClip letterOpenClip;
    [SerializeField] private AudioClip fragmentCompleteClip;
    [Header("New Park SFX")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip landingClip;
    [SerializeField] private AudioClip breathingLoopClip;
    [SerializeField] private AudioClip pressureLoopClip;
    [SerializeField] private AudioClip expulsionHitClip;
    [SerializeField] private AudioClip[] scareStingVariants;
    [SerializeField] private AudioClip[] presenceFoleyClips;
    
    [SerializeField, Range(0f, 1f)] private float whisperMaxVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] private float scareVolume = 0.9f;
    [Header("Pressure")]
    [SerializeField] private float pressureStartSeconds = 26f;
    [SerializeField] private float pressureDecayPerFragment = 3f;
    [SerializeField] private float pressureFreezeSeconds = 0.75f;
    [SerializeField] private int pressureFailuresBeforeExpulsion = 3;

    private Transform _player;
    private Camera _mainCamera;
    private FirstPersonController _playerController;
    private StarterAssetsInputs _playerInputs;
    private CharacterController _characterController;

    private GameObject _activeMarkerRoot;
    private Light _activeStoneLight;
    private Text _promptLabel;
    private GameObject _letterPanel;
    private Text _letterTitleLabel;
    private Text _letterBodyLabel;
    private Text _letterHintLabel;
    private Text _statusLabel;
    private Image _letterPanelBackground;
    private Image _letterBloodSmearTop;
    private Image _letterBloodSmearBottom;
    private Image _flashOverlay;
    private GameObject _endingOverlay;
    private Image _endingOverlayBackground;
    private Image _endingAuraGlow;
    private Image _endingQuestionPlate;
    private Text _endingHeaderLabel;
    private Text _endingQuestionLabel;
    private Text _endingConsequenceLabel;
    private Text _endingHintLabel;
    private Button _endingYesButton;
    private Button _endingNoButton;
    private readonly List<Image> _endingGlitchBars = new List<Image>();
    private AudioClip _endingAcceptClip;
    private AudioClip _endingRejectClip;
    private AudioClip _endingGlitchClip;

    private int _currentStoneIndex;
    private int _fragmentsRecovered;
    private bool _letterOpen;
    private bool _endingSequenceStarted;
    private bool _endingChoiceMade;
    private bool _endingInputEnabled;
    private float _nextFogFlashTime;
    private float _pressureTimer;
    private bool _pressureActive;
    private float _nextGlitchTime;
    private int _pressureFailures;
    private bool _expellingPlayer;

    private Material _runtimeSkybox;
    private VolumeProfile _runtimeVolumeProfile;
    private ColorAdjustments _colorAdjustments;
    private Vignette _vignette;
    private Bloom _bloom;
    private ChromaticAberration _chromaticAberration;
    private AudioSource _whisperSource;
    private AudioSource _scareSource;
    private AudioSource _stoneHumSource;
    private AudioSource _footstepSource;
    private AudioSource _breathingSource;
    private AudioSource _pressureSource;
    private AudioSource _presenceSource;

    private float _footstepDistanceCounter;
    private bool _wasGroundedLastFrame;
    private float _movementTimer;
    private float _nextPresenceTime;
    private bool _spawnedFallbackCamera;

    private GameObject _jumpWatcher;
    private Light _jumpWatcherLight;
    private Vector3 _jumpWatcherBaseScale;
    private Coroutine _jumpScareRoutine;
    private Coroutine _pressureRoutine;

    private void Awake()
    {
        ProgressionManager.Instance.MarkLocationVisited(LocationId.TreeScene);
        FindSceneReferences();
        PrepareRuntimeEnvironment();
        SetupAudio();
        CacheStones();
        BuildUi();
        CreateActiveStoneMarker();
        CreateWatchers();
        CreateAnomalyOrbs();
        CreateRiftSpikes();
        CreateBloodStains();
        ApplyAtmosphereStage(0);
        UpdateStonePresentation();
        SetLetterPanelVisible(false);
        SetPlayerLocked(false);

        _pressureActive = false;
        _pressureTimer = 0f;
        _pressureFailures = 0;
        _expellingPlayer = false;
        _nextGlitchTime = Time.time + Random.Range(5f, 10f);
    }

    private void Start()
    {
        SetPlayerLocked(false);
        StartCoroutine(EnsureGameplayCursorState());
    }

    private void Update()
    {
        if (_endingSequenceStarted)
        {
            HandleEndingChoiceInput();
            return;
        }

        if (_player == null || _mainCamera == null || _stones.Count == 0)
        {
            return;
        }

        UpdateAnomalyOrbs();
        UpdateWatchers();
        UpdateWhisperZones();
        UpdatePlayerAudio();
        TryFogPulse();
        TrySystemGlitch();
        UpdatePressure();
        HandlePresenceFoley();

        if (_fragmentsRecovered >= _stones.Count && WasReturnPressed())
        {
            SceneManager.LoadScene(DesktopSceneName);
        }

        if (_letterOpen)
        {
            if (WasInteractPressed() || WasCancelPressed())
            {
                CloseLetter();
            }

            return;
        }

        int nearbyStoneIndex = FindNearbyStoneIndex();
        bool isNear = nearbyStoneIndex >= 0;

        if (_promptLabel != null)
        {
            _promptLabel.enabled = isNear;
            if (isNear)
            {
                _promptLabel.text = $"Press E to inspect fragment {_fragmentsRecovered + 1}/{_stones.Count}";
            }
        }

        if (_statusLabel != null)
        {
            if (_fragmentsRecovered >= _stones.Count)
            {
                _statusLabel.text = "All fragments recovered. The park is waiting for your answer.";
            }
            else if (_expellingPlayer)
            {
                _statusLabel.text = "AEROOS WARNING // PARK CONTAINMENT FAILED // SESSION REJECTED";
            }
            else if (_pressureActive)
            {
                _statusLabel.text = $"Reach a new stone before the park reacts. Time left: {Mathf.CeilToInt(_pressureTimer)}s // Failures: {_pressureFailures}/{pressureFailuresBeforeExpulsion}";
            }
            else
            {
                _statusLabel.text = $"Follow the lit marker. Memory fragments recovered: {_fragmentsRecovered}/{_stones.Count}";
            }
        }

        if (isNear && WasInteractPressed() && _fragmentsRecovered < _stones.Count)
        {
            OpenLetter(nearbyStoneIndex);
        }
    }

    private void TrySystemGlitch()
    {
        if (Time.time < _nextGlitchTime || _fragmentsRecovered < 1) return;

        _nextGlitchTime = Time.time + Random.Range(8f, 15f) - (_fragmentsRecovered * 1.5f);
        StartCoroutine(GlitchRoutine());
    }

    private System.Collections.IEnumerator GlitchRoutine()
    {
        float intensity = 0.1f + (_fragmentsRecovered * 0.15f);
        if (_chromaticAberration != null) _chromaticAberration.intensity.Override(intensity);
        
        // Randomly shift status label text
        string originalStatus = _statusLabel.text;
        _statusLabel.text = "SYSTEM_ERROR: MEMORY_LEAK_DETECTED";
        _statusLabel.color = Color.red;

        yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));

        if (_chromaticAberration != null) _chromaticAberration.intensity.Override(0f);
        _statusLabel.text = originalStatus;
        _statusLabel.color = new Color(0.84f, 0.94f, 1f, 0.88f);
    }

    private void UpdatePressure()
    {
        if (_expellingPlayer || !_pressureActive || _letterOpen || _fragmentsRecovered >= _stones.Count)
        {
            if (_pressureSource != null)
                _pressureSource.volume = Mathf.MoveTowards(_pressureSource.volume, 0f, Time.deltaTime * 0.5f);
            return;
        }

        _pressureTimer -= Time.deltaTime;
        
        // Update pressure sound volume based on time remaining
        if (_pressureSource != null)
        {
            float maxTime = Mathf.Max(8f, pressureStartSeconds - (_fragmentsRecovered * pressureDecayPerFragment));
            float normalized = 1f - Mathf.Clamp01(_pressureTimer / maxTime);
            _pressureSource.volume = Mathf.Lerp(0.05f, 0.45f, normalized);
            _pressureSource.pitch = Mathf.Lerp(0.9f, 1.15f, normalized);
        }

        if (_pressureTimer <= 0f)
        {
            TriggerPressureEvent();
        }
    }

    private void UpdatePlayerAudio()
    {
        if (_player == null || _characterController == null || _letterOpen) return;

        // Footsteps logic
        bool isGrounded = _characterController.isGrounded;
        float speed = new Vector3(_characterController.velocity.x, 0, _characterController.velocity.z).magnitude;

        if (isGrounded && !_wasGroundedLastFrame)
        {
            // Just landed
            if (landingClip != null && _footstepSource != null)
                _footstepSource.PlayOneShot(landingClip, 0.7f);
        }

        if (isGrounded && speed > 0.5f)
        {
            float strideLength = _playerInputs != null && _playerInputs.sprint ? 1.8f : 2.4f;
            _footstepDistanceCounter += speed * Time.deltaTime;

            if (_footstepDistanceCounter >= strideLength)
            {
                _footstepDistanceCounter = 0f;
                if (footstepClips != null && footstepClips.Length > 0 && _footstepSource != null)
                {
                    AudioClip clip = footstepClips[UnityEngine.Random.Range(0, footstepClips.Length)];
                    _footstepSource.PlayOneShot(clip, UnityEngine.Random.Range(0.35f, 0.5f));
                }
            }
        }

        _wasGroundedLastFrame = isGrounded;

        // Breathing logic
        if (_breathingSource != null)
        {
            // Only breathe heavily when sprinting and moving
            bool isSprinting = _playerInputs != null && _playerInputs.sprint && speed > 0.5f;
            
            float targetVolume = 0f;
            if (isSprinting)
            {
                // Intensifies after 2nd and 4th fragment
                targetVolume = 0.08f;
                if (_fragmentsRecovered >= 2) targetVolume = 0.22f;
                if (_fragmentsRecovered >= 3) targetVolume = 0.45f;
            }
            
            // Slightly faster fade for responsiveness
            _breathingSource.volume = Mathf.MoveTowards(_breathingSource.volume, targetVolume, Time.deltaTime * 0.5f);
        }
    }

    private void HandlePresenceFoley()
    {
        if (_player == null || _letterOpen || _fragmentsRecovered < 1 || Time.time < _nextPresenceTime) return;

        _nextPresenceTime = Time.time + UnityEngine.Random.Range(15f, 35f) - (_fragmentsRecovered * 2f);

        if (presenceFoleyClips != null && presenceFoleyClips.Length > 0 && _presenceSource != null)
        {
            AudioClip clip = presenceFoleyClips[UnityEngine.Random.Range(0, presenceFoleyClips.Length)];
            
            // Random position around player
            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 6f;
            randomOffset.y = 0;
            if (randomOffset.magnitude < 3f) randomOffset = randomOffset.normalized * 3f;
            
            _presenceSource.transform.position = _player.position + randomOffset;
            _presenceSource.PlayOneShot(clip, UnityEngine.Random.Range(0.2f, 0.45f));
        }
    }

    private void BeginPressureWindow()
    {
        if (_fragmentsRecovered >= _stones.Count)
        {
            _pressureActive = false;
            _pressureTimer = 0f;
            return;
        }

        _pressureActive = true;
        _pressureTimer = Mathf.Max(8f, pressureStartSeconds - (_fragmentsRecovered * pressureDecayPerFragment));
    }

    private void TriggerPressureEvent()
    {
        _pressureActive = false;
        _pressureTimer = 0f;
        _pressureFailures++;
        if (_pressureRoutine == null)
        {
            _pressureRoutine = StartCoroutine(PressureOverrunRoutine());
        }
    }

    private IEnumerator PressureOverrunRoutine()
    {
        if (_scareSource != null)
        {
            AudioClip sting = GetRandomScareSting();
            if (sting != null) _scareSource.PlayOneShot(sting, scareVolume);
        }

        if (_jumpScareRoutine == null)
        {
            _jumpScareRoutine = StartCoroutine(TriggerWatcherScareRoutine());
        }

        SetPlayerLocked(true);
        yield return new WaitForSeconds(pressureFreezeSeconds);
        if (!_letterOpen)
        {
            SetPlayerLocked(false);
        }

        StartCoroutine(FogPulseRoutine());

        if (_pressureFailures >= pressureFailuresBeforeExpulsion)
        {
            yield return StartCoroutine(ExpelPlayerRoutine());
        }
        else
        {
            BeginPressureWindow();
        }

        _pressureRoutine = null;
    }

    private IEnumerator ExpelPlayerRoutine()
    {
        _expellingPlayer = true;
        _pressureActive = false;
        _pressureTimer = 0f;
        SetPlayerLocked(true);

        if (_scareSource != null && expulsionHitClip != null)
        {
            _scareSource.PlayOneShot(expulsionHitClip, 1f);
        }

        if (_statusLabel != null)
        {
            _statusLabel.text = "AEROOS WARNING // PARK CONTAINMENT FAILED // SESSION REJECTED";
            _statusLabel.color = new Color(1f, 0.25f, 0.25f, 1f);
        }

        float elapsed = 0f;
        float duration = 1.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(t * Mathf.PI);

            if (_flashOverlay != null)
            {
                _flashOverlay.color = new Color(0.65f, 0.05f, 0.06f, pulse * 0.42f);
            }

            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.Override(Mathf.Lerp(0.15f, 1f, pulse));
            }

            yield return null;
        }

        SceneManager.LoadScene(DesktopSceneName);
    }

    private void FindSceneReferences()
    {
        GameObject playerObject = GameObject.Find(PlayerRootName);
        if (playerObject != null)
        {
            _player = playerObject.transform;
            _playerController = playerObject.GetComponent<FirstPersonController>();
            _playerInputs = playerObject.GetComponent<StarterAssetsInputs>();
            _characterController = playerObject.GetComponent<CharacterController>();

#if ENABLE_INPUT_SYSTEM
            PlayerInput playerInput = playerObject.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                if (playerInput.camera == null)
                {
                    playerInput.camera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
                }

                if (playerInput.uiInputModule == null)
                {
                    playerInput.uiInputModule = FindAnyObjectByType<InputSystemUIInputModule>();
                }
            }
#endif
        }

        GameObject cameraObject = GameObject.Find(CameraName);
        if (cameraObject != null)
        {
            _mainCamera = cameraObject.GetComponent<Camera>();
        }
        else
        {
            _mainCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        }

        if (_mainCamera == null)
        {
            _mainCamera = CreateFallbackEndingCamera();
        }

        if (directionalLight == null)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include);
            foreach (Light lightSource in lights)
            {
                if (lightSource == null || lightSource.type != LightType.Directional)
                {
                    continue;
                }

                directionalLight = lightSource;
                break;
            }
        }

        if (globalVolume == null)
        {
            Volume[] volumes = FindObjectsByType<Volume>(FindObjectsInactive.Include);
            foreach (Volume volume in volumes)
            {
                if (volume == null || volume.sharedProfile == null)
                {
                    continue;
                }

                globalVolume = volume;
                break;
            }
        }
    }

    private Camera CreateFallbackEndingCamera()
    {
        GameObject cameraObject = new GameObject("MainCamera");
        cameraObject.tag = "MainCamera";
        Camera cameraComponent = cameraObject.AddComponent<Camera>();
        cameraComponent.clearFlags = CameraClearFlags.Skybox;
        cameraComponent.fieldOfView = 60f;
        cameraObject.AddComponent<AudioListener>();
        _spawnedFallbackCamera = true;
        return cameraComponent;
    }

    private void CacheStones()
    {
        _stones.Clear();

        foreach (string stoneName in _stoneNames)
        {
            GameObject stone = GameObject.Find(stoneName);
            if (stone != null)
            {
                _stones.Add(stone.transform);
            }
        }
    }

    private void PrepareRuntimeEnvironment()
    {
        if (RenderSettings.skybox != null)
        {
            _runtimeSkybox = Instantiate(RenderSettings.skybox);
            RenderSettings.skybox = _runtimeSkybox;
        }

        if (globalVolume != null && globalVolume.sharedProfile != null)
        {
            _runtimeVolumeProfile = Instantiate(globalVolume.sharedProfile);
            globalVolume.sharedProfile = _runtimeVolumeProfile;
            _runtimeVolumeProfile.TryGet(out _colorAdjustments);
            _runtimeVolumeProfile.TryGet(out _vignette);
            _runtimeVolumeProfile.TryGet(out _bloom);
            _runtimeVolumeProfile.TryGet(out _chromaticAberration);

            if (_chromaticAberration == null)
            {
                _chromaticAberration = _runtimeVolumeProfile.Add<ChromaticAberration>(true);
                _chromaticAberration.intensity.Override(0f);
            }
        }
    }

    private void SetupAudio()
    {
        _whisperSource = gameObject.AddComponent<AudioSource>();
        _whisperSource.playOnAwake = false;
        _whisperSource.loop = true;
        _whisperSource.spatialBlend = 0f;
        _whisperSource.volume = 0f;
        _whisperSource.clip = whisperLoopClip;
        if (whisperLoopClip != null)
        {
            _whisperSource.Play();
        }

        _stoneHumSource = gameObject.AddComponent<AudioSource>();
        _stoneHumSource.playOnAwake = false;
        _stoneHumSource.loop = true;
        _stoneHumSource.spatialBlend = 0f;
        _stoneHumSource.volume = 0f;
        _stoneHumSource.clip = stoneHumClip;
        if (stoneHumClip != null)
        {
            _stoneHumSource.Play();
        }

        _scareSource = gameObject.AddComponent<AudioSource>();
        _scareSource.playOnAwake = false;
        _scareSource.loop = false;
        _scareSource.spatialBlend = 0f;
        _scareSource.volume = scareVolume;

        _footstepSource = gameObject.AddComponent<AudioSource>();
        _footstepSource.playOnAwake = false;
        _footstepSource.spatialBlend = 0f;

        _breathingSource = gameObject.AddComponent<AudioSource>();
        _breathingSource.playOnAwake = false;
        _breathingSource.loop = true;
        _breathingSource.spatialBlend = 0f;
        _breathingSource.clip = breathingLoopClip;
        _breathingSource.volume = 0f;
        if (breathingLoopClip != null) _breathingSource.Play();

        _pressureSource = gameObject.AddComponent<AudioSource>();
        _pressureSource.playOnAwake = false;
        _pressureSource.loop = true;
        _pressureSource.spatialBlend = 0f;
        _pressureSource.clip = pressureLoopClip;
        _pressureSource.volume = 0f;
        if (pressureLoopClip != null) _pressureSource.Play();

        _presenceSource = gameObject.AddComponent<AudioSource>();
        _presenceSource.playOnAwake = false;
        _presenceSource.spatialBlend = 1f; // Spatial for presence
        _presenceSource.minDistance = 2f;
        _presenceSource.maxDistance = 15f;
        _presenceSource.rolloffMode = AudioRolloffMode.Logarithmic;

        _endingAcceptClip ??= fragmentCompleteClip != null ? fragmentCompleteClip : watcherStingClip;
        _endingRejectClip ??= fogPulseClip != null ? fogPulseClip : letterOpenClip;
        _endingGlitchClip ??= watcherStingClip != null ? watcherStingClip : fogPulseClip;

        _nextPresenceTime = Time.time + UnityEngine.Random.Range(10f, 25f);
        _nextFogFlashTime = Time.time + Random.Range(12f, 18f);
        }

    private void BuildUi()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("ParkStoryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        _promptLabel = CreateText("PromptLabel", canvasObject.transform, font, 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(920f, 60f));
        _promptLabel.color = new Color(0.75f, 0.93f, 1f, 0.95f);
        _promptLabel.text = string.Empty;
        _promptLabel.enabled = false;

        _statusLabel = CreateText("StatusLabel", canvasObject.transform, font, 24, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(1100f, 56f));
        _statusLabel.color = new Color(0.84f, 0.94f, 1f, 0.88f);

        _letterPanel = new GameObject("LetterPanel", typeof(RectTransform), typeof(Image));
        _letterPanel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = _letterPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(980f, 620f);
        _letterPanelBackground = _letterPanel.GetComponent<Image>();
        _letterPanelBackground.color = new Color(0.06f, 0.04f, 0.04f, 0.965f);

        CreatePanelBorder(font, panelRect);

        _letterTitleLabel = CreateText("LetterTitle", _letterPanel.transform, font, 34, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(44f, -34f), new Vector2(-88f, 88f));
        _letterTitleLabel.alignment = TextAnchor.UpperLeft;
        _letterTitleLabel.color = new Color(0.94f, 0.88f, 0.85f, 1f);

        _letterBodyLabel = CreateText("LetterBody", _letterPanel.transform, font, 26, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(52f, 112f), new Vector2(-96f, -150f));
        _letterBodyLabel.alignment = TextAnchor.UpperLeft;
        _letterBodyLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        _letterBodyLabel.verticalOverflow = VerticalWrapMode.Overflow;
        _letterBodyLabel.lineSpacing = 1.18f;
        _letterBodyLabel.color = new Color(0.92f, 0.89f, 0.86f, 0.98f);

        _letterHintLabel = CreateText("LetterHint", _letterPanel.transform, font, 22, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(42f, 24f), new Vector2(-84f, 60f));
        _letterHintLabel.alignment = TextAnchor.LowerLeft;
        _letterHintLabel.color = new Color(0.84f, 0.77f, 0.77f, 0.92f);

        _letterBloodSmearTop = CreateDecorativePanel("LetterBloodTop", _letterPanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120f, -36f), new Vector2(220f, 56f), new Color(0.42f, 0.05f, 0.04f, 0.78f), 12f);
        _letterBloodSmearBottom = CreateDecorativePanel("LetterBloodBottom", _letterPanel.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-140f, 44f), new Vector2(260f, 72f), new Color(0.32f, 0.03f, 0.03f, 0.64f), -10f);
        _letterBloodSmearTop.rectTransform.SetSiblingIndex(1);
        _letterBloodSmearBottom.rectTransform.SetSiblingIndex(2);

        _flashOverlay = CreateDecorativePanel("FlashOverlay", canvasObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2400f, 1400f), new Color(0.75f, 0.88f, 1f, 0f), 0f);
        _flashOverlay.raycastTarget = false;

        EnsureEventSystem();
        BuildEndingOverlay(canvasObject.transform, font);
    }

    private static Text CreateText(string name, Transform parent, Font font, int fontSize, TextAnchor anchor, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        if (anchorMin == anchorMax)
        {
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
        else
        {
            rect.offsetMin = anchoredPosition;
            rect.offsetMax = sizeDelta;
        }

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.supportRichText = true;
        text.resizeTextForBestFit = false;
        return text;
    }

    private static Image CreateDecorativePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color, float rotationZ)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private GameObject CreateHorrorVisual(GameObject prefab, PrimitiveType fallbackPrimitive, string objectName, Vector3 position, Vector3 scale, Quaternion rotation, Color baseColor, Color emissionColor)
    {
        GameObject instance = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(fallbackPrimitive);
        instance.name = objectName;
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        instance.transform.localScale = scale;

        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        if (prefab == null)
        {
            ApplyHorrorRendererStyle(instance, baseColor, emissionColor);
        }
        else
        {
            ConfigurePrefabVisuals(instance);
        }

        return instance;
    }

    private void ApplyHorrorRendererStyle(GameObject target, Color baseColor, Color emissionColor)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.color = baseColor;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor);
            renderer.material = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    private void ConfigurePrefabVisuals(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    private void CreatePanelBorder(Font font, RectTransform panelRect)
    {
        GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(panelRect, false);
        RectTransform borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0f, 0f);
        borderRect.anchorMax = new Vector2(1f, 1f);
        borderRect.offsetMin = new Vector2(14f, 14f);
        borderRect.offsetMax = new Vector2(-14f, -14f);
        Image borderImage = border.GetComponent<Image>();
        borderImage.color = new Color(0.2f, 0.45f, 0.56f, 0.5f);

        GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(border.transform, false);
        RectTransform innerRect = inner.GetComponent<RectTransform>();
        innerRect.anchorMin = new Vector2(0f, 0f);
        innerRect.anchorMax = new Vector2(1f, 1f);
        innerRect.offsetMin = new Vector2(4f, 4f);
        innerRect.offsetMax = new Vector2(-4f, -4f);
        Image innerImage = inner.GetComponent<Image>();
        innerImage.color = new Color(0.02f, 0.03f, 0.04f, 0.88f);

        Text footer = CreateText("Footer", inner.transform, font, 18, TextAnchor.LowerRight, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 18f), new Vector2(260f, 36f));
        footer.alignment = TextAnchor.LowerRight;
        footer.color = new Color(0.45f, 0.72f, 0.82f, 0.85f);
        footer.text = "PARK ARCHIVE / TREE ANOMALY";
    }

    private void BuildEndingOverlay(Transform parent, Font font)
    {
        _endingOverlay = new GameObject("EndingOverlay", typeof(RectTransform), typeof(Image));
        _endingOverlay.transform.SetParent(parent, false);
        RectTransform overlayRect = _endingOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        _endingOverlayBackground = _endingOverlay.GetComponent<Image>();
        _endingOverlayBackground.color = new Color(0f, 0f, 0f, 0f);

        _endingAuraGlow = CreateDecorativePanel("EndingAuraGlow", _endingOverlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1320f, 760f), new Color(0.2f, 0.7f, 0.85f, 0f), 0f);
        _endingAuraGlow.raycastTarget = false;

        _endingQuestionPlate = CreateDecorativePanel("EndingQuestionPlate", _endingOverlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1180f, 260f), new Color(0.1f, 0.16f, 0.2f, 0f), 0f);
        _endingQuestionPlate.raycastTarget = false;

        _endingHeaderLabel = CreateText("EndingHeader", _endingOverlay.transform, font, 20, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(52f, -42f), new Vector2(280f, 32f));
        _endingHeaderLabel.alignment = TextAnchor.MiddleLeft;
        _endingHeaderLabel.color = new Color(0.55f, 0.68f, 0.82f, 0f);
        _endingHeaderLabel.text = "02  THE CHOICE";

        _endingQuestionLabel = CreateText("EndingQuestion", _endingOverlay.transform, font, 46, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(1180f, 220f));
        _endingQuestionLabel.color = new Color(0.9f, 0.97f, 1f, 0f);
        _endingQuestionLabel.alignment = TextAnchor.MiddleCenter;
        _endingQuestionLabel.lineSpacing = 1.08f;

        _endingConsequenceLabel = CreateText("EndingConsequence", _endingOverlay.transform, font, 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -136f), new Vector2(1060f, 210f));
        _endingConsequenceLabel.color = new Color(0.84f, 0.92f, 0.96f, 0f);
        _endingConsequenceLabel.alignment = TextAnchor.MiddleCenter;
        _endingConsequenceLabel.lineSpacing = 1.08f;

        _endingHintLabel = CreateText("EndingHint", _endingOverlay.transform, font, 18, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(900f, 40f));
        _endingHintLabel.color = new Color(0.75f, 0.82f, 0.86f, 0f);
        _endingHintLabel.alignment = TextAnchor.MiddleCenter;
        _endingHintLabel.text = string.Empty;

        CreateEndingGlitchBars();

        _endingYesButton = CreateEndingButton("EndingYesButton", _endingOverlay.transform, font, "YES", new Vector2(-150f, -220f));
        _endingNoButton = CreateEndingButton("EndingNoButton", _endingOverlay.transform, font, "NO", new Vector2(150f, -220f));

        _endingYesButton.onClick.AddListener(() => ResolveEndingChoice(true));
        _endingNoButton.onClick.AddListener(() => ResolveEndingChoice(false));

        _endingOverlay.SetActive(false);
    }

    private Button CreateEndingButton(string name, Transform parent, Font font, string label, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(220f, 70f);

        Image background = buttonObject.GetComponent<Image>();
        bool isYes = label == "YES";
        background.color = new Color(0.03f, 0.05f, 0.08f, 0.88f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = background.color;
        colors.highlightedColor = isYes ? new Color(0.15f, 0.3f, 0.42f, 0.95f) : new Color(0.08f, 0.12f, 0.18f, 0.95f);
        colors.pressedColor = isYes ? new Color(0.09f, 0.2f, 0.3f, 1f) : new Color(0.06f, 0.08f, 0.12f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.6f);
        button.colors = colors;

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = isYes ? new Color(0.58f, 0.82f, 1f, 0.85f) : new Color(0.36f, 0.5f, 0.66f, 0.55f);
        outline.effectDistance = new Vector2(2f, -2f);

        Shadow glow = buttonObject.AddComponent<Shadow>();
        glow.effectColor = isYes ? new Color(0.46f, 0.78f, 1f, 0.45f) : new Color(0.3f, 0.4f, 0.55f, 0.28f);
        glow.effectDistance = new Vector2(0f, 0f);

        Text buttonLabel = CreateText($"{name}Label", buttonObject.transform, font, 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, rect.sizeDelta);
        buttonLabel.text = label;
        buttonLabel.color = new Color(0.95f, 0.98f, 1f, 1f);

        return button;
    }

    private void CreateEndingGlitchBars()
    {
        _endingGlitchBars.Clear();

        Vector2[] positions =
        {
            new Vector2(0f, 26f),
            new Vector2(-90f, 0f),
            new Vector2(84f, -18f),
            new Vector2(0f, -42f)
        };

        Vector2[] sizes =
        {
            new Vector2(620f, 2f),
            new Vector2(420f, 3f),
            new Vector2(520f, 2f),
            new Vector2(360f, 2f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            Image bar = CreateDecorativePanel($"EndingGlitchBar_{i}", _endingOverlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), positions[i], sizes[i], new Color(0.72f, 0.88f, 1f, 0f), 0f);
            bar.raycastTarget = false;
            _endingGlitchBars.Add(bar);
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private void CreateActiveStoneMarker()
    {
        _activeMarkerRoot = new GameObject("ActiveStoneMarker");
        _activeStoneLight = _activeMarkerRoot.AddComponent<Light>();
        _activeStoneLight.type = LightType.Point;
        _activeStoneLight.range = 8f;
        _activeStoneLight.intensity = 2.1f;
        _activeStoneLight.color = activeGlowColor;
    }

    private void CreateWatchers()
    {
        if (_stones.Count == 0)
        {
            return;
        }

        Vector3 center = Vector3.zero;
        foreach (Transform stone in _stones)
        {
            center += stone.position;
        }

        center /= _stones.Count;

        Vector3[] offsets =
        {
            new Vector3(-18f, 0f, 24f),
            new Vector3(22f, 0f, -18f),
            new Vector3(-26f, 0f, -22f),
            new Vector3(26f, 0f, 20f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 watcherPosition = center + offsets[i] + Vector3.up * 1.6f;
            GameObject watcher = CreateHorrorVisual(
                watcherPrefab,
                PrimitiveType.Capsule,
                $"ParkWatcher_{i + 1}",
                watcherPosition,
                new Vector3(1.2f, 2.7f, 1.2f),
                Quaternion.identity,
                new Color(0.03f, 0.05f, 0.06f, 1f),
                watcherGlowColor * 0.25f);
            _watcherBasePositions.Add(watcherPosition);

            Light watcherLight = watcher.AddComponent<Light>();
            watcherLight.type = LightType.Point;
            watcherLight.range = 4f;
            watcherLight.intensity = 0.55f;
            watcherLight.color = watcherGlowColor;

            watcher.SetActive(false);
            _watchers.Add(watcher);
        }

        _jumpWatcher = CreateHorrorVisual(
            watcherPrefab,
            PrimitiveType.Capsule,
            "ParkJumpWatcher",
            center + Vector3.up * 1.5f,
            new Vector3(1.3f, 2.95f, 1.3f),
            Quaternion.identity,
            new Color(0.01f, 0.01f, 0.015f, 1f),
            new Color(0.72f, 0.08f, 0.11f, 1f) * 0.8f);

        _jumpWatcherLight = _jumpWatcher.AddComponent<Light>();
        _jumpWatcherLight.type = LightType.Point;
        _jumpWatcherLight.range = 6f;
        _jumpWatcherLight.intensity = 0.9f;
        _jumpWatcherLight.color = new Color(0.75f, 0.1f, 0.12f, 1f);
        _jumpWatcherBaseScale = _jumpWatcher.transform.localScale;
        _jumpWatcher.SetActive(false);
    }

    private void CreateAnomalyOrbs()
    {
        if (_stones.Count == 0)
        {
            return;
        }

        Vector3 center = Vector3.zero;
        foreach (Transform stone in _stones)
        {
            center += stone.position;
        }

        center /= _stones.Count;

        Vector3[] offsets =
        {
            new Vector3(0f, 3.4f, 0f),
            new Vector3(-8f, 2.6f, 7f),
            new Vector3(8f, 2.8f, -5f),
            new Vector3(-13f, 3.1f, -10f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Color orbColor = Color.Lerp(new Color(0.17f, 0.85f, 1f, 1f), new Color(0.85f, 0.94f, 1f, 1f), i / 3f);
            GameObject orb = CreateHorrorVisual(
                anomalyOrbPrefab,
                PrimitiveType.Sphere,
                $"ParkAnomalyOrb_{i + 1}",
                center + offsets[i],
                Vector3.one * (0.55f + (i * 0.08f)),
                Quaternion.identity,
                new Color(0.02f, 0.05f, 0.06f, 1f),
                orbColor * 1.5f);
            _anomalyOrbBasePositions.Add(orb.transform.position);

            Light orbLight = orb.AddComponent<Light>();
            orbLight.type = LightType.Point;
            orbLight.range = 5f;
            orbLight.intensity = 0.75f;
            orbLight.color = Color.Lerp(activeGlowColor, watcherGlowColor, i / 3f);

            orb.SetActive(false);
            _anomalyOrbs.Add(orb);
        }
    }

    private void CreateRiftSpikes()
    {
        if (_stones.Count == 0)
        {
            return;
        }

        Vector3 center = Vector3.zero;
        foreach (Transform stone in _stones)
        {
            center += stone.position;
        }

        center /= _stones.Count;

        Vector3[] offsets =
        {
            new Vector3(-6f, 0f, 12f),
            new Vector3(5f, 0f, -11f),
            new Vector3(-14f, 0f, -4f),
            new Vector3(13f, 0f, 6f),
            new Vector3(0f, 0f, 17f),
            new Vector3(10f, 0f, 15f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject spike = CreateHorrorVisual(
                riftSpikePrefab,
                PrimitiveType.Cube,
                $"ParkRiftSpike_{i + 1}",
                center + offsets[i] + Vector3.up * (1.7f + i * 0.1f),
                new Vector3(0.65f, 3.6f + i * 0.3f, 0.65f),
                Quaternion.Euler(-8f, i * 33f, 7f),
                new Color(0.03f, 0.01f, 0.01f, 1f),
                new Color(0.55f, 0.06f, 0.08f, 1f) * 0.7f);
            _riftSpikeBasePositions.Add(spike.transform.position);

            Light spikeLight = spike.AddComponent<Light>();
            spikeLight.type = LightType.Point;
            spikeLight.range = 6f;
            spikeLight.intensity = 0.35f;
            spikeLight.color = new Color(0.7f, 0.08f, 0.1f, 1f);

            spike.SetActive(false);
            _riftSpikes.Add(spike);
        }
    }

    private void CreateBloodStains()
    {
        foreach (Transform stone in _stones)
        {
            if (stone == null)
            {
                continue;
            }

            GameObject stain = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stain.name = $"BloodStain_{stone.name}";
            stain.transform.position = stone.position + new Vector3(0.8f, 0.02f, -0.55f);
            stain.transform.localScale = new Vector3(1.9f, 0.02f, 1.4f);
            stain.transform.rotation = Quaternion.Euler(0f, 16f, 0f);

            Collider collider = stain.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            Renderer renderer = stain.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                Material material = new Material(shader);
                material.color = new Color(0.09f, 0.01f, 0.01f, 0.88f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.18f, 0.01f, 0.01f, 1f) * 0.2f);
                renderer.material = material;
                _bloodStainRenderers.Add(renderer);
            }
        }
    }

    private void UpdateStonePresentation()
    {
        if (_stones.Count == 0 || _activeMarkerRoot == null)
        {
            return;
        }

        Transform activeStone = null;
        if (_fragmentsRecovered < _stones.Count)
        {
            activeStone = _stones[Mathf.Clamp(_currentStoneIndex, 0, _stones.Count - 1)];
            _activeMarkerRoot.transform.position = activeStone.position + Vector3.up * 2.6f;
        }
        _activeMarkerRoot.SetActive(activeStone != null);

        for (int i = 0; i < _stones.Count; i++)
        {
            Renderer[] renderers = _stones[i].GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.material == null || !renderer.material.HasProperty("_EmissionColor"))
                {
                    continue;
                }

                Color emission = Color.black;
                if (_usedStoneIndices.Contains(i))
                {
                    emission = new Color(0.25f, 0.03f, 0.03f, 1f) * 0.4f;
                }
                else if (activeStone != null && i == _currentStoneIndex)
                {
                    emission = activeGlowColor * 0.6f;
                }
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", emission);
            }
        }
    }

    private void UpdateWatchers()
    {
        if (_player == null)
        {
            return;
        }

        float pulse = 0.4f + Mathf.PingPong(Time.time * 0.4f, 0.45f);

        for (int i = 0; i < _watchers.Count; i++)
        {
            GameObject watcher = _watchers[i];
            if (watcher == null)
            {
                continue;
            }

            Vector3 lookTarget = new Vector3(_player.position.x, watcher.transform.position.y, _player.position.z);
            watcher.transform.LookAt(lookTarget);

            Vector3 basePosition = _watcherBasePositions[i];
            bool watchedByPlayer = IsWatcherObserved(watcher.transform.position + Vector3.up * 1.4f);
            Vector3 groundToPlayer = _player.position - new Vector3(basePosition.x, _player.position.y, basePosition.z);
            if (groundToPlayer.sqrMagnitude > 0.001f)
            {
                Vector3 stalkDirection = groundToPlayer.normalized;
                float stalkStep = watchedByPlayer ? -0.35f : 0.55f;
                _watcherBasePositions[i] += stalkDirection * stalkStep * Time.deltaTime * Mathf.Clamp01(_fragmentsRecovered * 0.55f);
                basePosition = _watcherBasePositions[i];
            }

            basePosition.y = 3f + Mathf.Sin(Time.time * 0.7f + i) * 0.55f;
            watcher.transform.position = basePosition;
            watcher.transform.localScale = Vector3.Lerp(watcher.transform.localScale, watchedByPlayer ? new Vector3(1.16f, 2.62f, 1.16f) : new Vector3(1.24f, 2.78f, 1.24f), Time.deltaTime * 2.4f);

            Light watcherLight = watcher.GetComponent<Light>();
            if (watcherLight != null)
            {
                watcherLight.intensity = pulse + (_fragmentsRecovered * 0.08f) + (watchedByPlayer ? 0f : 0.18f);
            }

            watcher.SetActive(i < Mathf.Min(_fragmentsRecovered, _watchers.Count));
        }

        if (_jumpWatcher != null && _jumpWatcher.activeSelf)
        {
            Vector3 lookTarget = new Vector3(_player.position.x, _jumpWatcher.transform.position.y, _player.position.z);
            _jumpWatcher.transform.LookAt(lookTarget);
            if (_jumpWatcherLight != null)
            {
                _jumpWatcherLight.intensity = 0.65f + Mathf.PingPong(Time.time * 2.8f, 0.5f);
            }
        }
    }

    private bool IsWatcherObserved(Vector3 worldPosition)
    {
        if (_mainCamera == null)
        {
            return false;
        }

        Vector3 viewportPoint = _mainCamera.WorldToViewportPoint(worldPosition);
        if (viewportPoint.z <= 0f)
        {
            return false;
        }

        bool insideView = viewportPoint.x is > 0.08f and < 0.92f && viewportPoint.y is > 0.08f and < 0.92f;
        if (!insideView)
        {
            return false;
        }

        Vector3 toWatcher = (worldPosition - _mainCamera.transform.position).normalized;
        return Vector3.Dot(_mainCamera.transform.forward, toWatcher) > 0.72f;
    }

    private void UpdateAnomalyOrbs()
    {
        if (_anomalyOrbs.Count == 0)
        {
            return;
        }

        int visibleOrbCount = Mathf.Clamp(_fragmentsRecovered, 0, _anomalyOrbs.Count);
        for (int i = 0; i < _anomalyOrbs.Count; i++)
        {
            GameObject orb = _anomalyOrbs[i];
            if (orb == null)
            {
                continue;
            }

            bool isVisible = i < visibleOrbCount;
            orb.SetActive(isVisible);
            if (!isVisible)
            {
                continue;
            }

            Vector3 position = _anomalyOrbBasePositions[i];
            position.y += Mathf.Sin(Time.time * (0.9f + (i * 0.15f)) + i) * 0.45f;
            orb.transform.position = position;
            orb.transform.Rotate(0f, 20f * Time.deltaTime * (i + 1), 0f, Space.World);

            Light orbLight = orb.GetComponent<Light>();
            if (orbLight != null)
            {
                orbLight.intensity = 0.65f + Mathf.PingPong(Time.time * 0.6f + i, 0.55f);
            }
        }

        UpdateRiftSpikes();
        UpdateBloodStains();
    }

    private void UpdateRiftSpikes()
    {
        int visibleSpikeCount = Mathf.Clamp(_fragmentsRecovered + 1, 0, _riftSpikes.Count);
        for (int i = 0; i < _riftSpikes.Count; i++)
        {
            GameObject spike = _riftSpikes[i];
            if (spike == null)
            {
                continue;
            }

            bool isVisible = i < visibleSpikeCount && _fragmentsRecovered >= 1;
            spike.SetActive(isVisible);
            if (!isVisible)
            {
                continue;
            }

            Vector3 position = _riftSpikeBasePositions[i];
            position.y += Mathf.Sin(Time.time * (0.8f + i * 0.12f) + i) * 0.3f;
            spike.transform.position = position;
            spike.transform.Rotate(3f * Time.deltaTime, 24f * Time.deltaTime, -2f * Time.deltaTime, Space.World);

            Light spikeLight = spike.GetComponent<Light>();
            if (spikeLight != null)
            {
                spikeLight.intensity = 0.25f + _fragmentsRecovered * 0.12f + Mathf.PingPong(Time.time * 0.5f + i, 0.2f);
            }
        }
    }

    private void UpdateBloodStains()
    {
        float progress = _stones.Count == 0 ? 0f : Mathf.Clamp01(_fragmentsRecovered / (float)_stones.Count);
        float alpha = Mathf.Lerp(0.28f, 0.92f, progress);
        float glow = Mathf.Lerp(0.04f, 0.35f, progress);
        foreach (Renderer renderer in _bloodStainRenderers)
        {
            if (renderer == null || renderer.material == null)
            {
                continue;
            }

            Color color = renderer.material.color;
            color.a = alpha;
            renderer.material.color = color;
            if (renderer.material.HasProperty("_EmissionColor"))
            {
                renderer.material.SetColor("_EmissionColor", new Color(0.28f, 0.01f, 0.01f, 1f) * glow);
            }
        }
    }

    private void UpdateWhisperZones()
    {
        if (_whisperSource == null)
        {
            return;
        }

        bool hasWhispers = _player != null && _fragmentsRecovered > 0 && _stones.Count > 0 && whisperLoopClip != null;
        bool hasHum = _player != null && _stones.Count > 0 && stoneHumClip != null;

        if (!hasWhispers && !hasHum)
        {
            _whisperSource.volume = Mathf.MoveTowards(_whisperSource.volume, 0f, Time.deltaTime * 0.8f);
            if (_stoneHumSource != null)
            {
                _stoneHumSource.volume = Mathf.MoveTowards(_stoneHumSource.volume, 0f, Time.deltaTime * 0.8f);
            }
            return;
        }

        float nearestDistance = float.MaxValue;
        foreach (Transform stone in _stones)
        {
            if (stone == null)
            {
                continue;
            }

            float distance = Vector3.Distance(_player.position, stone.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
            }
        }

        float whisperRange = 20f;
        float normalized = 1f - Mathf.Clamp01(nearestDistance / whisperRange);

        if (hasWhispers)
        {
            float stageBoost = Mathf.Lerp(0.35f, 1f, _fragmentsRecovered / (float)_stones.Count);
            float targetVolume = normalized * whisperMaxVolume * stageBoost;
            _whisperSource.volume = Mathf.MoveTowards(_whisperSource.volume, targetVolume, Time.deltaTime * 0.7f);
        }

        if (hasHum && _stoneHumSource != null)
        {
            float humTarget = normalized * 0.35f;
            _stoneHumSource.volume = Mathf.MoveTowards(_stoneHumSource.volume, humTarget, Time.deltaTime * 0.5f);
        }
    }

    private void TryFogPulse()
    {
        if (_letterOpen || _fragmentsRecovered < 2 || Time.time < _nextFogFlashTime)
        {
            return;
        }

        _nextFogFlashTime = Time.time + Random.Range(10f, 18f) - (_fragmentsRecovered * 0.6f);
        StartCoroutine(FogPulseRoutine());
    }

    private System.Collections.IEnumerator FogPulseRoutine()
    {
        if (_scareSource != null && fogPulseClip != null)
        {
            _scareSource.PlayOneShot(fogPulseClip, scareVolume * 0.75f);
        }

        float duration = 0.42f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float pulse = Mathf.Sin(t * Mathf.PI);

            if (_flashOverlay != null)
            {
                _flashOverlay.color = new Color(0.82f, 0.9f, 1f, pulse * 0.18f);
            }

            if (_colorAdjustments != null)
            {
                _colorAdjustments.postExposure.Override(Mathf.Lerp(-0.2f, 0.45f, pulse));
            }

            if (_vignette != null)
            {
                _vignette.intensity.Override(Mathf.Lerp(0.24f, 0.48f, pulse));
            }

            yield return null;
        }

        if (_flashOverlay != null)
        {
            _flashOverlay.color = new Color(0.82f, 0.9f, 1f, 0f);
        }

        ApplyAtmosphereStage(_fragmentsRecovered);
    }

    private System.Collections.IEnumerator TriggerWatcherScareRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0.28f, 0.52f));
        if (_player == null || _jumpWatcher == null || _letterOpen)
        {
            _jumpScareRoutine = null;
            yield break;
        }

        Vector3 startOffset = new Vector3(Random.Range(-3.1f, 3.1f), 1.45f, Random.Range(5.8f, 7.1f));
        Vector3 endOffset = new Vector3(startOffset.x * 0.45f, 1.6f, Mathf.Max(2.7f, startOffset.z - 2.8f));
        yield return RunWatcherScareBeat(startOffset, endOffset, 0.8f, new Color(0.55f, 0.07f, 0.08f, 1f), 0.22f, 1.08f, true);
        _jumpScareRoutine = null;
    }

    private System.Collections.IEnumerator TriggerMilestoneWatcherSequence(int fragmentCount)
    {
        _triggeredMilestoneScares.Add(fragmentCount);

        yield return new WaitForSeconds(fragmentCount == 2 ? 0.38f : 0.24f);
        if (_player == null || _jumpWatcher == null || _letterOpen || _expellingPlayer)
        {
            _jumpScareRoutine = null;
            yield break;
        }

        if (fragmentCount == 2)
        {
            yield return RunWatcherScareBeat(new Vector3(-4.8f, 1.55f, 4.1f), new Vector3(-1.6f, 1.6f, 2.4f), 0.26f, new Color(0.48f, 0.09f, 0.1f, 1f), 0.16f, 0.96f, true);
            yield return new WaitForSeconds(0.08f);
            yield return RunWatcherScareBeat(new Vector3(4.6f, 1.52f, 3.6f), new Vector3(0.95f, 1.65f, 1.85f), 0.23f, new Color(0.6f, 0.07f, 0.08f, 1f), 0.18f, 1.02f, false);
        }
        else
        {
            yield return RunWatcherScareBeat(new Vector3(0.2f, 1.7f, 7f), new Vector3(0.05f, 1.75f, 2.1f), 0.22f, new Color(0.74f, 0.08f, 0.08f, 1f), 0.24f, 1.1f, true);
            yield return new WaitForSeconds(0.06f);
            yield return RunWatcherScareBeat(new Vector3(-3.7f, 1.45f, -1.6f), new Vector3(-0.9f, 1.55f, 1.1f), 0.2f, new Color(0.5f, 0.05f, 0.07f, 1f), 0.15f, 0.92f, false);
            yield return new WaitForSeconds(0.05f);
            yield return RunWatcherScareBeat(new Vector3(4.2f, 1.62f, 2.4f), new Vector3(0.4f, 1.72f, 0.9f), 0.18f, new Color(0.82f, 0.05f, 0.06f, 1f), 0.26f, 1.18f, true);
        }

        if (_flashOverlay != null)
        {
            _flashOverlay.color = new Color(0f, 0f, 0f, 0f);
        }

        if (_jumpWatcher != null)
        {
            _jumpWatcher.SetActive(false);
            _jumpWatcher.transform.localScale = _jumpWatcherBaseScale;
        }

        _jumpScareRoutine = null;
    }

    private AudioClip GetRandomScareSting()
    {
        if (scareStingVariants != null && scareStingVariants.Length > 0)
        {
            // Pick from variants or original
            int total = scareStingVariants.Length + 1;
            int r = UnityEngine.Random.Range(0, total);
            if (r < scareStingVariants.Length) return scareStingVariants[r];
        }
        return watcherStingClip;
    }

    private IEnumerator RunWatcherScareBeat(Vector3 startOffset, Vector3 endOffset, float duration, Color flashColor, float flashStrength, float scaleMultiplier, bool playSting)
    {
        if (_player == null || _jumpWatcher == null)
        {
            yield break;
        }

        Vector3 forward = _player.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }
        forward.Normalize();

        Vector3 right = _player.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.right;
        }
        right.Normalize();

        Vector3 startWorld = _player.position + right * startOffset.x + Vector3.up * startOffset.y + forward * startOffset.z;
        Vector3 endWorld = _player.position + right * endOffset.x + Vector3.up * endOffset.y + forward * endOffset.z;
        _jumpWatcher.transform.position = startWorld;
        _jumpWatcher.transform.localScale = _jumpWatcherBaseScale * scaleMultiplier;
        _jumpWatcher.SetActive(true);

        if (_jumpWatcherLight != null)
        {
            _jumpWatcherLight.intensity = 1.15f + flashStrength * 2.1f;
            _jumpWatcherLight.range = 6.5f + flashStrength * 5f;
        }

        if (playSting && _scareSource != null)
        {
            AudioClip sting = GetRandomScareSting();
            if (sting != null) _scareSource.PlayOneShot(sting, scareVolume);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Sin(t * Mathf.PI);

            _jumpWatcher.transform.position = Vector3.Lerp(startWorld, endWorld, t);
            Vector3 lookTarget = new Vector3(_player.position.x, _jumpWatcher.transform.position.y + 0.45f, _player.position.z);
            _jumpWatcher.transform.LookAt(lookTarget);

            if (_jumpWatcherLight != null)
            {
                _jumpWatcherLight.intensity = 0.8f + pulse * (1.3f + flashStrength * 2f);
            }

            if (_flashOverlay != null)
            {
                _flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, pulse * flashStrength);
            }

            yield return null;
        }

        _jumpWatcher.SetActive(false);
        _jumpWatcher.transform.localScale = _jumpWatcherBaseScale;

        if (_flashOverlay != null)
        {
            _flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
        }
    }

    private void ApplyAtmosphereStage(int stage)
    {
        float[] directionalIntensities = { 1.55f, 1.2f, 0.88f, 0.58f, 0.34f };
        float[] ambientIntensities = { 1f, 0.82f, 0.64f, 0.48f, 0.32f };
        float[] fogDensities = { 0.002f, 0.005f, 0.012f, 0.022f, 0.035f };
        float[] skyExposures = { 1f, 0.82f, 0.66f, 0.5f, 0.36f };
        float[] bloomIntensities = { 0.2f, 0.45f, 0.85f, 1.35f, 1.95f };
        float[] bloomThresholds = { 1.08f, 0.95f, 0.84f, 0.72f, 0.62f };
        float[] vignetteIntensities = { 0.12f, 0.22f, 0.34f, 0.45f, 0.58f };
        float[] postExposures = { 0f, -0.2f, -0.55f, -0.9f, -1.35f };
        float[] contrasts = { 0f, 10f, 25f, 45f, 65f };
        float[] saturations = { 0f, -10f, -25f, -45f, -65f };

        Color[] lightColors =
        {
            new Color(1f, 0.97f, 0.91f, 1f),
            new Color(0.88f, 0.91f, 0.95f, 1f),
            new Color(0.74f, 0.82f, 0.9f, 1f),
            new Color(0.58f, 0.7f, 0.8f, 1f),
            new Color(0.35f, 0.05f, 0.05f, 1f)
        };

        Color[] fogColors =
        {
            new Color(0.81f, 0.95f, 0.99f, 1f),
            new Color(0.64f, 0.79f, 0.84f, 1f),
            new Color(0.45f, 0.61f, 0.67f, 1f),
            new Color(0.15f, 0.1f, 0.1f, 1f),
            new Color(0.08f, 0.02f, 0.02f, 1f)
        };

        Color[] skyTints =
        {
            new Color(0.58f, 0.58f, 0.58f, 0.5f),
            new Color(0.5f, 0.54f, 0.56f, 0.7f),
            new Color(0.43f, 0.49f, 0.54f, 0.82f),
            new Color(0.36f, 0.42f, 0.47f, 0.92f),
            new Color(0.31f, 0.36f, 0.41f, 1f)
        };

        Color[] ambientSky =
        {
            new Color(0.5f, 0.6f, 0.8f, 1f),
            new Color(0.36f, 0.45f, 0.58f, 1f),
            new Color(0.25f, 0.32f, 0.39f, 1f),
            new Color(0.17f, 0.23f, 0.28f, 1f),
            new Color(0.12f, 0.17f, 0.2f, 1f)
        };

        Color[] ambientEquator =
        {
            new Color(0.4f, 0.45f, 0.5f, 1f),
            new Color(0.28f, 0.34f, 0.37f, 1f),
            new Color(0.21f, 0.26f, 0.29f, 1f),
            new Color(0.15f, 0.19f, 0.21f, 1f),
            new Color(0.11f, 0.15f, 0.16f, 1f)
        };

        Color[] ambientGround =
        {
            new Color(0.2f, 0.25f, 0.2f, 1f),
            new Color(0.16f, 0.2f, 0.17f, 1f),
            new Color(0.12f, 0.15f, 0.13f, 1f),
            new Color(0.08f, 0.11f, 0.1f, 1f),
            new Color(0.05f, 0.08f, 0.07f, 1f)
        };

        int index = Mathf.Clamp(stage, 0, directionalIntensities.Length - 1);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = fogDensities[index];
        RenderSettings.fogColor = fogColors[index];
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientIntensity = ambientIntensities[index];
        RenderSettings.ambientSkyColor = ambientSky[index];
        RenderSettings.ambientEquatorColor = ambientEquator[index];
        RenderSettings.ambientGroundColor = ambientGround[index];

        if (_runtimeSkybox != null)
        {
            _runtimeSkybox.SetColor("_Tint", skyTints[index]);
            _runtimeSkybox.SetFloat("_Exposure", skyExposures[index]);
            _runtimeSkybox.SetFloat("_Rotation", 12f + (stage * 8f));
        }

        if (directionalLight != null)
        {
            directionalLight.intensity = directionalIntensities[index];
            directionalLight.color = lightColors[index];
            directionalLight.transform.rotation = Quaternion.Euler(50f - (stage * 4f), 330f - (stage * 4f), 0f);
        }

        if (_colorAdjustments != null)
        {
            _colorAdjustments.postExposure.Override(postExposures[index]);
            _colorAdjustments.contrast.Override(contrasts[index]);
            _colorAdjustments.saturation.Override(saturations[index]);
        }

        if (_vignette != null)
        {
            _vignette.intensity.Override(vignetteIntensities[index]);
        }

        if (_bloom != null)
        {
            _bloom.intensity.Override(bloomIntensities[index]);
            _bloom.threshold.Override(bloomThresholds[index]);
        }

        if (_letterPanelBackground != null)
        {
            _letterPanelBackground.color = Color.Lerp(new Color(0.11f, 0.09f, 0.08f, 0.95f), new Color(0.07f, 0.03f, 0.03f, 0.98f), index / 4f);
        }

        if (_letterBloodSmearTop != null)
        {
            _letterBloodSmearTop.color = Color.Lerp(new Color(0.25f, 0.04f, 0.03f, 0.52f), new Color(0.58f, 0.05f, 0.05f, 0.88f), index / 4f);
        }

        if (_letterBloodSmearBottom != null)
        {
            _letterBloodSmearBottom.color = Color.Lerp(new Color(0.18f, 0.03f, 0.03f, 0.38f), new Color(0.42f, 0.03f, 0.03f, 0.76f), index / 4f);
        }
    }

    private void OpenLetter(int interactedStoneIndex)
    {
        _letterOpen = true;
        SetPlayerLocked(true);
        SetLetterPanelVisible(true);
        _pressureActive = false;
        _pressureTimer = 0f;

        if (_scareSource != null && letterOpenClip != null)
        {
            _scareSource.PlayOneShot(letterOpenClip, scareVolume * 0.8f);
        }

        _currentStoneIndex = Mathf.Clamp(interactedStoneIndex, 0, _stones.Count - 1);
        _usedStoneIndices.Add(_currentStoneIndex);

        int fragmentIndex = Mathf.Clamp(_fragmentsRecovered, 0, _letterTitles.Length - 1);
        _letterTitleLabel.text = _letterTitles[fragmentIndex];
        _letterBodyLabel.text = _letterBodies[fragmentIndex];

        bool isLastStone = fragmentIndex >= _stones.Count - 1;
        _letterHintLabel.text = isLastStone
            ? "Press E or Esc to close this fragment. The park will ask what comes next."
            : "Press E or Esc to close this fragment. Then find a different stone before the anomaly catches up.";
    }

    private void CloseLetter()
    {
        _letterOpen = false;
        SetLetterPanelVisible(false);
        SetPlayerLocked(false);

        if (_scareSource != null && fragmentCompleteClip != null)
        {
            _scareSource.PlayOneShot(fragmentCompleteClip, scareVolume * 0.9f);
        }

        _fragmentsRecovered = Mathf.Min(_fragmentsRecovered + 1, _stones.Count);

        ApplyAtmosphereStage(_fragmentsRecovered);

        _currentStoneIndex = FindNextUnusedStoneIndex();

        UpdateStonePresentation();

        if (_fragmentsRecovered >= _stones.Count)
        {
            StartEndingSequence();
            return;
        }

        if (_fragmentsRecovered % 2 == 0 && _fragmentsRecovered < _stones.Count && !_triggeredMilestoneScares.Contains(_fragmentsRecovered) && _jumpScareRoutine == null)
        {
            _jumpScareRoutine = StartCoroutine(TriggerMilestoneWatcherSequence(_fragmentsRecovered));
        }

        BeginPressureWindow();
    }

    private void SetPlayerLocked(bool isLocked)
    {
        if (_playerController != null)
        {
            _playerController.enabled = !isLocked;
        }

        if (_characterController != null && isLocked)
        {
            _characterController.Move(Vector3.zero);
        }

        if (_playerInputs != null)
        {
            _playerInputs.cursorLocked = !isLocked;
            _playerInputs.cursorInputForLook = !isLocked;
            _playerInputs.MoveInput(Vector2.zero);
            _playerInputs.LookInput(Vector2.zero);
            _playerInputs.JumpInput(false);
            _playerInputs.SprintInput(false);
        }

        Cursor.visible = isLocked;
        Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void SetLetterPanelVisible(bool isVisible)
    {
        if (_letterPanel != null)
        {
            _letterPanel.SetActive(isVisible);
        }
    }

    private void StartEndingSequence()
    {
        if (_endingSequenceStarted)
        {
            return;
        }

        _endingSequenceStarted = true;
        _endingInputEnabled = false;
        _pressureActive = false;
        _pressureTimer = 0f;
        _expellingPlayer = false;
        if (_promptLabel != null) _promptLabel.enabled = false;
        if (_statusLabel != null) _statusLabel.enabled = false;
        if (_flashOverlay != null) _flashOverlay.color = new Color(0f, 0f, 0f, 0f);
        StopParkAudio();
        SetPlayerLocked(true);
        StartCoroutine(EndingSequenceRoutine());
    }

    private IEnumerator EndingSequenceRoutine()
    {
        if (_endingOverlay == null)
        {
            yield break;
        }

        _endingOverlay.SetActive(true);
        _endingChoiceMade = false;
        _endingQuestionLabel.text = string.Empty;
        _endingConsequenceLabel.text = string.Empty;
        _endingHintLabel.text = string.Empty;
        _endingHeaderLabel.text = "02  THE CHOICE";
        _endingYesButton.gameObject.SetActive(false);
        _endingNoButton.gameObject.SetActive(false);
        SetEndingDecorAlpha(0f);

        yield return StartCoroutine(FadeEndingOverlay(0f, 1f, 0.45f));
        StartCoroutine(FadeText(_endingHeaderLabel, 0f, 0.75f, 0.35f));
        StartCoroutine(AnimateEndingGlitchBars());
        yield return StartCoroutine(TypeText(_endingQuestionLabel, "Ты хочешь узнать всю правду?\nТогда оставайся здесь.", 0.032f, playGlitchSound: true));

        _endingHintLabel.text = "Выбери ответ";
        StartCoroutine(FadeText(_endingHintLabel, 0f, 0.9f, 0.45f));

        _endingYesButton.gameObject.SetActive(true);
        _endingNoButton.gameObject.SetActive(true);
        yield return StartCoroutine(FadeButtonGroup(0f, 1f, 0.35f));
        _endingYesButton.interactable = true;
        _endingNoButton.interactable = true;
        _endingInputEnabled = true;
    }

    private void ResolveEndingChoice(bool stayHere)
    {
        if (_endingChoiceMade)
        {
            return;
        }

        _endingChoiceMade = true;
        _endingYesButton.interactable = false;
        _endingNoButton.interactable = false;
        StartCoroutine(ResolveEndingChoiceRoutine(stayHere));
    }

    private IEnumerator ResolveEndingChoiceRoutine(bool stayHere)
    {
        _endingInputEnabled = false;

        if (!stayHere)
        {
            yield return StartCoroutine(PlaySystemVictimEnding());
            yield break;
        }

        string consequence = stayHere
            ? "Ты остаёшься.\nГладкий свет парка больше не кажется мягким.\nВетви смыкаются, небо запечатывается стеклянной синевой,\nи AeroOS записывает твоё имя туда, где уже шепчут остальные."
            : "Ты говоришь «нет».\nПарк дёргается, будто теряет над тобой власть.\nТропа назад открывается лишь на миг,\nи ты уносишь с собой обрывок правды, оставляя шёпот позади.";

        Color consequenceColor = stayHere
            ? new Color(1f, 0.78f, 0.78f, 0f)
            : new Color(0.82f, 0.96f, 1f, 0f);

        _endingConsequenceLabel.color = consequenceColor;
        _endingHintLabel.text = string.Empty;

        if (_scareSource != null)
        {
            AudioClip choiceClip = stayHere ? _endingAcceptClip : _endingRejectClip;
            if (choiceClip != null)
            {
                _scareSource.PlayOneShot(choiceClip, stayHere ? scareVolume : scareVolume * 0.75f);
            }
        }

        yield return StartCoroutine(FadeButtonGroup(1f, 0f, 0.25f));
        _endingYesButton.gameObject.SetActive(false);
        _endingNoButton.gameObject.SetActive(false);
        StartCoroutine(FadeText(_endingHeaderLabel, 0.75f, 0f, 0.2f));
        yield return StartCoroutine(FadeText(_endingQuestionLabel, 1f, 0f, 0.2f));
        yield return StartCoroutine(PlayEndingChoiceFlash(stayHere));

        yield return StartCoroutine(TypeText(_endingConsequenceLabel, consequence, 0.026f, playGlitchSound: true));
        yield return new WaitForSeconds(stayHere ? 3.2f : 2.8f);

        yield return StartCoroutine(FadeEndingOverlay(1f, 1f, 0.1f));

        if (stayHere)
        {
            PlayerPrefs.SetInt(SystemEndingFlagKey, 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene(StoryIntroSceneName);
            yield break;
        }

        SceneManager.LoadScene(MainMenuSceneName);
    }

    private IEnumerator FadeEndingOverlay(float startAlpha, float targetAlpha, float duration)
    {
        if (_endingOverlayBackground == null)
        {
            yield break;
        }

        float elapsed = 0f;
        Color color = _endingOverlayBackground.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            _endingOverlayBackground.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        _endingOverlayBackground.color = new Color(color.r, color.g, color.b, targetAlpha);
    }

    private IEnumerator PlaySystemVictimEnding()
    {
        if (_endingOverlay != null)
        {
            _endingOverlay.SetActive(false);
        }

        if (_scareSource != null && _endingRejectClip != null)
        {
            _scareSource.PlayOneShot(_endingRejectClip, scareVolume * 0.95f);
        }

        yield return StartCoroutine(PlayEndingChoiceFlash(false));
        PrepareSilentParkState();
        SetPlayerLocked(false);
        yield return new WaitForSeconds(10f);
        yield return StartCoroutine(PlayEraseShockwave(GetParkCenterPoint()));
        yield return StartCoroutine(EraseParkObjectsWave(GetParkCenterPoint()));
        yield return new WaitForSeconds(7f);

        if (_endingOverlay != null)
        {
            _endingOverlay.SetActive(true);
        }

        _endingHeaderLabel.text = "03  SYSTEM VERDICT";
        _endingQuestionLabel.text = string.Empty;
        _endingConsequenceLabel.text = string.Empty;
        _endingHintLabel.text = string.Empty;
        _endingYesButton.gameObject.SetActive(false);
        _endingNoButton.gameObject.SetActive(false);
        SetEndingDecorAlpha(0f);

        yield return StartCoroutine(FadeEndingOverlay(0f, 1f, 0.8f));
        StartCoroutine(FadeText(_endingHeaderLabel, 0f, 0.75f, 0.35f));
        StartCoroutine(AnimateEndingGlitchBars());
        yield return StartCoroutine(TypeText(_endingQuestionLabel, "Ты стал жертвой системы.\nПравда была рядом, но выбор уже был сделан.", 0.034f, playGlitchSound: true));
        yield return new WaitForSeconds(3.4f);
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private void PrepareSilentParkState()
    {
        StopParkAudio();
        _pressureActive = false;
        _pressureTimer = 0f;
        _nextGlitchTime = float.MaxValue;
        _nextPresenceTime = float.MaxValue;

        if (_statusLabel != null) _statusLabel.enabled = false;
        if (_promptLabel != null) _promptLabel.enabled = false;
        SetLetterPanelVisible(false);

        if (_activeMarkerRoot != null)
        {
            _activeMarkerRoot.SetActive(false);
        }

        foreach (GameObject watcher in _watchers)
        {
            if (watcher != null) watcher.SetActive(false);
        }

        foreach (GameObject orb in _anomalyOrbs)
        {
            if (orb != null) orb.SetActive(false);
        }

        foreach (GameObject spike in _riftSpikes)
        {
            if (spike != null) spike.SetActive(false);
        }

        if (_jumpWatcher != null)
        {
            _jumpWatcher.SetActive(false);
        }
    }

    private Vector3 GetParkCenterPoint()
    {
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (Transform stone in _stones)
        {
            if (stone == null) continue;
            center += stone.position;
            count++;
        }

        if (count == 0)
        {
            if (_mainCamera != null)
            {
                return _mainCamera.transform.position + _mainCamera.transform.forward * 12f;
            }

            return Vector3.zero;
        }

        return center / count;
    }

    private IEnumerator PlayEraseShockwave(Vector3 center)
    {
        if (_scareSource != null && _endingGlitchClip != null)
        {
            _scareSource.PlayOneShot(_endingGlitchClip, scareVolume);
        }

        float duration = 0.45f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.Sin((elapsed / duration) * Mathf.PI);

            if (_flashOverlay != null)
            {
                _flashOverlay.color = new Color(0.75f, 0.9f, 1f, pulse * 0.18f);
            }

            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.Override(Mathf.Lerp(0f, 0.7f, pulse));
            }

            if (_mainCamera != null)
            {
                _mainCamera.transform.position += Random.insideUnitSphere * 0.03f;
            }

            yield return null;
        }

        if (_flashOverlay != null)
        {
            _flashOverlay.color = new Color(0f, 0f, 0f, 0f);
        }

        if (_chromaticAberration != null)
        {
            _chromaticAberration.intensity.Override(0f);
        }
    }

    private IEnumerator EraseParkObjectsWave(Vector3 center)
    {
        _parkErasedObjects.Clear();
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj == null || !obj.activeInHierarchy)
            {
                continue;
            }

            if (ShouldPreserveAfterParkErase(obj))
            {
                continue;
            }

            _parkErasedObjects.Add(obj);
        }

        _parkErasedObjects.Sort((a, b) =>
            Vector3.Distance(center, a.transform.position).CompareTo(Vector3.Distance(center, b.transform.position)));

        foreach (GameObject obj in _parkErasedObjects)
        {
            obj.SetActive(false);
            if (_scareSource != null && _endingRejectClip != null && Random.value > 0.82f)
            {
                _scareSource.PlayOneShot(_endingRejectClip, 0.18f);
            }

            yield return new WaitForSeconds(0.035f);
        }
    }

    private bool ShouldPreserveAfterParkErase(GameObject obj)
    {
        if (obj == gameObject || obj == _endingOverlay || obj == _flashOverlay?.gameObject || obj == _activeMarkerRoot)
        {
            return true;
        }

        if (_player != null && (obj == _player.gameObject || obj.transform.IsChildOf(_player)))
        {
            return true;
        }

        if (_mainCamera != null && (obj == _mainCamera.gameObject || obj.transform.IsChildOf(_mainCamera.transform)))
        {
            return true;
        }

        if (obj.GetComponent<Canvas>() != null || obj.GetComponent<EventSystem>() != null)
        {
            return true;
        }

        if (obj.GetComponent<Terrain>() != null || obj.GetComponent<TerrainCollider>() != null)
        {
            return true;
        }

        if (obj.GetComponent<Light>() != null || obj.GetComponent<Volume>() != null)
        {
            return true;
        }

        string lowerName = obj.name.ToLowerInvariant();
        if (lowerName.Contains("terrain") || lowerName.Contains("ground") || lowerName.Contains("grass"))
        {
            return true;
        }

        return false;
    }

    private IEnumerator FadeEndingDecor(float startAlpha, float targetAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            SetEndingDecorAlpha(alpha);
            yield return null;
        }

        SetEndingDecorAlpha(targetAlpha);
    }

    private void SetEndingDecorAlpha(float alpha)
    {
        if (_endingAuraGlow != null)
        {
            _endingAuraGlow.color = new Color(0.45f, 0.72f, 1f, alpha * 0.08f);
        }

        if (_endingQuestionPlate != null)
        {
            _endingQuestionPlate.color = new Color(0f, 0f, 0f, alpha * 0.18f);
        }

        foreach (Image bar in _endingGlitchBars)
        {
            if (bar != null)
            {
                Color c = bar.color;
                bar.color = new Color(c.r, c.g, c.b, alpha * 0.18f);
            }
        }
    }

    private IEnumerator FadeText(Text text, float startAlpha, float targetAlpha, float duration)
    {
        if (text == null)
        {
            yield break;
        }

        float elapsed = 0f;
        Color baseColor = text.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }

        text.color = new Color(baseColor.r, baseColor.g, baseColor.b, targetAlpha);
    }

    private IEnumerator FadeButtonGroup(float startAlpha, float targetAlpha, float duration)
    {
        float elapsed = 0f;
        Image yesImage = _endingYesButton != null ? _endingYesButton.GetComponent<Image>() : null;
        Image noImage = _endingNoButton != null ? _endingNoButton.GetComponent<Image>() : null;
        Text yesText = _endingYesButton != null ? _endingYesButton.GetComponentInChildren<Text>() : null;
        Text noText = _endingNoButton != null ? _endingNoButton.GetComponentInChildren<Text>() : null;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            SetButtonVisualAlpha(yesImage, yesText, alpha);
            SetButtonVisualAlpha(noImage, noText, alpha);
            yield return null;
        }

        SetButtonVisualAlpha(yesImage, yesText, targetAlpha);
        SetButtonVisualAlpha(noImage, noText, targetAlpha);
    }

    private void SetButtonVisualAlpha(Image image, Text text, float alpha)
    {
        if (image != null)
        {
            Color imageColor = image.color;
            image.color = new Color(imageColor.r, imageColor.g, imageColor.b, alpha * 0.92f);
        }

        if (text != null)
        {
            Color textColor = text.color;
            text.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
        }
    }

    private IEnumerator PlayEndingChoiceFlash(bool stayHere)
    {
        if (_flashOverlay == null)
        {
            yield break;
        }

        Color flashColor = stayHere
            ? new Color(0.78f, 0.12f, 0.16f, 0f)
            : new Color(0.25f, 0.82f, 0.95f, 0f);
        float strength = stayHere ? 0.38f : 0.24f;
        float duration = stayHere ? 0.7f : 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.Sin((elapsed / duration) * Mathf.PI);
            _flashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, pulse * strength);
            yield return null;
        }

        _flashOverlay.color = new Color(0f, 0f, 0f, 0f);
    }

    private IEnumerator AnimateEndingGlitchBars()
    {
        if (_endingGlitchBars.Count == 0)
        {
            yield break;
        }

        float lifetime = 6f;
        float elapsed = 0f;
        while (_endingOverlay != null && _endingOverlay.activeSelf && !_endingChoiceMade && elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < _endingGlitchBars.Count; i++)
            {
                Image bar = _endingGlitchBars[i];
                if (bar == null)
                {
                    continue;
                }

                RectTransform rect = bar.rectTransform;
                Vector2 basePos = rect.anchoredPosition;
                rect.anchoredPosition = new Vector2(basePos.x + Random.Range(-36f, 36f), basePos.y + Random.Range(-4f, 4f));
                float alpha = Random.Range(0.1f, 0.38f);
                bar.color = new Color(0.72f, 0.88f, 1f, alpha);
            }

            if (_flashOverlay != null && Random.value > 0.72f)
            {
                _flashOverlay.color = new Color(0.52f, 0.82f, 1f, Random.Range(0.04f, 0.1f));
            }

            yield return new WaitForSeconds(Random.Range(0.02f, 0.08f));
        }

        foreach (Image bar in _endingGlitchBars)
        {
            if (bar != null)
            {
                bar.color = new Color(0.72f, 0.88f, 1f, 0f);
            }
        }
    }

    private IEnumerator TypeText(Text text, string content, float characterDelay, bool playGlitchSound = false)
    {
        if (text == null)
        {
            yield break;
        }

        RectTransform rect = text.rectTransform;
        Vector2 originalPosition = rect.anchoredPosition;
        Color originalColor = text.color;
        text.text = string.Empty;
        yield return StartCoroutine(FadeText(text, 0f, 1f, 0.35f));

        for (int i = 0; i < content.Length; i++)
        {
            char nextChar = content[i];
            bool shouldGlitch = playGlitchSound && !char.IsWhiteSpace(nextChar) && nextChar != '\n';
            string confirmedText = text.text;

            if (shouldGlitch)
            {
                yield return StartCoroutine(PlayTypingGlitch(text, originalPosition, originalColor, confirmedText, nextChar, i, content));
            }

            text.text = confirmedText + nextChar;
            if (playGlitchSound && _scareSource != null && _endingGlitchClip != null && !char.IsWhiteSpace(content[i]) && i % 2 == 0)
            {
                _scareSource.PlayOneShot(_endingGlitchClip, 0.2f);
            }
            yield return new WaitForSeconds(characterDelay);
        }

        rect.anchoredPosition = originalPosition;
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, text.color.a);
    }

    private IEnumerator PlayTypingGlitch(Text text, Vector2 originalPosition, Color originalColor, string confirmedText, char finalChar, int index, string content)
    {
        RectTransform rect = text.rectTransform;
        char[] glitchChars = { '#', '/', '\\', '|', '_', '-', '=', '+', '0', '1' };
        char fakeChar = glitchChars[(index + content.Length) % glitchChars.Length];

        for (int step = 0; step < 3; step++)
        {
            rect.anchoredPosition = originalPosition + new Vector2(Random.Range(-7f, 7f), Random.Range(-3f, 3f));
            text.color = step % 2 == 0
                ? new Color(0.72f, 0.9f, 1f, text.color.a)
                : new Color(1f, 0.86f, 0.9f, text.color.a);
            text.text = confirmedText + fakeChar;

            foreach (Image bar in _endingGlitchBars)
            {
                if (bar != null)
                {
                    bar.color = new Color(0.82f, 0.94f, 1f, Random.Range(0.2f, 0.5f));
                }
            }

            if (_flashOverlay != null)
            {
                _flashOverlay.color = step % 2 == 0
                    ? new Color(0.55f, 0.82f, 1f, 0.06f)
                    : new Color(1f, 0.22f, 0.28f, 0.035f);
            }

            yield return new WaitForSeconds(0.012f);
        }

        rect.anchoredPosition = originalPosition + new Vector2(Random.Range(-2f, 2f), Random.Range(-1f, 1f));
        text.color = new Color(0.96f, 0.99f, 1f, text.color.a);
        text.text = confirmedText + finalChar;

        if (_flashOverlay != null)
        {
            _flashOverlay.color = new Color(0.55f, 0.82f, 1f, 0.07f);
        }

        yield return new WaitForSeconds(0.01f);

        rect.anchoredPosition = originalPosition;
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, text.color.a);
        text.text = confirmedText;

        foreach (Image bar in _endingGlitchBars)
        {
            if (bar != null)
            {
                bar.color = new Color(0.72f, 0.88f, 1f, 0.04f);
            }
        }

        if (_flashOverlay != null)
        {
            _flashOverlay.color = new Color(0f, 0f, 0f, 0f);
        }
    }

    private void StopParkAudio()
    {
        StopAudioSource(_whisperSource);
        StopAudioSource(_stoneHumSource);
        StopAudioSource(_footstepSource);
        StopAudioSource(_breathingSource);
        StopAudioSource(_pressureSource);
        StopAudioSource(_presenceSource);
    }

    private static void StopAudioSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.Stop();
        source.volume = 0f;
        source.enabled = false;
    }

    private void HandleEndingChoiceInput()
    {
        if (!_endingInputEnabled || _endingChoiceMade)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.yKey.wasPressedThisFrame)
            {
                ResolveEndingChoice(true);
                return;
            }

            if (Keyboard.current.nKey.wasPressedThisFrame)
            {
                ResolveEndingChoice(false);
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.Y))
        {
            ResolveEndingChoice(true);
            return;
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            ResolveEndingChoice(false);
        }
#endif
    }

    private System.Collections.IEnumerator EnsureGameplayCursorState()
    {
        yield return null;
        SetPlayerLocked(false);
        yield return new WaitForSeconds(0.2f);
        if (!_letterOpen)
        {
            SetPlayerLocked(false);
        }
    }

    private static bool WasCancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private static bool WasReturnPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }

    private static bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    private int FindNearbyStoneIndex()
    {
        if (_player == null || _stones.Count == 0)
        {
            return -1;
        }

        int nearestIndex = -1;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < _stones.Count; i++)
        {
            Transform stone = _stones[i];
            if (stone == null)
            {
                continue;
            }

            if (_usedStoneIndices.Contains(i))
            {
                continue;
            }

            float distance = Vector3.Distance(_player.position, stone.position);
            if (distance <= interactDistance && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private int FindNextUnusedStoneIndex()
    {
        for (int i = 0; i < _stones.Count; i++)
        {
            if (!_usedStoneIndices.Contains(i))
            {
                return i;
            }
        }

        return Mathf.Clamp(_stones.Count - 1, 0, _stones.Count - 1);
    }
}
