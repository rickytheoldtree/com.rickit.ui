using UnityEngine;
using UnityEngine.UI;

namespace RicKit.UI
{
    [CreateAssetMenu(menuName = "RicKit/创建UIManager配置")]
    public class UISettings : ScriptableObject
    {
        public int cullingMask = 32;
        public string sortingLayerName = "UI";
        public Vector2 referenceResolution = new Vector2(1080, 1920);
        public CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        public CameraClearFlags cameraClearFlags = CameraClearFlags.Depth;
        public float nearClipPlane = 1;
        public float farClipPlane = 15;
        [HideInInspector]
        public string panelLoaderType = typeof(DefaultPanelLoader).FullName;
    }
}