using UnityEngine;
using UnityEngine.UIElements;

public class ControllerBase : MonoBehaviour
{
    protected static bool IsPointerOverUIToolkit(Vector2 screenPos)
    {
        var allPanels = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);

        foreach (var doc in allPanels)
        {
            if (doc?.rootVisualElement == null) continue;
            if (doc.rootVisualElement.style.display == DisplayStyle.None) continue;

            var panel = doc.rootVisualElement.panel;
            if (panel == null) continue;

            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel, screenPos);
            if (panel.Pick(panelPos) != null) return true;
        }
        return false;
    }

}
