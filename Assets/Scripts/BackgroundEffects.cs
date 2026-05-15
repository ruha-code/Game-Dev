using UnityEngine;
using UnityEngine.UIElements;

public class BackgroundEffects : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private VisualElement background;
    private VisualElement root;

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        background = root.Q<VisualElement>("background");
    }

    private void Update()
    {
        // Suble Parallax
        if (background != null)
        {
            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            float moveX = (mousePos.x / Screen.width - 0.5f) * -20f;
            float moveY = (mousePos.y / Screen.height - 0.5f) * -20f;
            
            background.style.translate = new Translate(moveX, moveY, 0);
        }
    }
}
