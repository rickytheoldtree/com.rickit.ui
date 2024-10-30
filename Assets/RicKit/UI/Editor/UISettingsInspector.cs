using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RicKit.UI.Editor
{
    [CustomEditor(typeof(UISettings))]
    public class UISettingsInspector : UnityEditor.Editor
    {
        private UISettings settings;
        private GUIStyle titleStyle;

        private SerializedProperty cameraClearFlags,
            backgroundColor,
            cullingMask,
            depth,
            nearClipPlane,
            farClipPlane,
            referenceResolution,
            screenMatchMode,
            matchWidthOrHeight,
            assetPathPrefix;

        private void OnEnable()
        {
            settings = (UISettings)target;
            cameraClearFlags = serializedObject.FindProperty("cameraClearFlags");
            backgroundColor = serializedObject.FindProperty("backgroundColor");
            cullingMask = serializedObject.FindProperty("cullingMask");
            depth = serializedObject.FindProperty("depth");
            nearClipPlane = serializedObject.FindProperty("nearClipPlane");
            farClipPlane = serializedObject.FindProperty("farClipPlane");
            referenceResolution = serializedObject.FindProperty("referenceResolution");
            screenMatchMode = serializedObject.FindProperty("screenMatchMode");
            matchWidthOrHeight = serializedObject.FindProperty("matchWidthOrHeight");
            assetPathPrefix = serializedObject.FindProperty("assetPathPrefix");
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Label("Camera", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cameraClearFlags);
            if (settings.cameraClearFlags == CameraClearFlags.SolidColor || settings.cameraClearFlags == CameraClearFlags.Skybox)
            {
                EditorGUILayout.PropertyField(backgroundColor);
            }
            EditorGUILayout.PropertyField(cullingMask);
            EditorGUILayout.PropertyField(depth);
            EditorGUILayout.PropertyField(nearClipPlane);
            EditorGUILayout.PropertyField(farClipPlane);
            EditorGUI.indentLevel--;
            GUILayout.Label("Canvas", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(referenceResolution);
            EditorGUILayout.PropertyField(screenMatchMode);
            if (settings.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(matchWidthOrHeight);
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
            GUILayout.Label("Asset", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(assetPathPrefix);
            serializedObject.ApplyModifiedProperties();
        }
        
        [MenuItem("RicKit/UI/Create UISettings")]
        public static void CreateSettings()
        {
            var config = ScriptableObject.CreateInstance<UISettings>();
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            AssetDatabase.CreateAsset(config, "Assets/Resources/UISettings.asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;
        }
    }
}