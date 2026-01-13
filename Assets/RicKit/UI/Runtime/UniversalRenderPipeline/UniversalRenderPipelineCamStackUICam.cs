#if USING_URP
using UnityEngine;

namespace RicKit.UI.URP
{
    [RequireComponent(typeof(Camera))]
    public class UniversalRenderPipelineCamStackUICam : MonoBehaviour
    {
        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            if (UIManager.I == null) return;
            UIManager.I.RegisterBaseCam(cam);
        }

        private void OnDisable()
        {
            if (UIManager.I == null) return;
            UIManager.I.UnregisterBaseCam(cam);
        }
    }
}
#endif