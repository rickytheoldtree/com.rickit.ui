using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using RicKit.UI.Component;
using RicKit.UI.Extensions.TaskExtension;
using RicKit.UI.Interfaces;
using RicKit.UI.Panels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RicKit.UI.Tests
{
    public class ControlledPanel : AbstractUIPanel
    {
        public int ShowCount;
        public int HideCount;
        public int EnableCount;
        public int Data;
        public int DataOnEnable;
        public bool FailShow;
        public bool FailHide;
        public bool Keep;
        public bool AutoDestroy;
        public UniTaskCompletionSource ShowGate;
        public UniTaskCompletionSource HideGate;
        public override bool DontDestroyOnClear => Keep;
        public override bool DestroyOnClose => AutoDestroy;
        private void OnEnable() { EnableCount++; DataOnEnable = Data; }
        public override void OnESCClick() { }
        protected override async UniTask OnAnimationIn(CancellationToken token)
        {
            ShowCount++;
            if (FailShow) throw new InvalidOperationException("show failed");
            if (ShowGate != null) await ShowGate.Task.AttachExternalCancellation(token);
            token.ThrowIfCancellationRequested();
            CanvasGroup.alpha = 1;
        }
        protected override async UniTask OnAnimationOut(CancellationToken token)
        {
            HideCount++;
            if (FailHide) throw new InvalidOperationException("hide failed");
            if (HideGate != null) await HideGate.Task.AttachExternalCancellation(token);
            token.ThrowIfCancellationRequested();
            CanvasGroup.alpha = 0;
        }
    }
    public class PrimaryPanel : ControlledPanel { }
    public class SecondaryPanel : ControlledPanel { }
    public class ThirdPanel : ControlledPanel { }

    public class RetainedPopupPanel : PopUIPanel
    {
        public override bool DontDestroyOnClear => true;
        public Vector3 PanelScale => panel.localScale;
        public float BlockerAlpha => cgBlocker.alpha;
        public void Configure()
        {
            panel = new GameObject("Popup", typeof(RectTransform)).transform;
            panel.SetParent(transform, false);
            cgBlocker = new GameObject("PopupBlocker", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<CanvasGroup>();
            cgBlocker.transform.SetParent(transform, false);
        }
    }
    internal sealed class RecordingLoader : IReleasablePanelLoader
    {
        public readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        public int Loads;
        public int Releases;
        public bool FailNext;
        public UniTaskCompletionSource NextLoadGate;
        public GameObject LoadPrefab(string path)
        {
            Loads++;
            if (FailNext) { FailNext = false; throw new InvalidOperationException("load failed"); }
            return Prefabs.TryGetValue(path, out var prefab) ? prefab : null;
        }
        public async UniTask<GameObject> LoadPrefabAsync(string path)
        {
            var prefab = LoadPrefab(path);
            var gate = NextLoadGate;
            NextLoadGate = null;
            if (gate != null) await gate.Task;
            return prefab;
        }
        public void ReleasePrefab(string path, GameObject prefab) { Releases++; }
    }

    internal sealed class CancellationAwareLoader : ICancellablePanelLoader
    {
        public CancellationToken ReceivedToken;
        public int Cancellations;
        public GameObject LoadPrefab(string path) => throw new InvalidOperationException("Unexpected sync load.");
        public UniTask<GameObject> LoadPrefabAsync(string path) => throw new InvalidOperationException("Missing cancellation token.");
        public async UniTask<GameObject> LoadPrefabAsync(string path, CancellationToken token)
        {
            ReceivedToken = token;
            try { await UniTask.Delay(10000, cancellationToken: token); }
            catch (OperationCanceledException) { Cancellations++; throw; }
            return null;
        }
    }
    internal sealed class WorkerReturningLoader : IReleasablePanelLoader
    {
        public GameObject Prefab;
        public bool FailNext;
        public bool ReleasedOnMainThread;
        public GameObject LoadPrefab(string path) => Prefab;
        public async UniTask<GameObject> LoadPrefabAsync(string path)
        {
            var fail = FailNext;
            FailNext = false;
            await UniTask.SwitchToThreadPool();
            if (fail) throw new InvalidOperationException("background load failed");
            return Prefab;
        }
        public void ReleasePrefab(string path, GameObject prefab) => ReleasedOnMainThread = PlayerLoopHelper.IsMainThread;
    }
    public class WorkerAnimationPanel : AbstractUIPanel
    {
        public bool FailHide;
        public override void OnESCClick() { }
        protected override async UniTask OnAnimationIn(CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();
            token.ThrowIfCancellationRequested();
        }
        protected override async UniTask OnAnimationOut(CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();
            token.ThrowIfCancellationRequested();
            if (FailHide) throw new InvalidOperationException("background animation failed");
        }
    }
    public partial class UIManagerTests
    {
        private UIManager manager;
        private UISettings settings;
        private RecordingLoader loader;
        private readonly List<GameObject> extras = new List<GameObject>();

        [UnitySetUp]
        public IEnumerator SetUp() => Run(async () =>
        {
            UIManager.Shutdown();
            await Frames(2);
            Time.timeScale = 1;
            settings = ScriptableObject.CreateInstance<UISettings>();
            loader = new RecordingLoader();
            AddPrefab<PrimaryPanel>();
            AddPrefab<SecondaryPanel>();
            AddPrefab<ThirdPanel>();
            AddPrefab<RetainedPopupPanel>();
            AddPrefab<WorkerAnimationPanel>();
            manager = new UIManager();
            manager.Initiate(settings, loader);
        });

        [UnityTearDown]
        public IEnumerator TearDown() => Run(async () =>
        {
            Time.timeScale = 1;
            manager?.Dispose();
            UIManager.Shutdown();
            await Frames(3);
            if (loader != null)
                foreach (var prefab in loader.Prefabs.Values) if (prefab) Object.Destroy(prefab);
            foreach (var extra in extras) if (extra) Object.Destroy(extra);
            extras.Clear();
            if (settings) Object.Destroy(settings);
            await Frames(2);
        });

        private void AddPrefab<T>() where T : AbstractUIPanel
        {
            var go = new GameObject(typeof(T).Name, typeof(RectTransform));
            go.SetActive(false);
            go.AddComponent<T>();
            loader.Prefabs.Add("UI/" + typeof(T).Name, go);
        }
        private static IEnumerator Run(Func<UniTask> test)
            => UniTask.ToCoroutine(() => test().Timeout(TimeSpan.FromSeconds(10)));
        private static async UniTask Frames(int count)
        {
            for (var i = 0; i < count; i++) await UniTask.NextFrame();
        }
        private static async UniTask<Exception> Capture(UniTask task)
        {
            try { await task; return null; }
            catch (Exception exception) { return exception; }
        }

        [UnityTest]
        public IEnumerator DuplicateShowReusesInstanceAndDoesNotDuplicateNavigation() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>(panel => panel.Data = 42);
            var first = manager.GetUI<PrimaryPanel>();
            Assert.AreEqual(42, first.DataOnEnable);
            await manager.ShowUIAsync<PrimaryPanel>(panel => panel.Data = 99);
            Assert.AreSame(first, manager.GetUI<PrimaryPanel>());
            Assert.AreEqual(1, loader.Loads);
            Assert.AreEqual(1, first.ShowCount);
            Assert.AreEqual(99, first.Data);
            await manager.BackAsync();
            Assert.IsNull(manager.CurrentUIPanel);
            Assert.IsFalse(first.IsShow);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator ReopeningExistingPanelMovesItToTop() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>();
            await manager.ShowUIAsync<SecondaryPanel>();
            await manager.ShowUIAsync<PrimaryPanel>();
            await manager.BackAsync();
            Assert.AreSame(manager.GetUI<SecondaryPanel>(), manager.CurrentUIPanel);
            await manager.BackAsync();
            Assert.IsNull(manager.CurrentUIPanel);
        });

        [UnityTest]
        public IEnumerator ConcurrentShowAndPreloadOnlyLoadOneInstance() => Run(async () =>
        {
            var gate = loader.NextLoadGate = new UniTaskCompletionSource();
            var preload = manager.PreloadUIAsync<PrimaryPanel>();
            var first = manager.ShowUIAsync<PrimaryPanel>();
            var second = manager.ShowUIAsync<PrimaryPanel>();
            Assert.AreEqual(1, loader.Loads);
            Assert.AreEqual(UniTaskStatus.Pending, second.Status);
            gate.TrySetResult();
            await UniTask.WhenAll(preload, first, second);
            Assert.AreEqual(1, loader.Loads);
            Assert.AreEqual(1, manager.GetUI<PrimaryPanel>().ShowCount);
            await manager.BackAsync();
            Assert.IsNull(manager.CurrentUIPanel);
        });

        [UnityTest]
        public IEnumerator LoadFailureUnlocksInputAndQueueContinues() => Run(async () =>
        {
            loader.FailNext = true;
            var failed = manager.ShowUIAsync<PrimaryPanel>();
            var next = manager.ShowUIAsync<SecondaryPanel>();
            Assert.IsInstanceOf<InvalidOperationException>(await Capture(failed));
            await next;
            Assert.AreSame(manager.GetUI<SecondaryPanel>(), manager.CurrentUIPanel);
            Assert.IsNull(manager.GetUI<PrimaryPanel>());
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator InitAndAnimationFailuresPreservePreviousNavigation() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>();
            Assert.IsInstanceOf<InvalidOperationException>(await Capture(manager.ShowUIAsync<SecondaryPanel>(
                panel => throw new InvalidOperationException("init failed"))));
            Assert.AreSame(manager.GetUI<PrimaryPanel>(), manager.CurrentUIPanel);
            Assert.IsInstanceOf<InvalidOperationException>(await Capture(manager.ShowUIAsync<SecondaryPanel>(
                panel => panel.FailShow = true)));
            Assert.AreSame(manager.GetUI<PrimaryPanel>(), manager.CurrentUIPanel);
            Assert.AreEqual(UIPanelState.Hidden, manager.GetUI<SecondaryPanel>().State);
            Assert.IsFalse(manager.GetUI<SecondaryPanel>().IsShow);
            Assert.IsFalse(manager.IsLockInput());
            await manager.ShowUIAsync<SecondaryPanel>(panel => panel.FailShow = false);
        });

        [UnityTest]
        public IEnumerator EventFailureUnlocksInputAndCanBeRetried() => Run(async () =>
        {
            manager.OnShowEnd = panel => throw new InvalidOperationException("event failed");
            Assert.IsInstanceOf<InvalidOperationException>(await Capture(manager.ShowUIAsync<PrimaryPanel>()));
            Assert.IsFalse(manager.IsLockInput());
            Assert.IsNull(manager.CurrentUIPanel);
            manager.OnShowEnd = null;
            await manager.ShowUIAsync<PrimaryPanel>();
            manager.OnHide = panel => throw new InvalidOperationException("hide event failed");
            Assert.IsInstanceOf<InvalidOperationException>(await Capture(manager.CloseCurrentAsync()));
            Assert.AreSame(manager.GetUI<PrimaryPanel>(), manager.CurrentUIPanel);
            Assert.IsTrue(manager.CurrentUIPanel.CanInteract);
            Assert.IsFalse(manager.IsLockInput());
            manager.OnHide = null;
            await manager.CloseCurrentAsync();
        });

        [UnityTest]
        public IEnumerator HideFailureKeepsPanelInNavigationUntilSuccessfulRetry() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>(panel => panel.FailHide = true);
            Assert.IsInstanceOf<InvalidOperationException>(await Capture(manager.CloseCurrentAsync(true)));
            Assert.AreSame(manager.GetUI<PrimaryPanel>(), manager.CurrentUIPanel);
            Assert.IsTrue(manager.CurrentUIPanel.CanInteract);
            Assert.IsFalse(manager.IsLockInput());
            manager.GetUI<PrimaryPanel>().FailHide = false;
            await manager.CloseCurrentAsync(true);
            Assert.IsNull(manager.CurrentUIPanel);
            Assert.IsNull(manager.GetUI<PrimaryPanel>());
            await Frames(3);
            Assert.AreEqual(1, loader.Releases);
        });

        [UnityTest]
        public IEnumerator CloseUntilWaitsForEveryHideAnimation() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>();
            await manager.ShowUIAsync<SecondaryPanel>();
            await manager.ShowUIAsync<ThirdPanel>();
            var secondGate = manager.GetUI<SecondaryPanel>().HideGate = new UniTaskCompletionSource();
            var thirdGate = manager.GetUI<ThirdPanel>().HideGate = new UniTaskCompletionSource();
            var close = manager.CloseUntilAsync<PrimaryPanel>();
            Assert.AreEqual(UniTaskStatus.Pending, close.Status);
            Assert.AreEqual(0, manager.GetUI<SecondaryPanel>().HideCount);
            thirdGate.TrySetResult();
            await Frames(1);
            Assert.AreEqual(UniTaskStatus.Pending, close.Status);
            Assert.AreEqual(1, manager.GetUI<SecondaryPanel>().HideCount);
            secondGate.TrySetResult();
            await close;
            Assert.AreSame(manager.GetUI<PrimaryPanel>(), manager.CurrentUIPanel);
            Assert.IsFalse(manager.GetUI<SecondaryPanel>().IsShow);
            Assert.IsFalse(manager.GetUI<ThirdPanel>().IsShow);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator CloseByTypeWorksForUnmanagedPanel() => Run(async () =>
        {
            await manager.ShowUIUnmanagableAsync<PrimaryPanel>();
            Assert.IsNull(manager.CurrentUIPanel);
            await manager.CloseAsync<PrimaryPanel>(true);
            Assert.IsNull(manager.GetUI<PrimaryPanel>());
            await Frames(3);
            Assert.AreEqual(1, loader.Releases);
        });

        [UnityTest]
        public IEnumerator ReplacingPanelWithItselfDoesNotHideOrDestroyIt() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>();
            var panel = manager.GetUI<PrimaryPanel>();
            await manager.ShowThenClosePrevAsync<PrimaryPanel>(destroy: true);
            await manager.CloseThenShowUIAsync<PrimaryPanel>(destroy: true);
            await manager.ShowThenHidePrevAsync<PrimaryPanel>();
            Assert.AreSame(panel, manager.CurrentUIPanel);
            Assert.AreEqual(0, panel.HideCount);
            Assert.IsTrue(panel.CanInteract);
        });

        [UnityTest]
        public IEnumerator ShowThenCloseUsesPreviousOrderExactlyOnce() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>();
            await manager.ShowUIAsync<SecondaryPanel>();
            var enteringOrder = 0;
            manager.OnShowEnd = panel => enteringOrder = panel.OrderInLayer;
            await manager.ShowThenClosePrevAsync<ThirdPanel>();
            Assert.AreEqual(15, enteringOrder);
            Assert.AreEqual(10, manager.GetUI<ThirdPanel>().OrderInLayer);
            await manager.BackAsync();
            Assert.AreSame(manager.GetUI<PrimaryPanel>(), manager.CurrentUIPanel);
        });

        [UnityTest]
        public IEnumerator FailedReplacementRestoresPreviousVisiblePanel() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>();
            Assert.IsInstanceOf<InvalidOperationException>(await Capture(manager.CloseThenShowUIAsync<SecondaryPanel>(
                panel => panel.FailShow = true, destroy: true)));
            Assert.AreSame(manager.GetUI<PrimaryPanel>(), manager.CurrentUIPanel);
            Assert.IsTrue(manager.CurrentUIPanel.CanInteract);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator MissingOrMalformedPrefabFailsBeforeReplacingPreviousPanel() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>();
            var secondary = loader.Prefabs["UI/SecondaryPanel"];
            loader.Prefabs.Remove("UI/SecondaryPanel");
            Assert.IsInstanceOf<InvalidOperationException>(await Capture(manager.CloseThenShowUIAsync<SecondaryPanel>()));
            Assert.IsTrue(manager.CurrentUIPanel.CanInteract);
            loader.Prefabs["UI/SecondaryPanel"] = loader.Prefabs["UI/PrimaryPanel"];
            Assert.IsInstanceOf<InvalidOperationException>(await Capture(manager.ShowUIAsync<SecondaryPanel>()));
            Assert.AreEqual(1, loader.Releases);
            loader.Prefabs["UI/SecondaryPanel"] = secondary;
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator ClearDuringLegacyLoadCancelsQueueAndReleasesLateResult() => Run(async () =>
        {
            var gate = loader.NextLoadGate = new UniTaskCompletionSource();
            var opening = manager.ShowUIAsync<PrimaryPanel>();
            var pending = manager.ShowUIAsync<ThirdPanel>();
            manager.ClearAll();
            Assert.IsInstanceOf<OperationCanceledException>(await Capture(opening));
            Assert.IsInstanceOf<OperationCanceledException>(await Capture(pending));
            await manager.ShowUIAsync<SecondaryPanel>();
            Assert.IsFalse(manager.IsLockInput());
            gate.TrySetResult();
            await Frames(3);
            Assert.IsNull(manager.GetUI<PrimaryPanel>());
            Assert.AreSame(manager.GetUI<SecondaryPanel>(), manager.CurrentUIPanel);
            Assert.AreEqual(1, loader.Releases);
        });

        [UnityTest]
        public IEnumerator ClearDuringAnimationCancelsAndDoesNotResurrectPanel() => Run(async () =>
        {
            var gate = new UniTaskCompletionSource();
            var opening = manager.ShowUIAsync<PrimaryPanel>(panel => panel.ShowGate = gate);
            var panel = manager.GetUI<PrimaryPanel>();
            manager.ClearAll();
            Assert.IsInstanceOf<OperationCanceledException>(await Capture(opening));
            gate.TrySetResult();
            await Frames(3);
            Assert.IsFalse(panel);
            Assert.IsNull(manager.CurrentUIPanel);
            Assert.IsFalse(manager.IsLockInput());
            Assert.AreEqual(1, loader.Releases);
        });

        [UnityTest]
        public IEnumerator ClearPreservesOptedInPanel() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>(panel => panel.Keep = true);
            await manager.ShowUIAsync<SecondaryPanel>();
            var retained = manager.GetUI<PrimaryPanel>();
            manager.ClearAll();
            await Frames(3);
            Assert.AreSame(retained, manager.GetUI<PrimaryPanel>());
            Assert.AreSame(retained, manager.CurrentUIPanel);
            Assert.IsTrue(retained.CanInteract);
            Assert.AreEqual(1, loader.Releases);
        });

        [UnityTest]
        public IEnumerator NeverActivatedPreloadStillReleasesAfterDestruction() => Run(async () =>
        {
            await manager.PreloadUIAsync<PrimaryPanel>();
            var panel = manager.GetUI<PrimaryPanel>();
            Assert.AreEqual(0, panel.EnableCount);
            Assert.IsFalse(panel.IsShow);
            manager.ClearAll();
            Assert.AreEqual(0, loader.Releases);
            await Frames(3);
            Assert.IsFalse(panel);
            Assert.AreEqual(1, loader.Releases);
        });

        [UnityTest]
        public IEnumerator ExternalDestructionEvictsPanelAndReleasesResource() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>();
            Object.Destroy(manager.GetUI<PrimaryPanel>().gameObject);
            await Frames(3);
            Assert.IsNull(manager.GetUI<PrimaryPanel>());
            Assert.IsNull(manager.CurrentUIPanel);
            Assert.AreEqual(1, loader.Releases);
            await manager.ShowUIAsync<PrimaryPanel>();
            Assert.AreEqual(2, loader.Loads);
        });

        [UnityTest]
        public IEnumerator WaitForHideHandlesMissingAndDestroyedPanel() => Run(async () =>
        {
            await manager.WaitUntilUIHideEnd<PrimaryPanel>();
            await manager.ShowUIAsync<PrimaryPanel>();
            var wait = manager.WaitUntilUIHideEnd<PrimaryPanel>();
            manager.SafeDestroy(manager.GetUI<PrimaryPanel>());
            await wait;
            Assert.IsNull(manager.GetUI<PrimaryPanel>());
        });

        [UnityTest]
        public IEnumerator ShutdownDuringLoadAllowsReinitializationWithoutLateResurrection() => Run(async () =>
        {
            var gate = loader.NextLoadGate = new UniTaskCompletionSource();
            var opening = manager.ShowUIAsync<PrimaryPanel>();
            UIManager.Shutdown();
            Assert.IsInstanceOf<OperationCanceledException>(await Capture(opening));
            Assert.IsFalse(UIManager.TryGetInstance(out _));
            var replacement = new UIManager();
            replacement.Initiate(settings, loader);
            manager = replacement;
            await manager.ShowUIAsync<SecondaryPanel>();
            gate.TrySetResult();
            await Frames(3);
            Assert.IsNull(manager.GetUI<PrimaryPanel>());
            Assert.AreSame(manager.GetUI<SecondaryPanel>(), manager.CurrentUIPanel);
            Assert.AreEqual(1, loader.Releases);
        });

        [UnityTest]
        public IEnumerator InvalidInitializationCanBeRetried() => Run(async () =>
        {
            manager.Dispose();
            await Frames(2);
            manager = new UIManager();
            Assert.Throws<InvalidOperationException>(() => manager.Initiate((UISettings)null, loader));
            Assert.IsFalse(UIManager.TryGetInstance(out _));
            manager.Initiate(settings, loader);
            await manager.ShowUIAsync<PrimaryPanel>();
            Assert.IsTrue(manager.CurrentUIPanel.CanInteract);
        });

        [UnityTest]
        public IEnumerator ScopedInputLocksAreNestedAndIdempotent() => Run(async () =>
        {
            var first = manager.GetLockInputScope();
            var second = manager.GetLockInputScope();
            first.Dispose();
            first.Dispose();
            Assert.IsTrue(manager.IsLockInput());
            second.Dispose();
            Assert.IsFalse(manager.IsLockInput());
            var source = new UniTaskCompletionSource();
            manager.LockInputWhile(source.Task);
            Assert.IsTrue(manager.IsLockInput());
            source.TrySetCanceled();
            await Frames(1);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator FadeAndScaleCompleteWhileGameIsPaused() => Run(async () =>
        {
            var go = new GameObject("Animation", typeof(CanvasGroup));
            extras.Add(go);
            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 0;
            go.transform.localScale = Vector3.zero;
            Time.timeScale = 0;
            await UniTask.WhenAll(group.Fade(1, 0.05f), go.transform.Scale(Vector3.one, 0.05f));
            Assert.AreEqual(1, group.alpha);
            Assert.AreEqual(Vector3.one, go.transform.localScale);
        });

        [UnityTest]
        public IEnumerator CancelledAnimationDoesNotSnapToEndValue() => Run(async () =>
        {
            var go = new GameObject("Animation", typeof(CanvasGroup));
            extras.Add(go);
            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 0;
            using (var source = new CancellationTokenSource())
            {
                var fade = group.Fade(1, 1, cancellationToken: source.Token);
                await Frames(1);
                var beforeCancellation = group.alpha;
                source.Cancel();
                Assert.IsInstanceOf<OperationCanceledException>(await Capture(fade));
                Assert.AreEqual(beforeCancellation, group.alpha);
                Assert.Less(group.alpha, 1);
            }
        });

        [UnityTest]
        public IEnumerator LatestDirectTransitionWins() => Run(async () =>
        {
            await manager.PreloadUIAsync<PrimaryPanel>();
            var panel = manager.GetUI<PrimaryPanel>();
            panel.ShowGate = new UniTaskCompletionSource();
            var show = panel.OnShowAsync();
            await panel.OnHideAsync();
            Assert.IsInstanceOf<OperationCanceledException>(await Capture(show));
            panel.ShowGate.TrySetResult();
            await Frames(1);
            Assert.IsFalse(panel.IsShow);
            Assert.AreEqual(UIPanelState.Hidden, panel.State);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator PrecancelledTransitionDoesNotActivatePanel() => Run(async () =>
        {
            await manager.PreloadUIAsync<PrimaryPanel>();
            using (var source = new CancellationTokenSource())
            {
                source.Cancel();
                Assert.IsInstanceOf<OperationCanceledException>(await Capture(manager.GetUI<PrimaryPanel>().OnShowAsync(source.Token)));
            }
            Assert.IsFalse(manager.GetUI<PrimaryPanel>().IsShow);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator ExistingEventSystemSurvivesManagerShutdown() => Run(async () =>
        {
            manager.Dispose();
            await Frames(2);
            var external = new GameObject("External EventSystem", typeof(EventSystem));
            extras.Add(external);
            manager = new UIManager();
            manager.Initiate(settings, loader);
            manager.Dispose();
            await Frames(2);
            Assert.IsTrue(external);
        });

        [UnityTest]
        public IEnumerator ClearDoesNotDestroyWorkEnqueuedByCancellationContinuation() => Run(async () =>
        {
            var gate = loader.NextLoadGate = new UniTaskCompletionSource();
            var first = manager.ShowUIAsync<PrimaryPanel>();
            var resumed = new UniTaskCompletionSource();
            async UniTask ResumeAfterClear()
            {
                Assert.IsInstanceOf<OperationCanceledException>(await Capture(first));
                await manager.ShowUIAsync<SecondaryPanel>();
                resumed.TrySetResult();
            }
            ResumeAfterClear().Forget();
            manager.ClearAll();
            await resumed.Task;
            gate.TrySetResult();
            await Frames(3);
            Assert.IsTrue(manager.GetUI<SecondaryPanel>());
            Assert.IsTrue(manager.GetUI<SecondaryPanel>().CanInteract);
            Assert.AreSame(manager.GetUI<SecondaryPanel>(), manager.CurrentUIPanel);
        });

        [UnityTest]
        public IEnumerator CloseByTypeFailurePreservesAllNavigationEntries() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>(panel => panel.FailHide = true);
            await manager.ShowUIAsync<SecondaryPanel>();
            await manager.ShowUIAsync<ThirdPanel>();
            Assert.IsInstanceOf<InvalidOperationException>(await Capture(manager.CloseAsync<PrimaryPanel>()));
            Assert.AreSame(manager.GetUI<ThirdPanel>(), manager.CurrentUIPanel);
            await manager.BackAsync();
            await manager.BackAsync();
            Assert.AreSame(manager.GetUI<PrimaryPanel>(), manager.CurrentUIPanel);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator HideEndFailureRestoresVisibilityAndInput() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>();
            manager.OnHideEnd = panel => throw new InvalidOperationException("hide end failed");
            Assert.IsInstanceOf<InvalidOperationException>(await Capture(manager.CloseCurrentAsync()));
            Assert.IsTrue(manager.CurrentUIPanel.CanInteract);
            Assert.AreEqual(1, manager.CurrentUIPanel.GetComponent<CanvasGroup>().alpha);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator InitializationCallbackCanClearWithoutResurrectingPanel() => Run(async () =>
        {
            Assert.IsInstanceOf<OperationCanceledException>(await Capture(manager.ShowUIAsync<PrimaryPanel>(
                panel => manager.ClearAll())));
            await Frames(3);
            Assert.IsNull(manager.CurrentUIPanel);
            Assert.IsNull(manager.GetUI<PrimaryPanel>());
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator DefaultLoaderHonorsPrecancelledTokenAndMissingAsset() => Run(async () =>
        {
            var defaultLoader = new DefaultPanelLoader();
            using (var source = new CancellationTokenSource())
            {
                source.Cancel();
                var cancelled = defaultLoader.LoadPrefabAsync("__rickit_missing__", source.Token);
                Assert.IsInstanceOf<OperationCanceledException>(await Capture(cancelled.AsUniTask()));
            }
            Assert.IsNull(await defaultLoader.LoadPrefabAsync("__rickit_missing__"));
        });
        [UnityTest]
        public IEnumerator CancelledRetainedPopupRestoresScaleAndBlocker() => Run(async () =>
        {
            await manager.ShowUIAsync<RetainedPopupPanel>(panel => panel.Configure());
            var popup = manager.GetUI<RetainedPopupPanel>();
            var hiding = manager.HideCurrentAsync();
            await Frames(1);
            manager.ClearAll();
            Assert.IsInstanceOf<OperationCanceledException>(await Capture(hiding));
            await Frames(2);
            Assert.IsTrue(popup.CanInteract);
            Assert.AreEqual(Vector3.one, popup.PanelScale);
            Assert.AreEqual(1, popup.BlockerAlpha);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator DestroyOnClosePolicyReleasesCachedPanel() => Run(async () =>
        {
            await manager.ShowUIAsync<PrimaryPanel>(panel => panel.AutoDestroy = true);
            await manager.BackAsync();
            Assert.IsNull(manager.GetUI<PrimaryPanel>());
            await Frames(3);
            Assert.AreEqual(1, loader.Releases);
        });

        [UnityTest]
        public IEnumerator SafeDestroyDuringShowDoesNotReactivateDoomedPanel() => Run(async () =>
        {
            var show = manager.ShowUIAsync<PrimaryPanel>(panel => panel.ShowGate = new UniTaskCompletionSource());
            var panel = manager.GetUI<PrimaryPanel>();
            manager.SafeDestroy(panel);
            Assert.IsInstanceOf<OperationCanceledException>(await Capture(show));
            Assert.IsFalse(panel.IsShow);
            Assert.IsNull(manager.CurrentUIPanel);
            await Frames(3);
            Assert.AreEqual(1, loader.Releases);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator DestroyedAnimationTargetCancelsInsteadOfThrowingMissingReference() => Run(async () =>
        {
            var go = new GameObject("Animation", typeof(CanvasGroup));
            extras.Add(go);
            var fade = go.GetComponent<CanvasGroup>().Fade(0, 1);
            Object.Destroy(go);
            Assert.IsInstanceOf<OperationCanceledException>(await Capture(fade));
        });

        [UnityTest]
        public IEnumerator RepeatedShutdownDoesNotAffectReplacementManager() => Run(async () =>
        {
            var old = manager;
            old.Dispose();
            manager = new UIManager();
            manager.Initiate(settings, loader);
            old.Dispose();
            await Frames(3);
            Assert.IsTrue(UIManager.TryGetInstance(out var current));
            Assert.AreSame(manager, current);
            Assert.IsNotNull(Object.FindObjectOfType<EventSystem>());
            await manager.ShowUIAsync<PrimaryPanel>();
            Assert.IsTrue(manager.CurrentUIPanel.CanInteract);
        });
        [UnityTest]
        public IEnumerator OptionalCancellableLoaderReceivesClearCancellation() => Run(async () =>
        {
            manager.Dispose();
            var cancellationAware = new CancellationAwareLoader();
            manager = new UIManager();
            manager.Initiate(settings, cancellationAware);
            var show = manager.ShowUIAsync<PrimaryPanel>();
            Assert.IsTrue(cancellationAware.ReceivedToken.CanBeCanceled);
            manager.ClearAll();
            Assert.IsInstanceOf<OperationCanceledException>(await Capture(show));
            await Frames(2);
            Assert.IsTrue(cancellationAware.ReceivedToken.IsCancellationRequested);
            Assert.AreEqual(1, cancellationAware.Cancellations);
            Assert.IsNull(manager.CurrentUIPanel);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator InvalidConfigurationDoesNotPublishSingletonOrCreateObjects() => Run(async () =>
        {
            manager.Dispose();
            await Frames(2);
            manager = new UIManager();
            settings.referenceResolution = Vector2.zero;
            Assert.Throws<ArgumentException>(() => manager.Initiate(settings, loader));
            Assert.IsNull(manager.Mono);
            Assert.IsFalse(UIManager.TryGetInstance(out _));
            settings.referenceResolution = new Vector2(1080, 1920);
            manager.Initiate(settings, loader);
            await manager.ShowUIAsync<PrimaryPanel>();
        });
        [UnityTest]
        public IEnumerator BackgroundLoaderReturnsToMainThreadForInstantiationAndCleanup() => Run(async () =>
        {
            manager.Dispose();
            var worker = new WorkerReturningLoader { Prefab = loader.Prefabs["UI/PrimaryPanel"], FailNext = true };
            manager = new UIManager();
            manager.Initiate(settings, worker);
            var eventOnMainThread = false;
            manager.OnShowEnd = panel => eventOnMainThread = PlayerLoopHelper.IsMainThread;
            Assert.IsInstanceOf<InvalidOperationException>(await Capture(manager.ShowUIAsync<PrimaryPanel>()));
            Assert.IsTrue(PlayerLoopHelper.IsMainThread);
            Assert.IsFalse(manager.IsLockInput());
            await manager.ShowUIAsync<PrimaryPanel>();
            Assert.IsTrue(eventOnMainThread);
            await manager.CloseCurrentAsync(true);
            await Frames(3);
            Assert.IsTrue(worker.ReleasedOnMainThread);
        });

        [UnityTest]
        public IEnumerator BackgroundAnimationCompletionAndFailureRestoreOnMainThread() => Run(async () =>
        {
            var eventOnMainThread = false;
            manager.OnShowEnd = panel => eventOnMainThread = PlayerLoopHelper.IsMainThread;
            await manager.ShowUIAsync<WorkerAnimationPanel>(panel => panel.FailHide = true);
            Assert.IsTrue(eventOnMainThread);
            Assert.IsInstanceOf<InvalidOperationException>(await Capture(manager.CloseCurrentAsync()));
            Assert.IsTrue(PlayerLoopHelper.IsMainThread);
            Assert.IsTrue(manager.CurrentUIPanel.CanInteract);
            Assert.IsFalse(manager.IsLockInput());
            manager.GetUI<WorkerAnimationPanel>().FailHide = false;
            await manager.CloseCurrentAsync();
            Assert.IsNull(manager.CurrentUIPanel);
        });
        [UnityTest]
        public IEnumerator CameraEnabledBeforeInitializationIsRegistered() => Run(async () =>
        {
            manager.Dispose();
            await Frames(2);
            var go = new GameObject("External UI Camera", typeof(Camera), typeof(UIAdditionalCamera));
            extras.Add(go);
            manager = new UIManager();
            manager.Initiate(settings, loader);
            Assert.AreSame(go.GetComponent<Camera>(), manager.UICamera);
            go.SetActive(false);
            Assert.AreNotSame(go.GetComponent<Camera>(), manager.UICamera);
            Assert.IsTrue(manager.UICamera.enabled);
        });
    }
}
