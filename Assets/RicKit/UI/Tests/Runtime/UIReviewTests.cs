using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using RicKit.UI.Extensions.TaskExtension;
using RicKit.UI.Panels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RicKit.UI.Tests
{
    public class ReentrantPanel : ControlledPanel
    {
        public Action Disabled;
        public Action Enabled;
        private void OnEnable() => Enabled?.Invoke();
        private void OnDisable() => Disabled?.Invoke();
    }

    public class UncooperativePanel : AbstractUIPanel
    {
        public readonly UniTaskCompletionSource Gate = new UniTaskCompletionSource();
        public bool ThrowOnCancel;
        public CancellationToken AnimationToken;
        public override void OnESCClick() { }
        protected override UniTask OnAnimationIn(CancellationToken token)
        {
            AnimationToken = token;
            if (ThrowOnCancel) token.Register(() => throw new InvalidOperationException("cancel hook failed"));
            return Gate.Task;
        }
        protected override UniTask OnAnimationOut(CancellationToken token) => UniTask.CompletedTask;
    }

    public class ParallelFailurePanel : AbstractUIPanel
    {
        public override void OnESCClick() { }
        protected override UniTask OnAnimationIn(CancellationToken token)
        {
            CanvasGroup.alpha = 1;
            return UniTask.CompletedTask;
        }
        protected override UniTask OnAnimationOut(CancellationToken token)
            => UniTask.WhenAll(CanvasGroup.Fade(0, 1, cancellationToken: token), FailAsync());
        private static async UniTask FailAsync()
        {
            await UniTask.NextFrame();
            throw new InvalidOperationException("parallel animation failed");
        }
    }

    public partial class UIManagerTests
    {
        [UnityTest]
        public IEnumerator HideCallbackCanStartANewerShowWithoutStateBeingOverwritten() => Run(async () =>
        {
            AddPrefab<ReentrantPanel>();
            await manager.ShowUIAsync<ReentrantPanel>();
            var panel = manager.GetUI<ReentrantPanel>();
            var reopened = UniTask.CompletedTask;
            panel.Disabled = () =>
            {
                panel.Disabled = null;
                reopened = panel.OnShowAsync();
            };
            var closing = panel.OnHideAsync();
            await reopened;
            await Capture(closing);
            Assert.IsTrue(panel.IsShow);
            Assert.AreEqual(UIPanelState.Shown, panel.State);
            Assert.IsTrue(panel.CanInteract);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator EnableCallbackCanStartANewerHide() => Run(async () =>
        {
            AddPrefab<ReentrantPanel>();
            await manager.PreloadUIAsync<ReentrantPanel>();
            var panel = manager.GetUI<ReentrantPanel>();
            var hiding = UniTask.CompletedTask;
            panel.Enabled = () =>
            {
                panel.Enabled = null;
                hiding = panel.OnHideAsync();
            };
            var opening = panel.OnShowAsync();
            await hiding;
            Assert.IsInstanceOf<OperationCanceledException>(await Capture(opening));
            Assert.IsFalse(panel.IsShow);
            Assert.AreEqual(UIPanelState.Hidden, panel.State);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator CancellationCallbackCanSupersedeTheTransitionDoingTheCancellation() => Run(async () =>
        {
            AddPrefab<UncooperativePanel>();
            await manager.PreloadUIAsync<UncooperativePanel>();
            var panel = manager.GetUI<UncooperativePanel>();
            var opening = panel.OnShowAsync();
            var latest = UniTask.CompletedTask;
            using (panel.AnimationToken.Register(() => latest = panel.OnShowAsync()))
            {
                var hiding = panel.OnHideAsync();
                panel.Gate.TrySetResult();
                await latest;
                Assert.IsInstanceOf<OperationCanceledException>(await Capture(opening));
                Assert.IsInstanceOf<OperationCanceledException>(await Capture(hiding));
                Assert.AreEqual(UIPanelState.Shown, panel.State);
                Assert.IsTrue(panel.CanInteract);
                Assert.IsFalse(manager.IsLockInput());
            }
        });

        [UnityTest]
        public IEnumerator FailedSupersedingShowRestoresCommittedAlphaInsteadOfInterruptedAlpha() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>();
            var panel = manager.GetUI<PrimaryPanel>();
            var gate = new UniTaskCompletionSource();
            panel.HideGate = gate;
            try
            {
                var hiding = panel.OnHideAsync();
                panel.GetComponent<CanvasGroup>().alpha = 0.25f;
                panel.FailShow = true;
                Assert.IsInstanceOf<InvalidOperationException>(await Capture(panel.OnShowAsync()));
                Assert.IsInstanceOf<OperationCanceledException>(await Capture(hiding));
                Assert.AreEqual(1, panel.GetComponent<CanvasGroup>().alpha);
                Assert.AreEqual(UIPanelState.Shown, panel.State);
                Assert.IsTrue(panel.CanInteract);
                Assert.IsFalse(manager.IsLockInput());
            }
            finally { gate.TrySetResult(); }
        });

        [UnityTest]
        public IEnumerator ManualUnlockCannotReleaseAnOwnedScope() => Run(async () =>
        {
            using (manager.GetLockInputScope())
            {
                manager.SetLockInput(false);
                Assert.IsTrue(manager.IsLockInput());
                manager.SetLockInput(true);
            }
            Assert.IsTrue(manager.IsLockInput());
            manager.SetLockInput(false);
            Assert.IsFalse(manager.IsLockInput());
            await UniTask.CompletedTask;
        });

        [UnityTest]
        public IEnumerator LockInputWhileReleasesOnMainThreadAfterBackgroundCompletion() => Run(async () =>
        {
            var completion = new UniTaskCompletionSource();
            manager.LockInputWhile(completion.Task);
            Assert.IsTrue(manager.IsLockInput());
            await UniTask.SwitchToThreadPool();
            completion.TrySetResult();
            await UniTask.SwitchToMainThread();
            await Frames(2);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator BackgroundCleanupIsRejectedBeforeChangingManagerState() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>();
            Exception clearError = null;
            Exception disposeError = null;
            await UniTask.SwitchToThreadPool();
            try { manager.ClearAll(); } catch (Exception error) { clearError = error; }
            try { manager.Dispose(); } catch (Exception error) { disposeError = error; }
            await UniTask.SwitchToMainThread();
            Assert.IsInstanceOf<InvalidOperationException>(clearError);
            Assert.IsInstanceOf<InvalidOperationException>(disposeError);
            Assert.IsTrue(manager.CurrentUIPanel.CanInteract);
            Assert.IsTrue(UIManager.TryGetInstance(out var current));
            Assert.AreSame(manager, current);
        });

        [UnityTest]
        public IEnumerator SafeDestroyFromOnEnableCancelsWithoutReenteringActivation() => Run(async () =>
        {
            AddPrefab<ReentrantPanel>();
            await manager.PreloadUIAsync<ReentrantPanel>();
            var panel = manager.GetUI<ReentrantPanel>();
            panel.Enabled = () => manager.SafeDestroy(panel);
            Assert.IsInstanceOf<OperationCanceledException>(await Capture(manager.ShowUIAsync<ReentrantPanel>()));
            await Frames(3);
            Assert.IsFalse(panel);
            Assert.IsNull(manager.CurrentUIPanel);
            Assert.AreEqual(1, loader.Releases);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator NonFiniteAnimationDurationFailsWithoutChangingTargets() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>();
            var panel = manager.CurrentUIPanel;
            var group = panel.GetComponent<CanvasGroup>();
            var alpha = group.alpha;
            var scale = panel.transform.localScale;
            foreach (var duration in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                Assert.IsInstanceOf<ArgumentOutOfRangeException>(await Capture(group.Fade(0, duration)));
                Assert.IsInstanceOf<ArgumentOutOfRangeException>(await Capture(panel.transform.Scale(Vector3.zero, duration)));
            }
            Assert.AreEqual(alpha, group.alpha);
            Assert.AreEqual(scale, panel.transform.localScale);
        });

        [UnityTest]
        public IEnumerator InvalidSettingsAreRejectedAndValidClippingRangeKeepsCanvasVisible() => Run(async () =>
        {
            manager.Dispose();
            await Frames(2);
            manager = new UIManager();
            settings.referenceResolution.x = float.NaN;
            Assert.Throws<ArgumentException>(() => manager.Initiate(settings, loader));
            settings.referenceResolution.x = 1080;
            settings.farClipPlane = float.PositiveInfinity;
            Assert.Throws<ArgumentException>(() => manager.Initiate(settings, loader));
            settings.farClipPlane = 20;
            settings.matchWidthOrHeight = float.NaN;
            Assert.Throws<ArgumentException>(() => manager.Initiate(settings, loader));
            Assert.IsFalse(UIManager.TryGetInstance(out _));
            settings.matchWidthOrHeight = 0.5f;
            settings.nearClipPlane = 10;
            manager.Initiate(settings, loader);
            Assert.Greater(manager.UICanvas.planeDistance, manager.UICamera.nearClipPlane);
            Assert.Less(manager.UICanvas.planeDistance, manager.UICamera.farClipPlane);
        });

        [UnityTest]
        public IEnumerator FailedParallelAnimationStopsSiblingBeforeRestoringState() => Run(async () =>
        {
            AddPrefab<ParallelFailurePanel>();
            await manager.ShowUIAsync<ParallelFailurePanel>();
            var panel = manager.GetUI<ParallelFailurePanel>();
            Assert.IsInstanceOf<InvalidOperationException>(await Capture(manager.HideCurrentAsync()));
            await Frames(4);
            Assert.AreEqual(1, panel.GetComponent<CanvasGroup>().alpha);
            Assert.IsTrue(panel.CanInteract);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator UncooperativeAnimationCannotKeepInputLockedAfterClear() => Run(async () =>
        {
            AddPrefab<UncooperativePanel>();
            var opening = manager.ShowUIAsync<UncooperativePanel>();
            var panel = manager.GetUI<UncooperativePanel>();
            try
            {
                manager.ClearAll();
                Assert.IsInstanceOf<OperationCanceledException>(await Capture(opening));
                await Frames(2);
                Assert.IsFalse(manager.IsLockInput());
            }
            finally { panel.Gate.TrySetResult(); }
        });

        [UnityTest]
        public IEnumerator ThrowingCancellationCallbackDoesNotInterruptCleanupOrPendingCompletions() => Run(async () =>
        {
            AddPrefab<UncooperativePanel>();
            var opening = manager.ShowUIAsync<UncooperativePanel>(panel => panel.ThrowOnCancel = true);
            var panel = manager.GetUI<UncooperativePanel>();
            var pending = manager.ShowUIAsync<SecondaryPanel>();
            try
            {
                LogAssert.Expect(LogType.Exception, new Regex("cancel hook failed"));
                Assert.DoesNotThrow(() => manager.ClearAll());
                Assert.IsInstanceOf<OperationCanceledException>(await Capture(opening));
                Assert.IsInstanceOf<OperationCanceledException>(await Capture(pending));
                await Frames(2);
                Assert.IsFalse(manager.IsLockInput());
                Assert.IsNull(manager.CurrentUIPanel);
            }
            finally { panel.Gate.TrySetResult(); }
        });

        [UnityTest]
        public IEnumerator RequestedBaseTypeCacheIsRemovedWhenDerivedInstanceIsDestroyed() => Run(async () =>
        {
            loader.Prefabs["UI/ControlledPanel"] = loader.Prefabs["UI/PrimaryPanel"];
            await manager.ShowUIAsync<ControlledPanel>();
            var first = manager.GetUI<ControlledPanel>();
            manager.SafeDestroy(first);
            Assert.IsNull(manager.GetUI<ControlledPanel>());
            await manager.ShowUIAsync<ControlledPanel>();
            Assert.AreNotSame(first, manager.GetUI<ControlledPanel>());
            await Frames(3);
            Assert.AreEqual(1, loader.Releases);
        });

        [UnityTest]
        public IEnumerator RequestedBaseTypeShowFailureDoesNotLeaveHiddenNavigationEntry() => Run(async () =>
        {
            loader.Prefabs["UI/ControlledPanel"] = loader.Prefabs["UI/PrimaryPanel"];
            Assert.IsInstanceOf<InvalidOperationException>(await Capture(manager.ShowUIAsync<ControlledPanel>(p => p.FailShow = true)));
            Assert.IsNull(manager.CurrentUIPanel);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator DestroyingOnlyPanelComponentDoesNotOrphanItsObjectOrResource() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>();
            var panel = manager.GetUI<PrimaryPanel>();
            var go = panel.gameObject;
            Object.Destroy(panel);
            await Frames(2);
            manager.ClearAll();
            await Frames(3);
            Assert.IsFalse(go);
            Assert.AreEqual(1, loader.Releases);
        });

        [UnityTest]
        public IEnumerator SafeDestroyInInitializerDoesNotInsertDoomedPanelIntoNavigation() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>();
            Assert.IsInstanceOf<OperationCanceledException>(await Capture(manager.ShowUIAsync<SecondaryPanel>(manager.SafeDestroy)));
            Assert.AreSame(manager.GetUI<PrimaryPanel>(), manager.CurrentUIPanel);
            Assert.IsNull(manager.GetUI<SecondaryPanel>());
        });

        [UnityTest]
        public IEnumerator FailedUnmanagedShowRestoresExistingPanelSortingAndNavigation() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>();
            await manager.ShowUIAsync<SecondaryPanel>();
            var panel = manager.GetUI<PrimaryPanel>();
            var previousOrder = panel.OrderInLayer;
            Assert.IsInstanceOf<InvalidOperationException>(await Capture(manager.ShowUIUnmanagableAsync<PrimaryPanel>(
                p => throw new InvalidOperationException("unmanaged init failed"), sortingOrder: 900)));
            Assert.AreEqual(previousOrder, panel.OrderInLayer);
            Assert.AreSame(manager.GetUI<SecondaryPanel>(), manager.CurrentUIPanel);
        });

        [UnityTest]
        public IEnumerator ShownPanelsKeepOverrideSortingAfterInactiveSetup() => Run(async () =>
        {
            await manager.PreloadUIAsync<ThirdPanel>("Blocker");
            await manager.ShowUIAsync<PrimaryPanel>();
            await manager.ShowUIAsync<SecondaryPanel>();
            await manager.ShowUIAsync<ThirdPanel>();
            await manager.BackAsync();
            await manager.HideCurrentAsync();
            await manager.ShowUIAsync<SecondaryPanel>();
            foreach (var panel in new AbstractUIPanel[] { manager.GetUI<PrimaryPanel>(), manager.GetUI<SecondaryPanel>() })
            {
                var canvas = panel.GetComponent<Canvas>();
                Assert.IsTrue(canvas.overrideSorting, panel.name);
                Assert.AreEqual("UI", canvas.sortingLayerName, panel.name);
                Assert.AreEqual(panel.OrderInLayer, canvas.sortingOrder, panel.name);
            }
            Assert.Greater(manager.GetUI<SecondaryPanel>().OrderInLayer, manager.GetUI<PrimaryPanel>().OrderInLayer);
            await manager.ShowUIUnmanagableAsync<ThirdPanel>(layer: "Blocker", sortingOrder: 900);
            var unmanaged = manager.GetUI<ThirdPanel>().GetComponent<Canvas>();
            Assert.IsTrue(unmanaged.overrideSorting);
            Assert.AreEqual("Blocker", unmanaged.sortingLayerName);
            Assert.AreEqual(900, unmanaged.sortingOrder);
        });

        [UnityTest]
        public IEnumerator InputLockAlsoPreventsKeyboardSubmitOnSelectedButton() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>();
            var buttonObject = new GameObject("Submit", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(manager.CurrentUIPanel.transform, false);
            var button = buttonObject.GetComponent<Button>();
            var clicks = 0;
            button.onClick.AddListener(() => clicks++);
            var eventSystem = Object.FindObjectOfType<EventSystem>();
            eventSystem.SetSelectedGameObject(buttonObject);
            using (manager.GetLockInputScope())
            {
                ExecuteEvents.Execute(buttonObject, new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
                Assert.AreEqual(0, clicks);
                Assert.IsFalse(button.IsInteractable());
            }
            ExecuteEvents.Execute(buttonObject, new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
            Assert.AreEqual(1, clicks);
        });
    }
}