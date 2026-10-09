using UnityEngine;

namespace RicKit.UI.Component
{
    [RequireComponent(typeof(Camera))]
    public class UIAdditionalCamera : MonoBehaviour
    {
        private Camera cam;
        internal Camera Camera => cam ? cam : cam = GetComponent<Camera>();

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            // UIManager 还没初始化时不报错，初始化时会主动把已启用的相机注册上
            if (!UIManager.TryGetInstance(out var manager)) return;
            manager.RegisterAdditionalCam(Camera);
        }

        private void OnDisable()
        {
            if (!UIManager.TryGetInstance(out var manager)) return;
            manager.UnregisterAdditionalCam(Camera);
        }
    }
}
