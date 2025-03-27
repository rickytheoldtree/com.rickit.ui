#if USING_URP
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RicKit.UI.URP
{
    [RequireComponent(typeof(Camera))]
    public class UniversalRenderPipelineCamStackUICam : MonoBehaviour
    {
        private void Start()
        {
            var camData = GetComponent<Camera>().GetUniversalAdditionalCameraData();
            var uiCam = UIManager.I.UICamera;
            var uiCamData = uiCam.GetUniversalAdditionalCameraData();
            uiCamData.renderType = CameraRenderType.Overlay;
            if (camData.cameraStack.Contains(uiCam)) return;
            camData.cameraStack.Add(uiCam);
        }
    }
}
#endif