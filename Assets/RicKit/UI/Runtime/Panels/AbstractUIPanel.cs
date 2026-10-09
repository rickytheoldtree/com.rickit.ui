using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using RicKit.UI.Component;
using UnityEngine;
using UnityEngine.UI;

namespace RicKit.UI.Panels
{
    [RequireComponent(typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster))]
    public abstract class AbstractUIPanel : MonoBehaviour
    {
        public int OrderInLayer { get; private set; }
        public bool IsShow =>  gameObject.activeSelf;

        public bool CanInteract => IsShow && CanvasGroup.interactable;
        protected CanvasGroup CanvasGroup { get; private set; }
        protected RectTransform CanvasRect { get; private set; }
        private Canvas Canvas { get; set; }
        public string SortingLayerName => Canvas ? Canvas.sortingLayerName : "UI";
        public virtual bool DontDestroyOnClear => false;
        /// <summary>
        /// 关闭（Back、CloseCurrent、Close、CloseUntil、ShowThenClosePrev）时是否总是销毁，
        /// 相当于这些调用都传了 destroy: true。占内存大、又不常打开的面板可以重写为 true
        /// </summary>
        public virtual bool DestroyOnClose => false;
        protected static IUIManager UI => UIManager.I;
        private static readonly List<UISortingFollower> FollowerBuffer = new List<UISortingFollower>();
        protected virtual void Awake()
        {
            Canvas = GetComponent<Canvas>();
            Canvas.overrideSorting = true;
            Canvas.sortingLayerName = "UI";
            CanvasGroup = GetComponent<CanvasGroup>();
            CanvasRect = Canvas.GetComponent<RectTransform>();
        }
        public async UniTask OnShowAsync()
        {
            var ui = UI;
            ui.SetLockInput(true);
            // 动画中途被销毁（OperationCanceledException）或回调抛异常时也必须解锁，否则输入永久锁死
            try
            {
                gameObject.SetActive(true);
                ui.OnShow?.Invoke(this);
                CanvasGroup.blocksRaycasts = true;
                CanvasGroup.interactable = false;
                await OnAnimationIn(this.GetCancellationTokenOnDestroy());
                // 动画期间面板被销毁（ClearAll 等），后面的状态和回调都没有意义了
                if (!this) return;
                CanvasGroup.blocksRaycasts = true;
                CanvasGroup.interactable = true;
                ui.OnShowEnd?.Invoke(this);
            }
            finally
            {
                ui.SetLockInput(false);
            }
        }
        public async UniTask OnHideAsync()
        {
            var ui = UI;
            ui.SetLockInput(true);
            try
            {
                ui.OnHide?.Invoke(this);
                CanvasGroup.blocksRaycasts = true;
                CanvasGroup.interactable = false;
                await OnAnimationOut(this.GetCancellationTokenOnDestroy());
                // 动画期间面板被销毁（ClearAll 等），后面的状态和回调都没有意义了
                if (!this) return;
                gameObject.SetActive(false);
                ui.OnHideEnd?.Invoke(this);
            }
            finally
            {
                ui.SetLockInput(false);
            }
        }

        public abstract void OnESCClick();
        protected abstract UniTask OnAnimationIn(CancellationToken cancellationToken);

        protected abstract UniTask OnAnimationOut(CancellationToken cancellationToken);

        public virtual void SetOrderInLayer(int order)
        {
            OrderInLayer = order;
            Canvas.overrideSorting = true;
            Canvas.sortingOrder = order;
            RefreshSortingFollowers();
        }

        public virtual void SetSortingLayer(string layer)
        {
            Canvas.overrideSorting = true;
            Canvas.sortingLayerName = layer;
            RefreshSortingFollowers();
        }

        // 子节点上的粒子、SortingGroup 挂 UISortingFollower 后，面板层级一变就跟着改，不用在 OnAnimationIn 里手动同步
        private void RefreshSortingFollowers()
        {
            GetComponentsInChildren(true, FollowerBuffer);
            try
            {
                foreach (var follower in FollowerBuffer)
                    follower.Follow(this);
            }
            finally
            {
                FollowerBuffer.Clear();
            }
        }
    }
}