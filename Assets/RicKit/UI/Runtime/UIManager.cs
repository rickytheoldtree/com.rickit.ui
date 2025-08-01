using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
        UniTask WaitUntilUIHideEnd<T>() where T : AbstractUIPanel;
        UniTask PreloadUIAsync<T>(string layer = "UI", bool asyncLoadNew = true) where T : AbstractUIPanel;

        UniTask ShowThenClosePrevAsync<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI",
            int orderInLayerDelta = 5, bool asyncLoadNew = true) where T : AbstractUIPanel;
        UniTask ShowThenHidePrevAsync<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel;
        void ClearAll();
        void SetLockInput(bool on);
        bool IsLockInput();
        void LockInputWhile(UniTask task);
        T GetUI<T>() where T : AbstractUIPanel;
        Canvas GetCustomLayerCanvas(string name, int sortingOrder, string sortingLayerName = "UI");
        void SetCustomLayerSortOrder(string name, int sortOrder);
        void SafeDestroy(AbstractUIPanel panel);
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
        public Camera UICamera { get; private set; }
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
            instance = this;
            CreateUIManager(panelLoader);
        }

        private void CreateUIManager(IPanelLoader panelLoader)
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

            var settings = Settings = Resources.Load<UISettings>("UISettings");
            if (!settings)
            {
                Debug.LogError("UISettings not found in Resources.");
                return;
            }

            this.panelLoader = panelLoader ?? DefaultPanelLoader;
            new GameObject("UICam", typeof(Camera)).TryGetComponent(out Camera cam);
            UICamera = cam;
            UICamera.transform.SetParent(Mono.transform);
            UICamera.transform.localPosition = new Vector3(0, 0, -10);
            UICamera.clearFlags = settings.cameraClearFlags;
            if (UICamera.clearFlags == CameraClearFlags.SolidColor || UICamera.clearFlags == CameraClearFlags.Skybox)
                UICamera.backgroundColor = settings.backgroundColor;
            UICamera.cullingMask = settings.cullingMask;
            UICamera.depth = settings.depth;
            UICamera.orthographic = true;
            UICamera.orthographicSize = 5;
            UICamera.nearClipPlane = settings.nearClipPlane;
            UICamera.farClipPlane = settings.farClipPlane;

            new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
                .TryGetComponent(out canvas);
            canvas.gameObject.layer = uiLayerMask;
            canvas.transform.SetParent(Mono.transform);
            CanvasRectTransform = (RectTransform)canvas.transform;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = UICamera;
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
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (IsLockInput()) return;
            if (!CurrentUIPanel) return;
            if (!CurrentUIPanel.CanInteract) return;
            CurrentUIPanel.OnESCClick();
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

        #endregion


        #region 异步

        public async UniTask ShowUIAsync<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5,
            bool asyncLoadNew = true)
            where T : AbstractUIPanel
        {
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
            await form.OnHideAsync();
            if (destroy)
            {
                panelList.Remove(form);
                Object.Destroy(form.gameObject);
            }
        }


        public async UniTask HideCurrentAsync()
        {
            if (showStack.Count == 0) return;
            var form = showStack.Peek();
            await form.OnHideAsync();
        }

        public async UniTask CloseUntilAsync<T>(bool destroy = false) where T : AbstractUIPanel
        {
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
                        if (!destroy) return;
                        panelList.Remove(form);
                        Object.Destroy(form.gameObject);
                    }).Forget();
                }
                else
                {
                    if (!destroy) continue;
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

        public UniTask WaitUntilUIHideEnd<T>() where T : AbstractUIPanel
        {
            var panel = GetUI<T>();
            return UniTask.WaitUntil(() => panel.gameObject.activeSelf == false);
        }

        public async UniTask PreloadUIAsync<T>(string layer = "UI", bool asyncLoadNew = true) where T : AbstractUIPanel
        {
            var form = GetUI<T>();
            if (!form)
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
            if (destroy)
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
            SetLockInput(true);
            task.ContinueWith(() => SetLockInput(false)).Forget();
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

        #endregion

        public T GetUI<T>() where T : AbstractUIPanel
        {
            return panelList.Where(form => form.GetType() == typeof(T)).Cast<T>().FirstOrDefault();
        }

        private async UniTask<T> NewUIAsync<T>() where T : AbstractUIPanel
        {
            SetLockInput(true);
            var prefab = await panelLoader.LoadPrefabAsync($"{Settings.assetPathPrefix}{typeof(T).Name}");
            SetLockInput(false);
            var go = Object.Instantiate(prefab, defaultRoot);
            go.TryGetComponent(out RectTransform rect);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.TryGetComponent(out T form);
            return form;
        }

        private T NewUI<T>() where T : AbstractUIPanel
        {
            SetLockInput(true);
            var prefab = panelLoader.LoadPrefab($"{Settings.assetPathPrefix}{typeof(T).Name}");
            SetLockInput(false);
            var go = Object.Instantiate(prefab, defaultRoot);
            go.TryGetComponent(out RectTransform rect);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.TryGetComponent(out T form);
            return form;
        }

        public void ClearAll()
        {
            panelList.RemoveAll(ui =>
            {
                if (!ui || !ui.gameObject) return true;
                if (ui.DontDestroyOnClear) return false;
                Object.DestroyImmediate(ui.gameObject);
                return true;
            });

            var remainingForms = new Stack<AbstractUIPanel>();
            while (showStack.Count > 0)
            {
                var form = showStack.Pop();
                if (form && form.DontDestroyOnClear)
                {
                    remainingForms.Push(form);
                }
            }

            while (remainingForms.Count > 0)
            {
                showStack.Push(remainingForms.Pop());
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