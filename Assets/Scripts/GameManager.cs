using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GameManager");
                _instance = go.AddComponent<GameManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Settings")]
    public float masterVolume = 1.0f;
    public bool isFirstRun = true;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSystems();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSystems()
    {
        _ = ProgressionManager.Instance;
        _ = AudioManager.Instance;
        Debug.Log("Game Systems Initialized");
        // Add global initialization logic here
    }

    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
