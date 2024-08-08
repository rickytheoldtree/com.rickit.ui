using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using RicKit.UI.Interfaces;
using RicKit.UI.Panels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ADDRESSABLES_SUPPORT
using RicKit.UI.Extensions.AddressablesExtension;
#endif
#if YOO_SUPPORT
using RicKit.UI.Extensions.YooExtension;
#endif

namespace RicKit.UI
{
    public interface IUIManager
    {
        AbstractUIPanel CurrentAbstractUIPanel { get; }
        RectTransform RectTransform { get; }
        Camera UICamera { get; }
        UISettings Config { get; }
        void ShowUI<T>(Action<T> onInit = null) where T : AbstractUIPanel;
        void HideThenShowUI<T>(Action<T> onInit = null) where T : AbstractUIPanel;
        void CloseThenShowUI<T>(Action<T> onInit = null, bool destroy = false) where T : AbstractUIPanel;
        void ShowUIUnmanagable<T>(Action<T> onInit = null) where T : AbstractUIPanel;
        void Back(bool destroy = false);
        void CloseCurrent(bool destroy = false);
        void HideCurrent();
        void CloseUntil<T>(bool destroy = false) where T : AbstractUIPanel;
        void BackThenShow<T>(Action<T> onInit = null, bool destroy = false) where T : AbstractUIPanel;
        UniTask ShowUIAsync<T>(Action<T> onInit = null) where T : AbstractUIPanel;
        UniTask HideThenShowUIAsync<T>(Action<T> onInit = null) where T : AbstractUIPanel;
        UniTask CloseThenShowUIAsync<T>(Action<T> onInit = null, bool destroy = false) where T : AbstractUIPanel;
        UniTask ShowUIUnmanagableAsync<T>(Action<T> onInit = null) where T : AbstractUIPanel;
        UniTask BackAsync(bool destroy = false);
        UniTask CloseCurrentAsync(bool destroy = false);
        UniTask HideCurrentAsync();
        UniTask CloseUntilAsync<T>(bool destroy = false) where T : AbstractUIPanel;
        UniTask BackThenShowAsync<T>(Action<T> onInit, bool destroy = false) where T : AbstractUIPanel;
        void ClearAll();
        T GetUI<T>() where T : AbstractUIPanel;
        Canvas GetCustomLayer(string name, int sortOrder, string layerName = "UI");
        void SetCustomLayerSortOrder(string name, int sortOrder);
    }
    public class UIManager : MonoBehaviour, IUIManager
    {
        private static UIManager instance;
        public static UIManager I
        {
            get
            {
                if (!instance)
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
        public AbstractUIPanel CurrentAbstractUIPanel { get; private set; }
        public RectTransform RectTransform { get; private set; }
        public Camera UICamera {get; private set;}
        public UISettings Config { get; private set; }

        #region Events

        public static Action<AbstractUIPanel> OnShow { get; set; }
        public static Action<AbstractUIPanel> OnHide { get; set; }
        public static Action<AbstractUIPanel> OnShowEnd { get; set; }
        public static Action<AbstractUIPanel> OnHideEnd { get; set; }

        #endregion

        /// <summary>
        /// 需要在任何UI操作之前调用
        /// </summary>
        public static void Init()
        {
            new GameObject("UIManager", typeof(UIManager)).TryGetComponent(out instance);
            instance.CreateUIManager();
        }

        private void CreateUIManager()
        {
            var config = Config = Resources.Load<UISettings>("UISettings");
            switch (config.loadType)
            {
                default:
                    Debug.LogError($"LoadType {config.loadType} not found");
                    throw new ArgumentOutOfRangeException();
                case LoadType.Resources:
                    panelLoader = new DefaultPanelLoader();
                    Debug.Log($"UIManager use Resources, assetPathPrefix: {config.assetPathPrefix}");
                    break;
#if YOO_SUPPORT
                case LoadType.Yoo:
                    panelLoader = new YooAssetLoader(config.packageName, config.yooSyncLoad);
                    Debug.Log(
                        $"UIManager use YooAsset, assetPathPrefix: {config.assetPathPrefix}, packageName: {config.packageName}");
                    break;
#endif
#if ADDRESSABLES_SUPPORT
                case LoadType.Addressables:
                    panelLoader = new AddressablesLoader();
                    Debug.Log($"UIManager use Addressables, assetPathPrefix: {config.assetPathPrefix}");
                    break;
#endif
            }

            new GameObject("UICam", typeof(Camera)).TryGetComponent(out Camera cam);
            UICamera = cam;
            Transform transform1;
            (transform1 = UICamera.transform).SetParent(transform);
            transform1.localPosition = new Vector3(0, 0, -10);
            UICamera.clearFlags = config.cameraClearFlags;
            UICamera.cullingMask = config.cullingMask;
            UICamera.orthographic = true;
            UICamera.orthographicSize = 5;
            UICamera.nearClipPlane = config.nearClipPlane;
            UICamera.farClipPlane = config.farClipPlane;
            UICamera.depth = 0;

            new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
                .TryGetComponent(out canvas);
            canvas.transform.SetParent(transform);
            canvas.TryGetComponent(out RectTransform rect);
            RectTransform = rect;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = UICamera;
            canvas.planeDistance = 5;
            canvas.sortingLayerName = config.sortingLayerName;
            canvas.sortingOrder = 0;
            canvas.TryGetComponent<CanvasScaler>(out var canvasScaler);
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = config.referenceResolution;
            canvasScaler.screenMatchMode = config.screenMatchMode;
            canvasScaler.matchWidthOrHeight = config.matchWidthOrHeight;

            new GameObject("Blocker", typeof(CanvasGroup), typeof(CanvasRenderer), typeof(Canvas),
                typeof(Image), typeof(GraphicRaycaster)).TryGetComponent(out blockerCg);
            blockerCg.blocksRaycasts = false;
            blockerCg.TryGetComponent<Canvas>(out var blockerCanvas);
            blockerCg.transform.SetParent(canvas.transform, false);
            blockerCanvas.overrideSorting = true;
            blockerCanvas.sortingLayerName = config.sortingLayerName;
            blockerCanvas.sortingOrder = 1000;
            blockerCg.TryGetComponent<Image>(out var blockerImg);
            blockerImg.color = Color.clear;
            blockerCg.TryGetComponent<RectTransform>(out var blockerRt);
            blockerRt.anchorMin = Vector2.zero;
            blockerRt.anchorMax = Vector2.one;
            blockerRt.offsetMin = Vector2.zero;
            blockerRt.offsetMax = Vector2.zero;

            new GameObject("DefaultRoot", typeof(RectTransform)).TryGetComponent(out defaultRoot);
            defaultRoot.SetParent(canvas.transform, false);
            defaultRoot.anchorMin = Vector2.zero;
            defaultRoot.anchorMax = Vector2.one;
            defaultRoot.offsetMin = Vector2.zero;
            defaultRoot.offsetMax = Vector2.zero;
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && CurrentAbstractUIPanel && CurrentAbstractUIPanel.CanInteract)
                CurrentAbstractUIPanel.OnESCClick();
        }

        protected void Awake()
        {
            DontDestroyOnLoad(gameObject);
            var eventSystem = FindObjectOfType<EventSystem>();
            eventSystem = eventSystem
                ? eventSystem
                : new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule))
                    .GetComponent<EventSystem>();
            DontDestroyOnLoad(eventSystem);
        }

