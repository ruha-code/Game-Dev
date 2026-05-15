using UnityEngine;
using UnityEngine.UIElements;
using System;

public class TaskbarController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private Label _clockLabel;
    private VisualElement _startButton;
    private VisualElement _startMenu;

    void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null) return;

        var root = _uiDocument.rootVisualElement;
        
        // Find elements
        _clockLabel = root.Q<Label>("tray-clock");
        _startButton = root.Q<VisualElement>("start-button");
        _startMenu = root.Q<VisualElement>("start-menu");

        // Set initial clock
        UpdateClock();

        // Start button interaction
        if (_startButton != null)
        {
            _startButton.RegisterCallback<ClickEvent>(OnStartClicked);
        }
    }

    void Update()
    {
        // Update clock every second
        UpdateClock();
    }

    private void UpdateClock()
    {
        if (_clockLabel != null)
        {
            _clockLabel.text = DateTime.Now.ToString("h:mm tt");
        }
    }

    private void OnStartClicked(ClickEvent evt)
    {
        if (_startMenu != null)
        {
            if (_startMenu.ClassListContains("hidden"))
            {
                _startMenu.RemoveFromClassList("hidden");
                _startMenu.style.display = DisplayStyle.Flex;
            }
            else
            {
                _startMenu.AddToClassList("hidden");
                _startMenu.style.display = DisplayStyle.None;
            }
        }
    }
}