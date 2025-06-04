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

// 2. Addressables loading
public class AddressablesPanelLoader : IPanelLoader
{
    public GameObject LoadPrefab(string path)
        => Addressables.LoadAssetAsync<GameObject>(path).WaitForCompletion();

    public async UniTask<GameObject> LoadPrefabAsync(string path)
        => await Addressables.LoadAssetAsync<GameObject>(path);
}

// Usage
UIManager.Init(new MyPanelLoader());
```

---

## Universal RP Support

- To overlay UI camera on a game camera, add the `UniversalRenderPipelineCamStackUICam` script.
- If only using a UI camera, no extra steps needed.

---

## Feedback & Contact

- QQ Group: 851024152
- Issues and suggestions are welcome!

---
