using System;
using System.Collections.Generic;

namespace RicKit.UI.Editor
{
    [UnityEditor.CustomEditor(typeof(UISettings))]
    public class UISettingsInspector : UnityEditor.Editor
    {
        private string[] panelLoaderNames;
        private UISettings settings;
        private void OnEnable()
        {
            settings = (UISettings)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (settings.loadType == LoadType.Yoo)
            {
                var serializedField = serializedObject.FindProperty("packageName");
                UnityEditor.EditorGUILayout.PropertyField(serializedField);
            }
        }
    }
}