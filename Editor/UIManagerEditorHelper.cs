using UnityEngine;

namespace RicKit.UI.Editor
{
    public static class UIManagerEditorHelper
    {
        [UnityEditor.MenuItem("RicKit/UI/Create UISettings")]
        private static void CreateSettings()
        {
            var config = ScriptableObject.CreateInstance<UISettings>();
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }
            UnityEditor.AssetDatabase.CreateAsset(config, "Assets/Resources/UISettings.asset");
            UnityEditor.Selection.activeObject = config;
        }
    }
}