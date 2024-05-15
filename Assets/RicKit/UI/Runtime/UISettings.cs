using UnityEngine;
using UnityEngine.UI;

namespace RicKit.UI
{
    public enum LoadType
    {
        Resources,
#if YOO_SUPPORT
        Yoo,
#endif
#if ADDRESSABLES_SUPPORT
        Addressables,
#endif
    }
    
    [CreateAssetMenu(menuName = "RicKit/创建UIManager配置")]
    public class UISettings : ScriptableObject
    {
        
        public int cullingMask = 32;
        public string sortingLayerName = "UI";
        public Vector2 referenceResolution = new Vector2(1080, 1920);
        public CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        [Range(0, 1)]
        public float matchWidthOrHeight = 1f;
        public CameraClearFlags cameraClearFlags = CameraClearFlags.Depth;
        public float nearClipPlane = 1;
        public float farClipPlane = 15;
        public LoadType loadType = LoadType.Resources;
        public string assetPathPrefix = "UI/";
#if YOO_SUPPORT
        public string packageName; 
        public bool yooSyncLoad;
#endif
    }
}