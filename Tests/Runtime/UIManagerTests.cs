using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using RicKit.UI.Component;
using RicKit.UI.Panels;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RicKit.UI.Tests
{
    public class UIManagerTests
    {
        private const string PathA = "UI/PanelA";
        private const string PathB = "UI/PanelB";
        private const string PathD = "UI/DestroyOnClosePanel";
        private const string PathBroken = "UI/BrokenPanel";

        private UIManager manager;
        private TestLoader loader;
        private GameObject templateRoot;
        private UISettings settings;

        [SetUp]
        public void SetUp()
        {
            // 模板挂在未激活的根节点下：模板本身不跑 Awake，实例化到 UI 根节点下才激活
            templateRoot = new GameObject("Templates");
            templateRoot.SetActive(false);
            loader = new TestLoader();
            loader.Register(PathA, CreateTemplate<PanelA>());
            loader.Register(PathB, CreateTemplate<PanelB>());
            loader.Register("UI/PanelC", CreateTemplate<PanelC>());
            loader.Register(PathD, CreateTemplate<DestroyOnClosePanel>());
            var broken = new GameObject("BrokenPanel", typeof(RectTransform));
            broken.transform.SetParent(templateRoot.transform, false);
            loader.Register(PathBroken, broken);
            settings = ScriptableObject.CreateInstance<UISettings>();
            manager = new UIManager();
            manager.Initiate(settings, loader);
        }

        [TearDown]
        public void TearDown()
        {
            if (manager.Mono) Object.Destroy(manager.Mono.gameObject);
            Object.Destroy(templateRoot);
            Object.Destroy(settings);
        }

        private GameObject CreateTemplate<T>() where T : TestPanel
        {
            var go = new GameObject(typeof(T).Name, typeof(RectTransform));
            go.transform.SetParent(templateRoot.transform, false);
            go.AddComponent<T>();
            return go;
        }

        private static async UniTask WithTimeout(UniTask task, string what, int milliseconds = 3000)
        {
            var winner = await UniTask.WhenAny(task, UniTask.Delay(milliseconds, ignoreTimeScale: true));
            Assert.AreEqual(0, winner, what + " 超时，可能卡死");
        }

        private static async UniTask<Exception> Catch(Func<UniTask> action)
        {
            try
            {
                await action();
            }
            catch (Exception e)
            {
                return e;
            }
            return null;
        }

        #region 3.x 语义护栏：项目依赖这些行为，任何版本都不许改

        [UnityTest]
        public IEnumerator Back_OnHideEnd_SeesPanelBelowAsCurrent() => UniTask.ToCoroutine(async () =>
        {
            await manager.ShowUIAsync<PanelA>();
            await manager.ShowUIAsync<PanelB>();
            AbstractUIPanel currentAtHideEnd = null;
            manager.OnHideEnd += _ => currentAtHideEnd = manager.CurrentUIPanel;

            await manager.BackAsync();

            Assert.AreSame(manager.GetUI<PanelA>(), currentAtHideEnd);
        });

        [UnityTest]
        public IEnumerator ShowAgain_ReplaysAnimationIn() => UniTask.ToCoroutine(async () =>
        {
            await manager.ShowUIAsync<PanelA>();
            await manager.ShowUIAsync<PanelB>();
            await manager.ShowUIAsync<PanelA>();

            Assert.AreEqual(2, manager.GetUI<PanelA>().AnimationInCount);
        });

        [UnityTest]
        public IEnumerator OnInit_RunsAfterAwake() => UniTask.ToCoroutine(async () =>
        {
            var awakeBeforeInit = false;
            await manager.ShowUIAsync<PanelA>(p => awakeBeforeInit = p.AwakeCalled);

            Assert.IsTrue(awakeBeforeInit);
        });

        [UnityTest]
        public IEnumerator AwaitNavigationInsideAnimation_DoesNotDeadlock() => UniTask.ToCoroutine(async () =>
        {
            var show = manager.ShowUIAsync<PanelA>(p => p.AnimationInHook = _ => manager.ShowUIAsync<PanelB>());

            await WithTimeout(show, "动画回调里 await 导航");
            Assert.IsTrue(manager.GetUI<PanelB>().IsShow);
        });

        #endregion

        #region 输入锁

        [UnityTest]
        public IEnumerator DestroyedDuringShowAnimation_ReleasesLock() => UniTask.ToCoroutine(async () =>
        {
            manager.ShowUI<PanelA>(p => p.AnimationInHook = token => UniTask.Delay(5000, cancellationToken: token));
            await UniTask.DelayFrame(3);
            Assert.IsTrue(manager.IsLockInput());

            manager.ClearAll();
            await UniTask.DelayFrame(3);

            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator HookThrows_ReleasesLock() => UniTask.ToCoroutine(async () =>
        {
            manager.OnShow += _ => throw new InvalidOperationException("hook");

            var error = await Catch(() => manager.ShowUIAsync<PanelA>());

            Assert.IsInstanceOf<InvalidOperationException>(error);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator LoaderThrows_ReleasesLock() => UniTask.ToCoroutine(async () =>
        {
            loader.ThrowOnPath = PathA;

            var error = await Catch(() => manager.ShowUIAsync<PanelA>());

            Assert.IsInstanceOf<InvalidOperationException>(error);
            Assert.IsFalse(manager.IsLockInput());
        });

        [UnityTest]
        public IEnumerator MissingPrefab_ThrowsWithPath() => UniTask.ToCoroutine(async () =>
        {
            var error = await Catch(() => manager.ShowUIAsync<PanelNotRegistered>());

            Assert.IsInstanceOf<InvalidOperationException>(error);
            StringAssert.Contains("UI/PanelNotRegistered", error.Message);
            Assert.IsFalse(manager.IsLockInput());
        });

        #endregion

        #region 资源释放

        [UnityTest]
        public IEnumerator ClearAll_ReleasesEveryLoad() => UniTask.ToCoroutine(async () =>
        {
            await manager.ShowUIAsync<PanelA>();
            await manager.ShowUIAsync<PanelB>();

            manager.ClearAll();
            await UniTask.DelayFrame(2);

            Assert.AreEqual(1, loader.ReleasedOf(PathA));
            Assert.AreEqual(1, loader.ReleasedOf(PathB));
        });

        [UnityTest]
        public IEnumerator HiddenPanel_IsNotReleased() => UniTask.ToCoroutine(async () =>
        {
            await manager.ShowUIAsync<PanelA>();
            await manager.ShowUIAsync<PanelB>();

            await manager.BackAsync();
            await UniTask.DelayFrame(2);

            Assert.AreEqual(0, loader.ReleasedOf(PathB));
        });

        [UnityTest]
        public IEnumerator BackWithDestroy_ReleasesAfterInstanceIsGone() => UniTask.ToCoroutine(async () =>
        {
            await manager.ShowUIAsync<PanelA>();
            await manager.ShowUIAsync<PanelB>();

            await manager.BackAsync(true);
            // Destroy 延迟到帧末，实例还在时不能先归还
            Assert.AreEqual(0, loader.ReleasedOf(PathB));
            await UniTask.DelayFrame(2);

            Assert.AreEqual(1, loader.ReleasedOf(PathB));
            Assert.IsNull(manager.GetUI<PanelB>());
        });

        [UnityTest]
        public IEnumerator DestroyOnClose_DestroysAndReleasesOnBack() => UniTask.ToCoroutine(async () =>
        {
            await manager.ShowUIAsync<PanelA>();
            await manager.ShowUIAsync<DestroyOnClosePanel>();

            await manager.BackAsync();
            await UniTask.DelayFrame(2);

            Assert.IsNull(manager.GetUI<DestroyOnClosePanel>());
            Assert.AreEqual(1, loader.ReleasedOf(PathD));
        });

        [UnityTest]
        public IEnumerator ExternalDestroy_ReleasesAndNextShowLoadsAgain() => UniTask.ToCoroutine(async () =>
        {
            await manager.ShowUIAsync<PanelA>();
            Object.Destroy(manager.GetUI<PanelA>().gameObject);
            await UniTask.DelayFrame(2);

            Assert.AreEqual(1, loader.ReleasedOf(PathA));
            Assert.IsNull(manager.GetUI<PanelA>());

            await manager.ShowUIAsync<PanelA>();
            Assert.AreEqual(2, loader.LoadedOf(PathA));
            Assert.IsTrue(manager.GetUI<PanelA>().IsShow);
        });

        [UnityTest]
        public IEnumerator BrokenPrefab_ThrowsAndStillReleases() => UniTask.ToCoroutine(async () =>
        {
            var error = await Catch(() => manager.ShowUIAsync<BrokenPanel>());
            await UniTask.DelayFrame(2);

            Assert.IsInstanceOf<InvalidOperationException>(error);
            Assert.AreEqual(loader.LoadedOf(PathBroken), loader.ReleasedOf(PathBroken));
            Assert.IsFalse(manager.IsLockInput());
        });

        #endregion

        #region 等待与容错

        [UnityTest]
        public IEnumerator WaitUntilUIHideEnd_MissingPanel_Completes() => UniTask.ToCoroutine(async () =>
        {
            await WithTimeout(manager.WaitUntilUIHideEnd<PanelA>(), "等待不存在的面板");
        });

        [UnityTest]
        public IEnumerator WaitUntilUIHideEnd_DestroyedWhileWaiting_Completes() => UniTask.ToCoroutine(async () =>
        {
            await manager.ShowUIAsync<PanelA>();
            var wait = manager.WaitUntilUIHideEnd<PanelA>();

            manager.ClearAll();

            await WithTimeout(wait, "等待中被销毁的面板");
        });

        [UnityTest]
        public IEnumerator ShowUIAndWaitHide_CompletesAfterBack() => UniTask.ToCoroutine(async () =>
        {
            await manager.ShowUIAsync<PanelA>();
            var flow = manager.ShowUIAndWaitHideAsync<PanelB>();
            await UniTask.DelayFrame(5);
            Assert.AreEqual(UniTaskStatus.Pending, flow.Status);

            await manager.BackAsync();

            await WithTimeout(flow, "弹窗关闭后继续");
        });

        [UnityTest]
        public IEnumerator GetUI_SkipsDestroyedInstance() => UniTask.ToCoroutine(async () =>
        {
            await manager.ShowUIUnmanagableAsync<PanelA>();
            Object.Destroy(manager.GetUI<PanelA>().gameObject);
            await UniTask.DelayFrame(1);

            await manager.ShowUIUnmanagableAsync<PanelA>();

            var panel = manager.GetUI<PanelA>();
            Assert.IsTrue(panel);
            Assert.IsTrue(panel.IsShow);
        });

        #endregion

        #region 层级跟随

        [UnityTest]
        public IEnumerator SortingFollower_FollowsPanelOrder() => UniTask.ToCoroutine(async () =>
        {
            AddFollower(PathA, 2);

            await manager.ShowUIAsync<PanelA>();
            var panel = manager.GetUI<PanelA>();
            var group = panel.GetComponentInChildren<SortingGroup>(true);
            Assert.AreEqual(panel.OrderInLayer + 2, group.sortingOrder);

            panel.SetOrderInLayer(40);
            Assert.AreEqual(42, group.sortingOrder);
        });

        [UnityTest]
        public IEnumerator SortingFollower_FollowsShowThenClosePrevFinalOrder() => UniTask.ToCoroutine(async () =>
        {
            AddFollower(PathB, 1);
            await manager.ShowUIAsync<PanelA>();
            var orderA = manager.GetUI<PanelA>().OrderInLayer;

            await manager.ShowThenClosePrevAsync<PanelB>();

            var panelB = manager.GetUI<PanelB>();
            Assert.AreEqual(orderA, panelB.OrderInLayer);
            Assert.AreEqual(orderA + 1, panelB.GetComponentInChildren<SortingGroup>(true).sortingOrder);
        });

        private void AddFollower(string path, int offset)
        {
            var template = loader.LoadPrefab(path);
            loader.Loaded[path]--;
            var child = new GameObject("Effect", typeof(SortingGroup));
            child.transform.SetParent(template.transform, false);
            child.AddComponent<UISortingFollower>().Offset = offset;
        }

        #endregion
    }

    public class PanelNotRegistered : TestPanel
    {
    }
}
