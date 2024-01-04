using UnityEngine;

namespace RicKit.UI.Editor
{
    public static class UIManagerEditorHelper
    {
        //检测是否存在Config,如果不存在则创建
        [UnityEditor.InitializeOnLoadMethod]
        private static void CheckConfig()
        {
            if (Resources.Load("UIManagerConfig") == null)
            {
                var config = ScriptableObject.CreateInstance<UIManagerConfig>();
                if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                }
                UnityEditor.AssetDatabase.CreateAsset(config, "Assets/Resources/UIManagerConfig.asset");
                UnityEditor.AssetDatabase.SaveAssets();
            }
        }
    }
}