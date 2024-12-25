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
        AbstractUIPanel CurrentAbstractUIPanel { get; }
        RectTransform CanvasRectTransform { get; }
        Camera UICamera { get; }
        UISettings Settings { get; }
        Action<AbstractUIPanel> OnShow { get; set; }
        Action<AbstractUIPanel> OnHide { get; set; }
        Action<AbstractUIPanel> OnShowEnd { get; set; }
        Action<AbstractUIPanel> OnHideEnd { get; set; }
        void Initiate();
        void Initiate(IPanelLoader panelLoader);
        void ShowUI<T>(Action<T> onInit = null, string layer = "UI") where T : AbstractUIPanel;
        void HideThenShowUI<T>(Action<T> onInit = null, string layer = "UI") where T : AbstractUIPanel;
        void CloseThenShowUI<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI") where T : AbstractUIPanel;
        void ShowUIUnmanagable<T>(Action<T> onInit = null, string layer = "UI", int sortingOrder = 900) where T : AbstractUIPanel;
        void Back(bool destroy = false);
        void CloseCurrent(bool destroy = false);
        void HideCurrent();
        void CloseUntil<T>(bool destroy = false) where T : AbstractUIPanel;
        void BackThenShow<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI") where T : AbstractUIPanel;
        void PreloadUI<T>(string layer = "UI") where T : AbstractUIPanel;
        void ShowThenClosePrev<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI") where T : AbstractUIPanel;
        void ShowThenHidePrev<T>(Action<T> onInit = null, string layer = "UI") where T : AbstractUIPanel;
        UniTask ShowUIAsync<T>(Action<T> onInit = null, string layer = "UI") where T : AbstractUIPanel;
        UniTask HideThenShowUIAsync<T>(Action<T> onInit = null, string layer = "UI") where T : AbstractUIPanel;
        UniTask CloseThenShowUIAsync<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI") where T : AbstractUIPanel;
        UniTask ShowUIUnmanagableAsync<T>(Action<T> onInit = null, string layer = "UI", int sortingOrder = 900) where T : AbstractUIPanel;
        UniTask BackAsync(bool destroy = false);
        UniTask CloseCurrentAsync(bool destroy = false);
        UniTask HideCurrentAsync();
        UniTask CloseUntilAsync<T>(bool destroy = false) where T : AbstractUIPanel;
        UniTask BackThenShowAsync<T>(Action<T> onInit, bool destroy = false, string layer = "UI") where T : AbstractUIPanel;
        UniTask WaitUntilUIHideEnd<T>() where T : AbstractUIPanel;
        UniTask PreloadUIAsync<T>(string layer = "UI") where T : AbstractUIPanel;
        UniTask ShowThenClosePrevAsync<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI") where T : AbstractUIPanel;
        UniTask ShowThenHidePrevAsync<T>(Action<T> onInit = null, string layer = "UI") where T : AbstractUIPanel;
        void ClearAll();
        void SetLockInput(bool on);
        bool IsLockInput();
        T GetUI<T>() where T : AbstractUIPanel;
        Canvas GetCustomLayerCanvas(string name, int sortingOrder, string sortingLayerName = "UI");
        void SetCustomLayerSortOrder(string name, int sortOrder);
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
        private readonly Stack<AbstractUIPanel> showFormStack = new Stack<AbstractUIPanel>();
        private readonly List<AbstractUIPanel> uiFormsList = new List<AbstractUIPanel>();
        private Canvas canvas;
        private static readonly IPanelLoader DefaultPanelLoader = new DefaultPanelLoader();
        private readonly int uiLayerMask = LayerMask.NameToLayer("UI");
        public UIManagerMono Mono { get; private set; }
        public AbstractUIPanel CurrentAbstractUIPanel => showFormStack.Count == 0 ? null : showFormStack.Peek();
        public RectTransform CanvasRectTransform { get; private set; }
        public Camera UICamera {get; private set;}
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
            this.panelLoader = panelLoader ?? DefaultPanelLoader;
            new GameObject("UICam", typeof(Camera)).TryGetComponent(out Camera cam);
            UICamera = cam;
            UICamera.transform.SetParent(Mono.transform);
            UICamera.transform.localPosition = new Vector3(0, 0, -10);
            UICamera.clearFlags = settings.cameraClearFlags;
            if(UICamera.clearFlags == CameraClearFlags.SolidColor || UICamera.clearFlags == CameraClearFlags.Skybox)
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
            if (!Input.GetKeyDown(KeyCode.Escape))return;
            if(IsLockInput()) return;
            if(!CurrentAbstractUIPanel) return;
            if(!CurrentAbstractUIPanel.CanInteract) return; 
            CurrentAbstractUIPanel.OnESCClick();
        }

        #region 同步（只是同步调用，并不保证任务同帧开始或者完成）

        public void ShowUI<T>(Action<T> onInit = null, string layer = "UI") where T : AbstractUIPanel
        {
            ShowUIAsync(onInit, layer).Forget();
        }

        public void HideThenShowUI<T>(Action<T> onInit = null, string layer = "UI") where T : AbstractUIPanel
        {
            HideThenShowUIAsync(onInit, layer).Forget();
        }

        public void CloseThenShowUI<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI") where T : AbstractUIPanel
        {
            CloseThenShowUIAsync(onInit, destroy, layer).Forget();
        }

        public void ShowUIUnmanagable<T>(Action<T> onInit = null, string layer = "UI", int sortingOrder = 900) where T : AbstractUIPanel
        {
            ShowUIUnmanagableAsync(onInit, layer, sortingOrder).Forget();
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

        public void BackThenShow<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI") where T : AbstractUIPanel
        {
            BackThenShowAsync(onInit, destroy, layer).Forget();
        }

        public void PreloadUI<T>(string layer = "UI") where T : AbstractUIPanel
        {
            PreloadUIAsync<T>(layer).Forget();
        }

        public void ShowThenClosePrev<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI") where T : AbstractUIPanel
        {
            ShowThenClosePrevAsync(onInit, destroy, layer).Forget();
        }

        public void ShowThenHidePrev<T>(Action<T> onInit = null, string layer = "UI") where T : AbstractUIPanel
        {
            ShowThenHidePrevAsync(onInit, layer).Forget();
        }

        #endregion


        #region 异步

        public async UniTask ShowUIAsync<T>(Action<T> onInit = null, string layer = "UI") where T : AbstractUIPanel
        {
            var sortOrder = showFormStack.Count == 0 ? 1 : showFormStack.Peek().SortOrder + 5;
            var form = GetUI<T>();
            if (!form)
                form = await NewUI<T>();
            form.gameObject.SetActive(false);
            form.SetSortingLayer(layer);
            form.SetOrderInLayer(sortOrder);
            showFormStack.Push(form);
            if (!uiFormsList.Contains(form))
                uiFormsList.Add(form);
            onInit?.Invoke(form);
            form.BeforeShow();
            await form.OnShowAsync();
        }


        public async UniTask HideThenShowUIAsync<T>(Action<T> onInit = null, string layer = "UI") where T : AbstractUIPanel
        {
            await HideCurrentAsync();
            await ShowUIAsync(onInit, layer);
        }

        public async UniTask CloseThenShowUIAsync<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI") where T : AbstractUIPanel
        {
            await CloseCurrentAsync(destroy);
            await ShowUIAsync(onInit, layer);
        }

        public async UniTask ShowUIUnmanagableAsync<T>(Action<T> onInit = null, string layer = "UI", int sortingOrder = 900) where T : AbstractUIPanel
        {
            var form = GetUI<T>();
            if (!form)
                form = await NewUI<T>();
            form.gameObject.SetActive(false);
            form.SetSortingLayer(layer);
            form.SetOrderInLayer(sortingOrder);
            if (!uiFormsList.Contains(form)) uiFormsList.Add(form);
            onInit?.Invoke(form);
            form.BeforeShow();
            await form.OnShowAsync();
        }

        public async UniTask BackAsync(bool destroy = false)
        {
            await CloseCurrentAsync(destroy);
            if (!CurrentAbstractUIPanel) return;
            if (!CurrentAbstractUIPanel.IsShow)
            {
                await CurrentAbstractUIPanel.OnShowAsync();
            }
        }


        public async UniTask CloseCurrentAsync(bool destroy = false)
        {
            if (showFormStack.Count == 0) return;
            var form = showFormStack.Pop();
            await form.OnHideAsync();
            if (destroy)
            {
                uiFormsList.Remove(form);
                Object.Destroy(form.gameObject);
            }
        }


        public async UniTask HideCurrentAsync()
        {
            if (showFormStack.Count == 0) return;
            var form = showFormStack.Peek();
            await form.OnHideAsync();
        }

        public async UniTask CloseUntilAsync<T>(bool destroy = false) where T : AbstractUIPanel
        {
            while (showFormStack.Count > 0)
            {
                var form = showFormStack.Peek();
                if (form is T)
                {
                    if (!form.isActiveAndEnabled)
                        await form.OnShowAsync();
                    return;
                }

                form = showFormStack.Pop();
                if (form.isActiveAndEnabled)
                {
                    form.OnHideAsync().ContinueWith(() =>
                    {
                        if (!destroy) return;
                        uiFormsList.Remove(form);
                        Object.Destroy(form.gameObject);
                    }).Forget();
                }
                else
                {
                    if (!destroy) continue;
                    uiFormsList.Remove(form);
                    Object.Destroy(form.gameObject);
                }
                
            }
        }

        public async UniTask BackThenShowAsync<T>(Action<T> onInit, bool destroy = false, string layer = "UI") where T : AbstractUIPanel
        {
            await BackAsync(destroy);
            await ShowUIAsync(onInit, layer);
        }

        public UniTask WaitUntilUIHideEnd<T>() where T : AbstractUIPanel
        {
            var panel = GetUI<T>();
            return UniTask.WaitUntil(() => panel.gameObject.activeSelf == false);
        }

        public async UniTask PreloadUIAsync<T>(string layer = "UI") where T : AbstractUIPanel
        {
            var form = GetUI<T>();
            if (!form)
                form = await NewUI<T>();
            form.gameObject.SetActive(false);
            form.SetSortingLayer(layer);
            form.SetOrderInLayer(0);
            if (!uiFormsList.Contains(form))
                uiFormsList.Add(form);
        }

        public async UniTask ShowThenClosePrevAsync<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI") where T : AbstractUIPanel
        {
            var prev = showFormStack.Count > 0 ? showFormStack.Pop() : null;
            await ShowUIAsync(onInit, layer);
            if (!prev) return;
            await prev.OnHideAsync();
            if (destroy)
            {
                uiFormsList.Remove(prev);
                Object.Destroy(prev.gameObject);
            }
        }

        public async UniTask ShowThenHidePrevAsync<T>(Action<T> onInit = null, string layer = "UI") where T : AbstractUIPanel
        {
            var prev = showFormStack.Count > 0 ? showFormStack.Peek() : null;
            await ShowUIAsync(onInit, layer);
            if(!prev) return;
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

        #endregion

        public T GetUI<T>() where T : AbstractUIPanel
        {
            return uiFormsList.Where(form => form.GetType() == typeof(T)).Cast<T>().FirstOrDefault();
        }

        private async UniTask<T> NewUI<T>() where T : AbstractUIPanel
        {
            LockInput(true);
            var prefab = await panelLoader.LoadPrefab($"{Settings.assetPathPrefix}{typeof(T).Name}");
            LockInput(false);
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
            uiFormsList.RemoveAll(ui =>
            {
                if (!ui || !ui.gameObject) return true;
                if (ui.DontDestroyOnClear) return false;
                Object.DestroyImmediate(ui.gameObject);
                return true;
            });

            var remainingForms = new Stack<AbstractUIPanel>();
            while (showFormStack.Count > 0)
            {
                var form = showFormStack.Pop();
                if (form && form.DontDestroyOnClear)
                {
                    remainingForms.Push(form);
                }
            }

            while (remainingForms.Count > 0)
            {
                showFormStack.Push(remainingForms.Pop());
            }
        }

    }
    public class DefaultPanelLoader : IPanelLoader
    {
        public UniTask<GameObject> LoadPrefab(string path)
        {
            return UniTask.FromResult(Resources.Load<GameObject>(path));
        }
    }
}