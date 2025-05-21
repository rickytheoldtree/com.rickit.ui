using System;
using UnityEngine;

namespace RicKit.UI.Component
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform rectTrans;
        private Rect safeArea;
        private void Awake()
        {
            rectTrans = GetComponent<RectTransform>();
            rectTrans.anchorMin = Vector2.zero;
            rectTrans.anchorMax = Vector2.one;
            rectTrans.anchoredPosition = Vector2.zero;
            rectTrans.sizeDelta = Vector2.zero;
            AdaptAnchorsValue();
        }

        private void LateUpdate()
        {
            if(safeArea == Screen.safeArea) return;
            AdaptAnchorsValue();
        }

        private void AdaptAnchorsValue()
        {
            safeArea = Screen.safeArea;
            var maxWidth = Screen.width;
            var maxHeight = Screen.height;
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