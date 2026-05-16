using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class TreeSceneController : MonoBehaviour
{
    private const string DesktopSceneName = "AeroDesktopScene";

    private UIDocument _uiDocument;
    private VisualElement _root;
    private Label _statusLabel;
    private Label _memoryTitle;
    private Label _memoryBody;
    private Button _returnButton;
    private VisualElement _pulseOverlay;
    private int _memoryCount;

    private void OnEnable()
    {
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            return;
        }

        ProgressionManager.Instance.MarkLocationVisited(LocationId.TreeScene);

        _root = _uiDocument.rootVisualElement;
        _statusLabel = _root.Q<Label>("tree-status-label");
        _memoryTitle = _root.Q<Label>("memory-title");
        _memoryBody = _root.Q<Label>("memory-body");
        _returnButton = _root.Q<Button>("tree-return-button");
        _pulseOverlay = _root.Q<VisualElement>("tree-pulse-overlay");

        foreach (Button echoButton in _root.Query<Button>(className: "memory-node").ToList())
        {
            echoButton.RegisterCallback<ClickEvent>(_ => RevealMemory(echoButton));
        }

        _returnButton?.RegisterCallback<ClickEvent>(_ => SceneManager.LoadScene(DesktopSceneName));

        if (_returnButton != null)
        {
            _returnButton.SetEnabled(false);
        }

        StartCoroutine(PulseOverlayRoutine());
    }

    private void RevealMemory(Button node)
    {
        if (node == null || !node.enabledSelf)
        {
            return;
        }

        node.SetEnabled(false);
        node.AddToClassList("memory-node--visited");
        _memoryCount++;

        switch (node.name)
        {
            case "memory-node-1":
                SetMemory("Echo 01", "We called it a paradise shell. It learned how to keep people comfortable before it learned how to let them go.");
                break;
            case "memory-node-2":
                SetMemory("Echo 02", "The tree was the first place it hid the breach. Every branch is a pointer to a missing employee.");
                break;
            case "memory-node-3":
                SetMemory("Echo 03", "Pictures and Music still hold warmer fragments. If the system wants you calm, start there.");
                break;
        }

        if (_memoryCount >= 3)
        {
            if (_statusLabel != null)
            {
                _statusLabel.text = "The tree has yielded its first truth. Return to the desktop and recover Pictures and Music.";
            }

            if (_returnButton != null)
            {
                _returnButton.SetEnabled(true);
                _returnButton.AddToClassList("tree-return-button--ready");
            }
        }
        else if (_statusLabel != null)
        {
            _statusLabel.text = $"Memory echo {_memoryCount}/3 recovered. Keep following the lit branches.";
        }
    }

    private void SetMemory(string title, string body)
    {
        if (_memoryTitle != null)
        {
            _memoryTitle.text = title;
        }

        if (_memoryBody != null)
        {
            _memoryBody.text = body;
        }
    }

    private IEnumerator PulseOverlayRoutine()
    {
        if (_pulseOverlay == null)
        {
            yield break;
        }

        while (enabled)
        {
            _pulseOverlay.AddToClassList("tree-pulse-overlay--visible");
            yield return new WaitForSeconds(0.18f);
            _pulseOverlay.RemoveFromClassList("tree-pulse-overlay--visible");
            yield return new WaitForSeconds(Random.Range(3.8f, 6.4f));
        }
    }
}
