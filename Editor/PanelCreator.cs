using System;
using System.Collections.Generic;
using System.IO;
using RicKit.UI.Panels;
using UnityEditor;
using UnityEngine;


namespace RicKit.UI.Editor
{
    public class PanelCreator : EditorWindow
    {
        private static string PathKey => $"PanelCreatorEditorPath_{Application.identifier}";
        private static string path = "Assets/Resources/UIPanels";
        private List<Type> types;
        private GUIStyle dropAreaStyle;
        private Vector2 scrollPosition;
        private void OnEnable()
        {
            titleContent = new GUIContent("界面编辑器");
            types = GetAllTypes();
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
        [MenuItem("RicKit/UI/界面编辑器")]
        public static void Open()
        {
            var window = GetWindow<PanelCreator>();
            window.Show();
        }

        private void DropFolder()
        {
            var evt = Event.current;
            var dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "将你希望放UI界面预制体的文件夹拖入这里", dropAreaStyle);
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
            EditorGUILayout.TextField("预制体目录", path);
            GUI.enabled = true;
            DropFolder();
            //检查路径是否存在
            if (!Directory.Exists(path))
            {
                EditorGUILayout.HelpBox("文件夹路径不存在", MessageType.Error);
                return;
            }
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (var type in types)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var assetPath = $"{path}/{type.Name}.prefab";
                    GUILayout.Label(type.Name);
                    var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));
                    if (asset)
                    {
                        if (GUILayout.Button("Open", GUILayout.Width(100)))
                        {
                            AssetDatabase.OpenAsset(asset);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Create", GUILayout.Width(100)))
                        {
                            //创建GameObject
                            var go = new GameObject(type.Name, typeof(RectTransform), type);
                            //设置锚点
                            var rect = go.GetComponent<RectTransform>();
                            //设置层
                            go.layer = LayerMask.NameToLayer("UI");
                            //全屏
                            rect.anchorMin = Vector2.zero;
                            rect.anchorMax = Vector2.one;
                            rect.offsetMin = Vector2.zero;
                            rect.offsetMax = Vector2.zero;
                            //保存
                            PrefabUtility.SaveAsPrefabAsset(go, assetPath);
                            //打开
                            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)));
                            //销毁
                            DestroyImmediate(go);
                        }
                    }
                }
            }
            GUILayout.EndScrollView();
        }

        private List<Type> GetAllTypes()
        {
            //找到所有继承了AbstractUIPanel的类
            var list = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var assemblyTypes = assembly.GetTypes();
                foreach (var type in assemblyTypes)
                {
                    if (type.IsSubclassOf(typeof(AbstractUIPanel)) && !type.IsAbstract)
                    {
                        list.Add(type);
                    }
                }
            }
            return list;
        }
    }
}

