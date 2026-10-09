using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using RicKit.UI.Component;
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

    public class UIManager : IUIManager
    {
        private static IUIManager instance;

        public static IUIManager I
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("UIManager not initialized, please call UIManager.Init() first.");
                }

                return instance;
            }
        }

        /// <summary>
        /// 不确定是否已初始化时用这个，不会像 I 那样打错误日志
        /// </summary>
        public static bool TryGetInstance(out IUIManager manager)
        {
            manager = instance;
            return manager != null;
        }

        private CanvasGroup blockerCg;
        private RectTransform defaultRoot;
        private IPanelLoader panelLoader;
        private readonly Stack<AbstractUIPanel> showStack = new Stack<AbstractUIPanel>();
        private readonly List<AbstractUIPanel> panelList = new List<AbstractUIPanel>();
        private Canvas canvas;
        private static readonly IPanelLoader DefaultPanelLoader = new DefaultPanelLoader();
        private readonly int uiLayerMask = LayerMask.NameToLayer("UI");
        public UIManagerMono Mono { get; private set; }
        public AbstractUIPanel CurrentUIPanel => showStack.Count == 0 ? null : showStack.Peek();
        public Canvas UICanvas => canvas;
        public RectTransform CanvasRectTransform { get; private set; }

        public Camera UICamera
        {
            get => canvas.worldCamera;
            private set => canvas.worldCamera = value;
        }
        private Camera defaultCamera;
        private readonly List<Camera> uiCameras = new();
        public UISettings Settings { get; private set; }

        #region Events

        public Action<AbstractUIPanel> OnShow { get; set; }
        public Action<AbstractUIPanel> OnHide { get; set; }
        public Action<AbstractUIPanel> OnShowEnd { get; set; }
        public Action<AbstractUIPanel> OnHideEnd { get; set; }

        #endregion

        /// <summary>
        /// 需要在任何UI操作之前调用
        /// </summary>
        public static void Init(IPanelLoader panelLoader = null)
        {
            if (instance != null)
            {
                Debug.LogError("UIManager already initialized.");
                return;
            }

            new UIManager().Initiate(panelLoader);
        }

        public void Initiate()
        {
            Initiate(DefaultPanelLoader);
        }

        public void Initiate(IPanelLoader panelLoader)
        {
            Initiate(Resources.Load<UISettings>("UISettings"), panelLoader);
        }

        /// <summary>
        /// 直接传入设置，不要求 Resources 里有 UISettings（多套设置、测试时用）
        /// </summary>
        public void Initiate(UISettings settings, IPanelLoader panelLoader)
        {
            instance = this;
            CreateUIManager(settings, panelLoader);
            if (!canvas) return;
            // 在 UIManager 初始化之前就启用的 UIAdditionalCamera 当时注册不上，这里补上
            foreach (var additionalCamera in Object.FindObjectsOfType<UIAdditionalCamera>())
            {
                if (additionalCamera.isActiveAndEnabled && additionalCamera.Camera)
                    RegisterAdditionalCam(additionalCamera.Camera);
            }
        }

        private void CreateUIManager(UISettings uiSettings, IPanelLoader panelLoader)
        {
            Mono = new GameObject("UIManager").AddComponent<UIManagerMono>();
            Mono.gameObject.layer = uiLayerMask;
            Mono.SetUIManager(this);
            Object.DontDestroyOnLoad(Mono.gameObject);
            var eventSystem = Object.FindObjectOfType<EventSystem>();
            eventSystem = eventSystem
                ? eventSystem
                : new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule))
                    .GetComponent<EventSystem>();
            Object.DontDestroyOnLoad(eventSystem);

            var settings = Settings = uiSettings;
            if (!settings)
            {
                Debug.LogError("UISettings not found in Resources.");
                return;
            }

            this.panelLoader = panelLoader ?? DefaultPanelLoader;
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

            new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
                .TryGetComponent(out canvas);
            canvas.gameObject.layer = uiLayerMask;
            canvas.transform.SetParent(Mono.transform);
            CanvasRectTransform = (RectTransform)canvas.transform;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            UICamera = defaultCamera;
            canvas.planeDistance = 5;
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
        }

        public void Update()
        {
            // 只开了新输入系统时 Input.GetKeyDown 每帧都会抛异常
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (IsLockInput()) return;
            if (!CurrentUIPanel) return;
            if (!CurrentUIPanel.CanInteract) return;
            CurrentUIPanel.OnESCClick();
#endif
        }

        #region 同步（只是同步调用，并不保证任务同帧开始或者完成）

        public void ShowUI<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel
        {
            Fire(ShowUIAsync(onInit, layer, orderInLayerDelta, asyncLoadNew), "ShowUI");
        }

        public void HideThenShowUI<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel
        {
            Fire(HideThenShowUIAsync(onInit, layer, orderInLayerDelta, asyncLoadNew), "HideThenShowUI");
        }

        public void CloseThenShowUI<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            Fire(CloseThenShowUIAsync(onInit, destroy, layer, orderInLayerDelta, asyncLoadNew), "CloseThenShowUI");
        }

        public void ShowUIUnmanagable<T>(Action<T> onInit = null, string layer = "UI", int sortingOrder = 900,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel
        {
            Fire(ShowUIUnmanagableAsync(onInit, layer, sortingOrder, asyncLoadNew), "ShowUIUnmanagable");
        }

        public void Back(bool destroy = false)
        {
            Fire(BackAsync(destroy), "Back");
        }

        /// <summary>
        /// Close会出栈，且可销毁
        /// </summary>
        /// <param name="destroy">是否在退出动画后销毁</param>
        public void CloseCurrent(bool destroy = false)
        {
            Fire(CloseCurrentAsync(destroy), "CloseCurrent");
        }

        /// <summary>
        /// Hide不会出栈，且不可销毁
        /// </summary>
        public void HideCurrent()
        {
            Fire(HideCurrentAsync(), "HideCurrent");
        }

        public void CloseUntil<T>(bool destroy = false) where T : AbstractUIPanel
        {
            Fire(CloseUntilAsync<T>(destroy), "CloseUntil");
        }

        public void BackThenShow<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            Fire(BackThenShowAsync(onInit, destroy, layer, orderInLayerDelta, asyncLoadNew), "BackThenShow");
        }

        public void PreloadUI<T>(string layer = "UI", bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            Fire(PreloadUIAsync<T>(layer, asyncLoadNew), "PreloadUI");
        }

        public void ShowThenClosePrev<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            Fire(ShowThenClosePrevAsync(onInit, destroy, layer, orderInLayerDelta, asyncLoadNew), "ShowThenClosePrev");
        }

        public void ShowThenHidePrev<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel
        {
            Fire(ShowThenHidePrevAsync(onInit, layer, orderInLayerDelta, asyncLoadNew), "ShowThenHidePrev");
        }

        public void Close<T>(bool destroy = false) where T : AbstractUIPanel
        {
            Fire(CloseAsync<T>(destroy), "Close");
        }

        #endregion


        #region 异步

        public async UniTask ShowUIAsync<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel
        {
            if (Verbose) Log($"ShowUI<{typeof(T).Name}>");
            var sortOrder = showStack.Count == 0 ? orderInLayerDelta : showStack.Peek().OrderInLayer + orderInLayerDelta;
            var form = GetUI<T>();
            if (!form)
                form = asyncLoadNew ? await NewUIAsync<T>() : NewUI<T>();

            form.gameObject.SetActive(true);
            form.SetSortingLayer(layer);
            form.SetOrderInLayer(sortOrder);
            showStack.Push(form);
            if (!panelList.Contains(form))
                panelList.Add(form);
            onInit?.Invoke(form);
            await form.OnShowAsync();
        }


        public async UniTask HideThenShowUIAsync<T>(Action<T> onInit = null, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            await HideCurrentAsync();
            await ShowUIAsync(onInit, layer, orderInLayerDelta, asyncLoadNew);
        }

        public async UniTask CloseThenShowUIAsync<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            await CloseCurrentAsync(destroy);
            await ShowUIAsync(onInit, layer, orderInLayerDelta, asyncLoadNew);
        }

        public async UniTask ShowUIUnmanagableAsync<T>(Action<T> onInit = null, string layer = "UI",
            int sortingOrder = 900, bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            if (Verbose) Log($"ShowUIUnmanagable<{typeof(T).Name}>");
            var form = GetUI<T>();
            if (!form)
                form = asyncLoadNew ? await NewUIAsync<T>() : NewUI<T>();
            form.gameObject.SetActive(true);
            form.SetSortingLayer(layer);
            form.SetOrderInLayer(sortingOrder);
            if (!panelList.Contains(form)) panelList.Add(form);
            onInit?.Invoke(form);
            await form.OnShowAsync();
        }

        public async UniTask BackAsync(bool destroy = false)
        {
            await CloseCurrentAsync(destroy);
            if (!CurrentUIPanel) return;
            if (!CurrentUIPanel.IsShow)
            {
                await CurrentUIPanel.OnShowAsync();
            }
        }


        public async UniTask CloseCurrentAsync(bool destroy = false)
        {
            if (showStack.Count == 0) return;
            var form = showStack.Pop();
            if (Verbose) Log($"CloseCurrent {(form ? form.GetType().Name : "<destroyed>")}");
            await form.OnHideAsync();
            if (destroy || form.DestroyOnClose)
            {
                panelList.Remove(form);
                Object.Destroy(form.gameObject);
            }
        }


        public async UniTask HideCurrentAsync()
        {
            if (showStack.Count == 0) return;
            var form = showStack.Peek();
            if (Verbose) Log($"HideCurrent {(form ? form.GetType().Name : "<destroyed>")}");
            await form.OnHideAsync();
        }

        public async UniTask CloseUntilAsync<T>(bool destroy = false) where T : AbstractUIPanel
        {
            if (Verbose) Log($"CloseUntil<{typeof(T).Name}>");
            while (showStack.Count > 0)
            {
                var form = showStack.Peek();
                if (form is T)
                {
                    if (!form.isActiveAndEnabled)
                        await form.OnShowAsync();
                    return;
                }

                form = showStack.Pop();
                if (form.isActiveAndEnabled)
                {
                    form.OnHideAsync().ContinueWith(() =>
                    {
                        if (!destroy && !form.DestroyOnClose) return;
                        panelList.Remove(form);
                        Object.Destroy(form.gameObject);
                    }).Forget();
                }
                else
                {
                    if (!destroy && !form.DestroyOnClose) continue;
                    panelList.Remove(form);
                    Object.Destroy(form.gameObject);
                }
            }
        }

        public async UniTask BackThenShowAsync<T>(Action<T> onInit, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            await BackAsync(destroy);
            await ShowUIAsync(onInit, layer, orderInLayerDelta, asyncLoadNew);
        }

        public UniTask WaitUntilUIHideEnd<T>(CancellationToken token = default) where T : AbstractUIPanel
        {
            if (token.IsCancellationRequested) return UniTask.FromCanceled(token);
            var panel = GetUI<T>();
            // 面板不存在或等待中途被销毁（比如 ClearAll）都算已关闭，原来这两种情况会抛空引用
            if (!panel) return UniTask.CompletedTask;
            return UniTask.WaitUntil(() => !panel || !panel.gameObject.activeSelf, cancellationToken: token);
        }

        public async UniTask PreloadUIAsync<T>(string layer = "UI", bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            if (Verbose) Log($"PreloadUI<{typeof(T).Name}>");
            var form = GetUI<T>();
            if (form)
                return;
            form = asyncLoadNew ? await NewUIAsync<T>() : NewUI<T>();
            form.gameObject.SetActive(false);
            form.SetSortingLayer(layer);
            form.SetOrderInLayer(0);
            if (!panelList.Contains(form))
                panelList.Add(form);
        }

        public async UniTask ShowThenClosePrevAsync<T>(Action<T> onInit = null, bool destroy = false,
            string layer = "UI", int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            var prev = showStack.Count > 0 ? showStack.Pop() : null;
            var prevOrderInLayer = prev ? prev.OrderInLayer : 0;
            await ShowUIAsync(onInit, layer, prevOrderInLayer + orderInLayerDelta, asyncLoadNew);
            if (!prev) return;
            await prev.OnHideAsync();
            var current = GetUI<T>();
            if (current) 
                current.SetOrderInLayer(prevOrderInLayer);
            if (destroy || prev.DestroyOnClose)
            {
                panelList.Remove(prev);
                Object.Destroy(prev.gameObject);
            }
        }

        public async UniTask ShowThenHidePrevAsync<T>(Action<T> onInit = null, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            var prev = showStack.Count > 0 ? showStack.Peek() : null;
            await ShowUIAsync(onInit, layer, orderInLayerDelta, asyncLoadNew);
            if (!prev) return;
            await prev.OnHideAsync();
        }

        public async UniTask CloseAsync<T>(bool destroy = false) where T : AbstractUIPanel
        {
            if (Verbose) Log($"Close<{typeof(T).Name}>");
            var all = showStack.ToArray();
            showStack.Clear();
            foreach (var item in all.Reverse())
            {
                if (item is T)
                {
                    await item.OnHideAsync();
                    if (!destroy && !item.DestroyOnClose) continue;
                    panelList.Remove(item);
                    Object.Destroy(item.gameObject);
                }
                else
                {
                    showStack.Push(item);
                }
            }
        }

        #endregion


        #region Debug

        private bool Verbose => Settings && Settings.verboseLog;

        private void Log(string message)
        {
            Debug.Log($"[RicKit.UI] {message} | stack={showStack.Count} lock={lockCount}");
        }

        // 同步版导航原来直接 Forget，取消异常会被 UniTask 静默吞掉；verboseLog 打开时把它打出来
        private void Fire(UniTask task, string name)
        {
            if (Verbose) FireAsync(task, name).Forget();
            else task.Forget();
        }

        private async UniTaskVoid FireAsync(UniTask task, string name)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                Log($"{name} 被取消（面板可能在动画中被销毁）");
            }
        }

        #endregion


        #region LockInput

        private int lockCount;

        public void SetLockInput(bool on)
        {
            if (instance == null) return;
            lockCount += on ? 1 : -1;
            lockCount = lockCount < 0 ? 0 : lockCount;
#if UNITY_EDITOR
            blockerCg.name = $"Blocker {lockCount}";
#endif
            blockerCg.blocksRaycasts = lockCount > 0;
        }

        public bool IsLockInput()
        {
            return lockCount > 0;
        }

        public void LockInputWhile(UniTask task)
        {
            LockRoutine().Forget();
            return;
            async UniTaskVoid LockRoutine()
            {
                using (GetLockInputScope())
                {
                    await task;
                }
            }
        }
                
        public IDisposable GetLockInputScope()
        {
            return new LockInputScope(this);
        }
        
        private struct LockInputScope : IDisposable
        {
            private IUIManager manager;

            public LockInputScope(IUIManager manager)
            {
                this.manager = manager;
                this.manager.SetLockInput(true);
            }

            public void Dispose()
            {
                if (manager == null) return;
                manager.SetLockInput(false);
                manager = null;
            }
        }

        public static void LockInput(bool on)
        {
            I?.SetLockInput(on);
        }

        #endregion

        #region CustomLayer

        private readonly Dictionary<string, Canvas> customRootDict = new Dictionary<string, Canvas>();

        public Canvas GetCustomLayerCanvas(string name, int sortingOrder, string sortingLayerName = "UI")
        {
            if (customRootDict.TryGetValue(name, out var customCanvas) && customCanvas)
            {
                customCanvas.gameObject.SetActive(true);
                customCanvas.overrideSorting = true;
                customCanvas.sortingLayerName = sortingLayerName;
                customCanvas.sortingOrder = sortingOrder;
                return customCanvas;
            }

            new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster))
                .TryGetComponent(out RectTransform rect);
            rect.gameObject.layer = uiLayerMask;
            rect.SetParent(CanvasRectTransform);
            rect.localPosition = Vector3.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.TryGetComponent(out customCanvas);
            customCanvas.overrideSorting = true;
            customCanvas.sortingLayerName = sortingLayerName;
            customCanvas.sortingOrder = sortingOrder;
            customRootDict.Add(name, customCanvas);
            return customCanvas;
        }

        public void SetCustomLayerSortOrder(string name, int sortOrder)
        {
            if (customRootDict.TryGetValue(name, out var canvasNew) && canvasNew)
            {
                canvasNew.sortingOrder = sortOrder;
            }
        }

        public void SafeDestroy(AbstractUIPanel panel)
        {
            if (!panel) return;
            if (Verbose) Log($"SafeDestroy {panel.GetType().Name}");
            if (panelList.Contains(panel))
            {
                panelList.Remove(panel);
            }

            if (showStack.Contains(panel))
            {
                var all = showStack.ToArray();
                showStack.Clear();
                foreach (var item in all.Reverse())
                    if (item != panel)
                        showStack.Push(item);
            }

            Object.Destroy(panel.gameObject);
        }

        public void RegisterAdditionalCam(Camera cam)
        {
            if (uiCameras.Contains(cam)) return;
            uiCameras.Add(cam);
            UICamera = cam;
            defaultCamera.enabled = false;
        }

        public void UnregisterAdditionalCam(Camera cam)
        {
            if (!uiCameras.Contains(cam)) return;
            uiCameras.Remove(cam);
            if (uiCameras.Count > 0)
                UICamera = uiCameras[^1];
            else
            {
                if (!defaultCamera) return;
                defaultCamera.enabled = true;
                UICamera = defaultCamera;
            }
        }

        #endregion

        public virtual T GetUI<T>() where T : AbstractUIPanel
        {
            // 面板可能被外部直接 Destroy（比如自己销毁自己），顺手清掉，免得死实例排在新实例前面
            panelList.RemoveAll(form => !form);
            foreach (var form in panelList)
            {
                if (form.GetType() == typeof(T))
                    return (T)form;
            }
            return null;
        }

        protected virtual async UniTask<T> NewUIAsync<T>() where T : AbstractUIPanel
        {
            var path = $"{Settings.assetPathPrefix}{typeof(T).Name}";
            GameObject prefab;
            SetLockInput(true);
            try
            {
                prefab = await panelLoader.LoadPrefabAsync(path);
            }
            finally
            {
                SetLockInput(false);
            }
            return InstantiatePanel<T>(prefab, path);
        }

        protected virtual T NewUI<T>() where T : AbstractUIPanel
        {
            var path = $"{Settings.assetPathPrefix}{typeof(T).Name}";
            GameObject prefab;
            SetLockInput(true);
            try
            {
                prefab = panelLoader.LoadPrefab(path);
            }
            finally
            {
                SetLockInput(false);
            }
            return InstantiatePanel<T>(prefab, path);
        }

        private T InstantiatePanel<T>(GameObject prefab, string path) where T : AbstractUIPanel
        {
            if (!prefab)
                throw new InvalidOperationException($"[RicKit.UI] 找不到 {typeof(T).Name} 的预制体，路径：{path}");
            var releasable = panelLoader as IReleasablePanelLoader;
            GameObject go;
            try
            {
                go = Object.Instantiate(prefab, defaultRoot);
            }
            catch
            {
                if (releasable != null) ReleasePrefab(releasable, path, prefab);
                throw;
            }
            // 一次加载对应一个实例：实例真正销毁之后才归还，避免资源先卸载、实例还在显示导致丢贴图
            if (releasable != null)
                go.GetCancellationTokenOnDestroy().Register(() => ReleasePrefab(releasable, path, prefab));
            if (!go.TryGetComponent(out RectTransform rect) || !go.TryGetComponent(out T form))
            {
                Object.Destroy(go);
                throw new InvalidOperationException(
                    $"[RicKit.UI] 预制体 {path} 的根节点必须挂 RectTransform 和 {typeof(T).Name}");
            }
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return form;
        }

        private static bool applicationQuitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ListenApplicationQuitting()
        {
            // 关闭 Domain Reload 时静态字段不会重置，这里手动复位
            applicationQuitting = false;
            Application.quitting -= OnApplicationQuitting;
            Application.quitting += OnApplicationQuitting;
        }

        private static void OnApplicationQuitting() => applicationQuitting = true;

        private static void ReleasePrefab(IReleasablePanelLoader loader, string path, GameObject prefab)
        {
            // 退出时资源系统可能已经先关了，没必要再逐个归还
            if (applicationQuitting) return;
            try
            {
                loader.ReleasePrefab(path, prefab);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void ClearAll()
        {
            if (Verbose) Log("ClearAll");
            var keepSet = new HashSet<AbstractUIPanel>();

            var panels = panelList.ToArray();

            foreach (var ui in panels)
            {
                if (!ui || !ui.gameObject) continue;

                if (ui.DontDestroyOnClear)
                    keepSet.Add(ui);
                else
                    Object.Destroy(ui.gameObject);
            }

            panelList.Clear();
            panelList.AddRange(keepSet);

            if (showStack.Count == 0) return;

            var temp = showStack.ToArray();
            showStack.Clear();

            for (var i = temp.Length - 1; i >= 0; i--)
            {
                var panel = temp[i];
                if (panel && keepSet.Contains(panel))
                    showStack.Push(panel);
            }
        }
    }

    public class DefaultPanelLoader : IPanelLoader
    {
        public UniTask<GameObject> LoadPrefabAsync(string path)
        {
            return UniTask.FromResult(Resources.Load<GameObject>(path));
        }

        public GameObject LoadPrefab(string path)
        {
            return Resources.Load<GameObject>(path);
        }
    }
}