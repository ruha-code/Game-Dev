using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ParkSceneController : MonoBehaviour
{
    private const string DesktopSceneName = "AeroDesktopScene";
    private const string PlayerRootName = "PlayerCapsule";
    private const string CameraName = "MainCamera";

    private readonly string[] _stoneNames =
    {
        "PT_Menhir_Rock_02 (1)",
        "PT_Menhir_Rock_02 (2)",
        "PT_Menhir_Rock_02 (3)",
        "PT_Menhir_Rock_02 (4)",
        "PT_Menhir_Rock_02"
    };

    private readonly string[] _letterTitles =
    {
        "Fragment 01 // Arrival Log",
        "Fragment 02 // Groundskeeper Memo",
        "Fragment 03 // Missing Staff Notice",
        "Fragment 04 // Memory Bleed Report",
        "Fragment 05 // Exit Directive"
    };

    private readonly string[] _letterBodies =
    {
        "They moved the first corrupted branch into the park because nobody would question a quiet landscape. The tree kept rendering itself one season behind reality, and AeroOS started calling that delay a comfort feature.\n\nIf you can read this, the comfort layer is failing.",
        "We planted stone markers around the anomaly after sunset. By morning, each one had turned to face the hill as if something beneath the roots had spoken during the night.\n\nDo not answer if the park answers first.",
        "Three employees vanished while tracing the bark fractures. Their badges were recovered inside family photos they never took, smiling in places this park has never had.\n\nThe system is filing people as scenery.",
        "The menhirs are not monuments. They are needles stitched through one wound, holding a memory leak shut long enough for the desktop to stay calm.\n\nEvery letter you recover weakens the lie and strengthens whatever has been watching from the treeline.",
        "The park was only the shell. The warmer fragments were scattered where memories still pretend to be harmless: Pictures keeps the faces, Music keeps the voices.\n\nLeave before the fifth watcher reaches the path. Return to the desktop. Finish the recovery there."
    };

    private readonly List<Transform> _stones = new List<Transform>();
    private readonly List<GameObject> _watchers = new List<GameObject>();
    private readonly List<Vector3> _watcherBasePositions = new List<Vector3>();
    private readonly List<GameObject> _anomalyOrbs = new List<GameObject>();
    private readonly List<Vector3> _anomalyOrbBasePositions = new List<Vector3>();
    private readonly List<GameObject> _riftSpikes = new List<GameObject>();
    private readonly List<Vector3> _riftSpikeBasePositions = new List<Vector3>();
    private readonly List<Renderer> _bloodStainRenderers = new List<Renderer>();

    [SerializeField] private float interactDistance = 5f;
    [SerializeField] private Color activeGlowColor = new Color(0.4f, 0.85f, 1f, 1f);
    [SerializeField] private Color watcherGlowColor = new Color(0.22f, 0.6f, 0.9f, 1f);
    [SerializeField] private Light directionalLight;
    [SerializeField] private Volume globalVolume;
    [Header("Horror Audio")]
    [SerializeField] private AudioClip whisperLoopClip;
    [SerializeField] private AudioClip watcherStingClip;
    [SerializeField] private AudioClip fogPulseClip;
    [SerializeField] private AudioClip stoneHumClip;
    [SerializeField] private AudioClip letterOpenClip;
    [SerializeField] private AudioClip fragmentCompleteClip;
    [SerializeField, Range(0f, 1f)] private float whisperMaxVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] private float scareVolume = 0.9f;

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

    private int _currentStoneIndex;
    private int _fragmentsRecovered;
    private bool _letterOpen;
    private float _nextFogFlashTime;

    private Material _runtimeSkybox;
    private VolumeProfile _runtimeVolumeProfile;
    private ColorAdjustments _colorAdjustments;
    private Vignette _vignette;
    private Bloom _bloom;
    private AudioSource _whisperSource;
    private AudioSource _scareSource;
    private AudioSource _stoneHumSource;
    private GameObject _jumpWatcher;
    private Light _jumpWatcherLight;
    private Coroutine _jumpScareRoutine;

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
    }

    private void Start()
    {
        SetPlayerLocked(false);
        StartCoroutine(EnsureGameplayCursorState());
    }

    private void Update()
    {
        if (_player == null || _mainCamera == null || _stones.Count == 0)
        {
            return;
        }

        UpdateAnomalyOrbs();
        UpdateWatchers();
        UpdateWhisperZones();
        TryFogPulse();

        if (_letterOpen)
        {
            if (WasInteractPressed() || WasCancelPressed())
            {
                CloseLetter();
            }

            if (_fragmentsRecovered >= _stones.Count && WasReturnPressed())
            {
                SceneManager.LoadScene(DesktopSceneName);
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
                _statusLabel.text = "All fragments recovered. Pictures and Music now hold the next memories. Press R to return.";
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

    private void FindSceneReferences()
    {
        GameObject playerObject = GameObject.Find(PlayerRootName);
        if (playerObject != null)
        {
            _player = playerObject.transform;
            _playerController = playerObject.GetComponent<FirstPersonController>();
            _playerInputs = playerObject.GetComponent<StarterAssetsInputs>();
            _characterController = playerObject.GetComponent<CharacterController>();
        }

        GameObject cameraObject = GameObject.Find(CameraName);
        if (cameraObject != null)
        {
            _mainCamera = cameraObject.GetComponent<Camera>();
        }

        if (directionalLight == null)
        {
            GameObject lightObject = GameObject.Find("Directional Light");
            if (lightObject != null)
            {
                directionalLight = lightObject.GetComponent<Light>();
            }
        }

        if (globalVolume == null)
        {
            GameObject volumeObject = GameObject.Find("Global Volume");
            if (volumeObject != null)
            {
                globalVolume = volumeObject.GetComponent<Volume>();
            }
        }
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
            new Vector3(26f, 0f, 20f),
            new Vector3(0f, 0f, 31f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject watcher = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            watcher.name = $"ParkWatcher_{i + 1}";
            watcher.transform.position = center + offsets[i] + Vector3.up * 1.6f;
            _watcherBasePositions.Add(watcher.transform.position);
            watcher.transform.localScale = new Vector3(1.2f, 2.7f, 1.2f);

            Collider collider = watcher.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            Renderer renderer = watcher.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                Material material = new Material(shader);
                material.color = new Color(0.03f, 0.05f, 0.06f, 1f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", watcherGlowColor * 0.25f);
                renderer.material = material;
            }

            Light watcherLight = watcher.AddComponent<Light>();
            watcherLight.type = LightType.Point;
            watcherLight.range = 4f;
            watcherLight.intensity = 0.55f;
            watcherLight.color = watcherGlowColor;

            watcher.SetActive(false);
            _watchers.Add(watcher);
        }

        _jumpWatcher = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        _jumpWatcher.name = "ParkJumpWatcher";
        _jumpWatcher.transform.localScale = new Vector3(1.3f, 2.95f, 1.3f);
        Collider jumpCollider = _jumpWatcher.GetComponent<Collider>();
        if (jumpCollider != null)
        {
            jumpCollider.enabled = false;
        }

        Renderer jumpRenderer = _jumpWatcher.GetComponent<Renderer>();
        if (jumpRenderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material jumpMaterial = new Material(shader);
            jumpMaterial.color = new Color(0.01f, 0.01f, 0.015f, 1f);
            jumpMaterial.EnableKeyword("_EMISSION");
            jumpMaterial.SetColor("_EmissionColor", new Color(0.72f, 0.08f, 0.11f, 1f) * 0.8f);
            jumpRenderer.material = jumpMaterial;
        }

        _jumpWatcherLight = _jumpWatcher.AddComponent<Light>();
        _jumpWatcherLight.type = LightType.Point;
        _jumpWatcherLight.range = 6f;
        _jumpWatcherLight.intensity = 0.9f;
        _jumpWatcherLight.color = new Color(0.75f, 0.1f, 0.12f, 1f);
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
            new Vector3(-13f, 3.1f, -10f),
            new Vector3(12f, 3.3f, 11f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = $"ParkAnomalyOrb_{i + 1}";
            orb.transform.position = center + offsets[i];
            orb.transform.localScale = Vector3.one * (0.55f + (i * 0.08f));
            _anomalyOrbBasePositions.Add(orb.transform.position);

            Collider collider = orb.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            Renderer renderer = orb.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                Material material = new Material(shader);
                Color orbColor = Color.Lerp(new Color(0.17f, 0.85f, 1f, 1f), new Color(0.85f, 0.94f, 1f, 1f), i / 4f);
                material.color = new Color(0.02f, 0.05f, 0.06f, 1f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", orbColor * 1.5f);
                renderer.material = material;
            }

            Light orbLight = orb.AddComponent<Light>();
            orbLight.type = LightType.Point;
            orbLight.range = 5f;
            orbLight.intensity = 0.75f;
            orbLight.color = Color.Lerp(activeGlowColor, watcherGlowColor, i / 4f);

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
            GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spike.name = $"ParkRiftSpike_{i + 1}";
            spike.transform.position = center + offsets[i] + Vector3.up * (1.7f + i * 0.1f);
            spike.transform.localScale = new Vector3(0.65f, 3.6f + i * 0.3f, 0.65f);
            spike.transform.rotation = Quaternion.Euler(-8f, i * 33f, 7f);
            _riftSpikeBasePositions.Add(spike.transform.position);

            Collider collider = spike.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            Renderer renderer = spike.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                Material material = new Material(shader);
                material.color = new Color(0.03f, 0.01f, 0.01f, 1f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.55f, 0.06f, 0.08f, 1f) * 0.7f);
                renderer.material = material;
            }

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

        Transform activeStone = _stones[Mathf.Clamp(_currentStoneIndex, 0, _stones.Count - 1)];
        _activeMarkerRoot.transform.position = activeStone.position + Vector3.up * 2.6f;

        for (int i = 0; i < _stones.Count; i++)
        {
            Renderer[] renderers = _stones[i].GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.material == null || !renderer.material.HasProperty("_EmissionColor"))
                {
                    continue;
                }

                Color emission = i == _currentStoneIndex ? activeGlowColor * 0.6f : Color.black;
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
            basePosition.y = 3f + Mathf.Sin(Time.time * 0.7f + i) * 0.55f;
            watcher.transform.position = basePosition;

            Light watcherLight = watcher.GetComponent<Light>();
            if (watcherLight != null)
            {
                watcherLight.intensity = pulse + (_fragmentsRecovered * 0.08f);
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
            yield break;
        }

        Vector3 offset = _player.right * Random.Range(-3.1f, 3.1f) + _player.forward * Random.Range(4.4f, 6.3f);
        offset.y = 0f;
        _jumpWatcher.transform.position = _player.position + offset + Vector3.up * 1.5f;
        _jumpWatcher.SetActive(true);

        if (_scareSource != null && watcherStingClip != null)
        {
            _scareSource.PlayOneShot(watcherStingClip, scareVolume);
        }

        float duration = 0.8f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float pulse = 1f - Mathf.Clamp01(t);

            if (_flashOverlay != null)
            {
                _flashOverlay.color = new Color(0.55f, 0.07f, 0.08f, pulse * 0.22f);
            }

            _jumpWatcher.transform.position += _player.forward * (Mathf.Sin(t * Mathf.PI) * 0.012f);
            yield return null;
        }

        if (_flashOverlay != null)
        {
            _flashOverlay.color = new Color(0.55f, 0.07f, 0.08f, 0f);
        }

        _jumpWatcher.SetActive(false);
        _jumpScareRoutine = null;
    }

    private void ApplyAtmosphereStage(int stage)
    {
        float[] directionalIntensities = { 1.55f, 1.2f, 0.88f, 0.58f, 0.34f, 0.18f };
        float[] ambientIntensities = { 1f, 0.82f, 0.64f, 0.48f, 0.32f, 0.22f };
        float[] fogDensities = { 0.002f, 0.0045f, 0.008f, 0.0135f, 0.019f, 0.026f };
        float[] skyExposures = { 1f, 0.82f, 0.66f, 0.5f, 0.36f, 0.22f };
        float[] bloomIntensities = { 0.2f, 0.35f, 0.55f, 0.72f, 0.95f, 1.15f };
        float[] bloomThresholds = { 1.08f, 0.95f, 0.84f, 0.76f, 0.68f, 0.58f };
        float[] vignetteIntensities = { 0.12f, 0.18f, 0.24f, 0.33f, 0.42f, 0.5f };
        float[] postExposures = { 0f, -0.2f, -0.45f, -0.7f, -0.95f, -1.15f };
        float[] contrasts = { 0f, 5f, 10f, 16f, 22f, 28f };
        float[] saturations = { 0f, -6f, -14f, -22f, -30f, -38f };

        Color[] lightColors =
        {
            new Color(1f, 0.97f, 0.91f, 1f),
            new Color(0.88f, 0.91f, 0.95f, 1f),
            new Color(0.74f, 0.82f, 0.9f, 1f),
            new Color(0.58f, 0.7f, 0.8f, 1f),
            new Color(0.46f, 0.58f, 0.68f, 1f),
            new Color(0.37f, 0.47f, 0.56f, 1f)
        };

        Color[] fogColors =
        {
            new Color(0.81f, 0.95f, 0.99f, 1f),
            new Color(0.64f, 0.79f, 0.84f, 1f),
            new Color(0.45f, 0.61f, 0.67f, 1f),
            new Color(0.28f, 0.4f, 0.44f, 1f),
            new Color(0.16f, 0.24f, 0.26f, 1f),
            new Color(0.1f, 0.15f, 0.16f, 1f)
        };

        Color[] skyTints =
        {
            new Color(0.58f, 0.58f, 0.58f, 0.5f),
            new Color(0.5f, 0.54f, 0.56f, 0.7f),
            new Color(0.43f, 0.49f, 0.54f, 0.82f),
            new Color(0.36f, 0.42f, 0.47f, 0.92f),
            new Color(0.31f, 0.36f, 0.41f, 1f),
            new Color(0.26f, 0.31f, 0.35f, 1f)
        };

        Color[] ambientSky =
        {
            new Color(0.5f, 0.6f, 0.8f, 1f),
            new Color(0.36f, 0.45f, 0.58f, 1f),
            new Color(0.25f, 0.32f, 0.39f, 1f),
            new Color(0.17f, 0.23f, 0.28f, 1f),
            new Color(0.12f, 0.17f, 0.2f, 1f),
            new Color(0.11f, 0.15f, 0.18f, 1f)
        };

        Color[] ambientEquator =
        {
            new Color(0.4f, 0.45f, 0.5f, 1f),
            new Color(0.28f, 0.34f, 0.37f, 1f),
            new Color(0.21f, 0.26f, 0.29f, 1f),
            new Color(0.15f, 0.19f, 0.21f, 1f),
            new Color(0.11f, 0.15f, 0.16f, 1f),
            new Color(0.08f, 0.11f, 0.13f, 1f)
        };

        Color[] ambientGround =
        {
            new Color(0.2f, 0.25f, 0.2f, 1f),
            new Color(0.16f, 0.2f, 0.17f, 1f),
            new Color(0.12f, 0.15f, 0.13f, 1f),
            new Color(0.08f, 0.11f, 0.1f, 1f),
            new Color(0.05f, 0.08f, 0.07f, 1f),
            new Color(0.03f, 0.05f, 0.05f, 1f)
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
            _letterPanelBackground.color = Color.Lerp(new Color(0.11f, 0.09f, 0.08f, 0.95f), new Color(0.07f, 0.03f, 0.03f, 0.98f), index / 5f);
        }

        if (_letterBloodSmearTop != null)
        {
            _letterBloodSmearTop.color = Color.Lerp(new Color(0.25f, 0.04f, 0.03f, 0.52f), new Color(0.58f, 0.05f, 0.05f, 0.88f), index / 5f);
        }

        if (_letterBloodSmearBottom != null)
        {
            _letterBloodSmearBottom.color = Color.Lerp(new Color(0.18f, 0.03f, 0.03f, 0.38f), new Color(0.42f, 0.03f, 0.03f, 0.76f), index / 5f);
        }
    }

    private void OpenLetter(int interactedStoneIndex)
    {
        _letterOpen = true;
        SetPlayerLocked(true);
        SetLetterPanelVisible(true);

        if (_scareSource != null && letterOpenClip != null)
        {
            _scareSource.PlayOneShot(letterOpenClip, scareVolume * 0.8f);
        }

        _currentStoneIndex = Mathf.Clamp(interactedStoneIndex, 0, _stones.Count - 1);

        int fragmentIndex = Mathf.Clamp(_fragmentsRecovered, 0, _letterTitles.Length - 1);
        _letterTitleLabel.text = _letterTitles[fragmentIndex];
        _letterBodyLabel.text = _letterBodies[fragmentIndex];

        bool isLastStone = fragmentIndex >= _stones.Count - 1;
        _letterHintLabel.text = isLastStone
            ? "Press E or Esc to close this fragment. Then press R to return to the desktop."
            : "Press E or Esc to close this fragment. The next memory can be triggered from any marker.";
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

        _currentStoneIndex = Mathf.Clamp(_fragmentsRecovered, 0, _stones.Count - 1);

        UpdateStonePresentation();

        if (_fragmentsRecovered >= 2 && _jumpScareRoutine == null)
        {
            _jumpScareRoutine = StartCoroutine(TriggerWatcherScareRoutine());
        }
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

            float distance = Vector3.Distance(_player.position, stone.position);
            if (distance <= interactDistance && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }
}
