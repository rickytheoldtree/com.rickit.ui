using System;
using System.Collections.Generic;
using System.IO;
using RicKit.UI.Panels;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


namespace RicKit.UI.Editor
{
    public class UIEditorWindow : EditorWindow
    {
        private static string PathKey => $"PanelCreatorEditorPath_{Application.identifier}";
        private static string path = "Assets/Resources/UIPanels";
        private List<MonoScript> scripts;
        private Dictionary<MonoScript, Object> scriptToAssetMap = new Dictionary<MonoScript, Object>();
        private GUIStyle dropAreaStyle;
        private Vector2 scrollPosition;

        private void OnEnable()
        {
            UpdateMap();
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
        
        [MenuItem("RicKit/UI/Open Editor", false, 0)]
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
            GUI.Box(dropArea, "Drop your default UI directory here", dropAreaStyle);
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
            DropFolder();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Default UI Path:", path);
            }
            if (!Directory.Exists(path))
            {
                EditorGUILayout.HelpBox("path doesn't exist", MessageType.Error);
                return;
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (var script in scripts)
            {
                if (!script) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(script.name);
                    var asset = scriptToAssetMap.TryGetValue(script, out var assetObj) ? assetObj : null;
                    if (asset)
                    {
                        if (GUILayout.Button("Open", GUILayout.Width(80)))
                        {
                            AssetDatabase.OpenAsset(asset);
                        }
                    }
                    else
                    {
                        var type = script.GetClass();
                        var assetPath = $"{path}/{type.Name}.prefab";
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
                            UpdateMap();
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

        private void UpdateMap()
        {
            scripts = new List<MonoScript>();
            scriptToAssetMap.Clear();

            var scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");

            foreach (var sg in scriptGuids)
            {
                var scriptPath = AssetDatabase.GUIDToAssetPath(sg);
                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                var type = monoScript.GetClass();

                if (type == null
                    || !type.IsSubclassOf(typeof(AbstractUIPanel))
                    || type.IsAbstract)
                    continue;

                scripts.Add(monoScript);

                foreach (var pg in prefabGuids)
                {
                    var prefabPath = AssetDatabase.GUIDToAssetPath(pg);
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (!go) continue;
                    if (!go.GetComponent(type)) continue;
                    scriptToAssetMap[monoScript] = go;
                    break;
                }
            }

            scripts.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        }
    }
}