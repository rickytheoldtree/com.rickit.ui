using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using RicKit.UI.Extensions.TaskExtension;
using UnityEngine;
using UnityEngine.UI;

namespace RicKit.UI.Panels
{
    [RequireComponent(typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster))]
    public abstract class AbstractUIPanel : MonoBehaviour
    {
        public int SortOrder { get; private set; }
        public bool IsShow =>  gameObject.activeSelf;

        public bool CanInteract => IsShow && CanvasGroup.interactable;
        protected CanvasGroup CanvasGroup { get; private set; }
        protected RectTransform CanvasRect { get; private set; }
        private Canvas Canvas { get; set; }
        public virtual bool DontDestroyOnClear => false;
        protected static UIManager UI => UIManager.I;
        protected virtual void Awake()
        {
            Canvas = GetComponent<Canvas>();
            Canvas.overrideSorting = true;
            Canvas.sortingLayerName = UI.Config.sortingLayerName;
            CanvasGroup = GetComponent<CanvasGroup>();
            CanvasRect = Canvas.GetComponent<RectTransform>();
        }
        public async UniTask OnShowAsync()
        {
            UIManager.LockInput(true);
            UIManager.OnShow?.Invoke(this);
            gameObject.SetActive(true);
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = false;
            await OnAnimationIn(this.GetCancellationTokenOnDestroy());
            UIManager.OnShowEnd?.Invoke(this);
            UIManager.LockInput(false);
        }
        public async UniTask OnHideAsync()
        {
            UIManager.LockInput(true);
            UIManager.OnHide?.Invoke(this);
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = false;
            await OnAnimationOut(this.GetCancellationTokenOnDestroy());
            UIManager.OnHideEnd?.Invoke(this);
            UIManager.LockInput(false);
        }

        public virtual void BeforeShow()
        {
        }

        public abstract void OnESCClick();
        protected abstract UniTask OnAnimationIn(CancellationToken cancellationToken);

        protected abstract UniTask OnAnimationOut(CancellationToken cancellationToken);

        protected virtual void OnAnimationInEnd()
        {
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = true;
        }

        protected virtual void OnAnimationOutEnd()
        {
            gameObject.SetActive(false);
        }

        public void SetSortOrder(int order)
        {
            SortOrder = order;
            Canvas.overrideSorting = true;
            Canvas.sortingOrder = order;
        }


    }
}