        #region 同步（只是同步调用，并不保证任务同帧开始或者完成）

        public void ShowUI<T>(Action<T> onInit = null) where T : AbstractUIPanel
        {
            ShowUIAsync(onInit).Forget();
        }

        public void HideThenShowUI<T>(Action<T> onInit = null) where T : AbstractUIPanel
        {
            HideThenShowUIAsync(onInit).Forget();
        }

        public void CloseThenShowUI<T>(Action<T> onInit = null, bool destroy = false) where T : AbstractUIPanel
        {
            CloseThenShowUIAsync(onInit, destroy).Forget();
        }

        public void ShowUIUnmanagable<T>(Action<T> onInit = null) where T : AbstractUIPanel
        {
            ShowUIUnmanagableAsync(onInit).Forget();
        }

        public void Back(bool destroy = false)
        {
            BackAsync(destroy).Forget();
        }

        /// <summary>
        /// Close 会出栈，且可销毁
        /// </summary>
        /// <param name="destroy">是否在退出动画后销毁</param>
        public void CloseCurrent(bool destroy = false)
        {
            CloseCurrentAsync(destroy).Forget();
        }
        /// <summary>
        /// Hide 不会出栈，且不可销毁
        /// </summary>
        public void HideCurrent()
        {
            HideCurrentAsync().Forget();
        }

        public void CloseUntil<T>(bool destroy = false) where T : AbstractUIPanel
        {
            CloseUntilAsync<T>(destroy).Forget();
        }

        public void BackThenShow<T>(Action<T> onInit = null, bool destroy = false) where T : AbstractUIPanel
        {
            BackThenShowAsync(onInit, destroy).Forget();
        }
        
        #endregion


        #region 异步

        public async UniTask ShowUIAsync<T>(Action<T> onInit = null) where T : AbstractUIPanel
        {
            var sortOrder = showFormStack.Count == 0 ? 1 : showFormStack.Peek().SortOrder + 5;
            var form = GetUI<T>();
            if (!form)
                form = await NewUI<T>();
            form.gameObject.SetActive(false);
            onInit?.Invoke(form);
            form.SetSortOrder(sortOrder);
            showFormStack.Push(form);
            if (!uiFormsList.Contains(form))
                uiFormsList.Add(form);
            CurrentAbstractUIPanel = form;
            form.BeforeShow();
            await form.OnShowAsync();
        }


