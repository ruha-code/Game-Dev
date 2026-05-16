using UnityEngine;
using UnityEngine.UIElements;
using System;

public class DesktopUIController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _root;
    private VisualElement _startButton;
    private VisualElement _startMenu;
    private Label _clockLabel;
    private VisualElement _mainArea;
    private TetrisController _tetrisController;

    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null) return;

        _tetrisController = GetComponent<TetrisController>();

        _root = _uiDocument.rootVisualElement;

        // Query elements
        _startButton = _root.Q<VisualElement>("start-button");
        _startMenu = _root.Q<VisualElement>("start-menu");
        _clockLabel = _root.Q<Label>("tray-clock");
        _mainArea = _root.Q<VisualElement>("main-area");

        // Initialize Tetris
        if (_tetrisController != null)
        {
            _tetrisController.Initialize(_root);
        }

        // Register events
        if (_startButton != null)
        {
            _startButton.RegisterCallback<ClickEvent>(OnStartButtonClicked);
        }

        if (_mainArea != null)
        {
            _mainArea.RegisterCallback<ClickEvent>(OnMainAreaClicked);
        }

        // Initialize clock
        UpdateClock();
        InvokeRepeating(nameof(UpdateClock), 1f, 1f);

        // Setup desktop icons
        var icons = _root.Query<VisualElement>(className: "desktop-icon-wrapper").ToList();
        foreach (var icon in icons)
        {
            icon.RegisterCallback<ClickEvent>(evt => OnIconClicked(icon));
        }
    }

    private void UpdateClock()
    {
        if (_clockLabel != null)
        {
            _clockLabel.text = DateTime.Now.ToString("h:mm tt");
        }
    }

    private void OnStartButtonClicked(ClickEvent evt)
    {
        if (_startMenu != null)
        {
            bool isHidden = _startMenu.ClassListContains("hidden");
            if (isHidden)
            {
                _startMenu.RemoveFromClassList("hidden");
            }
            else
            {
                _startMenu.AddToClassList("hidden");
            }
            evt.StopPropagation();
        }
    }

    private void OnMainAreaClicked(ClickEvent evt)
    {
        // Close start menu when clicking background
        if (_startMenu != null && !_startMenu.ClassListContains("hidden"))
        {
            _startMenu.AddToClassList("hidden");
        }
    }

    private void OnIconClicked(VisualElement icon)
    {
        // Deselect others
        var allIcons = _root.Query<VisualElement>(className: "desktop-icon-wrapper").ToList();
        foreach (var other in allIcons)
        {
            other.RemoveFromClassList("desktop-icon-wrapper--selected");
        }

        var label = icon.Q<Label>(className: "desktop-icon-label");
        string iconName = label != null ? label.text : "Unknown Icon";
        Debug.Log($"Desktop Icon Clicked: {iconName}");
        
        // Add selected visual feedback
        icon.AddToClassList("desktop-icon-wrapper--selected");

        if (iconName == "Tetris")
        {
            if (_tetrisController != null)
            {
                _tetrisController.Show();
            }
        }
    }
    }