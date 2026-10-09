# RicKit UI

[![openupm](https://img.shields.io/npm/v/com.rickit.ui?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.rickit.ui/)

**[English Documentation](./README.md) | 中文文档**

RicKit UI 是一个异步的 Unity UI 管理插件，支持栈式管理、动画以及自定义资源加载。  
适用于需要高效、灵活 UI 控制的 Unity 项目。

---

## 示例

![演示动图](https://github.com/rickytheoldtree/com.rickit.rui/blob/main/Gif/0.gif)

- 示例项目可在 Package Manager 中 RicKit UI 的 Samples 里找到
- [WebGL DEMO 在线体验](https://rickytheoldtree.github.io/com.rickit.ui/)

---

## 安装方式

1. **通过 Package Manager 安装**
    - 打开 `Edit > Project Settings > Package Manager`
    - 添加自定义 Registry（国际、实时更新）：
        - Name: package.openupm.com
        - URL: https://package.openupm.com
        - Scope(s): `com.rickit.ui`, `com.cysharp.unitask`
    - 在 `Window > Package Manager` 左上角选择 `My Registries`，刷新列表后找到并下载 RicKit UI

---

## 使用简介

- **栈式管理界面顺序**，支持出场/入场动画，动画过程中自动屏蔽输入
- 默认支持 Esc 返回，动画期间忽略 Esc
- 使用前请在 `Resources/UISettings` 下设置所有参数（如 相机、缩放、分辨率和资源路径前缀等）
- 通过菜单 `RicKit > UI > Open Settings` 创建 UISettings
- 首次使用需手动调用 `UIManager.Init()`，会自动创建 `UICam`、`Blocker` 等核心组件
- 自定义 UIPanel 请继承 `AbstractUIPanel`，创建的窗口预制体可在界面编辑器中编辑

---

## 主要 API（常用接口）

RicKit UI 通过 `IUIManager` 统一管理 UI，支持同步和异步写法，主要常用接口如下：

- **初始化与设置**
    - `UIManager.Init()` / `UIManager.Init(panelLoader)`
- **显示与切换界面**
    - `ShowUI<T>()` / `ShowUIAsync<T>()`
    - `HideThenShowUI<T>()` / `HideThenShowUIAsync<T>()`
    - `CloseThenShowUI<T>()` / `CloseThenShowUIAsync<T>()`
    - `ShowUIUnmanagable<T>()`
    - `ShowThenClosePrev<T>()` / `ShowThenClosePrevAsync<T>()`
- **返回与栈操作**
    - `Back()` / `BackAsync()`
    - `CloseCurrent()` / `CloseCurrentAsync()`
    - `HideCurrent()` / `HideCurrentAsync()`
    - `CloseUntil<T>()` / `CloseUntilAsync<T>()`
    - `BackThenShow<T>()` / `BackThenShowAsync<T>()`
    - `Close<T>` / `CloseAsync<T>()`
- **预加载与等待**
    - `PreloadUI<T>()` / `PreloadUIAsync<T>()`
    - `WaitUntilUIHideEnd<T>()`
- **其它辅助**
    - `GetUI<T>()`
    - `ClearAll()`
    - `SetLockInput(bool)` / `IsLockInput()`
    - 事件委托，如 `OnShow`, `OnHide` 等

> **泛型 T 必须继承自 `AbstractUIPanel`**  
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

// 使用方式
UIManager.Init(new MyPanelLoader());
```

---

## 导航与生命周期约定

- 管理器操作需在 Unity 主线程调用。导航、预加载和非托管显示使用同一个队列；组合切换作为一次操作执行。返回 `void` 的 API 入队后立即返回。
- 按请求的泛型类型缓存一个实例；预制体上的面板可以是该类型的派生类。重复显示会执行 `onInit`、将已有实例移到栈顶，不会重复入栈；已经显示的面板不会重播入场动画和显示事件。
- 新实例在 `onInit` 完成之前保持未激活。`Awake` 可能在首次激活时才执行，因此 `onInit` 所需字段应通过字段初始化或序列化数据准备，不要依赖 `Awake`。
- `CloseUntilAsync` 会等待所有退出动画结束。动画或回调失败时会释放输入锁并保留可恢复的导航状态；替换操作在新面板成功显示后才移除旧面板。
- 面板的直接显示/隐藏转换以最新调用为准。取消回调、`OnEnable` 和 `OnDisable` 中的重入不会让旧转换覆盖新状态；激活回调内再次转换会延后至下一帧。转换失败时恢复最后一次完成的显示状态和透明度，并取消尚未结束的同组动画。
- 输入锁同时使用射线遮罩和根 CanvasGroup，阻止正常继承父组状态的 UI 控件接收鼠标/触摸与键盘/手柄提交。`ignoreParentGroups` 或自定义输入逻辑需自行遵守锁状态。手动 `SetLockInput` 与作用域锁独立计数，不能通过手动解锁提前释放框架锁；作用域应在主线程释放。
- 事件回调和动画覆写中不要等待另一个排队的管理器操作，否则新操作需要等待当前操作结束而形成相互等待。可用 `void` API 排队后续导航，或在当前操作完成后再等待下一步。
- `ClearAll()` 取消正在执行和排队的操作，再保留 `DontDestroyOnClear` 为 true 的面板。取消回调中新排队的导航在清理结束后执行。旧加载器无法取消时，迟到的加载结果会被释放，不会重新创建界面。
- 使用 `UIManager.Shutdown()` 或 `((UIManager)UIManager.I).Dispose()` 取消任务并销毁管理器拥有的对象，之后可重新初始化。已有场景 EventSystem 仍归场景管理；使用新 Input System 的项目应预先配置 EventSystem。自动 Esc 返回使用旧 Input Manager。
- 可以通过 `new UIManager().Initiate(settings, loader)` 显式注入配置；传入 `null` 使用默认 Resources 加载器。配置验证成功后才发布单例。
- 关闭后默认保留面板缓存。可勾选面板的 **Destroy On Close**，或覆写 `DestroyOnClose`，让低频面板在关闭后销毁；`destroy: true` 始终请求销毁。`DontDestroyOnClear` 不阻止管理器关闭时的销毁。

`Fade` 和 `Scale` 默认使用不受 `timeScale` 影响的时间；传入 `ignoreTimeScale: false` 可跟随游戏时间。取消会抛出 `OperationCanceledException`，不会直接跳到动画终值。自定义面板动画应遵守取消令牌，可覆写 `RestoreAnimationState(bool shown)` 在失败或取消后恢复额外动画目标。内置弹窗会自动恢复缩放与遮罩透明度。动画时长必须是有限值；零或负时长立即完成。即使自定义动画未响应取消，框架也会结束等待并释放自身的输入锁，但自定义动画仍需检查令牌，避免继续写入已失效的目标。取消回调抛错会记录异常，其他清理和排队任务的取消仍会继续。

### 加载器资源所有权

原有 `IPanelLoader` 实现继续兼容。可选接口 `ICancellablePanelLoader` 接收取消令牌，加载器应自行清理失败或取消的加载；`IReleasablePanelLoader.ReleasePrefab(path, prefab)` 则在成功加载对应的实例真正销毁后调用一次，也覆盖未使用的预加载、无效预制体和清理后的迟到结果。资源随实例 GameObject 跟踪，单独移除面板组件后，下次缓存访问或清理仍会销毁其对象并释放资源。只隐藏缓存面板不会释放预制体；释放回调前需保证预制体及其依赖仍有效，不要在实例化后立即释放 Addressables 句柄。

默认加载器已改用 `Resources.LoadAsync`。上面的 Addressables 示例为每次成功加载保留一个句柄，需要引入 `System`、`System.Collections.Generic`、`UnityEngine.AddressableAssets`、`UnityEngine.ResourceManagement.AsyncOperations`，并在等待句柄时引用 UniTask 的 Addressables 集成。不支持同步 Addressables 加载的平台应使用异步接口。

### 测试

`Assets/RicKit/UI/Tests/Runtime` 提供 PlayMode 回归测试，覆盖重复与并发请求、加载/动画/事件失败、激活及取消回调重入、输入锁隔离与键盘提交、派生类缓存、组件单独销毁、预加载释放、弹窗恢复、暂停动画、非法配置、关闭重建，以及相机和 EventSystem 所有权。安装 Unity Test Framework 后可在 Test Runner 中运行。

---
## Universal RP 支持

- 在需要渲染 UI 的相机上添加 `UIAdditionalCamera`。启用时使用该相机，禁用时恢复之前注册的相机或默认 UI 相机。
- URP 相机堆叠、渲染类型与剔除层需在项目中配置；插件不修改 URP 相机堆叠。

---

## 交流与反馈

- QQ 群：851024152
- 欢迎提出问题反馈与建议！

---
