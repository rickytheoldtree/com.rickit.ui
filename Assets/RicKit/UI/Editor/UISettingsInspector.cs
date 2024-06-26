﻿using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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
            matchWidthOrHeight,
            cameraClearFlags,
            nearClipPlane,
            farClipPlane,
            loadType,
            assetPathPrefix;
#if YOO_SUPPORT
        private SerializedProperty
            packageName,
            yooSyncLoad;
#endif

        private void OnEnable()
        {
            settings = (UISettings)target;
            cullingMask = serializedObject.FindProperty("cullingMask");
            loadType = serializedObject.FindProperty("loadType");
            sortingLayerName = serializedObject.FindProperty("sortingLayerName");
            referenceResolution = serializedObject.FindProperty("referenceResolution");
            screenMatchMode = serializedObject.FindProperty("screenMatchMode");
            matchWidthOrHeight = serializedObject.FindProperty("matchWidthOrHeight");
            cameraClearFlags = serializedObject.FindProperty("cameraClearFlags");
            nearClipPlane = serializedObject.FindProperty("nearClipPlane");
            farClipPlane = serializedObject.FindProperty("farClipPlane");
            assetPathPrefix = serializedObject.FindProperty("assetPathPrefix");
#if YOO_SUPPORT
            packageName = serializedObject.FindProperty("packageName");
            yooSyncLoad = serializedObject.FindProperty("yooSyncLoad");
#endif
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Label("Camera", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cullingMask);
            EditorGUILayout.PropertyField(sortingLayerName);
            EditorGUILayout.PropertyField(referenceResolution);
            EditorGUILayout.PropertyField(screenMatchMode);
            if (settings.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(matchWidthOrHeight);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(cameraClearFlags);
            EditorGUILayout.PropertyField(nearClipPlane);
            EditorGUILayout.PropertyField(farClipPlane);
            EditorGUI.indentLevel--;

            GUILayout.Label("Asset", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(loadType);
            EditorGUILayout.PropertyField(assetPathPrefix);
            
#if YOO_SUPPORT
           if (settings.loadType == LoadType.Yoo)
            {
                GUILayout.Label("Yoo Asset", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(packageName);
                EditorGUILayout.PropertyField(yooSyncLoad);
            }
            EditorGUI.indentLevel--; 
#endif
            
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