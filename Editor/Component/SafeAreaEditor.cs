using RicKit.UI.Component;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RicKit.UI.Editor.Component
{
    public class SafeAreaEditor : UnityEditor.Editor
    {
        [MenuItem("GameObject/UI/SafeArea(Mobile)", false, 0)]
        public static void CreateSafeArea()
        {
            var parent = Selection.activeTransform;
            if (!parent || !parent.GetComponent<RectTransform>())
            {
                var canvas = new GameObject("Canvas").AddComponent<Canvas>();
                canvas.transform.SetParent(parent);
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.gameObject.AddComponent<CanvasScaler>();
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                parent = canvas.transform;
            }
            var safeArea = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeArea));
            safeArea.transform.SetParent(parent);
            var rect = safeArea.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            Selection.activeGameObject = safeArea;
        }
    }
}