using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace RicKit.UI.Panels
{
    public enum UIPanelState { Hidden, Showing, Shown, Hiding }

    [RequireComponent(typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster))]
    public abstract class AbstractUIPanel : MonoBehaviour
    {
        public int OrderInLayer { get; private set; }
        public bool IsShow => gameObject.activeSelf;
        public bool CanInteract => IsShow && State == UIPanelState.Shown && CanvasGroup && CanvasGroup.interactable;
        public UIPanelState State { get; private set; }
        public bool IsTransitioning => State == UIPanelState.Showing || State == UIPanelState.Hiding;
        public string SortingLayerName => sortingLayer;
        protected CanvasGroup CanvasGroup { get; private set; }
        protected RectTransform CanvasRect { get; private set; }
        private Canvas Canvas { get; set; }
        private IUIManager owner;
        private CancellationTokenSource transitionCancellation;
        private bool stableShown;
        private float stableAlpha;
        private bool destroyRequested;
        private int activationDepth;
        private string sortingLayer = "UI";
        [SerializeField] private bool destroyOnClose;
        public virtual bool DontDestroyOnClear => false;
        public virtual bool DestroyOnClose => destroyOnClose;
        protected static IUIManager UI => UIManager.I;

        protected virtual void Awake() => EnsureComponents();

        private void EnsureComponents()
        {
            if (Canvas) return;
            Canvas = GetComponent<Canvas>();
            CanvasGroup = GetComponent<CanvasGroup>();
            CanvasRect = (RectTransform)transform;
            ApplySorting();
            stableAlpha = CanvasGroup.alpha;
        }

        internal void BindManager(IUIManager manager)
        {
            owner = manager;
            EnsureComponents();
        }

        internal void CancelTransition() => UIManager.CancelSafely(transitionCancellation);

        internal void PrepareForDestroy()
        {
            destroyRequested = true;
            CancelTransition();
            // Destroy is deferred by Unity; avoid reentering activation when called from OnEnable.
            if (activationDepth == 0) SetPanelActive(false);
        }

        /// <summary>Restore custom animation targets when a transition fails or is cancelled.</summary>
        protected virtual void RestoreAnimationState(bool shown) { }

        public UniTask OnShowAsync() => OnShowAsync(CancellationToken.None);
        public UniTask OnHideAsync() => OnHideAsync(CancellationToken.None);
        public UniTask OnShowAsync(CancellationToken cancellationToken) => TransitionAsync(true, cancellationToken);
        public UniTask OnHideAsync(CancellationToken cancellationToken) => TransitionAsync(false, cancellationToken);

        private async UniTask TransitionAsync(bool show, CancellationToken cancellationToken)
        {
            UIManager.EnsureMainThread();
            cancellationToken.ThrowIfCancellationRequested();
            if (!this || destroyRequested) throw new OperationCanceledException("The panel is being destroyed.");
            EnsureComponents();
            if (!IsTransitioning && (show ? State == UIPanelState.Shown && IsShow : State == UIPanelState.Hidden && !IsShow))
                return;
            var manager = owner;
            if (manager == null && !UIManager.TryGetInstance(out manager))
                throw new InvalidOperationException("The panel has no initialized UIManager.");
            owner = manager;
            var previous = transitionCancellation;
            var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, gameObject.GetAsyncDestroyTrigger().CancellationToken);
            var token = source.Token;
            var wasShown = stableShown;
            // A superseded animation may already have changed alpha. Restore the last committed value.
            var previousAlpha = stableAlpha;
            transitionCancellation = source;
            IDisposable inputLock = null;
            try
            {
                inputLock = manager.GetLockInputScope();
                // Publish ownership before cancellation: user cancellation callbacks can start a newer transition.
                UIManager.CancelSafely(previous);
                // Unity rejects SetActive reentry from OnEnable/OnDisable. Keep the new owner,
                // but wait until the activation callback has unwound before changing the object.
                if (activationDepth > 0) await UniTask.NextFrame(cancellationToken: token);
                CheckTransition(source, token);
                State = show ? UIPanelState.Showing : UIPanelState.Hiding;
                CanvasGroup.blocksRaycasts = true;
                CanvasGroup.interactable = false;
                if (show)
                {
                    SetPanelActive(true);
                    CheckTransition(source, token);
                    manager.OnShow?.Invoke(this);
                }
                else manager.OnHide?.Invoke(this);
                CheckTransition(source, token);

                // Release our own state and input lock even if a custom animation ignores cancellation.
                // Custom animations must still observe the token before modifying their targets.
                await (show ? OnAnimationIn(token) : OnAnimationOut(token)).AttachExternalCancellation(token);
                await UniTask.SwitchToMainThread();
                CheckTransition(source, token);
                if (!show)
                {
                    SetPanelActive(false);
                    CheckTransition(source, token);
                }
                stableShown = show;
                stableAlpha = CanvasGroup.alpha;
                State = show ? UIPanelState.Shown : UIPanelState.Hidden;
                CanvasGroup.blocksRaycasts = show;
                CanvasGroup.interactable = show;
                if (show) manager.OnShowEnd?.Invoke(this);
                else manager.OnHideEnd?.Invoke(this);
                CheckTransition(source, token);
            }
            catch
            {
                await UniTask.SwitchToMainThread();
                // WhenAll can fail before its other animations finish. Stop those before restoring visuals.
                UIManager.CancelSafely(source);
                if (activationDepth > 0) await UniTask.NextFrame();
                if (this && !destroyRequested && ReferenceEquals(transitionCancellation, source))
                {
                    stableShown = wasShown;
                    stableAlpha = previousAlpha;
                    State = wasShown ? UIPanelState.Shown : UIPanelState.Hidden;
                    CanvasGroup.alpha = previousAlpha;
                    CanvasGroup.blocksRaycasts = wasShown;
                    CanvasGroup.interactable = wasShown;
                    try { RestoreAnimationState(wasShown); }
                    catch (Exception exception) { Debug.LogException(exception); }
                    // Both the restore hook and OnEnable/OnDisable may reenter. Do no further writes after SetActive.
                    if (this && !destroyRequested && ReferenceEquals(transitionCancellation, source))
                        SetPanelActive(wasShown);
                }
                throw;
            }
            finally
            {
                if (ReferenceEquals(transitionCancellation, source)) transitionCancellation = null;
                source.Dispose();
                inputLock?.Dispose();
            }
        }

        private void SetPanelActive(bool active)
        {
            activationDepth++;
            try { gameObject.SetActive(active); }
            finally { activationDepth--; }
            // Unity drops overrideSorting set on an inactive nested Canvas. Reapply it once active.
            if (active && this) ApplySorting();
        }

        private void ApplySorting()
        {
            Canvas.overrideSorting = true;
            Canvas.sortingLayerName = sortingLayer;
            Canvas.sortingOrder = OrderInLayer;
        }

        private void CheckTransition(CancellationTokenSource source, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!this || destroyRequested || !ReferenceEquals(transitionCancellation, source))
                throw new OperationCanceledException(token);
        }

        public abstract void OnESCClick();
        protected abstract UniTask OnAnimationIn(CancellationToken cancellationToken);
        protected abstract UniTask OnAnimationOut(CancellationToken cancellationToken);

        public virtual void SetOrderInLayer(int order)
        {
            UIManager.EnsureMainThread();
            EnsureComponents();
            OrderInLayer = order;
            ApplySorting();
        }

        public virtual void SetSortingLayer(string layer)
        {
            UIManager.EnsureMainThread();
            EnsureComponents();
            sortingLayer = layer;
            ApplySorting();
        }
    }
}
