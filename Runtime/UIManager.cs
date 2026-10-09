using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using RicKit.UI.Interfaces;
using RicKit.UI.Panels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RicKit.UI
{
    public interface IUIManager
    {
        UIManagerMono Mono { get; }
        AbstractUIPanel CurrentUIPanel { get; }
        Canvas UICanvas { get; }
        RectTransform CanvasRectTransform { get; }
        Camera UICamera { get; }
        UISettings Settings { get; }
        Action<AbstractUIPanel> OnShow { get; set; }
        Action<AbstractUIPanel> OnHide { get; set; }
        Action<AbstractUIPanel> OnShowEnd { get; set; }
        Action<AbstractUIPanel> OnHideEnd { get; set; }
        void Initiate();
        void Initiate(IPanelLoader panelLoader);
        void ShowUI<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel;
        void HideThenShowUI<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel;
        void CloseThenShowUI<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel;
        void ShowUIUnmanagable<T>(Action<T> onInit = null, string layer = "UI", int sortingOrder = 900,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel;
        void Back(bool destroy = false);
        void CloseCurrent(bool destroy = false);
        void HideCurrent();
        void CloseUntil<T>(bool destroy = false) where T : AbstractUIPanel;
        void BackThenShow<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel;
        void PreloadUI<T>(string layer = "UI", bool asyncLoadNew = true) where T : AbstractUIPanel;
        void ShowThenClosePrev<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel;
        void ShowThenHidePrev<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel;
        void Close<T>(bool destroy = false) where T : AbstractUIPanel;
        UniTask ShowUIAsync<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel;
        UniTask HideThenShowUIAsync<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel;
        UniTask CloseThenShowUIAsync<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel;
        UniTask ShowUIUnmanagableAsync<T>(Action<T> onInit = null, string layer = "UI", int sortingOrder = 900,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel;
        UniTask BackAsync(bool destroy = false);
        UniTask CloseCurrentAsync(bool destroy = false);
        UniTask HideCurrentAsync();
        UniTask CloseUntilAsync<T>(bool destroy = false) where T : AbstractUIPanel;
        UniTask BackThenShowAsync<T>(Action<T> onInit, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel;
        UniTask WaitUntilUIHideEnd<T>(CancellationToken token = default) where T : AbstractUIPanel;
        UniTask PreloadUIAsync<T>(string layer = "UI", bool asyncLoadNew = true) where T : AbstractUIPanel;

        UniTask ShowThenClosePrevAsync<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel;
        UniTask ShowThenHidePrevAsync<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel;
        UniTask CloseAsync<T>(bool destroy = false) where T : AbstractUIPanel;
        void ClearAll();
        void SetLockInput(bool on);
        bool IsLockInput();
        void LockInputWhile(UniTask task);
        IDisposable GetLockInputScope();
        T GetUI<T>() where T : AbstractUIPanel;
        Canvas GetCustomLayerCanvas(string name, int sortingOrder, string sortingLayerName = "UI");
        void SetCustomLayerSortOrder(string name, int sortOrder);
        void SafeDestroy(AbstractUIPanel panel);
        void RegisterAdditionalCam(Camera cam);
        void UnregisterAdditionalCam(Camera cam);
    }

    public class UIManager : IUIManager, IDisposable
    {
        private static IUIManager instance;

        public static IUIManager I
        {
            get
            {
                if (instance == null)
                    Debug.LogError("UIManager not initialized, please call UIManager.Init() first.");
                return instance;
            }
        }

        public static bool TryGetInstance(out IUIManager manager)
        {
            manager = instance;
            return manager != null;
        }

        private sealed class PanelEntry
        {
            public Type Key;
            public AbstractUIPanel Panel;
            public GameObject GameObject;
            public IPanelLoader Loader;
            public string Path;
            public GameObject Prefab;
            public bool DestroyRequested;
        }
        private CanvasGroup blockerCg;
        private RectTransform defaultRoot;
        private Transform stagingRoot;
        private IPanelLoader panelLoader;
        private readonly List<AbstractUIPanel> showStack = new List<AbstractUIPanel>();
        private readonly List<PanelEntry> panelList = new List<PanelEntry>();
        private readonly Dictionary<Type, PanelEntry> panelByType = new Dictionary<Type, PanelEntry>();
        private readonly Queue<NavigationOperation> operations = new Queue<NavigationOperation>();
        private CancellationTokenSource operationsCancellation = new CancellationTokenSource();
        private bool draining;
        private int operationSuspensions;
        private bool disposed;
        private bool initialized;
        private GameObject ownedEventSystem;
        private Canvas canvas;
        private static readonly IPanelLoader DefaultPanelLoader = new DefaultPanelLoader();
        private int uiLayerMask;
        private Camera defaultCamera;
        private readonly List<Camera> uiCameras = new List<Camera>();
        private readonly Dictionary<string, Canvas> customRootDict = new Dictionary<string, Canvas>();
        private int manualLockCount;
        private int scopedLockCount;
        private CanvasGroup interactionGroup;

        public UIManagerMono Mono { get; private set; }
        public AbstractUIPanel CurrentUIPanel
        {
            get
            {
                while (showStack.Count > 0 && !showStack[showStack.Count - 1])
                    showStack.RemoveAt(showStack.Count - 1);
                return showStack.Count == 0 ? null : showStack[showStack.Count - 1];
            }
        }
        public Canvas UICanvas => canvas;
        public RectTransform CanvasRectTransform { get; private set; }
        public Camera UICamera
        {
            get => canvas ? canvas.worldCamera : null;
            private set { if (canvas) canvas.worldCamera = value; }
        }
        public UISettings Settings { get; private set; }
        public Action<AbstractUIPanel> OnShow { get; set; }
        public Action<AbstractUIPanel> OnHide { get; set; }
        public Action<AbstractUIPanel> OnShowEnd { get; set; }
        public Action<AbstractUIPanel> OnHideEnd { get; set; }

        public static void Init(IPanelLoader panelLoader = null)
        {
            EnsureMainThread();
            if (instance != null)
            {
                Debug.LogError("UIManager already initialized.");
                return;
            }
            new UIManager().Initiate(panelLoader);
        }

        public static void Shutdown()
        {
            if (instance is UIManager manager) manager.Dispose();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Shutdown();
            instance = null;
        }

        public void Initiate() => Initiate(DefaultPanelLoader);

        public void Initiate(IPanelLoader loader)
        {
            EnsureMainThread();
            Initiate(Resources.Load<UISettings>("UISettings"), loader);
        }

        /// <summary>Initialize with explicit settings, without requiring a Resources asset.</summary>
        public void Initiate(UISettings settings, IPanelLoader loader)
        {
            EnsureMainThread();
            if (disposed) throw new ObjectDisposedException(nameof(UIManager));
            if (initialized || instance != null)
                throw new InvalidOperationException("UIManager already initialized.");
            if (!settings) throw new InvalidOperationException("UISettings not found. Create Resources/UISettings or supply settings explicitly.");
            uiLayerMask = LayerMask.NameToLayer("UI");
            if (uiLayerMask < 0) throw new InvalidOperationException("The UI GameObject layer is missing.");
            if (!IsFinite(settings.referenceResolution.x) || !IsFinite(settings.referenceResolution.y) ||
                settings.referenceResolution.x <= 0 || settings.referenceResolution.y <= 0)
                throw new ArgumentException("UI reference resolution must be positive.", nameof(settings));
            if (!IsFinite(settings.nearClipPlane) || !IsFinite(settings.farClipPlane) ||
                settings.nearClipPlane <= 0 || settings.farClipPlane <= settings.nearClipPlane)
                throw new ArgumentException("UI camera clipping planes are invalid.", nameof(settings));

            if (!IsFinite(settings.matchWidthOrHeight) || settings.matchWidthOrHeight < 0 || settings.matchWidthOrHeight > 1)
                throw new ArgumentException("UI width/height match must be between zero and one.", nameof(settings));

            Settings = settings;
            panelLoader = loader ?? DefaultPanelLoader;
            try
            {
                CreateUIManager();
                initialized = true;
                instance = this;
                foreach (var additional in Object.FindObjectsOfType<Component.UIAdditionalCamera>())
                    additional.Register(this);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void CreateUIManager()
        {
            var settings = Settings;
            Mono = new GameObject("UIManager").AddComponent<UIManagerMono>();
            Mono.gameObject.layer = uiLayerMask;
            Mono.SetUIManager(this);
            Object.DontDestroyOnLoad(Mono.gameObject);
            if (!Object.FindObjectOfType<EventSystem>())
            {
                ownedEventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                ownedEventSystem.transform.SetParent(Mono.transform, false);
            }
            new GameObject("UICam", typeof(Camera)).TryGetComponent(out defaultCamera);
            defaultCamera.transform.SetParent(Mono.transform);
            defaultCamera.transform.localPosition = new Vector3(0, 0, -10);
            defaultCamera.clearFlags = settings.cameraClearFlags;
            if (defaultCamera.clearFlags == CameraClearFlags.SolidColor || defaultCamera.clearFlags == CameraClearFlags.Skybox)
                defaultCamera.backgroundColor = settings.backgroundColor;
            defaultCamera.cullingMask = settings.cullingMask;
            defaultCamera.depth = settings.depth;
            defaultCamera.orthographic = true;
            defaultCamera.orthographicSize = 5;
            defaultCamera.nearClipPlane = settings.nearClipPlane;
            defaultCamera.farClipPlane = settings.farClipPlane;

            new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup))
                .TryGetComponent(out canvas);
            interactionGroup = canvas.GetComponent<CanvasGroup>();
            canvas.gameObject.layer = uiLayerMask;
            canvas.transform.SetParent(Mono.transform);
            CanvasRectTransform = (RectTransform)canvas.transform;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            UICamera = defaultCamera;
            canvas.planeDistance = settings.nearClipPlane < 5 && settings.farClipPlane > 5
                ? 5 : settings.nearClipPlane * 0.5f + settings.farClipPlane * 0.5f;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 0;
            canvas.TryGetComponent<CanvasScaler>(out var canvasScaler);
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = settings.referenceResolution;
            canvasScaler.screenMatchMode = settings.screenMatchMode;
            canvasScaler.matchWidthOrHeight = settings.matchWidthOrHeight;

            new GameObject("Blocker", typeof(CanvasGroup), typeof(CanvasRenderer), typeof(Canvas),
                typeof(Image), typeof(GraphicRaycaster)).TryGetComponent(out blockerCg);
            blockerCg.blocksRaycasts = false;
            blockerCg.TryGetComponent<Canvas>(out var blockerCanvas);
            blockerCanvas.gameObject.layer = uiLayerMask;
            blockerCg.transform.SetParent(canvas.transform, false);
            blockerCanvas.overrideSorting = true;
            blockerCanvas.sortingLayerName = "Blocker";
            blockerCanvas.sortingOrder = 0;
            blockerCg.TryGetComponent<Image>(out var blockerImg);
            blockerImg.color = Color.clear;
            var blockerRt = (RectTransform)blockerCg.transform;
            blockerRt.anchorMin = Vector2.zero;
            blockerRt.anchorMax = Vector2.one;
            blockerRt.offsetMin = Vector2.zero;
            blockerRt.offsetMax = Vector2.zero;

            new GameObject("DefaultRoot", typeof(RectTransform)).TryGetComponent(out defaultRoot);
            defaultRoot.gameObject.layer = uiLayerMask;
            defaultRoot.SetParent(canvas.transform, false);
            defaultRoot.anchorMin = Vector2.zero;
            defaultRoot.anchorMax = Vector2.one;
            defaultRoot.offsetMin = Vector2.zero;
            defaultRoot.offsetMax = Vector2.zero;
            stagingRoot = new GameObject("StagingRoot", typeof(RectTransform)).transform;
            stagingRoot.SetParent(Mono.transform, false);
            stagingRoot.gameObject.SetActive(false);
        }

        public void Update()
        {
            if (!initialized || disposed) return;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (IsLockInput()) return;
            var current = CurrentUIPanel;
            if (current && current.CanInteract) current.OnESCClick();
#endif
        }
        #region 同步（只是同步调用，并不保证任务同帧开始或者完成）

        public void ShowUI<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel
        {
            ShowUIAsync(onInit, layer, orderInLayerDelta, asyncLoadNew).Forget();
        }

        public void HideThenShowUI<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel
        {
            HideThenShowUIAsync(onInit, layer, orderInLayerDelta, asyncLoadNew).Forget();
        }

        public void CloseThenShowUI<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            CloseThenShowUIAsync(onInit, destroy, layer, orderInLayerDelta, asyncLoadNew).Forget();
        }

        public void ShowUIUnmanagable<T>(Action<T> onInit = null, string layer = "UI", int sortingOrder = 900,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel
        {
            ShowUIUnmanagableAsync(onInit, layer, sortingOrder, asyncLoadNew).Forget();
        }

        public void Back(bool destroy = false)
        {
            BackAsync(destroy).Forget();
        }

        /// <summary>
        /// Close会出栈，且可销毁
        /// </summary>
        /// <param name="destroy">是否在退出动画后销毁</param>
        public void CloseCurrent(bool destroy = false)
        {
            CloseCurrentAsync(destroy).Forget();
        }

        /// <summary>
        /// Hide不会出栈，且不可销毁
        /// </summary>
        public void HideCurrent()
        {
            HideCurrentAsync().Forget();
        }

        public void CloseUntil<T>(bool destroy = false) where T : AbstractUIPanel
        {
            CloseUntilAsync<T>(destroy).Forget();
        }

        public void BackThenShow<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            BackThenShowAsync(onInit, destroy, layer, orderInLayerDelta, asyncLoadNew).Forget();
        }

        public void PreloadUI<T>(string layer = "UI", bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            PreloadUIAsync<T>(layer, asyncLoadNew).Forget();
        }

        public void ShowThenClosePrev<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            ShowThenClosePrevAsync(onInit, destroy, layer, orderInLayerDelta, asyncLoadNew).Forget();
        }

        public void ShowThenHidePrev<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel
        {
            ShowThenHidePrevAsync(onInit, layer, orderInLayerDelta, asyncLoadNew).Forget();
        }

        public void Close<T>(bool destroy = false) where T : AbstractUIPanel
        {
            CloseAsync<T>(destroy).Forget();
        }

        #endregion

        // Composite navigation calls use the core methods to keep the entire operation in one queue entry.
        private sealed class NavigationOperation
        {
            public Func<CancellationToken, UniTask> Action;
            public CancellationToken Token;
            public readonly UniTaskCompletionSource Completion = new UniTaskCompletionSource();
        }

        private UniTask Enqueue(Func<CancellationToken, UniTask> action)
        {
            EnsureReady();
            var operation = new NavigationOperation { Action = action, Token = operationsCancellation.Token };
            operations.Enqueue(operation);
            if (!draining && operationSuspensions == 0) DrainOperationsAsync().Forget();
            return operation.Completion.Task;
        }

        private async UniTask DrainOperationsAsync()
        {
            draining = true;
            try
            {
                while (operations.Count > 0 && operationSuspensions == 0)
                {
                    var operation = operations.Dequeue();
                    try
                    {
                        operation.Token.ThrowIfCancellationRequested();
                        using (GetLockInputScope())
                        {
                            // A legacy loader may not cancel. Its eventual result is checked and released by NewUIAsync.
                            try { await operation.Action(operation.Token).AttachExternalCancellation(operation.Token); }
                            finally { await UniTask.SwitchToMainThread(); }
                            operation.Token.ThrowIfCancellationRequested();
                        }
                        operation.Completion.TrySetResult();
                    }
                    catch (OperationCanceledException)
                    {
                        operation.Completion.TrySetCanceled(operation.Token);
                    }
                    catch (Exception exception)
                    {
                        operation.Completion.TrySetException(exception);
                    }
                }
            }
            finally { draining = false; }
        }

        public UniTask ShowUIAsync<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true) where T : AbstractUIPanel
            => Enqueue(token => ShowCoreAsync(onInit, layer, orderInLayerDelta, asyncLoadNew, token));

        private async UniTask ShowCoreAsync<T>(Action<T> onInit, string layer, int delta, bool asyncLoad,
            CancellationToken token, int? absoluteOrder = null, bool managed = true) where T : AbstractUIPanel
        {
            var form = await GetOrCreateAsync<T>(asyncLoad, token);
            var oldIndex = showStack.IndexOf(form);
            var oldOrder = form.OrderInLayer;
            var oldLayer = form.SortingLayerName;
            var navigationChanged = false;
            try
            {
                var top = CurrentUIPanel;
                var order = absoluteOrder ?? (top == form ? oldOrder : (top ? top.OrderInLayer : 0) + delta);
                form.SetSortingLayer(layer);
                form.SetOrderInLayer(order);
                onInit?.Invoke(form);
                token.ThrowIfCancellationRequested();
                if (!form || !IsRegistered(form)) throw new OperationCanceledException("The panel was destroyed during initialization.");
                if (managed)
                {
                    RemoveFromStack(form);
                    showStack.Add(form);
                    navigationChanged = true;
                }
                await form.OnShowAsync(token);
                token.ThrowIfCancellationRequested();
                if (!managed) RemoveFromStack(form);
            }
            catch
            {
                if (!token.IsCancellationRequested)
                {
                    if (navigationChanged) RemoveFromStack(form);
                    if (form && IsRegistered(form))
                    {
                        if (navigationChanged && oldIndex >= 0) showStack.Insert(Math.Min(oldIndex, showStack.Count), form);
                        form.SetOrderInLayer(oldOrder);
                        form.SetSortingLayer(oldLayer);
                    }
                }
                throw;
            }
        }

        public UniTask HideThenShowUIAsync<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true) where T : AbstractUIPanel
            => Enqueue(async token =>
            {
                var target = await GetOrCreateAsync<T>(asyncLoadNew, token);
                var previous = CurrentUIPanel;
                if (previous && previous != target) await previous.OnHideAsync(token);
                try { await ShowCoreAsync(onInit, layer, orderInLayerDelta, asyncLoadNew, token); }
                catch { await RestorePreviousAsync(previous, token); throw; }
            });

        public UniTask CloseThenShowUIAsync<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel
            => Enqueue(async token =>
            {
                var target = await GetOrCreateAsync<T>(asyncLoadNew, token);
                var previous = CurrentUIPanel;
                var order = previous ? previous.OrderInLayer : orderInLayerDelta;
                if (previous && previous != target) await previous.OnHideAsync(token);
                try { await ShowCoreAsync(onInit, layer, orderInLayerDelta, asyncLoadNew, token, order); }
                catch { await RestorePreviousAsync(previous, token); throw; }
                if (previous && previous != target) ClosePanel(previous, destroy);
            });

        public UniTask ShowUIUnmanagableAsync<T>(Action<T> onInit = null, string layer = "UI", int sortingOrder = 900,
            bool asyncLoadNew = true) where T : AbstractUIPanel
            => Enqueue(token => ShowCoreAsync(onInit, layer, 0, asyncLoadNew, token, sortingOrder, false));
        public UniTask BackAsync(bool destroy = false) => Enqueue(token => BackCoreAsync(destroy, token));

        private async UniTask BackCoreAsync(bool destroy, CancellationToken token)
        {
            await CloseCurrentCoreAsync(destroy, token);
            var current = CurrentUIPanel;
            if (current && !current.IsShow) await current.OnShowAsync(token);
        }

        public UniTask CloseCurrentAsync(bool destroy = false)
            => Enqueue(token => CloseCurrentCoreAsync(destroy, token));

        private async UniTask CloseCurrentCoreAsync(bool destroy, CancellationToken token)
        {
            var form = CurrentUIPanel;
            if (!form) return;
            await form.OnHideAsync(token);
            token.ThrowIfCancellationRequested();
            ClosePanel(form, destroy);
        }

        private void ClosePanel(AbstractUIPanel form, bool destroy)
        {
            RemoveFromStack(form);
            if (destroy || form.DestroyOnClose) SafeDestroy(form);
        }

        public UniTask HideCurrentAsync() => Enqueue(async token =>
        {
            var form = CurrentUIPanel;
            if (form) await form.OnHideAsync(token);
        });

        public UniTask CloseUntilAsync<T>(bool destroy = false) where T : AbstractUIPanel
            => Enqueue(async token =>
            {
                while (CurrentUIPanel)
                {
                    var form = CurrentUIPanel;
                    if (form is T)
                    {
                        if (!form.IsShow) await form.OnShowAsync(token);
                        return;
                    }
                    await CloseCurrentCoreAsync(destroy, token);
                }
            });

        public UniTask BackThenShowAsync<T>(Action<T> onInit, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel
            => Enqueue(async token =>
            {
                await GetOrCreateAsync<T>(asyncLoadNew, token);
                await BackCoreAsync(destroy, token);
                await ShowCoreAsync(onInit, layer, orderInLayerDelta, asyncLoadNew, token);
            });

        public UniTask WaitUntilUIHideEnd<T>(CancellationToken token = default) where T : AbstractUIPanel
        {
            token.ThrowIfCancellationRequested();
            var panel = GetUI<T>();
            return !panel ? UniTask.CompletedTask :
                UniTask.WaitUntil(() => !panel || !panel.IsShow, cancellationToken: token);
        }

        public UniTask PreloadUIAsync<T>(string layer = "UI", bool asyncLoadNew = true) where T : AbstractUIPanel
            => Enqueue(async token =>
            {
                if (GetUI<T>()) return;
                var form = await GetOrCreateAsync<T>(asyncLoadNew, token);
                form.SetSortingLayer(layer);
                form.SetOrderInLayer(0);
            });

        public UniTask ShowThenClosePrevAsync<T>(Action<T> onInit = null, bool destroy = false,
            string layer = "UI", int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel
            => Enqueue(async token =>
            {
                var previous = CurrentUIPanel;
                var oldOrder = previous ? previous.OrderInLayer : orderInLayerDelta;
                await ShowCoreAsync(onInit, layer, orderInLayerDelta, asyncLoadNew, token);
                var current = GetUI<T>();
                if (!previous || previous == current) return;
                await previous.OnHideAsync(token);
                token.ThrowIfCancellationRequested();
                ClosePanel(previous, destroy);
                if (current) current.SetOrderInLayer(oldOrder);
            });

        public UniTask ShowThenHidePrevAsync<T>(Action<T> onInit = null, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel
            => Enqueue(async token =>
            {
                var previous = CurrentUIPanel;
                await ShowCoreAsync(onInit, layer, orderInLayerDelta, asyncLoadNew, token);
                if (previous && previous != GetUI<T>()) await previous.OnHideAsync(token);
            });

        public UniTask CloseAsync<T>(bool destroy = false) where T : AbstractUIPanel
            => Enqueue(async token =>
            {
                var form = GetUI<T>();
                if (!form) return;
                await form.OnHideAsync(token);
                token.ThrowIfCancellationRequested();
                ClosePanel(form, destroy);
            });

        private async UniTask RestorePreviousAsync(AbstractUIPanel previous, CancellationToken token)
        {
            if (token.IsCancellationRequested || !previous || !IsRegistered(previous) || previous.IsShow) return;
            try { await previous.OnShowAsync(token); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private async UniTask<T> GetOrCreateAsync<T>(bool asyncLoad, CancellationToken token) where T : AbstractUIPanel
        {
            token.ThrowIfCancellationRequested();
            var form = GetUI<T>();
            if (form && panelByType.TryGetValue(typeof(T), out var cached) && ReferenceEquals(cached.Panel, form))
                return form;
            if (!form) form = asyncLoad ? await NewUIAsync<T>() : NewUI<T>();
            await UniTask.SwitchToMainThread();
            if (token.IsCancellationRequested)
            {
                if (form) SafeDestroy(form);
                token.ThrowIfCancellationRequested();
            }
            if (!form) throw new InvalidOperationException($"Panel loader did not create {typeof(T).FullName}.");
            var entry = FindPanelEntry(form) ?? TrackPanel(typeof(T), form, null, null, null);
            if (entry.DestroyRequested) throw new OperationCanceledException("The panel is being destroyed.");
            if (entry.Key != typeof(T))
                throw new InvalidOperationException("A panel factory must not return an instance already registered under a different type.");
            form.BindManager(this);
            panelByType[typeof(T)] = entry;
            return form;
        }

        public virtual T GetUI<T>() where T : AbstractUIPanel
        {
            EnsureMainThread();
            if (!panelByType.TryGetValue(typeof(T), out var entry)) return null;
            if (entry.Panel && entry.GameObject && !entry.DestroyRequested) return entry.Panel as T;
            // Removing only the MonoBehaviour must not leave its UI object and loaded resources orphaned.
            DestroyPanel(entry);
            return null;
        }
        protected virtual async UniTask<T> NewUIAsync<T>() where T : AbstractUIPanel
        {
            var token = operationsCancellation.Token;
            var loader = panelLoader;
            var path = $"{Settings.assetPathPrefix}{typeof(T).Name}";
            var prefab = loader is ICancellablePanelLoader cancellable
                ? await cancellable.LoadPrefabAsync(path, token)
                : await loader.LoadPrefabAsync(path);
            await UniTask.SwitchToMainThread();
            if (token.IsCancellationRequested)
            {
                ReleasePrefab(loader, path, prefab);
                token.ThrowIfCancellationRequested();
            }
            return InstantiatePanel<T>(loader, path, prefab);
        }

        protected virtual T NewUI<T>() where T : AbstractUIPanel
        {
            var token = operationsCancellation.Token;
            var loader = panelLoader;
            var path = $"{Settings.assetPathPrefix}{typeof(T).Name}";
            var prefab = loader.LoadPrefab(path);
            if (token.IsCancellationRequested)
            {
                ReleasePrefab(loader, path, prefab);
                token.ThrowIfCancellationRequested();
            }
            return InstantiatePanel<T>(loader, path, prefab);
        }

        private T InstantiatePanel<T>(IPanelLoader loader, string path, GameObject prefab) where T : AbstractUIPanel
        {
            GameObject go = null;
            bool ownsLease = false;
            try
            {
                if (!prefab) throw new InvalidOperationException($"UI prefab '{path}' was not found.");
                if (!prefab.TryGetComponent<T>(out _) || !prefab.TryGetComponent<RectTransform>(out _))
                    throw new InvalidOperationException($"UI prefab '{path}' must have {typeof(T).FullName} and RectTransform on its root.");
                // Instantiate under an inactive parent so OnEnable cannot run before onInit.
                go = Object.Instantiate(prefab, stagingRoot);
                var form = go.GetComponent<T>();
                TrackPanel(typeof(T), form, loader, path, prefab);
                ownsLease = true;
                go.SetActive(false);
                go.transform.SetParent(defaultRoot, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                form.BindManager(this);
                return form;
            }
            catch
            {
                if (go) Object.Destroy(go);
                if (!ownsLease) ReleasePrefab(loader, path, prefab);
                throw;
            }
        }

        private PanelEntry TrackPanel(Type key, AbstractUIPanel panel, IPanelLoader loader, string path, GameObject prefab)
        {
            var entry = new PanelEntry { Key = key, Panel = panel, GameObject = panel.gameObject, Loader = loader, Path = path, Prefab = prefab };
            panelList.Add(entry);
            TrackPanelLifetimeAsync(entry).Forget();
            return entry;
        }

        private async UniTask TrackPanelLifetimeAsync(PanelEntry entry)
        {
            // Track the owned object, not just its panel component. Never-activated preloads are monitored too.
            await entry.GameObject.GetAsyncDestroyTrigger().OnDestroyAsync();
            await UniTask.WaitUntil(() => !entry.GameObject);
            ForgetPanel(entry);
            panelList.Remove(entry);
            ReleasePrefab(entry.Loader, entry.Path, entry.Prefab);
        }

        private static void ReleasePrefab(IPanelLoader loader, string path, GameObject prefab)
        {
            if (ReferenceEquals(prefab, null) || !(loader is IReleasablePanelLoader releasable)) return;
            try { releasable.ReleasePrefab(path, prefab); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private PanelEntry FindPanelEntry(AbstractUIPanel panel)
        {
            foreach (var entry in panelList)
                if (ReferenceEquals(entry.Panel, panel)) return entry;
            return null;
        }

        private bool IsRegistered(AbstractUIPanel panel)
        {
            var entry = FindPanelEntry(panel);
            return entry != null && !entry.DestroyRequested && panelByType.TryGetValue(entry.Key, out var cached) && ReferenceEquals(cached, entry);
        }

        private void ForgetPanel(PanelEntry entry)
        {
            RemoveFromStack(entry.Panel);
            if (panelByType.TryGetValue(entry.Key, out var cached) && ReferenceEquals(cached, entry))
                panelByType.Remove(entry.Key);
        }

        private void DestroyPanel(PanelEntry entry)
        {
            ForgetPanel(entry);
            if (entry.DestroyRequested) return;
            entry.DestroyRequested = true;
            if (entry.Panel) entry.Panel.PrepareForDestroy();
            else if (entry.GameObject) entry.GameObject.SetActive(false);
            if (entry.GameObject)
            {
                Object.Destroy(entry.GameObject);
            }
        }
        private void RemoveFromStack(AbstractUIPanel panel)
        {
            for (var index = showStack.Count - 1; index >= 0; index--)
                if (ReferenceEquals(showStack[index], panel)) showStack.RemoveAt(index);
        }

        public void SafeDestroy(AbstractUIPanel panel)
        {
            EnsureMainThread();
            if (ReferenceEquals(panel, null)) return;
            var entry = FindPanelEntry(panel);
            if (entry != null) { DestroyPanel(entry); return; }
            RemoveFromStack(panel);
            if (!panel) return;
            panel.PrepareForDestroy();
            Object.Destroy(panel.gameObject);
        }
        public void ClearAll()
        {
            EnsureReady();
            operationSuspensions++;
            try
            {
                CancelOperations();
                foreach (var entry in panelList.ToArray())
                {
                    if (entry.DestroyRequested) continue;
                    if (entry.Panel && entry.Panel.DontDestroyOnClear) entry.Panel.CancelTransition();
                    else DestroyPanel(entry);
                }
            }
            finally
            {
                operationSuspensions--;
                if (!disposed && !draining && operationSuspensions == 0 && operations.Count > 0)
                    DrainOperationsAsync().Forget();
            }
        }

        private void CancelOperations()
        {
            var old = operationsCancellation;
            operationsCancellation = new CancellationTokenSource();
            // Remove pending work before cancellation continuations can enqueue new work.
            var pending = operations.ToArray();
            operations.Clear();
            try
            {
                CancelSafely(old);
                foreach (var operation in pending) operation.Completion.TrySetCanceled(operation.Token);
            }
            finally { old.Dispose(); }
        }

        public void Dispose()
        {
            EnsureMainThread();
            if (disposed) return;
            disposed = true;
            initialized = false;
            // Exclude owned cameras and EventSystem from same-frame replacement initialization.
            if (ReferenceEquals(instance, this)) instance = null;
            if (Mono) Mono.gameObject.SetActive(false);
            CancelOperations();
            operationsCancellation.Dispose();
            foreach (var entry in panelList.ToArray()) DestroyPanel(entry);
            panelByType.Clear();
            showStack.Clear();
            customRootDict.Clear();
            uiCameras.Clear();
            manualLockCount = scopedLockCount = 0;
            if (blockerCg) blockerCg.blocksRaycasts = false;
            if (defaultCamera) defaultCamera.enabled = false;
            OnShow = OnHide = OnShowEnd = OnHideEnd = null;
            if (Mono) Object.Destroy(Mono.gameObject);
        }

        internal static void CancelSafely(CancellationTokenSource source)
        {
            if (source == null) return;
            try { source.Cancel(); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        internal static void EnsureMainThread()
        {
            if (!PlayerLoopHelper.IsMainThread)
                throw new InvalidOperationException("UI operations must be called on Unity's main thread.");
        }
        private void EnsureReady()
        {
            EnsureMainThread();
            if (disposed) throw new ObjectDisposedException(nameof(UIManager));
            if (!initialized) throw new InvalidOperationException("UIManager is not initialized.");
        }

        public void SetLockInput(bool on)
        {
            EnsureMainThread();
            if (disposed) return;
            manualLockCount = Math.Max(0, manualLockCount + (on ? 1 : -1));
            ApplyInputLock();
        }

        private void ChangeScopedLock(int delta)
        {
            EnsureMainThread();
            if (disposed) return;
            scopedLockCount += delta;
            ApplyInputLock();
        }

        private void ApplyInputLock()
        {
            var locked = IsLockInput();
            if (interactionGroup && interactionGroup.interactable == locked) interactionGroup.interactable = !locked;
            if (!blockerCg) return;
#if UNITY_EDITOR
            blockerCg.name = $"Blocker {manualLockCount + scopedLockCount}";
#endif
            if (blockerCg.blocksRaycasts != locked) blockerCg.blocksRaycasts = locked;
        }

        public bool IsLockInput() => manualLockCount > 0 || scopedLockCount > 0;

        public void LockInputWhile(UniTask task) => LockInputWhileAsync(task).Forget();

        private async UniTask LockInputWhileAsync(UniTask task)
        {
            using (GetLockInputScope())
            {
                try { await task; }
                finally { await UniTask.SwitchToMainThread(); }
            }
        }

        public IDisposable GetLockInputScope()
        {
            EnsureReady();
            return new LockInputScope(this);
        }

        private sealed class LockInputScope : IDisposable
        {
            private UIManager manager;
            public LockInputScope(UIManager manager)
            {
                this.manager = manager;
                manager.ChangeScopedLock(1);
            }
            public void Dispose()
            {
                EnsureMainThread();
                var owner = manager;
                manager = null;
                owner?.ChangeScopedLock(-1);
            }
        }
        public static void LockInput(bool on) { if (instance != null) instance.SetLockInput(on); }

        public Canvas GetCustomLayerCanvas(string name, int sortingOrder, string sortingLayerName = "UI")
        {
            EnsureReady();
            if (!customRootDict.TryGetValue(name, out var customCanvas) || !customCanvas)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                go.layer = uiLayerMask;
                var rect = (RectTransform)go.transform;
                rect.SetParent(CanvasRectTransform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                customCanvas = go.GetComponent<Canvas>();
                customRootDict[name] = customCanvas;
            }
            customCanvas.gameObject.SetActive(true);
            customCanvas.overrideSorting = true;
            customCanvas.sortingLayerName = sortingLayerName;
            customCanvas.sortingOrder = sortingOrder;
            return customCanvas;
        }

        public void SetCustomLayerSortOrder(string name, int sortOrder)
        {
            EnsureMainThread();
            if (customRootDict.TryGetValue(name, out var customCanvas) && customCanvas)
                customCanvas.sortingOrder = sortOrder;
        }

        public void RegisterAdditionalCam(Camera cam)
        {
            EnsureMainThread();
            if (disposed || !initialized || !cam || uiCameras.Contains(cam)) return;
            uiCameras.Add(cam);
            UICamera = cam;
            if (defaultCamera) defaultCamera.enabled = false;
        }

        public void UnregisterAdditionalCam(Camera cam)
        {
            EnsureMainThread();
            uiCameras.Remove(cam);
            uiCameras.RemoveAll(item => !item);
            if (disposed) return;
            if (uiCameras.Count > 0) UICamera = uiCameras[uiCameras.Count - 1];
            else if (defaultCamera)
            {
                defaultCamera.enabled = true;
                UICamera = defaultCamera;
            }
        }
    }

    public class DefaultPanelLoader : ICancellablePanelLoader
    {
        public UniTask<GameObject> LoadPrefabAsync(string path) => LoadPrefabAsync(path, CancellationToken.None);

        public async UniTask<GameObject> LoadPrefabAsync(string path, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Resources.LoadAsync<GameObject>(path).ToUniTask(cancellationToken: cancellationToken) as GameObject;
        }

        public GameObject LoadPrefab(string path) => Resources.Load<GameObject>(path);
    }
}