using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameKey
{
    DocumentsKey,
    PicturesKey,
    MusicKey,
    ComputerKey,
    ControlPanelKey,
    NetworkKey,
    VideosKey,
    RecycleBinKey
}

public enum LocationId
{
    TreeScene,
    CityScene,
    BalloonScene
}

public enum ObjectiveId
{
    ReviewDocuments,
    InvestigateTree,
    RecoverTreeMemory,
    AccessComputer,
    InvestigateCity,
    ConfigureControlPanel,
    RepairNetwork,
    InvestigateBalloon,
    RecoverVideoEvidence,
    SearchRecycleBin,
    AccessCore
}

public class ProgressionManager : MonoBehaviour
{
    private static ProgressionManager _instance;

    public static ProgressionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ProgressionManager");
                _instance = go.AddComponent<ProgressionManager>();
                DontDestroyOnLoad(go);
            }

            return _instance;
        }
    }

    public static bool HasInstance => _instance != null;

    public event Action ProgressionChanged;

    private const string SaveExistsKey = "SaveExists";
    private const string MemoryShardsKey = "Progression.MemoryShards";
    private const string CurrentObjectiveKey = "Progression.CurrentObjective";
    private const string LastPopupObjectiveKey = "Progression.LastPopupObjective";
    private const string TetrisRewardClaimedKey = "Progression.TetrisRewardClaimed";

    private readonly HashSet<GameKey> _unlockedKeys = new HashSet<GameKey>();
    private readonly HashSet<LocationId> _visitedLocations = new HashSet<LocationId>();

    public int MemoryShards { get; private set; }
    public ObjectiveId CurrentObjective { get; private set; }
    public ObjectiveId LastPopupObjective { get; private set; }
    public bool TetrisRewardClaimed { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance == null)
        {
            _ = Instance;
            return;
        }

        _instance.LoadProgress();
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public bool HasKey(GameKey key)
    {
        return _unlockedKeys.Contains(key);
    }

    public bool IsLocationUnlocked(LocationId location)
    {
        return location switch
        {
            LocationId.TreeScene => HasKey(GameKey.RecycleBinKey),
            LocationId.CityScene => false,
            LocationId.BalloonScene => false,
            _ => false
        };
    }

    public bool HasVisitedLocation(LocationId location)
    {
        return _visitedLocations.Contains(location);
    }

    public bool UnlockKey(GameKey key)
    {
        if (!_unlockedKeys.Add(key))
        {
            return false;
        }

        Debug.Log($"[Progression] Unlocked key: {key}");
        UpdateObjectiveAndSave();
        return true;
    }

    public bool MarkLocationVisited(LocationId location)
    {
        if (!_visitedLocations.Add(location))
        {
            return false;
        }

        Debug.Log($"[Progression] Visited location: {location}");
        UpdateObjectiveAndSave();
        return true;
    }

    public void AddShard()
    {
        MemoryShards++;
        Debug.Log($"[Progression] Memory shard added. Total: {MemoryShards}");
        UpdateObjectiveAndSave();
    }

    public bool ClaimTetrisReward()
    {
        if (TetrisRewardClaimed)
        {
            return false;
        }

        TetrisRewardClaimed = true;
        MemoryShards++;
        Debug.Log($"[Progression] Tetris bonus reward claimed. Memory shards: {MemoryShards}");
        UpdateObjectiveAndSave();
        return true;
    }

    public string GetCurrentObjectiveText()
    {
        return CurrentObjective switch
        {
            ObjectiveId.ReviewDocuments => "Current Task: Review Documents",
            ObjectiveId.InvestigateTree => "Current Task: Click the tree and enter Park",
            ObjectiveId.RecoverTreeMemory => "Current Task: Clear 5 lines in Tetris to recover the hidden shard",
            ObjectiveId.AccessComputer => "Current Task: Open Computer and trace the deletion command",
            ObjectiveId.InvestigateCity => "Current Task: Continue investigation",
            ObjectiveId.ConfigureControlPanel => "Current Task: Continue investigation",
            ObjectiveId.RepairNetwork => "Current Task: Continue investigation",
            ObjectiveId.InvestigateBalloon => "Current Task: Continue investigation",
            ObjectiveId.RecoverVideoEvidence => "Current Task: Continue investigation",
            ObjectiveId.SearchRecycleBin => "Current Task: Search the Recycle Bin for truth fragments",
            ObjectiveId.AccessCore => "Current Task: Resolve the remaining anomalies and prepare for the endgame",
            _ => "Current Task: Continue investigation"
        };
    }

    public string GetObjectivePopupMessage()
    {
        return CurrentObjective switch
        {
            ObjectiveId.ReviewDocuments => "Recovery incomplete. Please review Documents.",
            ObjectiveId.InvestigateTree => "Recycle Bin restored. Visit the park. The tree is responding.",
            ObjectiveId.RecoverTreeMemory => "A hidden shard is trapped inside Tetris. Clear 5 lines to pull it free.",
            ObjectiveId.AccessComputer => "Computer diagnostics are now available. Follow the deletion trail.",
            ObjectiveId.InvestigateCity => "Objective updated.",
            ObjectiveId.ConfigureControlPanel => "Objective updated.",
            ObjectiveId.RepairNetwork => "Objective updated.",
            ObjectiveId.InvestigateBalloon => "Objective updated.",
            ObjectiveId.RecoverVideoEvidence => "Objective updated.",
            ObjectiveId.SearchRecycleBin => "Discarded personnel profiles detected in Recycle Bin.",
            ObjectiveId.AccessCore => "Primary route resolved. Endgame path is stabilizing.",
            _ => "Objective updated."
        };
    }

    public bool HasUnseenObjectivePopup()
    {
        return CurrentObjective != LastPopupObjective;
    }

    public void AcknowledgeCurrentObjectivePopup()
    {
        if (LastPopupObjective == CurrentObjective)
        {
            return;
        }

        LastPopupObjective = CurrentObjective;
        PlayerPrefs.SetInt(LastPopupObjectiveKey, (int)LastPopupObjective);
        PlayerPrefs.Save();
    }

    public void SaveProgress()
    {
        foreach (GameKey key in Enum.GetValues(typeof(GameKey)))
        {
            PlayerPrefs.SetInt(GetGameKeySaveKey(key), HasKey(key) ? 1 : 0);
        }

        foreach (LocationId location in Enum.GetValues(typeof(LocationId)))
        {
            PlayerPrefs.SetInt(GetVisitedLocationSaveKey(location), HasVisitedLocation(location) ? 1 : 0);
        }

        PlayerPrefs.SetInt(MemoryShardsKey, MemoryShards);
        PlayerPrefs.SetInt(CurrentObjectiveKey, (int)CurrentObjective);
        PlayerPrefs.SetInt(LastPopupObjectiveKey, (int)LastPopupObjective);
        PlayerPrefs.SetInt(TetrisRewardClaimedKey, TetrisRewardClaimed ? 1 : 0);
        PlayerPrefs.SetInt(SaveExistsKey, 1);
        PlayerPrefs.Save();
    }

    public void LoadProgress()
    {
        _unlockedKeys.Clear();
        _visitedLocations.Clear();

        foreach (GameKey key in Enum.GetValues(typeof(GameKey)))
        {
            if (PlayerPrefs.GetInt(GetGameKeySaveKey(key), 0) == 1)
            {
                _unlockedKeys.Add(key);
            }
        }

        foreach (LocationId location in Enum.GetValues(typeof(LocationId)))
        {
            if (PlayerPrefs.GetInt(GetVisitedLocationSaveKey(location), 0) == 1)
            {
                _visitedLocations.Add(location);
            }
        }

        MemoryShards = PlayerPrefs.GetInt(MemoryShardsKey, 0);
        TetrisRewardClaimed = PlayerPrefs.GetInt(TetrisRewardClaimedKey, 0) == 1;
        CurrentObjective = EvaluateObjective();
        LastPopupObjective = PlayerPrefs.HasKey(LastPopupObjectiveKey)
            ? (ObjectiveId)PlayerPrefs.GetInt(LastPopupObjectiveKey)
            : (ObjectiveId)(-1);
        PlayerPrefs.SetInt(CurrentObjectiveKey, (int)CurrentObjective);
        PlayerPrefs.Save();
    }

    public void ResetProgress()
    {
        _unlockedKeys.Clear();
        _visitedLocations.Clear();
        MemoryShards = 0;
        TetrisRewardClaimed = false;
        CurrentObjective = EvaluateObjective();
        LastPopupObjective = CurrentObjective;
        SaveProgress();
        NotifyProgressionChanged();
    }

    private void UpdateObjectiveAndSave()
    {
        CurrentObjective = EvaluateObjective();
        SaveProgress();
        NotifyProgressionChanged();
    }

    private ObjectiveId EvaluateObjective()
    {
        if (!HasKey(GameKey.DocumentsKey))
        {
            return ObjectiveId.ReviewDocuments;
        }

        if (!TetrisRewardClaimed)
        {
            return ObjectiveId.RecoverTreeMemory;
        }

        if (!HasKey(GameKey.RecycleBinKey))
        {
            return ObjectiveId.SearchRecycleBin;
        }

        if (!HasVisitedLocation(LocationId.TreeScene))
        {
            return ObjectiveId.InvestigateTree;
        }

        return ObjectiveId.AccessCore;
    }

    private void NotifyProgressionChanged()
    {
        ProgressionChanged?.Invoke();
    }

    private static string GetGameKeySaveKey(GameKey key)
    {
        return $"Progression.Key.{key}";
    }

    private static string GetVisitedLocationSaveKey(LocationId location)
    {
        return $"Progression.LocationVisited.{location}";
    }
}
