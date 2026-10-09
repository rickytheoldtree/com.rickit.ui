using UnityEngine;
using UnityEngine.UI;

namespace RicKit.UI
{
    public class UISettings : ScriptableObject
    {
        public CameraClearFlags cameraClearFlags = CameraClearFlags.Depth;
        public Color backgroundColor = new Color(0.1921569f, 0.1921569f, 0.1921569f, 1);
        public int cullingMask = 32;
        public int depth;
        public float nearClipPlane = 1;
        public float farClipPlane = 15;
        public Vector2 referenceResolution = new Vector2(1080, 1920);
        public CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        [Range(0, 1)]
        public float matchWidthOrHeight = 1f;
        public string assetPathPrefix = "UI/";
    }
}