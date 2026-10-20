using System;
using System.Collections.Generic;
using System.Linq;
using RicKit.UI.Panels;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


namespace RicKit.UI.Editor
{
    public class UIEditorWindow : EditorWindow
    {
        private static string LegacyPathKey => $"PanelCreatorEditorPath_{Application.identifier}";
        private static UIEditorSettings Settings => UIEditorSettings.instance;
        private readonly List<MonoScript> scripts = new List<MonoScript>();
        private readonly Dictionary<Type, GameObject> typeToPrefab = new Dictionary<Type, GameObject>();
        private readonly Dictionary<Type, List<PanelIssue>> issuesByType = new Dictionary<Type, List<PanelIssue>>();
        private List<PanelIssue> otherIssues = new List<PanelIssue>();
        private bool validated;
        private GUIStyle dropAreaStyle;
        private Vector2 scrollPosition;
        private string search = "";

        private void OnEnable()
        {
            MigrateLegacyPath();
            UpdateMap();
            dropAreaStyle = new GUIStyle
            {
                normal =
                {
                    textColor = Color.white
                },
                alignment = TextAnchor.MiddleCenter
            };
        }

        [MenuItem("RicKit/UI/Open Editor", false, 0)]
        public static void Open()
        {
            var window = GetWindow<UIEditorWindow>("UI Editor", true,
                typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ProjectBrowser"));
            window.Show();
        }

        // 旧版本把目录存在 EditorPrefs，只在本机有效，迁到 ProjectSettings
        private static void MigrateLegacyPath()
        {
            if (!EditorPrefs.HasKey(LegacyPathKey)) return;
            var legacy = EditorPrefs.GetString(LegacyPathKey);
            EditorPrefs.DeleteKey(LegacyPathKey);
            if (string.IsNullOrEmpty(Settings.CreateFolder) && AssetDatabase.IsValidFolder(legacy))
                Settings.CreateFolder = legacy;
        }

        private void DropFolder()
        {
            var evt = Event.current;
            var dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop the folder for new panels here", dropAreaStyle);
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
                            if (AssetDatabase.IsValidFolder(draggedObject))
                            {
                                Settings.CreateFolder = draggedObject;
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
            DrawCreateFolder();
            DrawToolbar();
            DrawOtherIssues();
            DrawPanels();
        }

        private void DrawCreateFolder()
        {
            var folder = Settings.CreateFolder;
            var isSet = !string.IsNullOrEmpty(folder);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Create Folder:", isSet ? folder : $"{UIEditorSettings.DefaultCreateFolder} (not set)");
                }
                using (new EditorGUI.DisabledScope(!isSet))
                {
                    if (GUILayout.Button("Clear", GUILayout.Width(50)))
                        Settings.CreateFolder = null;
                }
            }
            if (isSet && !AssetDatabase.IsValidFolder(folder))
            {
                EditorGUILayout.HelpBox($"Folder doesn't exist, new panels will be created in {UIEditorSettings.DefaultCreateFolder}",
                    MessageType.Warning);
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    UpdateMap();
                if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    RunValidation();
            }
        }

        // 不属于某个面板的问题（比如找不到 UISettings）
        private void DrawOtherIssues()
        {
            foreach (var issue in otherIssues)
                EditorGUILayout.HelpBox(issue.Message,
                    issue.Severity == PanelIssueSeverity.Error ? MessageType.Error : MessageType.Warning);
        }

        private void DrawPanels()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (var script in scripts)
            {
                if (!script) continue;
                if (!string.IsNullOrEmpty(search) && script.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                var type = script.GetClass();
                if (type == null) continue;
                issuesByType.TryGetValue(type, out var issues);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (validated) DrawStatus(issues);
                    GUILayout.Label(script.name);
                    if (typeToPrefab.TryGetValue(type, out var prefab))
                    {
                        if (GUILayout.Button("Open", GUILayout.Width(80)))
                        {
                            AssetDatabase.OpenAsset(prefab);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Create", GUILayout.Width(80)))
                        {
                            // 打开预制体会切场景，放到 GUI 之外做，避免布局报错
                            EditorApplication.delayCall += () => CreatePanel(type);
                        }
                    }

                    if (GUILayout.Button("Edit Script", GUILayout.Width(80)))
                    {
                        AssetDatabase.OpenAsset(script);
                    }
                }

                if (issues != null)
                {
                    foreach (var issue in issues)
                        DrawIssue(issue);
                }

                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
            }

            GUILayout.EndScrollView();
        }

        private static void DrawStatus(List<PanelIssue> issues)
        {
            var icon = issues == null ? "TestPassed"
                : issues.Any(i => i.Severity == PanelIssueSeverity.Error) ? "console.erroricon.sml"
                : "console.warnicon.sml";
            GUILayout.Label(EditorGUIUtility.IconContent(icon), GUILayout.Width(18), GUILayout.Height(18));
        }

        private static void DrawIssue(PanelIssue issue)
        {
            var type = issue.Severity == PanelIssueSeverity.Error ? MessageType.Error : MessageType.Warning;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.HelpBox(issue.Message, type);
                var asset = string.IsNullOrEmpty(issue.AssetPath) ? null : AssetDatabase.LoadAssetAtPath<Object>(issue.AssetPath);
                using (new EditorGUI.DisabledScope(!asset))
                {
                    if (GUILayout.Button("Ping", GUILayout.Width(40), GUILayout.ExpandHeight(true)))
                        EditorGUIUtility.PingObject(asset);
                }
            }
        }

        private void RunValidation()
        {
            var issues = PanelValidator.Validate();
            issuesByType.Clear();
            foreach (var issue in issues.Where(i => i.PanelType != null))
            {
                if (!issuesByType.TryGetValue(issue.PanelType, out var list))
                    issuesByType[issue.PanelType] = list = new List<PanelIssue>();
                list.Add(issue);
            }
            otherIssues = issues.Where(i => i.PanelType == null).ToList();
            validated = true;
            UpdateMap();
        }

        private void CreatePanel(Type type)
        {
            var assetPath = $"{Settings.ResolvedCreateFolder}/{type.Name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath))
            {
                EditorUtility.DisplayDialog("RicKit UI", $"{assetPath} already exists", "OK");
                return;
            }
            var go = new GameObject(type.Name, typeof(RectTransform), type);
            GameObject prefab;
            try
            {
                var rect = go.GetComponent<RectTransform>();
                go.layer = LayerMask.NameToLayer("UI");
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                prefab = PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            }
            finally
            {
                DestroyImmediate(go);
            }
            if (validated) RunValidation();
            else UpdateMap();
            AssetDatabase.OpenAsset(prefab);
        }

        private void UpdateMap()
        {
            scripts.Clear();
            typeToPrefab.Clear();

            foreach (var sg in AssetDatabase.FindAssets("t:MonoScript"))
            {
                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(sg));
                var type = monoScript ? monoScript.GetClass() : null;

                if (type == null
                    || !type.IsSubclassOf(typeof(AbstractUIPanel))
                    || type.IsAbstract)
                    continue;

                scripts.Add(monoScript);
            }

            // 运行时按类名加载，只认根节点上的精确类型；同类型多个预制体时优先和类名同名的
            foreach (var pg in AssetDatabase.FindAssets("t:Prefab"))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(pg));
                if (!go) continue;
                foreach (var panel in go.GetComponents<AbstractUIPanel>())
                {
                    if (!panel) continue;
                    var type = panel.GetType();
                    if (typeToPrefab.TryGetValue(type, out var existing) && existing.name == type.Name) continue;
                    typeToPrefab[type] = go;
                }
            }

            scripts.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
        }
    }
}
