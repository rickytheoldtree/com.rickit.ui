using System;
using System.Collections.Generic;

namespace RicKit.UI.Editor
{
    [UnityEditor.CustomEditor(typeof(UISettings))]
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
                panelLoaderNames[i] = panelLoaderTypes[i].FullName;
            }
            var config = (UISettings)target;
            for (var i = 0; i < panelLoaderTypes.Count; i++)
            {
                if (panelLoaderTypes[i].FullName == config.panelLoaderType)
                {
                    panelLoaderIndex = i;
                    break;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            panelLoaderIndex = UnityEditor.EditorGUILayout.Popup("PanelLoader", panelLoaderIndex, panelLoaderNames);
            if (panelLoaderIndex < 0 || panelLoaderIndex >= panelLoaderTypes.Count)
            {
                return;
            }
            var panelLoaderType = panelLoaderTypes[panelLoaderIndex];
            var config = (UISettings)target ;
            if (config.panelLoaderType != panelLoaderType.FullName)
            {
                config.panelLoaderType = panelLoaderType.FullName;
                UnityEditor.EditorUtility.SetDirty(config);
            }
        }
    }
}