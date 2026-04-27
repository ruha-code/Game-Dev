using UnityEngine;
using System.Collections;

/// <summary>
/// Полная катсцена с использованием готовых камер
/// 1. Темнота → 2. Комната → 3. Компьютер → 4. Глитчи → 5. Существо → 6. Засасывание → 7. Windows
/// </summary>
public class BootIntroCinematic : MonoBehaviour
{
    public enum ScenePhase
    {
        BlackScreen, EnterRoom, SeeComputer, Glitches,
        CreatureAppears, CreatureShows, PullIn, PlayerPOV, WhiteFlash, LoadWindows
    }

    [Header("🎥 Камеры (автоматически)")]
    public Camera mainCamera;
    
    [Header("💻 Монитор")]
    public BootScreenController screenController;
    public Light monitorLight;
    
    [Header("👾 Существо")]
    public GameObject creaturePrefab;
    public Transform creatureSpawnPoint;
    public float creatureScale = 1f;
    
    [Header("⏱️ Время (секунды)")]
    public float blackScreenDuration = 1.5f;
    public float enterRoomDuration = 3f;
    public float seeComputerDuration = 2.5f;
    public float glitchesDuration = 2f;
    public float creatureAppearsDuration = 1.5f;
    public float creatureShowsDuration = 2f;
    public float pullInDuration = 1f;
    public float playerPOVDuration = 1.5f;
    public float whiteFlashDuration = 0.5f;
    public float loadWindowsDelay = 1f;
    
    [Header("🎬 Настройки")]
    public float startFOV = 50f;
    public float endFOV = 100f;
    public string nextScene = "AeroDesktopScene";
    
    // Состояние
    private ScenePhase currentPhase;
    private float phaseTimer;
    private Vector3 cameraVelocity;
    private GameObject activeCreature;
    private bool flashActive;
    private float flashAlpha;
    private float blackOverlayAlpha;
    private Light monitorGlowLight;
    private Light[] otherLights;
    private float[] otherLightsOriginalIntensity;
    private Vector3 monitorWorldPosition;
    
    // Камеры
    private Transform[] cameraPositions;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        // Находим экран
        if (screenController == null)
        {
            BootScreenController[] controllers = FindObjectsByType<BootScreenController>(FindObjectsInactive.Include);
            if (controllers.Length > 0)
                screenController = controllers[0];
        }
        
        // Единый поиск всех источников света
        Light[] allSceneLights = FindObjectsByType<Light>(FindObjectsInactive.Include);
        System.Collections.Generic.List<Light> others = new System.Collections.Generic.List<Light>();
        
        foreach (Light l in allSceneLights)
        {
            string nameLower = l.name.ToLower();
            
            if (monitorGlowLight == null && nameLower.Contains("monitor") && nameLower.Contains("blue"))
            {
                monitorGlowLight = l;
            }
            else if (monitorLight == null && (nameLower.Contains("monitor") || nameLower.Contains("light")))
            {
                monitorLight = l;
            }
            else
            {
                others.Add(l);
            }
        }
        
        // Fallback для monitorGlowLight: ближайший свет к экрану
        if (monitorGlowLight == null && screenController != null)
        {
            float closestDist = float.MaxValue;
            foreach (Light l in allSceneLights)
            {
                float d = Vector3.Distance(l.transform.position, screenController.transform.position);
                if (d < closestDist && d < 3f)
                {
                    closestDist = d;
                    monitorGlowLight = l;
                    // Убираем из others если был добавлен
                    others.Remove(l);
                }
            }
        }
        
        otherLights = others.ToArray();
        otherLightsOriginalIntensity = new float[otherLights.Length];
        for (int i = 0; i < otherLights.Length; i++)
            otherLightsOriginalIntensity[i] = otherLights[i].intensity;
        
        // Находим реальную позицию монитора (экран, не родительский объект)
        GameObject screenSurface = GameObject.Find("Boot Screen Surface");
        monitorWorldPosition = screenSurface != null ? screenSurface.transform.position : new Vector3(0f, 1.35f, 2.24f);
        
        // Создаём камеры
        CreateCameraPositions();
        
        // Инициализация
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = Color.black;
        mainCamera.transform.position = cameraPositions[0].position;
        mainCamera.transform.rotation = cameraPositions[0].rotation;
        mainCamera.fieldOfView = startFOV;
        
        if (monitorLight != null)
            monitorLight.intensity = 0f;
        
