using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;

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

    private VisualElement _window;
    private VisualElement _board;
    private Label _scoreLabel;
    private Label _levelLabel;
    private VisualElement _nextPreview;
    private Button _closeButton;

    private List<PieceData> _pieceTemplates;
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

    private int _score;
private int _level = 1;
    private bool _isGameOver;
    private bool _isPaused = true;

    public void Initialize(VisualElement root)
    {
        _window = root.Q<VisualElement>("tetris-window");
        _board = root.Q<VisualElement>("tetris-board");
        _scoreLabel = root.Q<Label>("score-label");
        _levelLabel = root.Q<Label>("level-label");
        _nextPreview = root.Q<VisualElement>("next-piece-preview");
        _closeButton = root.Q<Button>("close-button");

        _closeButton.RegisterCallback<ClickEvent>(evt => Hide());

        LoadPieces();
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
        _isPaused = false;
        if (_isGameOver) ResetGame();
    }

    public void Hide()
    {
        _window.AddToClassList("hidden");
        _isPaused = true;
    }

    private void ResetGame()
    {
        _score = 0;
        _level = 1;
        _dropInterval = 1f;
        _isGameOver = false;
        
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
            return;
        }

        UpdateNextPreview();
    }

    private PieceData GetRandomPiece()
    {
        return _pieceTemplates[UnityEngine.Random.Range(0, _pieceTemplates.Count)];
    }

    private void Update()
    {
        if (_isPaused || _isGameOver) return;

        HandleInput();

        _dropTimer += Time.deltaTime;
        if (_dropTimer >= _dropInterval)
        {
            _dropTimer = 0;
            MoveDown();
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

        if (IsValidMove(rotated, _currentX, _currentY))
        {
            _currentPiece = rotated;
            UpdateBoardVisuals();
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
        }
        else
        {
            LockPiece();
        }
    }

    private void HardDrop()
    {
        while (IsValidMove(_currentPiece, _currentX, _currentY + 1))
        {
            _currentY++;
        }
        LockPiece();
    }

    private void LockPiece()
    {
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
            _score += linesCleared * 100 * _level;
            if (_score > _level * 500)
            {
                _level++;
                _dropInterval = Mathf.Max(0.1f, 1f - (_level * 0.1f));
            }
            UpdateUI();
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
        // Ghost current piece? Nah, let's keep it simple.
        // We only need to render the current piece on top.
        // I'll use a separate container or just dynamic elements.
        
        // Let's remove old temporary blocks
        var tempBlocks = _board.Query(className: "temp-block").ToList();
        foreach (var b in tempBlocks) _board.Remove(b);

        int size = _currentPiece.matrix.Count;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (_currentPiece.matrix[i].row[j] == 1)
                {
                    VisualElement block = new VisualElement();
                    block.AddToClassList("tetris-block");
                    block.AddToClassList("temp-block");
                    
                    Color color;
                    ColorUtility.TryParseHtmlString(_currentPiece.color, out color);
                    block.style.backgroundColor = color;

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
    }
}