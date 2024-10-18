using System.Threading;
using Cysharp.Threading.Tasks;
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
        protected static IUIManager UI => UIManager.I;
        protected virtual void Awake()
        {
            Canvas = GetComponent<Canvas>();
            Canvas.overrideSorting = true;
            Canvas.sortingLayerName = UI.Settings.sortingLayerName;
            CanvasGroup = GetComponent<CanvasGroup>();
            CanvasRect = Canvas.GetComponent<RectTransform>();
        }
        public async UniTask OnShowAsync()
        {
            UI.SetLockInput(true);
            UI.OnShow?.Invoke(this);
            gameObject.SetActive(true);
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = false;
            await OnAnimationIn(this.GetCancellationTokenOnDestroy());
            UI.OnShowEnd?.Invoke(this);
            UI.SetLockInput(false);
        }
        public async UniTask OnHideAsync()
        {
            UI.SetLockInput(true);
            UI.OnHide?.Invoke(this);
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = false;
            await OnAnimationOut(this.GetCancellationTokenOnDestroy());
            UI.OnHideEnd?.Invoke(this);
            UI.SetLockInput(false);
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

        public void SetOrderInLayer(int order)
        {
            SortOrder = order;
            Canvas.overrideSorting = true;
            Canvas.sortingOrder = order;
        }

        public void SetSortingLayer(string layer)
        {
            Canvas.overrideSorting = true;
            Canvas.sortingLayerName = layer;
        }
    }
}