        if (monitorGlowLight != null)
            monitorGlowLight.intensity = 0f;
        
        blackOverlayAlpha = 1f;
        SwitchPhase(ScenePhase.BlackScreen);
    }

    void Update()
    {
        phaseTimer += Time.deltaTime;
        
        switch (currentPhase)
        {
            case ScenePhase.BlackScreen:
                if (phaseTimer >= blackScreenDuration)
                    SwitchPhase(ScenePhase.EnterRoom);
                break;
                
            case ScenePhase.EnterRoom:
                HandleEnterRoom();
                if (phaseTimer >= enterRoomDuration)
                    SwitchPhase(ScenePhase.SeeComputer);
                break;
                
            case ScenePhase.SeeComputer:
                HandleSeeComputer();
                if (phaseTimer >= seeComputerDuration)
                    SwitchPhase(ScenePhase.Glitches);
                break;
                
            case ScenePhase.Glitches:
                HandleGlitches();
                if (phaseTimer >= glitchesDuration)
                    SwitchPhase(ScenePhase.CreatureAppears);
                break;
                
            case ScenePhase.CreatureAppears:
                HandleCreatureAppears();
                if (phaseTimer >= creatureAppearsDuration)
                    SwitchPhase(ScenePhase.CreatureShows);
                break;
                
            case ScenePhase.CreatureShows:
                HandleCreatureShows();
                if (phaseTimer >= creatureShowsDuration)
                    SwitchPhase(ScenePhase.PullIn);
                break;
                
            case ScenePhase.PullIn:
                HandlePullIn();
                if (phaseTimer >= pullInDuration)
                    SwitchPhase(ScenePhase.PlayerPOV);
                break;
                
            case ScenePhase.PlayerPOV:
                HandlePlayerPOV();
                if (phaseTimer >= playerPOVDuration)
                    SwitchPhase(ScenePhase.WhiteFlash);
                break;
                
            case ScenePhase.WhiteFlash:
                HandleWhiteFlash();
                if (phaseTimer >= whiteFlashDuration)
                    SwitchPhase(ScenePhase.LoadWindows);
                break;
                
            case ScenePhase.LoadWindows:
                HandleLoadWindows();
                break;
        }
    }

    #region Phase Handlers

    void HandleEnterRoom()
    {
        float t = phaseTimer / enterRoomDuration;
        if (t > 1f) t = 1f;
        
        // Плавное затухание чёрного оверлея — комната проявляется постепенно
        // Первые 30% фазы — всё ещё почти чёрный экран
        float fadeT = Mathf.Clamp01((t - 0.15f) / 0.7f);
        blackOverlayAlpha = Mathf.Lerp(1f, 0f, fadeT * fadeT);
        
        // Медленное плавное движение от внешней позиции к дверному проёму
        // Используем easing для кинематографичной скорости (медленно-быстро-медленно)
        float smoothT = t * t * (3f - 2f * t); // smoothstep
        mainCamera.transform.position = Vector3.Lerp(
            cameraPositions[0].position,
            cameraPositions[1].position,
            smoothT
        );
        
        // Камера смотрит прямо вперёд, слегка вниз к центру комнаты
        Vector3 lookTarget = Vector3.Lerp(
            new Vector3(0f, 1.2f, -2f),
            new Vector3(0f, 1.3f, 0f),
            t
        );
        mainCamera.transform.LookAt(lookTarget);
        
        // FOV слегка расширяется при входе — ощущение пространства
        mainCamera.fieldOfView = Mathf.Lerp(startFOV, startFOV + 8f, t);
        
        // Едва заметное свечение монитора вдали
        if (monitorGlowLight != null)
            monitorGlowLight.intensity = t * 0.15f;
        if (monitorLight != null)
            monitorLight.intensity = Mathf.Lerp(0f, 0.15f, t);
    }

    void HandleSeeComputer()
    {
        float t = phaseTimer / seeComputerDuration;
        if (t > 1f) t = 1f;
        
        // Продолжаем плавное движение внутрь комнаты к компьютеру
        float smoothT = t * t * (3f - 2f * t);
        mainCamera.transform.position = Vector3.Lerp(
            cameraPositions[1].position,
            cameraPositions[2].position,
            smoothT
        );
        
        // Камера постепенно центрируется на мониторе
        Vector3 monitorPos = monitorWorldPosition;
        // Начинаем смотреть чуть в сторону, плавно переходим к центру экрана
        Vector3 lookOffset = Vector3.Lerp(
            new Vector3(-0.5f, 0f, -1f),  // слегка смещённый взгляд
            Vector3.zero,                    // точно в центр монитора
            smoothT
        );
        mainCamera.transform.LookAt(monitorPos + lookOffset);
        
        // FOV сужается — внимание фокусируется на мониторе
        mainCamera.fieldOfView = Mathf.Lerp(startFOV + 8f, 35f, smoothT);
        
        // Свечение монитора нарастает по мере приближения
        // Мягкий экспоненциальный рост — сначала медленно, потом ярче
        float glowIntensity = Mathf.Pow(t, 1.5f);
        if (monitorGlowLight != null)
            monitorGlowLight.intensity = Mathf.Lerp(0f, 3f, glowIntensity);
        
        if (monitorLight != null)
            monitorLight.intensity = Mathf.Lerp(0f, 1.5f, glowIntensity);
        
        // Остальные источники света приглушаются — монитор становится ярчайшим объектом
        float otherDim = Mathf.Lerp(1f, 0.15f, smoothT);
        for (int i = 0; i < otherLights.Length; i++)
        {
            if (otherLights[i] != null)
                otherLights[i].intensity = otherLightsOriginalIntensity[i] * otherDim;
        }
        
        if (screenController != null)
            screenController.SetMode(BootScreenController.ScreenMode.Idle, 0.2f + t * 0.5f);
    }

    void HandleGlitches()
    {
        float t = phaseTimer / glitchesDuration;
        float shake = t * 0.03f;
        
        mainCamera.transform.localPosition = new Vector3(
            Mathf.PerlinNoise(Time.time * 15f, 0f) * shake - shake/2f,
            Mathf.PerlinNoise(Time.time * 15f, 1f) * shake - shake/2f,
            0f
        );
        
        if (screenController != null)
            screenController.SetMode(BootScreenController.ScreenMode.Hallucination, 0.5f + t * 0.5f);
    }

    void HandleCreatureAppears()
    {
        float t = phaseTimer / creatureAppearsDuration;
        if (t > 1f) t = 1f;
        
        if (activeCreature == null)
        {
            SpawnCreature();
        }
        
        if (activeCreature != null)
        {
            activeCreature.transform.localScale = Vector3.Lerp(
                Vector3.one * 0.1f,
                Vector3.one * creatureScale,
                Mathf.Sin(t * Mathf.PI * 0.5f)
            );
            
            Vector3 lookDir = mainCamera.transform.position - activeCreature.transform.position;
            if (lookDir.sqrMagnitude > 0.001f)
                activeCreature.transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        }
        
        if (screenController != null)
            screenController.SetMode(BootScreenController.ScreenMode.Hallucination, 1f);
    }

    void HandleCreatureShows()
    {
        float t = phaseTimer / creatureShowsDuration;
        if (t > 1f) t = 1f;
        
        if (activeCreature != null)
        {
            activeCreature.transform.localScale = Vector3.Lerp(
                Vector3.one * creatureScale,
                Vector3.one * creatureScale * 1.3f,
                t
            );
            
            Vector3 moveDir = (mainCamera.transform.position - activeCreature.transform.position).normalized;
            activeCreature.transform.position += moveDir * 0.02f * t;
        }
        
        mainCamera.fieldOfView = Mathf.Lerp(endFOV - 20f, endFOV, t);
    }

    void HandlePullIn()
    {
        float t = phaseTimer / pullInDuration;
        if (t > 1f) t = 1f;
        
        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            cameraPositions[3].position,
            t * t
        );
        
        mainCamera.fieldOfView = Mathf.Lerp(endFOV, endFOV + 10f, t);
        
        if (activeCreature != null)
            activeCreature.transform.localScale = Vector3.one * creatureScale * (1f - t);
        
        if (screenController != null)
            screenController.SetMode(BootScreenController.ScreenMode.Pull, t);
    }

    void HandlePlayerPOV()
    {
        float t = phaseTimer / playerPOVDuration;
        if (t > 1f) t = 1f;
        
        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            cameraPositions[4].position,
            t
        );
        
        mainCamera.transform.rotation = Quaternion.Euler(
            0f,
            Mathf.Lerp(0f, 360f, t),
            Mathf.Lerp(0f, 45f, Mathf.Sin(t * Mathf.PI))
        );
        
        mainCamera.fieldOfView = Mathf.Lerp(endFOV + 10f, endFOV + 20f, t);
        
        if (screenController != null)
            screenController.SetMode(BootScreenController.ScreenMode.Pull, 1f);
    }

    void HandleWhiteFlash()
    {
        flashActive = true;
        float t = phaseTimer / whiteFlashDuration;
        
        if (t < 0.5f)
            flashAlpha = t * 2f;
        else
            flashAlpha = 1f - (t - 0.5f) * 2f;
        
        if (flashAlpha < 0f) flashAlpha = 0f;
    }

    void HandleLoadWindows()
    {
        float t = (phaseTimer - whiteFlashDuration) / loadWindowsDelay;
        
        if (t >= 1f && !string.IsNullOrEmpty(nextScene))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextScene);
        }
    }

    #endregion

    #region Helpers

    void CreateCameraPositions()
    {
        cameraPositions = new Transform[5];
        
        GameObject parent = new GameObject("🎬 Scene Cameras");
        parent.transform.SetParent(GameObject.Find("BootScene_Setup")?.transform);
        
        // Реальная геометрия: дверь z=-2.92, монитор z=2.24, задняя стена z=3.05
        // 0: Снаружи комнаты (коридор) — перед дверью
        cameraPositions[0] = CreateCameraPoint(parent.transform, "Outside", new Vector3(0f, 1.5f, -7f), new Vector3(0f, 0f, 0f));
        
        // 1: Дверной проём — камера входит в комнату
        cameraPositions[1] = CreateCameraPoint(parent.transform, "Doorway", new Vector3(0f, 1.5f, -2.5f), new Vector3(0f, 0f, 0f));
        
        // 2: Середина комнаты — монитор виден впереди у дальней стены
        cameraPositions[2] = CreateCameraPoint(parent.transform, "Computer", new Vector3(0f, 1.4f, 0.5f), new Vector3(0f, 0f, 0f));
        
        // 3: Близко к монитору — перед столом
        cameraPositions[3] = CreateCameraPoint(parent.transform, "Close", new Vector3(0f, 1.3f, 1.5f), new Vector3(0f, 0f, 0f));
        
        // 4: Внутри экрана (Player POV)
        cameraPositions[4] = CreateCameraPoint(parent.transform, "PlayerEyes", new Vector3(0f, 1.3f, 2.5f), new Vector3(0f, 180f, 0f));
    }

    Transform CreateCameraPoint(Transform parent, string name, Vector3 pos, Vector3 rot)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localRotation = Quaternion.Euler(rot);
        return go.transform;
    }

    void SpawnCreature()
    {
        if (creaturePrefab == null)
        {
            // Создаём светящуюся капсулу
            creaturePrefab = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            creaturePrefab.name = "Creature";
            creaturePrefab.transform.localScale = new Vector3(0.25f, 0.9f, 0.25f);
            
            Renderer rend = creaturePrefab.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0f, 1f, 1f, 0.8f);
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", new Color(0f, 1f, 1f, 1f));
            }
            
            creaturePrefab.AddComponent<HallucinationPresence>();
        }
        
        Vector3 spawnPos = creatureSpawnPoint != null ? 
            creatureSpawnPoint.position : 
            new Vector3(0.3f, 1.2f, 2f);
        
        activeCreature = Instantiate(creaturePrefab, spawnPos, Quaternion.identity);
        activeCreature.transform.SetParent(GameObject.Find("BootScene_Setup")?.transform);
        
        HallucinationPresence presence = activeCreature.GetComponent<HallucinationPresence>();
        if (presence != null)
        {
            presence.targetCamera = mainCamera;
            presence.SetVisible(true);
        }
    }

    void SwitchPhase(ScenePhase newPhase)
    {
        currentPhase = newPhase;
        phaseTimer = 0f;
        
        if (newPhase != ScenePhase.Glitches)
            mainCamera.transform.localPosition = Vector3.zero;
    }

    void OnGUI()
    {
        if (blackOverlayAlpha > 0.001f)
        {
            Color c = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, blackOverlayAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = c;
        }
        
        if (flashActive && flashAlpha > 0f)
        {
            Color c = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, flashAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = c;
        }
    }

    void OnDestroy()
    {
        if (activeCreature != null)
            Destroy(activeCreature);
    }

    #endregion
}
