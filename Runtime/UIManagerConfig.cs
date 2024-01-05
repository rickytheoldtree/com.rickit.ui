using UnityEngine;
using UnityEngine.UI;

namespace RicKit.UI
{
    [CreateAssetMenu(menuName = "RicKit/创建UIManager配置")]
    public class UIManagerConfig : ScriptableObject
    {
        public int cullingMask = 32;
        public string sortingLayerName = "UI";
        public Vector2 referenceResolution = new Vector2(1080, 1920);
        public CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        [HideInInspector]
        public string panelLoaderType = typeof(DefaultPanelLoader).FullName;
    }
}