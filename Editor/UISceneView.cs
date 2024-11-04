using UnityEditor;
using UnityEngine;

namespace RicKit.UI.Editor
{
    [InitializeOnLoad]
    public class UISceneView
    {
        private static readonly int ControlID = GUIUtility.GetControlID(FocusType.Passive);
        private static Rect windowRect = new Rect(10, 10, 80, 70);
        private static Rect lastRect;
        private const string WindowRectKey = "RicKit.Editor.UISceneView.WindowRect";

        static UISceneView()
        {
            LoadWindowRect();
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
            if (GUILayout.Button("Editor"))
            {
                UIEditorWindow.Open();
            }
            if (GUILayout.Button("Settings"))
            {
                UISettingsInspector.Open();
            }
            GUI.DragWindow();
            SaveWindowRect();
        }

        private static void SaveWindowRect()
        {
            if (windowRect == lastRect) return;
            lastRect = windowRect;
            EditorPrefs.SetFloat(WindowRectKey + "_x", windowRect.x);
            EditorPrefs.SetFloat(WindowRectKey + "_y", windowRect.y);
        }

        private static void LoadWindowRect()
        {
            if (!EditorPrefs.HasKey(WindowRectKey + "_x")) return;
            windowRect.x = EditorPrefs.GetFloat(WindowRectKey + "_x");
            windowRect.y = EditorPrefs.GetFloat(WindowRectKey + "_y");
        }
    }
}