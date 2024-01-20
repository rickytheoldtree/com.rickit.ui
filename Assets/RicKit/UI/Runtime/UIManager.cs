using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RicKit.UI.Helpers;
using RicKit.UI.Panels;
using RicKit.UI.TaskExtension;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RicKit.UI
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance;

        public static UIManager I
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    return instance;
#endif
                if (!instance)
                    CreateInstance();
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
        public RectTransform RectTransform => rectTransform;
        private RectTransform rectTransform;
        public Camera UICamera => uiCamera;
        private Camera uiCamera;
        public UISettings Config { get; private set; }

        private static void CreateInstance()
        {
            new GameObject("UIManager", typeof(UIManager)).TryGetComponent(out instance);
            instance.Config = Resources.Load<UISettings>("UISettings");

            new GameObject("UICam", typeof(Camera)).TryGetComponent(out instance.uiCamera);
            instance.uiCamera.transform.SetParent(instance.transform);
            instance.uiCamera.transform.localPosition = new Vector3(0, 0, -10);
            instance.uiCamera.clearFlags = instance.Config.cameraClearFlags;
            instance.uiCamera.cullingMask = instance.Config.cullingMask;
            instance.uiCamera.orthographic = true;
            instance.uiCamera.orthographicSize = 5;
            instance.uiCamera.nearClipPlane = 1f;
            instance.uiCamera.farClipPlane = 10;
            instance.uiCamera.depth = 0;

            new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
                .TryGetComponent(out instance.canvas);
            instance.canvas.transform.SetParent(instance.transform);
            instance.canvas.TryGetComponent(out instance.rectTransform);
            instance.canvas.renderMode = RenderMode.ScreenSpaceCamera;
            instance.canvas.worldCamera = instance.uiCamera;
            instance.canvas.planeDistance = 5;
            instance.canvas.sortingLayerName = instance.Config.sortingLayerName;
            instance.canvas.sortingOrder = 0;
            instance.canvas.TryGetComponent<CanvasScaler>(out var canvasScaler);
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = instance.Config.referenceResolution;
            canvasScaler.screenMatchMode = instance.Config.screenMatchMode;

            new GameObject("Blocker", typeof(CanvasGroup), typeof(CanvasRenderer), typeof(Canvas),
                typeof(Image), typeof(GraphicRaycaster)).TryGetComponent(out instance.blockerCg);
            instance.blockerCg.TryGetComponent<Canvas>(out var blockerCanvas);
            instance.blockerCg.transform.SetParent(instance.canvas.transform, false);
            blockerCanvas.overrideSorting = true;
            blockerCanvas.sortingLayerName = instance.Config.sortingLayerName;
            blockerCanvas.sortingOrder = 1000;
            instance.blockerCg.TryGetComponent<Image>(out var blockerImg);
            blockerImg.color = Color.clear;
            instance.blockerCg.TryGetComponent<RectTransform>(out var blockerRt);
            blockerRt.anchorMin = Vector2.zero;
            blockerRt.anchorMax = Vector2.one;
            blockerRt.offsetMin = Vector2.zero;
            blockerRt.offsetMax = Vector2.zero;

            new GameObject("DefaultRoot", typeof(RectTransform)).TryGetComponent(out instance.defaultRoot);
            instance.defaultRoot.SetParent(instance.canvas.transform, false);
            instance.defaultRoot.anchorMin = Vector2.zero;
            instance.defaultRoot.anchorMax = Vector2.one;
            instance.defaultRoot.offsetMin = Vector2.zero;
            instance.defaultRoot.offsetMax = Vector2.zero;

            instance.panelLoader = Activator.CreateInstance(ReflectionHelper.GetType(instance.Config.panelLoaderType)
                                                            ?? typeof(DefaultPanelLoader)) as IPanelLoader;
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

        #region 同步

        public void ShowUI<T>(Action<T> onInit = null) where T : AbstractUIPanel
        {
            ShowUIAsync(onInit).WrapErrors();
        }

        public void HideThenShowUI<T>(Action<T> onInit = null) where T : AbstractUIPanel
        {
            HideThenShowUIAsync(onInit).WrapErrors();
        }

        public void CloseThenShowUI<T>(Action<T> onInit = null) where T : AbstractUIPanel
        {
            CloseThenShowUIAsync(onInit).WrapErrors();
        }

        public void ShowUIUnmanagable<T>(Action<T> onInit = null) where T : AbstractUIPanel
        {
            ShowUIUnmanagableAsync(onInit).WrapErrors();
        }

        public void Back()
        {
            BackAsync().WrapErrors();
        }

        public void CloseCurrent()
        {
            CloseCurrentAsync().WrapErrors();
        }

        public void HideCurrent()
        {
            HideCurrentAsync().WrapErrors();
        }

        public void HideUntil<T>() where T : AbstractUIPanel
        {
            HideUntilAsync<T>().WrapErrors();
        }

        public void BackThenShow<T>(Action<T> onInit = null) where T : AbstractUIPanel
        {
            BackThenShowAsync(onInit).WrapErrors();
        }

        #endregion


        #region 异步

        public async Task ShowUIAsync<T>(Action<T> onInit = null) where T : AbstractUIPanel
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


        public async Task HideThenShowUIAsync<T>(Action<T> onInit = null) where T : AbstractUIPanel
        {
            await HideCurrentAsync();
            await ShowUIAsync(onInit);
        }

        public async Task CloseThenShowUIAsync<T>(Action<T> onInit = null) where T : AbstractUIPanel
        {
            await CloseCurrentAsync();
            await ShowUIAsync(onInit);
        }

        public async Task ShowUIUnmanagableAsync<T>(Action<T> onInit = null) where T : AbstractUIPanel
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

        public async Task BackAsync()
        {
            await CloseCurrentAsync();
            if (showFormStack.Count == 0) return;
            var form = showFormStack.Peek();
            CurrentAbstractUIPanel = form;
            if (!form.IsShow)
            {
                await form.OnShowAsync();
            }
        }


        public async Task CloseCurrentAsync()
        {
            if (showFormStack.Count == 0) return;
            var form = showFormStack.Pop();
            CurrentAbstractUIPanel = null;
            await form.OnHideAsync();
        }


        public async Task HideCurrentAsync()
        {
            if (showFormStack.Count == 0) return;
            var form = showFormStack.Peek();
            await form.OnHideAsync();
        }

        public async Task HideUntilAsync<T>() where T : AbstractUIPanel
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

                CloseCurrentAsync().WrapErrors();
            }
        }

        public async Task BackThenShowAsync<T>(Action<T> onInit) where T : AbstractUIPanel
        {
            await BackAsync();
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
        public Canvas GetCustomLayer(string name, int sortOrder)
        {
            if (customLayerDict.TryGetValue(name, out var canvasNew) && canvasNew)
            {
                canvasNew.gameObject.SetActive(true);
                if (canvasNew.TryGetComponent(out Canvas c))
                {
                    c.overrideSorting = true;
                    c.sortingLayerName = Config.sortingLayerName;
                    c.sortingOrder = sortOrder;
                }
                return canvasNew;
            }
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.transform.SetParent(rectTransform);
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

        private async Task<T> NewUI<T>() where T : AbstractUIPanel
        {
            LockInput(true);
            var prefab = await panelLoader.LoadPrefab(typeof(T).Name);
            LockInput(false);
            var go = Instantiate(prefab, defaultRoot);
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

    public interface IPanelLoader
    {
        Task<GameObject> LoadPrefab(string name);
    }

    public class DefaultPanelLoader : IPanelLoader
    {
        private const string PrefabPath = "UIPanels/";

        public Task<GameObject> LoadPrefab(string name)
        {
            return Task.FromResult(Resources.Load<GameObject>(PrefabPath + name));
        }
    }
}