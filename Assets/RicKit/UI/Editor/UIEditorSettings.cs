using UnityEditor;
using UnityEngine;

namespace RicKit.UI.Editor
{
    // 只给编辑器用，放在 ProjectSettings 下跟着项目走
    [FilePath("ProjectSettings/RicKitUIEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class UIEditorSettings : ScriptableSingleton<UIEditorSettings>
    {
        public const string DefaultCreateFolder = "Assets";

        [SerializeField] private string createFolder;

        public string CreateFolder
        {
            get => createFolder;
            set
            {
                createFolder = value;
                Save(true);
            }
        }

        /// <summary>
        /// 没设置或目录已不存在时生成到 Assets
        /// </summary>
        public string ResolvedCreateFolder =>
            !string.IsNullOrEmpty(createFolder) && AssetDatabase.IsValidFolder(createFolder)
                ? createFolder
                : DefaultCreateFolder;
    }
}
