using System;
using System.Collections.Generic;
using System.IO;
using RicKit.UI.Panels;
using UnityEditor;
using UnityEngine;


namespace RicKit.UI.Editor
{
    public class UIEditorWindow : EditorWindow
    {
        private static string PathKey => $"PanelCreatorEditorPath_{Application.identifier}";
        private static string path = "Assets/Resources/UIPanels";
        private List<MonoScript> scripts;
        private GUIStyle dropAreaStyle;
        private Vector2 scrollPosition;

        private void OnEnable()
        {
            scripts = GetAllScripts();
            dropAreaStyle = new GUIStyle
            {
                normal =
                {
                    textColor = Color.white
                },
                alignment = TextAnchor.MiddleCenter
            };
            path = EditorPrefs.GetString(PathKey, path);
        }
        
        [MenuItem("RicKit/UI/UI Editor")]
        public static void Open()
        {
            var window = GetWindow<UIEditorWindow>("UI Editor", true,
                typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ProjectBrowser"));
            window.Show();
        }


        private void DropFolder()
        {
            var evt = Event.current;
            var dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop your UI prefab directory here", dropAreaStyle);
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (var draggedObject in DragAndDrop.paths)
                        {
                            if (Directory.Exists(draggedObject))
                            {
                                path = draggedObject;
                                EditorPrefs.SetString(PathKey, path);
                                break;
                            }
                        }
                    }

                    Event.current.Use();
                    break;
            }
        }

        private void OnGUI()
        {
            GUI.enabled = false;
            EditorGUILayout.TextField("Path:", path);
            GUI.enabled = true;
            DropFolder();
            if (!Directory.Exists(path))
            {
                EditorGUILayout.HelpBox("path doesn't exist", MessageType.Error);
                return;
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (var script in scripts)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var type = script.GetClass();
                    var assetPath = $"{path}/{type.Name}.prefab";
                    GUILayout.Label(type.Name);
                    var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));
                    if (asset)
                    {
                        if (GUILayout.Button("Open", GUILayout.Width(80)))
                        {
                            AssetDatabase.OpenAsset(asset);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Create", GUILayout.Width(80)))
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
                            DestroyImmediate(go);
                        }
                    }

                    if (GUILayout.Button("Edit Script", GUILayout.Width(80)))
                    {
                        AssetDatabase.OpenAsset(script);
                    }
                }

                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
            }

            GUILayout.EndScrollView();
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

            list.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            return list;
        }
    }
}