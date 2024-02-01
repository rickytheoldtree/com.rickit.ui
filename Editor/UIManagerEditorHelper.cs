using System.Linq;
using UnityEngine;

namespace RicKit.UI.Editor
{
    public static class UIManagerEditorHelper
    {
        //检测是否存在Config,如果不存在则创建
        [UnityEditor.InitializeOnLoadMethod]
        private static void CheckConfig()
        {
            //是否任意Resources文件夹下存在UIManagerConfig
            if (!UnityEditor.AssetDatabase.FindAssets("t:UISettings", new []{"Assets"}).Any())
            {
                
                var config = ScriptableObject.CreateInstance<UISettings>();
                if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                }
                UnityEditor.AssetDatabase.CreateAsset(config, "Assets/Resources/UISettings.asset");
            }
        }
    }
}