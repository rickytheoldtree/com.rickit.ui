#if USING_URP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RicKit.UI.URP
{
    public static class UniversalRenderPipelineExtension
    {
        private static readonly HashSet<Camera> RegisteredCams = new();

        public static void RegisterBaseCam(this IUIManager uiManager, Camera cam)
        {
            if (!cam) return;
            if (!RegisteredCams.Add(cam)) return;

            var uiCam = uiManager.UICamera;
            if (!uiCam) return;

            var camData = cam.GetUniversalAdditionalCameraData();
            if (camData.renderType != CameraRenderType.Base)
                return;

            var uiCamData = uiCam.GetUniversalAdditionalCameraData();

            if (RegisteredCams.Count == 1)
                uiCamData.renderType = CameraRenderType.Overlay;

            if (!camData.cameraStack.Contains(uiCam))
                camData.cameraStack.Add(uiCam);
        }

        public static void UnregisterBaseCam(this IUIManager uiManager, Camera cam)
        {
            if (!cam) return;
            if (!RegisteredCams.Remove(cam)) return;

            var uiCam = uiManager.UICamera;
            if (!uiCam) return;

            var camData = cam.GetUniversalAdditionalCameraData();
            camData.cameraStack.Remove(uiCam);

            if (RegisteredCams.Count == 0) 
                uiCam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Base;
        }
    }
}
#endif