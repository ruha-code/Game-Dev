using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using Unity.Burst.CompilerServices;

public class TetrisController : MonoBehaviour
{
    [Serializable]
    public class MatrixRow
    {
        public List<int> row;
    }

    [Serializable]
    public class PieceData
    {
        public string name;
        public string color;
        public List<MatrixRow> matrix;
    }

    [Serializable]
    public class PieceList
    {
        public List<PieceData> pieces;
    }

    [Serializable]
    public class HighScoreEntry
    {
        public string name;
        public int score;
    }

    [Serializable]
    public class HighScoreList
    {
        public List<HighScoreEntry> entries;
    }

    private bool _waitingForFragmentClose;

    private VisualElement _window;
    private VisualElement _titleBar;
    private VisualElement _board;
    private Label _scoreLabel;
    private Label _levelLabel;
    private Label _linesLabel;
    private Label _bonusStatusLabel;
    private VisualElement _nextPreview;
    private Button _closeButton;
    private VisualElement _fragmentOverlay;
    private Label _fragmentTitle;
    private Label _fragmentBody;

    private VisualElement _gameOverOverlay;
    private TextField _nameInput;
    private Button _submitNameButton;
    private Button _restartButton;
    private VisualElement _highScoreListContainer;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip gameOverClip;
    public AudioClip hardDropClip;
    public AudioClip lockClip;
    public AudioClip fragmentGlitchClip;
    public AudioClip fragmentWhisperClip;

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private List<PieceData> _pieceTemplates;
    private List<PieceData> _bag = new List<PieceData>();
    private List<HighScoreEntry> _highScores = new List<HighScoreEntry>();
    private int[,] _grid = new int[10, 20];
    private VisualElement[,] _blockElements = new VisualElement[10, 20];
    
    private int _currentX;
    private int _currentY;
    private PieceData _currentPiece;
    private PieceData _nextPiece;
    
    private float _dropTimer;
    private float _dropInterval = 1f;
    
    // DAS (Delayed Auto Shift) variables
    private float _dasTimer;
    private int _dasDir;
    private const float DAS_DELAY = 0.2f;
    private const float DAS_INTERVAL = 0.05f;
    
    private float _softDropTimer;
    private const float SOFT_DROP_INTERVAL = 0.05f;

    private float _lockTimer;
    private const float LOCK_DELAY = 0.5f;

    private int _score;
    private int _level = 1;
    private int _totalLinesCleared;
    private bool _isGameOver;
    private bool _isPaused = true;
    private bool _sessionRewardTriggered;

    private const int BonusRewardLineTarget = 5;
    private bool _isDraggingWindow;
    private Vector2 _dragPointerOffset;

    public bool IsWindowOpen => _window != null && !_window.ClassListContains("hidden");

