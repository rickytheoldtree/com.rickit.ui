using System;
using System.Threading;
using System.Threading.Tasks;
using RicKit.UI.TaskExtension;
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
        public async Task OnShowAsync()
        {
            UIManager.LockInput(true);
            gameObject.SetActive(true);
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = false;
            await OnAnimationIn(this.GetCancellationTokenOnDestroy());
            UIManager.LockInput(false);
        }
        public async Task OnHideAsync()
        {
            UIManager.LockInput(true);
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = false;
            await OnAnimationOut(this.GetCancellationTokenOnDestroy());
            UIManager.LockInput(false);
        }

        public void UpdateData<T>(Action<T> onUpdateData) where T : AbstractUIPanel
        {
            onUpdateData?.Invoke((T)this);
        }

        public virtual void BeforeShow()
        {
        }

        public abstract void OnESCClick();
        protected abstract Task OnAnimationIn(CancellationToken cancellationToken);

        protected abstract Task OnAnimationOut(CancellationToken cancellationToken);

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