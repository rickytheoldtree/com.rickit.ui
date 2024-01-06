using System;
using UnityEngine;

namespace RicKit.UI.SubUI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class AbstractSubPanel : MonoBehaviour
    {
        public bool IsShow { get; private set; }
        public bool CanInteract => IsShow && CanvasGroup.interactable;
        protected CanvasGroup CanvasGroup { get; private set; }
        protected static UIManager UI => UIManager.I;
        public SubUICtrl SubUICtrl { get; set; }

        protected virtual void Awake()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
        }

        public void OnShow()
        {
            IsShow = true;
            gameObject.SetActive(true);
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = false;
            OnAnimationIn();
        }

        public void OnHide(Action callback = null)
        {
            IsShow = false;
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = false;
            OnAnimationOut(callback);
        }

        public void UpdateData<T>(Action<T> onUpdateData) where T : AbstractSubPanel
        {
            onUpdateData?.Invoke((T)this);
        }

        protected abstract void OnAnimationIn();

        protected abstract void OnAnimationOut(Action callback);

        protected virtual void OnAnimationInEnd()
        {
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = true;
        }

        protected virtual void OnAnimationOutEnd(Action callback)
        {
            callback?.Invoke();
            gameObject.SetActive(false);
        }
    }
}