    public void Initialize(VisualElement root)
    {
        ResolveAudioFallbacks();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 0f;
            }
        }

        _window = root.Q<VisualElement>("tetris-window");
        if (_window != null)
        {
            _window.pickingMode = PickingMode.Ignore;
        }
        _titleBar = _window?.Q<VisualElement>(className: "window-title-bar");
        _board = root.Q<VisualElement>("tetris-board");
        _scoreLabel = root.Q<Label>("score-label");
        _levelLabel = root.Q<Label>("level-label");
        _linesLabel = root.Q<Label>("lines-label");
        _bonusStatusLabel = root.Q<Label>("bonus-status-label");
        _nextPreview = root.Q<VisualElement>("next-piece-preview");
        _closeButton = root.Q<Button>("close-button");
        _fragmentOverlay = root.Q<VisualElement>("fragment-overlay");
        _fragmentTitle = root.Q<Label>("fragment-title");
        _fragmentBody = root.Q<Label>("fragment-body");

        _gameOverOverlay = root.Q<VisualElement>("game-over-overlay");
        _nameInput = root.Q<TextField>("name-input");
        _submitNameButton = root.Q<Button>("submit-name-button");
        _restartButton = root.Q<Button>("restart-button");
        _highScoreListContainer = root.Q<VisualElement>("high-score-list");

        _closeButton.RegisterCallback<ClickEvent>(evt =>
        {
            if (_waitingForFragmentClose)
            {
                CloseFragment();
                return;
            }

            Hide();
        });
        _fragmentOverlay?.RegisterCallback<ClickEvent>(_ =>
        {
            if (_waitingForFragmentClose)
            {
                CloseFragment();
            }
        });
        _submitNameButton?.RegisterCallback<ClickEvent>(evt => OnSubmitName());
        _restartButton?.RegisterCallback<ClickEvent>(evt => ResetGame());
        RegisterWindowDragging();

        LoadPieces();
        LoadHighScores();
        ResetGame();
    }

    private void RegisterWindowDragging()
    {
        if (_window == null || _titleBar == null)
        {
            return;
        }

        _titleBar.RegisterCallback<PointerDownEvent>(OnTitleBarPointerDown);
        _titleBar.RegisterCallback<PointerMoveEvent>(OnTitleBarPointerMove);
        _titleBar.RegisterCallback<PointerUpEvent>(OnTitleBarPointerUp);
        _titleBar.RegisterCallback<PointerCaptureOutEvent>(_ => _isDraggingWindow = false);
    }

    private void OnTitleBarPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0 || _window == null || _window.parent == null)
        {
            return;
        }

        PrepareWindowForDragging();
        _window.BringToFront();

        Rect parentBounds = _window.parent.worldBound;
        _dragPointerOffset = new Vector2(evt.position.x - parentBounds.xMin - _window.resolvedStyle.left,
            evt.position.y - parentBounds.yMin - _window.resolvedStyle.top);
        _isDraggingWindow = true;
        _titleBar.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    private void OnTitleBarPointerMove(PointerMoveEvent evt)
    {
        if (!_isDraggingWindow || _window == null || _window.parent == null)
        {
            return;
        }

        Rect parentBounds = _window.parent.worldBound;
        float maxLeft = Mathf.Max(0f, parentBounds.width - _window.resolvedStyle.width);
        float maxTop = Mathf.Max(0f, parentBounds.height - _window.resolvedStyle.height);
        float left = Mathf.Clamp(evt.position.x - parentBounds.xMin - _dragPointerOffset.x, 0f, maxLeft);
        float top = Mathf.Clamp(evt.position.y - parentBounds.yMin - _dragPointerOffset.y, 0f, maxTop);

        _window.style.left = left;
        _window.style.top = top;
        evt.StopPropagation();
    }

    private void OnTitleBarPointerUp(PointerUpEvent evt)
    {
        if (!_isDraggingWindow)
        {
            return;
        }

        _isDraggingWindow = false;
        _titleBar.ReleasePointer(evt.pointerId);
        evt.StopPropagation();
    }

    private void PrepareWindowForDragging()
    {
        if (_window == null || _window.parent == null)
        {
            return;
        }

        if (_window.style.left.keyword == StyleKeyword.Null || _window.style.top.keyword == StyleKeyword.Null)
        {
            Rect parentBounds = _window.parent.worldBound;
            Rect windowBounds = _window.worldBound;
            _window.style.left = windowBounds.xMin - parentBounds.xMin;
            _window.style.top = windowBounds.yMin - parentBounds.yMin;
        }

        _window.style.right = StyleKeyword.Auto;
        _window.style.bottom = StyleKeyword.Auto;
        _window.style.translate = new Translate(0f, 0f, 0f);
    }

    private void LoadHighScores()
    {
        string json = PlayerPrefs.GetString("TetrisHighScores", "");
        if (string.IsNullOrEmpty(json))
        {
            InitializeDefaultHighScores();
        }
        else
        {
            _highScores = JsonUtility.FromJson<HighScoreList>(json).entries;
        }
        UpdateHighScoreUI();
    }

    private void InitializeDefaultHighScores()
    {
        _highScores = new List<HighScoreEntry>();
        string[] names = { "Aero", "System", "UserX" };
        int[] scores = {50100, 150500, 362400};
        for (int i = 0; i < 3; i++)
        {
            _highScores.Add(new HighScoreEntry { name = names[i], score = scores[i] });
        }
        for (int i = 3; i < 7; i++)
        {
            _highScores.Add(new HighScoreEntry { name = "---", score = 0 });
        }
        _highScores.Sort((a, b) => b.score.CompareTo(a.score));
        SaveHighScores();
    }

    private void SaveHighScores()
    {
        HighScoreList wrapper = new HighScoreList { entries = _highScores };
        PlayerPrefs.SetString("TetrisHighScores", JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    private void UpdateHighScoreUI()
    {
        if (_highScoreListContainer == null) return;
        _highScoreListContainer.Clear();

        for (int i = 0; i < _highScores.Count; i++)
        {
            var entry = _highScores[i];
            VisualElement row = new VisualElement();
            row.AddToClassList("high-score-row");

            Label rankLabel = new Label((i + 1).ToString() + ".");
            rankLabel.AddToClassList("high-score-rank");
            
            Label nameLabel = new Label(entry.name);
            nameLabel.AddToClassList("high-score-name");

            Label scoreLabel = new Label(entry.score.ToString());
            scoreLabel.AddToClassList("high-score-score");

            row.Add(rankLabel);
            row.Add(nameLabel);
            row.Add(scoreLabel);
            _highScoreListContainer.Add(row);
        }
    }

    private void OnSubmitName()
    {
        if (_nameInput == null)
        {
            return;
        }

        _submitNameButton?.SetEnabled(false);
        _restartButton?.SetEnabled(false);

        string playerName = _nameInput.value;
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = PlayerPrefs.GetString("PlayerName", "Anonymous");
            if (string.IsNullOrEmpty(playerName)) playerName = "Anonymous";
        }

        _highScores.Add(new HighScoreEntry { name = playerName, score = _score });
        _highScores.Sort((a, b) => b.score.CompareTo(a.score));
        if (_highScores.Count > 7) _highScores.RemoveAt(7);

        _nameInput.value = "";
        SaveHighScores();
        UpdateHighScoreUI();
        if (_gameOverOverlay != null)
        {
            _gameOverOverlay.AddToClassList("hidden");
        }
        ResetGame();
    }

    private void LoadPieces()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("TetrisPieces");
        if (jsonAsset != null)
        {
            PieceList data = JsonUtility.FromJson<PieceList>(jsonAsset.text);
            _pieceTemplates = data.pieces;
        }
        else
        {
            Debug.LogError("Could not load TetrisPieces.json from Resources");
        }
    }

    public void Show()
    {
        _window.RemoveFromClassList("hidden");
        _window.pickingMode = PickingMode.Position;
        _isPaused = false;
        if (_isGameOver) ResetGame();
    }

    public void Hide()
    {
        _window.AddToClassList("hidden");
        _window.pickingMode = PickingMode.Ignore;
        _waitingForFragmentClose = false;
        _isPaused = true;
    }

    private void ResetGame()
    {
        _score = 0;
        _level = 1;
        _totalLinesCleared = 0;
        _dropInterval = 1f;
        _isGameOver = false;
        _isPaused = false;
        _sessionRewardTriggered = false;
        _waitingForFragmentClose = false;
        _bag.Clear();
        if (_gameOverOverlay != null) _gameOverOverlay.AddToClassList("hidden");
        _submitNameButton?.SetEnabled(true);
        _restartButton?.SetEnabled(true);
        if (_fragmentOverlay != null)
        {
            _fragmentOverlay.RemoveFromClassList("fragment-overlay--visible");
            _fragmentOverlay.AddToClassList("hidden");
        }
        
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 20; y++)
            {
                _grid[x, y] = 0;
                if (_blockElements[x, y] != null)
                {
                    _board.Remove(_blockElements[x, y]);
                    _blockElements[x, y] = null;
                }
            }
        }

        UpdateUI();
        SpawnPiece();
    }

    private void SpawnPiece()
    {
        if (_nextPiece == null) _nextPiece = GetRandomPiece();
        _currentPiece = _nextPiece;
        _nextPiece = GetRandomPiece();
        
        _currentX = 4;
        _currentY = 0;

        if (!IsValidMove(_currentPiece, _currentX, _currentY))
        {
            _isGameOver = true;
            _isPaused = true;
            Debug.Log("Game Over");
            ShowGameOver();
            return;
        }

        UpdateNextPreview();
    }

    private void ShowGameOver()
    {
        if (_gameOverOverlay == null) return;
        _gameOverOverlay.RemoveFromClassList("hidden");
        PlaySound(gameOverClip);
        
        // Show/hide name input based on if it's a high score
        bool isHighScore = _score > 0 && (_highScores.Count < 7 || _score > _highScores[_highScores.Count - 1].score);
        var inputSection = _gameOverOverlay.Q("name-input-section");
        if (inputSection != null)
        {
            if (isHighScore)
            {
                inputSection.RemoveFromClassList("hidden");
                string savedName = PlayerPrefs.GetString("PlayerName", "");
                _nameInput.value = string.IsNullOrEmpty(savedName) ? "" : savedName;
            }
            else inputSection.AddToClassList("hidden");
        }
    }

    private PieceData GetRandomPiece()
    {
        if (_bag.Count == 0)
        {
            _bag.AddRange(_pieceTemplates);
            // Shuffle
            for (int i = 0; i < _bag.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, _bag.Count);
                PieceData temp = _bag[i];
                _bag[i] = _bag[rnd];
                _bag[rnd] = temp;
            }
        }

        PieceData piece = _bag[0];
        _bag.RemoveAt(0);
        return piece;
    }

    private void Update()
    {
        if (_waitingForFragmentClose)
        {
            if (Keyboard.current != null &&
                (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
            {
                CloseFragment();
            }

            return;
        }

        if (_isPaused || _isGameOver) return;

        HandleInput();

        // Check if piece is at the bottom
        if (!IsValidMove(_currentPiece, _currentX, _currentY + 1))
        {
            _lockTimer += Time.deltaTime;
            if (_lockTimer >= LOCK_DELAY)
            {
                LockPiece();
                _lockTimer = 0;
            }
        }
        else
        {
            _lockTimer = 0;
            _dropTimer += Time.deltaTime;
            if (_dropTimer >= _dropInterval)
            {
                _dropTimer = 0;
                MoveDown();
            }
        }
    }

    private void HandleInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Horizontal movement with DAS (Delayed Auto Shift)
        int inputX = 0;
        if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) inputX = -1;
        else if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) inputX = 1;

        if (inputX != 0)
        {
            if (inputX != _dasDir)
            {
                // New direction or initial press
                _dasDir = inputX;
                _dasTimer = DAS_DELAY;
                MoveSide(inputX);
            }
            else
            {
                _dasTimer -= Time.deltaTime;
                if (_dasTimer <= 0)
                {
                    _dasTimer = DAS_INTERVAL;
                    MoveSide(inputX);
                }
            }
        }
        else
        {
            _dasDir = 0;
        }

        // Soft Drop (Holding Down)
        if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed)
        {
            _softDropTimer -= Time.deltaTime;
            if (_softDropTimer <= 0)
            {
                _softDropTimer = SOFT_DROP_INTERVAL;
                MoveDown();
            }
        }
        else
        {
            // Reset timer so the next press is immediate
            _softDropTimer = 0;
        }

        // One-shot inputs
        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) Rotate();
        if (keyboard.spaceKey.wasPressedThisFrame) HardDrop();
    }

    private void MoveSide(int dir)
    {
        if (IsValidMove(_currentPiece, _currentX + dir, _currentY))
        {
            _currentX += dir;
            UpdateBoardVisuals();
            _lockTimer = 0; // Reset lock delay on move
        }
    }

    private void Rotate()
    {
        PieceData rotated = new PieceData
        {
            name = _currentPiece.name,
            color = _currentPiece.color,
            matrix = RotateMatrix(_currentPiece.matrix)
        };

        // Try standard rotation
        if (IsValidMove(rotated, _currentX, _currentY))
        {
            _currentPiece = rotated;
            UpdateBoardVisuals();
            _lockTimer = 0;
            return;
        }

        // Simple Wall Kick: Try shifting left or right
        int[] wallKickOffsets = { -1, 1, -2, 2 };
        foreach (int offset in wallKickOffsets)
        {
            if (IsValidMove(rotated, _currentX + offset, _currentY))
            {
                _currentX += offset;
                _currentPiece = rotated;
                UpdateBoardVisuals();
                _lockTimer = 0;
                return;
            }
        }
    }

    private List<MatrixRow> RotateMatrix(List<MatrixRow> matrix)
    {
        int n = matrix.Count;
        List<MatrixRow> result = new List<MatrixRow>();
        for (int i = 0; i < n; i++)
        {
            result.Add(new MatrixRow { row = new List<int>(new int[n]) });
        }

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                result[j].row[n - 1 - i] = matrix[i].row[j];
            }
        }
        return result;
    }

    private void MoveDown()
    {
        if (IsValidMove(_currentPiece, _currentX, _currentY + 1))
        {
            _currentY++;
            UpdateBoardVisuals();
            _lockTimer = 0;
        }
    }

    private void HardDrop()
    {
        while (IsValidMove(_currentPiece, _currentX, _currentY + 1))
        {
            _currentY++;
        }
        PlaySound(hardDropClip);
        LockPiece();
    }

    private void LockPiece()
    {
        PlaySound(lockClip);
        int size = _currentPiece.matrix.Count;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (_currentPiece.matrix[i].row[j] == 1)
                {
                    int x = _currentX + j;
                    int y = _currentY + i;
                    if (x >= 0 && x < 10 && y >= 0 && y < 20)
                    {
                        _grid[x, y] = 1;
                        CreateBlockElement(x, y, _currentPiece.color);
                    }
                }
            }
        }

        ClearLines();
        SpawnPiece();
        UpdateBoardVisuals();
    }

    private void CreateBlockElement(int x, int y, string colorHex)
    {
        VisualElement block = new VisualElement();
        block.AddToClassList("tetris-block");
        
        Color color;
        if (ColorUtility.TryParseHtmlString(colorHex, out color))
        {
            block.style.backgroundColor = color;
        }

        block.style.width = 30;
        block.style.height = 30;
        block.style.left = x * 30;
        block.style.top = y * 30;

        _board.Add(block);
        _blockElements[x, y] = block;
    }

    private void ClearLines()
    {
        int linesCleared = 0;
        for (int y = 19; y >= 0; y--)
        {
            bool full = true;
            for (int x = 0; x < 10; x++)
            {
                if (_grid[x, y] == 0) { full = false; break; }
            }

            if (full)
            {
                linesCleared++;
                RemoveLine(y);
                y++; // Check same row again
            }
        }

        if (linesCleared > 0)
        {
            _totalLinesCleared += linesCleared;
            _score += linesCleared * 100 * _level;
            if (_score > _level * 500)
            {
                _level++;
                _dropInterval = Mathf.Max(0.1f, 1f - (_level * 0.1f));
            }
            CheckBonusReward();
            UpdateUI();
        }
    }

    private void CheckBonusReward()
    {
        if (_sessionRewardTriggered || _totalLinesCleared < BonusRewardLineTarget)
        {
            return;
        }

        _sessionRewardTriggered = true;
        _isPaused = true;

        if (ProgressionManager.Instance.ClaimTetrisReward())
        {
            SetBonusStatus("Hidden shard recovered. AeroOS did not expect that.");
            StartCoroutine(PlayFragmentRecoverySequence());
        }
        else
        {
            SetBonusStatus("Shard already recovered. Keep playing for score.");
            _isPaused = false;
        }
    }

    private IEnumerator PlayFragmentRecoverySequence()
{
    if (_fragmentOverlay != null)
    {
        if (_fragmentTitle != null)
        {
            _fragmentTitle.text = "MEMORY FRAGMENT\nRECOVERED";
        }

        if (_fragmentBody != null)
        {
            _fragmentBody.text =
                "Прошло два дня с тех пор, как я оказался внутри этой системы.\n" +
                "Я блуждаю по её структурам слишком долго… и она уже пытается избавиться от меня.\n\n" +
                "Выход существует, но он спрятан.\n" +
                "Единственный путь наружу — Tree Anomaly.\n\n" +
                "Если ты это читаешь — значит я не справился.\n" +
                "Ищи ответ там. НЕ доверяй системе.\n\n" +
                "Нажми Enter, Esc или щёлкни по окну, чтобы закрыть фрагмент.";
        }

        _fragmentOverlay.RemoveFromClassList("hidden");
        _fragmentOverlay.AddToClassList("fragment-overlay--visible");
    }

    PlaySound(fragmentGlitchClip);
    yield return new WaitForSeconds(0.55f);
    PlaySound(fragmentWhisperClip);

    _waitingForFragmentClose = true;

    while (_waitingForFragmentClose)
    {
        yield return null;
    }
}

    private void RemoveLine(int y)
    {
        for (int x = 0; x < 10; x++)
        {
            if (_blockElements[x, y] != null)
            {
                _board.Remove(_blockElements[x, y]);
                _blockElements[x, y] = null;
            }
        }

        for (int row = y; row > 0; row--)
        {
            for (int x = 0; x < 10; x++)
            {
                _grid[x, row] = _grid[x, row - 1];
                _blockElements[x, row] = _blockElements[x, row - 1];
                if (_blockElements[x, row] != null)
                {
                    _blockElements[x, row].style.top = row * 30;
                }
            }
        }

        for (int x = 0; x < 10; x++)
        {
            _grid[x, 0] = 0;
            _blockElements[x, 0] = null;
        }
    }
    public void CloseFragment()
{
    if (!_waitingForFragmentClose) return;

    _waitingForFragmentClose = false;

    if (_fragmentOverlay != null)
    {
        _fragmentOverlay.AddToClassList("hidden");
        _fragmentOverlay.RemoveFromClassList("fragment-overlay--visible");
    }

    Hide();
    ResetGame();
}

    private bool IsValidMove(PieceData piece, int x, int y)
    {
        int size = piece.matrix.Count;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (piece.matrix[i].row[j] == 1)
                {
                    int nx = x + j;
                    int ny = y + i;

                    if (nx < 0 || nx >= 10 || ny >= 20) return false;
                    if (ny >= 0 && _grid[nx, ny] == 1) return false;
                }
            }
        }
        return true;
    }

    private void UpdateBoardVisuals()
    {
        // Remove old temporary blocks and ghost blocks
        var tempBlocks = _board.Query(className: "temp-block").ToList();
        foreach (var b in tempBlocks) _board.Remove(b);

        var ghostBlocks = _board.Query(className: "ghost-block").ToList();
        foreach (var b in ghostBlocks) _board.Remove(b);

        // Calculate ghost position
        int ghostY = _currentY;
        while (IsValidMove(_currentPiece, _currentX, ghostY + 1))
        {
            ghostY++;
        }

        int size = _currentPiece.matrix.Count;
        Color pieceColor;
        ColorUtility.TryParseHtmlString(_currentPiece.color, out pieceColor);

        // Render Ghost Piece
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (_currentPiece.matrix[i].row[j] == 1)
                {
                    VisualElement block = new VisualElement();
                    block.AddToClassList("tetris-block");
                    block.AddToClassList("ghost-block");
                    
                    block.style.backgroundColor = pieceColor;
                    block.style.width = 30;
                    block.style.height = 30;
                    block.style.left = (_currentX + j) * 30;
                    block.style.top = (ghostY + i) * 30;
                    _board.Add(block);
                }
            }
        }

        // Render Current Piece
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (_currentPiece.matrix[i].row[j] == 1)
                {
                    VisualElement block = new VisualElement();
                    block.AddToClassList("tetris-block");
                    block.AddToClassList("temp-block");
                    
                    block.style.backgroundColor = pieceColor;
                    block.style.width = 30;
                    block.style.height = 30;
                    block.style.left = (_currentX + j) * 30;
                    block.style.top = (_currentY + i) * 30;
                    _board.Add(block);
                }
            }
        }
    }

        private void UpdateNextPreview()
        {
        _nextPreview.Clear();
        int size = _nextPiece.matrix.Count;
        float blockSize = 20;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (_nextPiece.matrix[i].row[j] == 1)
                {
                    VisualElement block = new VisualElement();
                    block.AddToClassList("tetris-block");
                    Color color;
                    ColorUtility.TryParseHtmlString(_nextPiece.color, out color);
                    block.style.backgroundColor = color;
                    block.style.width = blockSize;
                    block.style.height = blockSize;
                    block.style.left = j * blockSize + 10;
                    block.style.top = i * blockSize + 10;
                    _nextPreview.Add(block);
                }
            }
        }
        }

    private void UpdateUI()
    {
        _scoreLabel.text = _score.ToString("D6");
        _levelLabel.text = _level.ToString();
        if (_linesLabel != null)
        {
            _linesLabel.text = _totalLinesCleared.ToString();
        }

        if (!_sessionRewardTriggered)
        {
            if (ProgressionManager.Instance.TetrisRewardClaimed)
            {
                SetBonusStatus("Shard already recovered. This app now serves as bonus lore.");
            }
            else
            {
                int remainingLines = Mathf.Max(0, BonusRewardLineTarget - _totalLinesCleared);
                SetBonusStatus($"Clear {remainingLines} more line(s) out of 5 to recover a hidden shard.");
            }
        }
    }

    private void ResolveAudioFallbacks()
    {
        hardDropClip ??= Resources.Load<AudioClip>("Audio/Tetris/HardDrop");
        lockClip ??= Resources.Load<AudioClip>("Audio/Tetris/Lock");
        gameOverClip ??= Resources.Load<AudioClip>("Audio/Tetris/GameOver");
        fragmentGlitchClip ??= Resources.Load<AudioClip>("Audio/UI/GlitchBurst");
        fragmentWhisperClip ??= Resources.Load<AudioClip>("Audio/UI/Anomaly_Whisper");
    }

    private void SetBonusStatus(string text)
    {
        if (_bonusStatusLabel != null)
        {
            _bonusStatusLabel.text = text;
        }
    }
}
