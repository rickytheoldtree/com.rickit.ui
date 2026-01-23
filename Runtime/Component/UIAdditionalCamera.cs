using UnityEngine;

namespace RicKit.UI.Component
{
    [RequireComponent(typeof(Camera))]
    public class UIAdditionalCamera : MonoBehaviour
    {
        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            if (UIManager.I == null) return;
            UIManager.I.RegisterAdditionalCam(cam);
        }

        private void OnDisable()
        {
            if (UIManager.I == null) return;
            UIManager.I.UnregisterAdditionalCam(cam);
        }
    }
}