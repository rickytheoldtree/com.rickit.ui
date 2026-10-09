# RicKit UI

[![openupm](https://img.shields.io/npm/v/com.rickit.ui?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.rickit.ui/)

**English | [中文文档 (Chinese)](./README.zh-CN.md)**

RicKit UI is an asynchronous Unity UI management plugin supporting stack-based management, animation, and custom resource loading.  
It is ideal for Unity projects that require efficient and flexible UI control.

---

## Demo

![Demo GIF](https://github.com/rickytheoldtree/com.rickit.rui/blob/main/Gif/0.gif)

- Sample projects can be found under `Samples` in RicKit UI in the Package Manager.
- [WebGL Online Demo](https://rickytheoldtree.github.io/com.rickit.ui/)

---

## Installation

1. **Via Package Manager**
    - Open `Edit > Project Settings > Package Manager`
    - Add a custom Registry (international, real-time update):
        - Name: package.openupm.com
        - URL: https://package.openupm.com
        - Scope(s): `com.rickit.ui`, `com.cysharp.unitask`
    - In `Window > Package Manager`, select `My Registries` in the upper left and refresh. Download RicKit UI.

---

## Quick Start

- **Stack-based UI management**: Supports enter/exit animations and auto input blocking during transitions.
- Esc key returns by default, Esc is ignored during animations.
- Before use, set all parameters under `Resources/UISettings` (e.g., camera, scaling, resolution, and asset path prefix).
- Create UISettings via the menu: `RicKit > UI > Open Settings`.
- First use: manually call `UIManager.Init()` to automatically create core components like `UICam`, `Blocker`.
- To create a custom UIPanel, inherit from `AbstractUIPanel`. The created window prefab can be edited in the inspector.

---

## Main APIs

RicKit UI manages UI through `IUIManager`, supports both synchronous and asynchronous usage. Main APIs:

- **Initialization & Settings**
    - `UIManager.Init()` / `UIManager.Init(panelLoader)`
- **Show & Switch Panels**
    - `ShowUI<T>()` / `ShowUIAsync<T>()`
    - `HideThenShowUI<T>()` / `HideThenShowUIAsync<T>()`
    - `CloseThenShowUI<T>()` / `CloseThenShowUIAsync<T>()`
    - `ShowUIUnmanagable<T>()`
    - `ShowThenClosePrev<T>()` / `ShowThenClosePrevAsync<T>()`
- **Back & Stack Operations**
    - `Back()` / `BackAsync()`
    - `CloseCurrent()` / `CloseCurrentAsync()`
    - `HideCurrent()` / `HideCurrentAsync()`
    - `CloseUntil<T>()` / `CloseUntilAsync<T>()`
    - `BackThenShow<T>()` / `BackThenShowAsync<T>()`
    - `Close<T>` / `CloseAsync<T>()`
- **Preload & Await**
    - `PreloadUI<T>()` / `PreloadUIAsync<T>()`
    - `WaitUntilUIHideEnd<T>()`
- **Other Helpers**
    - `GetUI<T>()`
    - `ClearAll()`
    - `SetLockInput(bool)` / `IsLockInput()`
    - Event delegates: `OnShow`, `OnHide`, etc.

> **Note:** Generic `T` must inherit from `AbstractUIPanel`.  
> Asynchronous management is recommended for complex transitions, all async APIs rely on UniTask.

---

## Custom Resource Loading (IPanelLoader Example)

Supports custom UI resource loading by implementing `IPanelLoader` and passing it during initialization:

```csharp
// 1. Resources loading (sync/async)
public class MyPanelLoader : IPanelLoader
{
    // Synchronous
    public GameObject LoadPrefab(string path)
        => Resources.Load<GameObject>(path);

    // Asynchronous
    public async UniTask<GameObject> LoadPrefabAsync(string path)
    {
        var req = Resources.LoadAsync<GameObject>(path);
        await UniTask.WaitUntil(() => req.isDone);
        return req.asset as GameObject;
    }
}

// 2. Addressables loading with explicit ownership (requires the Addressables package)
public class AddressablesPanelLoader : IReleasablePanelLoader
{
    private readonly Dictionary<GameObject, Stack<AsyncOperationHandle<GameObject>>> handles
        = new Dictionary<GameObject, Stack<AsyncOperationHandle<GameObject>>>();

    public GameObject LoadPrefab(string path)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(path);
        try
        {
            handle.WaitForCompletion();
            return Retain(handle);
        }
        catch
        {
            if (handle.IsValid()) Addressables.Release(handle);
            throw;
        }
    }

    public async UniTask<GameObject> LoadPrefabAsync(string path)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(path);
        try
        {
            await handle;
            return Retain(handle);
        }
        catch
        {
            if (handle.IsValid()) Addressables.Release(handle);
            throw;
        }
    }

    private GameObject Retain(AsyncOperationHandle<GameObject> handle)
    {
        if (handle.Status != AsyncOperationStatus.Succeeded || !handle.Result)
            throw handle.OperationException ?? new InvalidOperationException("UI asset load failed.");
        var prefab = handle.Result;
        if (!handles.TryGetValue(prefab, out var stack))
            handles.Add(prefab, stack = new Stack<AsyncOperationHandle<GameObject>>());
        stack.Push(handle);
        return prefab;
    }

    public void ReleasePrefab(string path, GameObject prefab)
    {
        if (!handles.TryGetValue(prefab, out var stack)) return;
        Addressables.Release(stack.Pop());
        if (stack.Count == 0) handles.Remove(prefab);
    }
}

// Usage
UIManager.Init(new MyPanelLoader());
```

---

## Navigation and lifecycle guarantees

- Call manager operations on Unity's main thread. Navigation, preloading, and unmanaged shows share one queue; composite switches execute as one operation. Void APIs enqueue work and return immediately.
- Each requested generic panel type has one cached instance; its prefab may contain a derived panel type. Showing an existing panel refreshes `onInit` and moves it to the top without adding another entry. An already shown panel does not replay its entrance animation or show events.
- New instances remain inactive until `onInit` has run. `Awake` may run on first activation, so initialize fields required by `onInit` in field initializers or serialized data rather than depending on `Awake`.
- `CloseUntilAsync` waits for every exit animation. Failed animations and callbacks release input locks and preserve recoverable navigation state. Replacement operations keep the previous panel until the new panel succeeds.
- Direct panel transitions give ownership to the latest call. Reentry from cancellation callbacks, `OnEnable`, and `OnDisable` cannot let an older transition overwrite newer state; transitions inside activation callbacks resume on the next frame. Failed transitions restore the last committed visibility and alpha, and cancel unfinished sibling animations.
- Input locks use a raycast blocker and a root CanvasGroup to block pointer input and keyboard/controller submission on UI controls that inherit parent group state. Controls using `ignoreParentGroups` or custom input must honor the lock themselves. Manual `SetLockInput` calls and scoped locks have independent counters; manual unlocks cannot release framework scopes. Dispose scopes on the main thread.
- Callbacks and animation overrides must not await another queued manager operation from inside the current operation: the new operation waits for the current one. Enqueue follow-up navigation with a void API, or await it after the current operation completes.
- `ClearAll()` cancels active and queued work, then preserves panels whose `DontDestroyOnClear` is true. Navigation enqueued by cancellation callbacks runs after clearing completes. A non-cancellable loader's late result is released rather than instantiated.
- `UIManager.Shutdown()` or `((UIManager)UIManager.I).Dispose()` cancels pending work and destroys owned objects. Initialization can then run again. Existing scene EventSystems remain owned by the scene; configure one for projects using the new Input System. Automatic Esc handling uses the legacy Input Manager.
- To inject settings, use `new UIManager().Initiate(settings, loader)`; pass `null` for the default Resources loader. Configuration is validated before the singleton is published.
- Closed panels remain cached by default. Enable **Destroy On Close** on a panel or override `DestroyOnClose` to release rarely used panels. `destroy: true` always requests destruction. `DontDestroyOnClear` does not exempt panels from shutdown.

`Fade` and `Scale` use unscaled time by default; pass `ignoreTimeScale: false` to follow game time. Cancellation throws `OperationCanceledException` without snapping to the final value. Custom panel animations must honor their cancellation token and can override `RestoreAnimationState(bool shown)` to restore additional targets after failure or cancellation. The supplied popup animation restores its scale and blocker automatically. Durations must be finite; zero or negative durations complete immediately. The framework stops waiting and releases its own input lock even if a custom animation ignores cancellation, but custom animations must still check the token before writing to their targets. Exceptions from cancellation callbacks are logged while cleanup and cancellation of queued requests continue.

### Loader ownership

Existing `IPanelLoader` implementations remain supported. Optional `ICancellablePanelLoader` implementations receive a cancellation token and must clean up failed/cancelled loads themselves. Optional `IReleasablePanelLoader.ReleasePrefab(path, prefab)` is called once for each successful load when its instance is actually destroyed, including unused preloads, invalid prefabs, and late results after clearing. Resources follow the instance GameObject. If only its panel component is removed, the next cache access or cleanup still destroys the object and releases its resources. Hiding a cached panel does not release its prefab. Keep the loaded prefab and its dependencies valid until that callback; do not release an Addressables handle immediately after instantiation.

The default loader now uses `Resources.LoadAsync`. Addressables adapters should retain one handle per successful load, as in the example above. It needs `System`, `System.Collections.Generic`, `UnityEngine.AddressableAssets`, and `UnityEngine.ResourceManagement.AsyncOperations` imports, plus UniTask's Addressables integration when awaiting handles. Avoid synchronous Addressables loading on platforms that do not support it.

### Tests

PlayMode regression tests are in `Assets/RicKit/UI/Tests/Runtime`. They cover duplicate and concurrent requests, failed loads/animations/events, activation/cancellation reentrancy, input lock isolation and keyboard submission, derived panel caches, component-only destruction, preloads and resource release, popup recovery, paused animations, invalid settings, shutdown, and camera/EventSystem ownership. Run them from Unity's Test Runner with the Unity Test Framework installed.

---
## Universal RP Support

- Add `UIAdditionalCamera` to a camera that should render UI. Enabling it selects that camera; disabling it restores the previous registered camera or the default UI camera.
- Configure URP camera stacking, render types, and culling masks in your project; the plugin does not modify the URP camera stack.

---

## Feedback & Contact

- QQ Group: 851024152
- Issues and suggestions are welcome!

---
