using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using RicKit.UI.Panels;

namespace RicKit.UI.Editor
{
    [InitializeOnLoad]
    public class UISceneView
    {
        private static Vector2 scrollPosition;
        private static string path = "Assets/Resources/UIPanels";
        private static List<MonoScript> scripts;
        private static GUIStyle dropAreaStyle;
        private static Rect windowRect = new Rect(0, 200, 300, 500);
        private static bool isExtraContentVisible;

        static UISceneView()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Initialize();
        }
        
        private static void Initialize()
        {
            scripts = GetAllScripts();
            dropAreaStyle = new GUIStyle
            {
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };
            path = EditorPrefs.GetString($"PanelCreatorEditorPath_{Application.identifier}", path);
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (isExtraContentVisible)
            {
                windowRect.width = 300;
                windowRect.height = 500;
            }
            else 
            {
                windowRect.width = 120;
                windowRect.height = 50;
            }
            windowRect = GUILayout.Window(0, windowRect, OnWindowGUI, "RicKit UI");
        }

        private static void OnWindowGUI(int id)
        {
            if (GUILayout.Button(isExtraContentVisible ? "Hide" : "Show"))
            {
                isExtraContentVisible = !isExtraContentVisible;
            }
            DisplayExtraContent();
            GUI.DragWindow();
            windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - windowRect.width);
            windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - windowRect.height);
        }

        private static void DisplayExtraContent()
        {
            if(!isExtraContentVisible) return;
            EditorGUILayout.LabelField("UI Panels Path:");
            GUI.enabled = false;
            EditorGUILayout.TextField(path);
            GUI.enabled = true;
            DropFolder();
            if (!Directory.Exists(path))
            {
                EditorGUILayout.HelpBox("Directory doesn't exist", MessageType.Error);
                return;
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (var script in scripts)
            {
                DrawScriptOption(script);
            }

            GUILayout.EndScrollView();
        }

        private static void DrawScriptOption(MonoScript script)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var type = script.GetClass();
                var assetPath = $"{path}/{type.Name}.prefab";
                GUILayout.Label(type.Name);

                var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));
                if (asset)
                {
                    if (GUILayout.Button("Open", GUILayout.Width(50)))
                    {
                        AssetDatabase.OpenAsset(asset);
                    }
                }
                else
                {
                    if (GUILayout.Button("Create", GUILayout.Width(50)))
                    {
                        CreateUIPanelPrefab(type, assetPath);
                    }
                }

                if (GUILayout.Button("Edit Script", GUILayout.Width(75)))
                {
                    AssetDatabase.OpenAsset(script);
                }
            }

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
        }

        private static void DropFolder()
        {
            var evt = Event.current;
            var dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop UI Prefab directory here", dropAreaStyle);

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (!dropArea.Contains(evt.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (var draggedObject in DragAndDrop.paths)
                    {
                        if (Directory.Exists(draggedObject))
                        {
                            path = draggedObject;
                            EditorPrefs.SetString($"PanelCreatorEditorPath_{Application.identifier}", path);
                            break;
                        }
                    }

                    Event.current.Use();
                }
            }
        }

        private static void CreateUIPanelPrefab(System.Type type, string assetPath)
        {
            var go = new GameObject(type.Name, typeof(RectTransform), type);
            var rect = go.GetComponent<RectTransform>();
            go.layer = LayerMask.NameToLayer("UI");
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)));
            Object.DestroyImmediate(go);
        }

        private static List<MonoScript> GetAllScripts()
        {
            var list = new List<MonoScript>();
            var guids = AssetDatabase.FindAssets("t:MonoScript");
            foreach (var guid in guids)
            {
                var guidToAssetPath = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(guidToAssetPath);
                var type = script.GetClass();
                if (type != null && type.IsSubclassOf(typeof(AbstractUIPanel)) && !type.IsAbstract)
                {
                    list.Add(script);
                }
            }
            list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            return list;
        }
    }
}