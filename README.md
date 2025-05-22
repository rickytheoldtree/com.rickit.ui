# RicKit UI

[![openupm](https://img.shields.io/npm/v/com.rickit.ui?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.rickit.ui/)

异步的 Unity UI 管理插件，支持栈式管理、动画、以及自定义资源加载。  
本插件适用于需要高效、灵活 UI 控制的 Unity 项目。

---

## 示例

![演示动图](https://github.com/rickytheoldtree/com.rickit.rui/blob/main/Gif/0.gif)

- 示例项目可在 Package Manager 中 RicKit UI 下的 Samples 中找到
- [WebGL DEMO 在线体验](https://rickytheoldtree.github.io/com.rickit.ui/)

---

## 安装方式

1. **通过 Package Manager 安装**
    - 打开 `Edit > Project Settings > Package Manager`
    - 添加自定义 Registry（国际、实时更新）
        - Name: package.openupm.com
        - URL: https://package.openupm.com
        - Scope(s): `com.rickit.ui`, `com.cysharp.unitask`
    - 在 `Window > Package Manager` 左上角选择 `My Registries`，刷新列表后找到 `RicKit UI` 下载

---

## 使用简介

- **栈式管理界面顺序**，支持出场/入场动画，动画过程中自动屏蔽输入
- 默认支持 Esc 返回，动画期间忽略 Esc
- 使用前请在 `Resources/UISettings` 下设置所有参数（如 CurvingMasks、SortingLayerName、分辨率等）
- 通过 `Rickit => UI => Create UISettings` 菜单创建 UISettings
- 首次使用需手动调用 `UIManager.Init()`，会自动创建 `UICam`、`Blocker` 等核心组件
- 自定义 UIPanel 请继承 `AbstractUIPanel`，创建的窗口预制体可在界面编辑器中编辑

---

## 主要 API（常用接口）

RicKit UI 通过 `IUIManager` 统一管理 UI，支持同步和异步写法，主要常用接口如下：

- **初始化与设置**
    - `UIManager.Init()` / `UIManager.Init(panelLoader)`  
      初始化 UI 管理器，可指定自定义面板加载器。
    - `PanelAsyncLoading`  
      是否异步加载面板。

- **显示与切换界面**
    - `ShowUI<T>()` / `ShowUIAsync<T>()`  
      显示指定类型的 UI，自动入栈。可传初始化回调。
    - `HideThenShowUI<T>()` / `HideThenShowUIAsync<T>()`  
      隐藏当前 UI 后显示目标 UI。
    - `CloseThenShowUI<T>()` / `CloseThenShowUIAsync<T>()`  
      关闭（可选择销毁）当前 UI 后显示目标 UI。
    - `ShowUIUnmanagable<T>()`  
      显示不受栈管理的 UI（如弹窗）。
    - `ShowThenClosePrev<T>()` / `ShowThenClosePrevAsync<T>()`  
      显示新 UI 并关闭前一个 UI。

- **返回与栈操作**
    - `Back()` / `BackAsync()`  
      返回上一个 UI（关闭栈顶）。
    - `CloseCurrent()` / `CloseCurrentAsync()`  
      关闭当前 UI（可销毁）。
    - `HideCurrent()` / `HideCurrentAsync()`  
      隐藏当前 UI（不出栈）。
    - `CloseUntil<T>()` / `CloseUntilAsync<T>()`  
      关闭直到某类型的 UI。
    - `BackThenShow<T>()` / `BackThenShowAsync<T>()`  
      返回后立即显示目标 UI。

- **预加载与等待**
    - `PreloadUI<T>()` / `PreloadUIAsync<T>()`  
      预加载指定 UI 资源。
    - `WaitUntilUIHideEnd<T>()`  
      等待指定类型 UI 隐藏完成。

- **其它辅助**
    - `GetUI<T>()`  
      获取某类型 UI 实例。
    - `ClearAll()`  
      清除所有 UI。
    - `SetLockInput(bool)` / `IsLockInput()`  
      输入锁定与查询。
    - 事件委托（如 `OnShow`, `OnHide` 等）

> **泛型 T** 均需继承自 `AbstractUIPanel`。  
> 推荐异步管理复杂切换，所有异步接口基于 UniTask。

---

## 资源加载自定义（IPanelLoader 示例）

支持自定义 UI 资源加载方式，只需实现 `IPanelLoader` 接口并在初始化时传入：

```csharp
// 1. Resources 加载同步/异步
public class MyPanelLoader : IPanelLoader
{
    // 同步加载
    public GameObject LoadPrefab(string path)
        => Resources.Load<GameObject>(path);

    // 异步加载
    public async UniTask<GameObject> LoadPrefabAsync(string path)
    {
        var req = Resources.LoadAsync<GameObject>(path);
        await UniTask.WaitUntil(() => req.isDone);
        return req.asset as GameObject;
    }
}

// 2. Addressables 加载
public class AddressablesPanelLoader : IPanelLoader
{
    public GameObject LoadPrefab(string path)
        => Addressables.LoadAssetAsync<GameObject>(path).WaitForCompletion();

    public async UniTask<GameObject> LoadPrefabAsync(string path)
        => await Addressables.LoadAssetAsync<GameObject>(path);
}

// 使用
UIManager.Init(new MyPanelLoader());
```

---

## Universal RP 支持

- 在需要叠加 UI 相机的游戏相机上添加 `UniversalRenderPipelineCamStackUICam` 脚本
- 仅使用 UI 相机则无需额外操作

---

## 交流与反馈

- QQ 群：851024152
- 欢迎提出问题反馈与建议
