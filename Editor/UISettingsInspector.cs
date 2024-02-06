using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RicKit.UI.Editor
{
    [UnityEditor.CustomEditor(typeof(UISettings))]
    public class UISettingsInspector : UnityEditor.Editor
    {
        private UISettings settings;
        private GUIStyle titleStyle;

        private SerializedProperty cullingMask,
            loadType,
            packageName,
            sortingLayerName,
            referenceResolution,
            screenMatchMode,
            cameraClearFlags,
            nearClipPlane,
            farClipPlane,
            assetPathPrefix;

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
            }
            EditorGUI.indentLevel--;
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}