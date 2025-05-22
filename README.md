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

## 主要 API（IUIManager）

`IUIManager` 是 UI 管理的核心接口，负责 UI 栈管理、界面切换、资源加载等。主要接口说明如下：

```csharp
public interface IUIManager
{
    // 组件和配置
    UIManagerMono Mono { get; }                  // Mono 管理器实例
    AbstractUIPanel CurrentAbstractUIPanel { get; } // 当前活跃的 UI 面板
    RectTransform CanvasRectTransform { get; }   // UI 根节点 RectTransform
    Camera UICamera { get; }                     // UI 相机
    UISettings Settings { get; }                 // UI 配置

    // 事件
    Action<AbstractUIPanel> OnShow { get; set; }     // 面板显示回调
    Action<AbstractUIPanel> OnHide { get; set; }     // 面板隐藏回调
    Action<AbstractUIPanel> OnShowEnd { get; set; }  // 动画结束后回调
    Action<AbstractUIPanel> OnHideEnd { get; set; }  // 动画结束后回调

    // 初始化
    void Initiate();                          // 用默认 Loader 初始化
    void Initiate(IPanelLoader panelLoader);  // 用自定义 Loader 初始化
    bool PanelAsyncLoading { get; set; }      // 是否异步加载面板

    // 界面切换（同步）
    void ShowUI<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5) where T : AbstractUIPanel;
    // 显示 UI，自动叠加到栈顶

    void HideThenShowUI<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5) where T : AbstractUIPanel;
    // 隐藏当前 UI 后显示新 UI

    void CloseThenShowUI<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI", int orderInLayerDelta = 5) where T : AbstractUIPanel;
    // 关闭（可销毁）当前 UI 后显示新 UI

    // 其它界面操作
    void Back(bool destroy = false);         // 返回（关闭栈顶 UI）
    void CloseCurrent(bool destroy = false); // 关闭当前 UI
    void HideCurrent();                      // 隐藏当前 UI
    void CloseUntil<T>(bool destroy = false) where T : AbstractUIPanel;
    // 关闭直到某个类型的 UI 为止

    // 获取面板实例
    T GetUI<T>() where T : AbstractUIPanel;  // 获取某类型的 UI 实例
    void ClearAll(); // 清理所有 UI

    // 异步版本（支持 UniTask，推荐异步管理复杂切换）
    UniTask ShowUIAsync<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5) where T : AbstractUIPanel;
    UniTask HideThenShowUIAsync<T>(Action<T> onInit = null, string layer = "UI", int orderInLayerDelta = 5) where T : AbstractUIPanel;
    UniTask CloseThenShowUIAsync<T>(Action<T> onInit = null, bool destroy = false, string layer = "UI", int orderInLayerDelta = 5) where T : AbstractUIPanel;
    UniTask BackAsync(bool destroy = false);
    UniTask CloseCurrentAsync(bool destroy = false);
    UniTask HideCurrentAsync();
    UniTask CloseUntilAsync<T>(bool destroy = false) where T : AbstractUIPanel;
    UniTask BackThenShowAsync<T>(Action<T> onInit, bool destroy = false, string layer = "UI", int orderInLayerDelta = 5) where T : AbstractUIPanel;
    UniTask WaitUntilUIHideEnd<T>() where T : AbstractUIPanel; // 等待 UI 隐藏结束
    UniTask PreloadUIAsync<T>(string layer = "UI") where T : AbstractUIPanel; // 预加载 UI
}
```

### 常用接口功能简述

- `ShowUI<T>()` / `ShowUIAsync<T>()`  
  显示类型为 T 的 UI 面板，自动叠加到 UI 栈顶。

- `HideThenShowUI<T>()` / `HideThenShowUIAsync<T>()`  
  先隐藏当前 UI，再显示目标 UI。

- `CloseThenShowUI<T>()` / `CloseThenShowUIAsync<T>()`  
  先关闭（可选择销毁）当前 UI，再显示目标 UI。

- `Back()` / `BackAsync()`  
  返回上一个 UI（即关闭当前栈顶 UI）。

- `CloseCurrent()` / `CloseCurrentAsync()`  
  关闭当前 UI（可选择销毁）。

- `HideCurrent()` / `HideCurrentAsync()`  
  隐藏当前 UI（但不出栈）。

- `CloseUntil<T>()` / `CloseUntilAsync<T>()`  
  持续关闭 UI 直到遇到某个类型的 UI。

- `PreloadUIAsync<T>()`  
  异步预加载某类型的 UI 面板资源。

- `WaitUntilUIHideEnd<T>()`  
  等待某类型 UI 完全隐藏后继续后续逻辑。

---

## 资源加载自定义

可通过实现 `IPanelLoader` 接口自定义资源加载方式，支持同步和异步、Resource/Addressables 等：

```csharp
// Resources 示例
public class DefaultPanelLoader : IPanelLoader
{
    public UniTask<GameObject> LoadPrefab(string path)
    {
        return UniTask.FromResult(Resources.Load<GameObject>(path));
    }
}

// Addressables 示例
public class AddressablesPanelLoader : IPanelLoader
{
    public UniTask<GameObject> LoadPrefab(string path)
    {
        return UniTask.FromResult(Addressables.LoadAssetAsync<GameObject>(path).WaitForCompletion());
    }
}
```

---

## Universal RP 支持

- 在需要叠加 UI 相机的游戏相机上添加 `UniversalRenderPipelineCamStackUICam` 脚本
- 仅使用 UI 相机则无需额外操作

---

## 交流与反馈

- QQ 群：851024152
- 欢迎提出问题反馈与建议

---

## License

MIT
