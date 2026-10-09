using UnityEngine;

namespace RicKit.UI.Component
{
    [RequireComponent(typeof(Camera))]
    public class UIAdditionalCamera : MonoBehaviour
    {
        private Camera cam;
        private IUIManager owner;

        private void Awake() => cam = GetComponent<Camera>();

        private void OnEnable()
        {
            if (UIManager.TryGetInstance(out var manager)) Register(manager);
        }

        internal void Register(IUIManager manager)
        {
            if (!isActiveAndEnabled || ReferenceEquals(owner, manager)) return;
            owner?.UnregisterAdditionalCam(cam);
            if (!cam) cam = GetComponent<Camera>();
            owner = manager;
            owner.RegisterAdditionalCam(cam);
        }

        private void OnDisable()
        {
            owner?.UnregisterAdditionalCam(cam);
            owner = null;
        }
    }
}