        public async UniTask HideThenShowUIAsync<T>(Action<T> onInit = null) where T : AbstractUIPanel
        {
            await HideCurrentAsync();
            await ShowUIAsync(onInit);
        }

        public async UniTask CloseThenShowUIAsync<T>(Action<T> onInit = null, bool destroy = false) where T : AbstractUIPanel
        {
            await CloseCurrentAsync(destroy);
            await ShowUIAsync(onInit);
        }

        public async UniTask ShowUIUnmanagableAsync<T>(Action<T> onInit = null) where T : AbstractUIPanel
        {
            const int sortOrder = 900;
            var form = GetUI<T>();
            if (!form)
                form = await NewUI<T>();
            form.gameObject.SetActive(false);
            onInit?.Invoke(form);
            form.SetSortOrder(sortOrder);
            if (!uiFormsList.Contains(form)) uiFormsList.Add(form);
            form.BeforeShow();
            await form.OnShowAsync();
        }

        public async UniTask BackAsync(bool destroy = false)
        {
            await CloseCurrentAsync(destroy);
            if (showFormStack.Count == 0) return;
            var form = showFormStack.Peek();
            CurrentAbstractUIPanel = form;
            if (!form.IsShow)
            {
                await form.OnShowAsync();
            }
        }


        public async UniTask CloseCurrentAsync(bool destroy = false)
        {
            if (showFormStack.Count == 0) return;
            var form = showFormStack.Pop();
            CurrentAbstractUIPanel = showFormStack.Count == 0 ? null : showFormStack.Peek();
            await form.OnHideAsync();
            if (destroy)
            {
                uiFormsList.Remove(form);
                Destroy(form.gameObject);
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
                    CurrentAbstractUIPanel = form;
                    return;
                }

                CloseCurrentAsync(destroy).Forget();
            }
        }

        public async UniTask BackThenShowAsync<T>(Action<T> onInit, bool destroy = false) where T : AbstractUIPanel
        {
            await BackAsync(destroy);
            await ShowUIAsync(onInit);
        }

        #endregion


        #region LockInput

        private int lockCount;

        public static void LockInput(bool on)
        {
            if (!instance) return;
            instance.lockCount += on ? 1 : -1;
            instance.lockCount = instance.lockCount < 0 ? 0 : instance.lockCount;
#if UNITY_EDITOR
            instance.blockerCg.name = $"Blocker {instance.lockCount}";
#endif
            instance.blockerCg.blocksRaycasts = instance.lockCount > 0;
        }

        #endregion

        #region CustomLayer

        private readonly Dictionary<string, Canvas> customLayerDict = new Dictionary<string, Canvas>();

        public Canvas GetCustomLayer(string name, int sortOrder, string layerName = "UI")
        {
            if (customLayerDict.TryGetValue(name, out var canvasNew) && canvasNew)
            {
                canvasNew.gameObject.SetActive(true);
                canvasNew.gameObject.layer = LayerMask.NameToLayer(layerName);
                if (canvasNew.TryGetComponent(out Canvas c))
                {
                    c.overrideSorting = true;
                    c.sortingLayerName = Config.sortingLayerName;
                    c.sortingOrder = sortOrder;
                }

                return canvasNew;
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster))
                {
                    layer = LayerMask.NameToLayer(layerName)
                };
            go.transform.SetParent(RectTransform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.TryGetComponent(out RectTransform rect);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.TryGetComponent(out canvasNew);
            canvasNew.overrideSorting = true;
            canvasNew.sortingLayerName = Config.sortingLayerName;
            canvasNew.sortingOrder = sortOrder;
            customLayerDict.Add(name, canvasNew);
            return canvasNew;
        }

        public void SetCustomLayerSortOrder(string name, int sortOrder)
        {
            if (customLayerDict.TryGetValue(name, out var canvasNew) && canvasNew)
            {
                canvasNew.sortingOrder = sortOrder;
            }
        }

        #endregion

        public T GetUI<T>() where T : AbstractUIPanel
        {
            return uiFormsList.Where(form => form is T).Cast<T>().FirstOrDefault();
        }

        private async UniTask<T> NewUI<T>() where T : AbstractUIPanel
        {
            LockInput(true);
            var prefab = await panelLoader.LoadPrefab($"{Config.assetPathPrefix}{typeof(T).Name}");
            LockInput(false);
            var go = Instantiate(prefab, defaultRoot);
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
            foreach (var uIForm in uiFormsList.Where(uIForm => !uIForm.DontDestroyOnClear))
                DestroyImmediate(uIForm.gameObject);
            uiFormsList.Clear();
            showFormStack.Clear();
            StopAllCoroutines();
            CurrentAbstractUIPanel = null;
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