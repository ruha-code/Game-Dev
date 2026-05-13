using UnityEngine;
using UnityEngine.UIElements;
using AeroOS.UI;
using System.Collections.Generic;

public class MainMenuController : MonoBehaviour
{
    private VisualElement root;
    private AeroActiveBackground activeBackground;
    private List<VisualElement> menuItems = new List<VisualElement>();

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;
        activeBackground = root.Q<AeroActiveBackground>();

        // Find all menu items
        var menuBox = root.Q<VisualElement>("menu");
        if (menuBox != null)
        {
            foreach (var item in menuBox.Children())
            {
                if (item.ClassListContains("menu-item"))
                {
                    menuItems.Add(item);
                    item.RegisterCallback<ClickEvent>(evt => OnMenuItemClicked(item));
                }
            }
        }

        // Example binding
        var quitBtn = root.Q<VisualElement>("quit-item");
        if (quitBtn != null)
        {
            quitBtn.RegisterCallback<ClickEvent>(evt => QuitGame());
        }
    }

    private void OnMenuItemClicked(VisualElement clickedItem)
    {
        // 1. Manage background
        if (activeBackground != null)
        {
            // Move background to be first child of the clicked item
            // Using Insert(0) to ensure it's behind the content
            clickedItem.Insert(0, activeBackground);
        }

        // 2. Manage items and chevrons
        foreach (var item in menuItems)
        {
            item.RemoveFromClassList("menu-item-active");
            
            var chevron = item.Q<VisualElement>(className: "menu-chevron");
            if (chevron != null) chevron.style.display = DisplayStyle.None;
        }

        clickedItem.AddToClassList("menu-item-active");
        
        var activeChevron = clickedItem.Q<VisualElement>(className: "menu-chevron");
        if (activeChevron != null) activeChevron.style.display = DisplayStyle.Flex;

        Debug.Log($"Menu item clicked: {clickedItem.name}");
    }

    private void QuitGame()
    {
        Debug.Log("Quitting game...");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
