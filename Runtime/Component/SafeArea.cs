using System;
using UnityEngine;

namespace RicKit.UI.Component
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform rectTrans;
        private ScreenOrientation lastOrientation;
        private void Awake()
        {
            rectTrans = GetComponent<RectTransform>();
            lastOrientation = Screen.orientation;
            
            
            rectTrans.anchorMin = Vector2.zero;
            rectTrans.anchorMax = Vector2.one;
            rectTrans.anchoredPosition = Vector2.zero;
            rectTrans.sizeDelta = Vector2.zero;
            AdaptAnchorsValue();
        }

        private void LateUpdate()
        {
            if (lastOrientation == Screen.orientation) return;
            lastOrientation = Screen.orientation;
            AdaptAnchorsValue();
        }

        private void AdaptAnchorsValue()
        {
            var maxWidth = Display.main.systemWidth;
            var maxHeight = Display.main.systemHeight;
            var safeArea = UnityEngine.Screen.safeArea;
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= maxWidth;
            anchorMin.y /= maxHeight;
            anchorMax.x /= maxWidth;
            anchorMax.y /= maxHeight;

            rectTrans.anchorMin = anchorMin;
            rectTrans.anchorMax = anchorMax;
        }
    }
}