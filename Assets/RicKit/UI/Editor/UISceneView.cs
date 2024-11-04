using UnityEditor;
using UnityEngine;

namespace RicKit.UI.Editor
{
    [InitializeOnLoad]
    public class UISceneView
    {
        private static readonly int ControlID = GUIUtility.GetControlID(FocusType.Passive);
        private static bool isShow;
        private static Rect windowRect = new Rect(10, 10, 100, 70);
        private static Rect lastRect;
        private const string WindowRectKey = "RicKit.UI.Editor.UISceneView.WindowRect";
        static UISceneView()
        {
            LoadPrefs();
            if(!isShow) return;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        [MenuItem("RicKit/UI/Toggle Scene View", false, 2)]
        private static void Toggle()
        {
            isShow = !isShow;
            SceneView.duringSceneGui -= OnSceneGUI;
            if (!isShow) return;
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        private static void OnSceneGUI(SceneView sceneView)
        {
            windowRect = GUI.Window(ControlID, windowRect, OnWindow, "RicKit UI");
            windowRect.x = Mathf.Clamp(windowRect.x, 0, sceneView.position.width - windowRect.width);
            windowRect.y = Mathf.Clamp(windowRect.y, 0, sceneView.position.height - windowRect.height);
        }

        private static void OnWindow(int id)
        {
            if (GUILayout.Button("Open Editor"))
            {
                UIEditorWindow.Open();
            }
            if (GUILayout.Button("Open Settings"))
            {
                UISettingsInspector.Open();
            }
            GUI.DragWindow();
            SavePrefs();
        }

        private static void SavePrefs()
        {
            if (windowRect == lastRect) return;
            lastRect = windowRect;
            EditorPrefs.SetFloat(WindowRectKey + "_x", windowRect.x);
            EditorPrefs.SetFloat(WindowRectKey + "_y", windowRect.y);
            EditorPrefs.SetBool(WindowRectKey + "_show", isShow);
        }

        private static void LoadPrefs()
        {
            windowRect.x = EditorPrefs.GetFloat(WindowRectKey + "_x", 10);
            windowRect.y = EditorPrefs.GetFloat(WindowRectKey + "_y", 20);
            isShow = EditorPrefs.GetBool(WindowRectKey + "_show", true);
            lastRect = windowRect;
        }
    }
}