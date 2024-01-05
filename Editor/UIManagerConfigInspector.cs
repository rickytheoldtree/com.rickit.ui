using System;
using System.Collections.Generic;
using UnityEngine;

namespace RicKit.UI.Editor
{
    [UnityEditor.CustomEditor(typeof(UIManagerConfig))]
    public class UIManagerConfigInspector : UnityEditor.Editor
    {
        private string[] panelLoaderNames;
        private List<Type> panelLoaderTypes;
        private int panelLoaderIndex;
        private void OnEnable()
        {
            panelLoaderTypes = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(IPanelLoader).IsAssignableFrom(type) && type != typeof(IPanelLoader))
                    {
                        panelLoaderTypes.Add(type);
                    }
                }
            }
            panelLoaderNames = new string[panelLoaderTypes.Count];
            for (var i = 0; i < panelLoaderTypes.Count; i++)
            {
                panelLoaderNames[i] = panelLoaderTypes[i].Name;
            }
            var config = (UIManagerConfig)target;
            panelLoaderIndex = panelLoaderTypes.IndexOf(config.panelLoaderType);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            panelLoaderIndex = UnityEditor.EditorGUILayout.Popup("PanelLoader", panelLoaderIndex, panelLoaderNames);
            var panelLoaderType = panelLoaderTypes[panelLoaderIndex];
            var config = (UIManagerConfig)target ;
            if (config.panelLoaderType != panelLoaderType)
            {
                config.panelLoaderType = panelLoaderType;
                UnityEditor.EditorUtility.SetDirty(config);
            }
        }
    }
}