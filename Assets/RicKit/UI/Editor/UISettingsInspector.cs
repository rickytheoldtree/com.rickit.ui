using UnityEditor;
using UnityEngine;

namespace RicKit.UI.Editor
{
    [CustomEditor(typeof(UISettings))]
    public class UISettingsInspector : UnityEditor.Editor
    {
        private UISettings settings;
        private GUIStyle titleStyle;

        private SerializedProperty cullingMask,
            sortingLayerName,
            referenceResolution,
            screenMatchMode,
            cameraClearFlags,
            nearClipPlane,
            farClipPlane,
            assetPathPrefix,
            loadType,
            packageName,
            yooSyncLoad;

        private void OnEnable()
        {
            settings = (UISettings)target;
            cullingMask = serializedObject.FindProperty("cullingMask");
            loadType = serializedObject.FindProperty("loadType");
            packageName = serializedObject.FindProperty("packageName");
            sortingLayerName = serializedObject.FindProperty("sortingLayerName");
            referenceResolution = serializedObject.FindProperty("referenceResolution");
            screenMatchMode = serializedObject.FindProperty("screenMatchMode");
            cameraClearFlags = serializedObject.FindProperty("cameraClearFlags");
            nearClipPlane = serializedObject.FindProperty("nearClipPlane");
            farClipPlane = serializedObject.FindProperty("farClipPlane");
            assetPathPrefix = serializedObject.FindProperty("assetPathPrefix");
            yooSyncLoad = serializedObject.FindProperty("yooSyncLoad");
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Label("Camera", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cullingMask);
            EditorGUILayout.PropertyField(sortingLayerName);
            EditorGUILayout.PropertyField(referenceResolution);
            EditorGUILayout.PropertyField(screenMatchMode);
            EditorGUILayout.PropertyField(cameraClearFlags);
            EditorGUILayout.PropertyField(nearClipPlane);
            EditorGUILayout.PropertyField(farClipPlane);
            EditorGUI.indentLevel--;

            GUILayout.Label("Asset", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(loadType);
            EditorGUILayout.PropertyField(assetPathPrefix);
            if (settings.loadType == LoadType.Yoo)
            {
                GUILayout.Label("Yoo Asset", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(packageName);
                EditorGUILayout.PropertyField(yooSyncLoad);
            }
            EditorGUI.indentLevel--;
            
            serializedObject.ApplyModifiedProperties();
        }
        
        [MenuItem("RicKit/UI/Create UISettings")]
        private static void CreateSettings()
        {
            var config = CreateInstance<UISettings>();
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            AssetDatabase.CreateAsset(config, "Assets/Resources/UISettings.asset");
            Selection.activeObject = config;
        }
    }
}