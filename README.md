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
- Before use, set all parameters under `Resources/UISettings` (e.g., CurvingMasks, SortingLayerName, resolution, etc.).
- Create UISettings via the menu: `Rickit => UI => Create UISettings`.
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
    - `ShowUIAndWaitHideAsync<T>()`: show a panel and wait until it is closed, for chaining popups
- **Other Helpers**
    - `GetUI<T>()`
    - `ClearAll()`
    - `SetLockInput(bool)` / `IsLockInput()`
    - Event delegates: `OnShow`, `OnHide`, etc.
    - `UIManager.TryGetInstance(out ui)`: check for an instance without logging an error

> **Note:** Generic `T` must inherit from `AbstractUIPanel`.  
> Asynchronous management is recommended for complex transitions, all async APIs rely on UniTask.

### Panel Contract

- `Back` and `CloseCurrent` **pop the stack before** the exit animation, so `CurrentUIPanel` inside `OnHideEnd` is the panel revealed underneath
- Showing a panel that is already shown pushes it again and replays `OnAnimationIn`
- A new panel runs `Awake` before `onInit`, so initialization in `Awake` is safe
- `OnAnimationIn` / `OnAnimationOut` may await other navigation calls
- Override `DestroyOnClose => true` to always destroy a panel when it is closed (same as passing `destroy: true`), useful for large, rarely used panels
- Add `UISortingFollower` (with an `offset`) to particles, `SortingGroup`s or child `Canvas`es inside a panel to follow the panel's sorting order automatically
- Turn on `UISettings.verboseLog` to log navigation calls, stack depth and the input lock count when tracking down a hang

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

// 2. Addressables loading: implement IReleasablePanelLoader to give the reference back once the panel is destroyed
public class AddressablesPanelLoader : IReleasablePanelLoader
{
    public GameObject LoadPrefab(string path)
        => Addressables.LoadAssetAsync<GameObject>(path).WaitForCompletion();

    public async UniTask<GameObject> LoadPrefabAsync(string path)
        => await Addressables.LoadAssetAsync<GameObject>(path);

    public void ReleasePrefab(string path, GameObject prefab)
        => Addressables.Release(prefab);
}

// Usage
UIManager.Init(new MyPanelLoader());
```

### Releasing Resources (IReleasablePanelLoader)

- Every successful load gets exactly one `ReleasePrefab`, called **after** the panel instance from that load is destroyed: `ClearAll`, closing with `destroy`, `DestroyOnClose`, `SafeDestroy` and a plain external `Destroy` all count; hiding (`Back` without `destroy`) does not
- A loaded prefab whose root lacks the panel component is released as well
- **Caution:** objects a panel instantiates from its own prefab and parents outside the panel must be cleaned up before the panel is destroyed, otherwise they lose their textures once the bundle is unloaded

---

## Universal RP Support

- To render UI with a game camera, add the `UIAdditionalCamera` component to it (cameras enabled before `UIManager` is initialized are registered too).
- If only using a UI camera, no extra steps needed.

---

## Feedback & Contact

- QQ Group: 851024152
- Issues and suggestions are welcome!

---
