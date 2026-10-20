# RicKit UI：给 AI 的约定

在用 RicKit UI 的项目里新建、修改 UI 面板前先读这份文档。改完面板一定要跑一次「验证」一节里的检查。

## 加载规则（最容易出错）

- 运行时按 `UISettings.assetPathPrefix + 类名` 加载预制体，实际怎么加载由项目的 `IPanelLoader` 决定（默认是 `Resources.Load`）
- **预制体文件名必须和类名完全一致**，面板组件必须挂在预制体**根节点**上，根节点要有 `RectTransform`
- **一个面板类一个 .cs 文件，文件名等于类名**，否则 Unity 挂不上这个组件
- 面板类名在整个项目里不能重复（不同命名空间也不行），因为只按类名加载
- 新建面板后要做项目自己的登记，比如加到 Addressables 并把地址设成 `前缀 + 类名`，或者更新项目里的路径映射。具体看项目的 CLAUDE.md / AGENTS.md 和 `IPanelLoader` 实现

## 基类选择

所有面板都继承 `RicKit.UI.Panels.AbstractUIPanel`，基类上的 `[RequireComponent]` 会自动加上 `Canvas`、`CanvasGroup`、`GraphicRaycaster`。

| 基类 | 必须实现 | 预制体必填字段 | 用途 |
|------|----------|----------------|------|
| `AbstractUIPanel` | `OnESCClick()`、`OnAnimationIn(CancellationToken)`、`OnAnimationOut(CancellationToken)`（返回 `UniTask`） | 无 | 自定义进出场动画 |
| `FadeUIPanel` | `OnESCClick()` | 无 | 全屏界面，淡入淡出 0.2s |
| `PopUIPanel` | 无 | `cgBlocker`（背景遮罩的 CanvasGroup）、`panel`（缩放的内容节点）；`moreBtnBacks` 里不能有空元素 | 弹窗，遮罩淡入 + 内容 OutBack 缩放 |

- `PopUIPanel` 的 `btnBack` 可以不填；点击 `btnBack` / `moreBtnBacks` 会走 `OnBackClick()`，默认是 `UI.Back()`。Esc 只在其中有按钮处于激活状态时才响应，所以不想让 Esc 关掉的弹窗就不要给返回按钮
- 项目里通常有自己的中间基类，先在项目里找现成的同类面板照着写
- 面板里用 `UI` 拿到 `IUIManager`
- 重写 `Awake` 时必须先调 `base.Awake()`
- `OnESCClick` 一般写 `UI.Back();`，不允许返回的面板留空

## 打开、关闭与生命周期

- 传参用 `onInit`：`await UI.ShowUIAsync<UIShop>(p => p.Init(data));`。`onInit` 在入栈之后、入场动画之前调用；新建的面板 `Awake` 早于 `onInit`
- `ShowUIAsync` 在入场动画播完、面板可以交互之后才返回；不带 Async 的同步版本是"发出去不等"
- 弹窗链：`await UI.ShowUIAndWaitHideAsync<UIReward>(p => p.Init(rewards));` 会等这个面板关掉再继续。也可以用 `ShowUIAsync` 加 `WaitUntilUIHideEnd<T>()`
- `Back` / `CloseCurrent` **先出栈再播退场动画**，所以 `OnHide`、`OnHideEnd` 里读到的 `CurrentUIPanel` 已经是下一层面板
- `Back` 关掉当前面板后，如果下一层是被 `HideCurrent` / `HideThenShowUI` 隐藏的，会重新显示它
- 对已经在显示的面板再调一次 `ShowUI`，会重新入栈并重播入场动画
- 默认关闭只是隐藏，面板实例会留着复用；传 `destroy: true` 或重写 `DestroyOnClose => true` 才销毁。下次打开前状态不会自动重置，需要在 `onInit` 里重置
- 动画期间输入是锁住的（计数锁）。自己调了 `SetLockInput(true)` 就必须成对调 `false`，更推荐 `using (UI.GetLockInputScope())`
- 面板里的粒子、`SortingGroup`、子 `Canvas` 挂 `UISortingFollower` 来跟随面板的层级，不要在动画里手动改 `sortingOrder`
- `UIManager.TryGetInstance(out var ui)` 在不确定是否已初始化时使用

## 验证

改完面板脚本或预制体后运行检查。项目在 Unity 里打开着的话用菜单 `RicKit/UI/Validate Panels`；在终端里跑（项目不能同时被 Unity 打开）：

```
Unity -batchmode -nographics -projectPath <项目路径> -executeMethod RicKit.UI.Editor.PanelValidator.RunBatch -rickitUIReport Logs/RicKitUI/panel-validation.txt -logFile -
```

- 退出码：0 没有错误，1 有错误，2 检查本身出错。脚本编译不过时 Unity 会直接退出，报 `Scripts have compiler errors`
- 报告每行一条：`[ERROR|WARN] 类名 (资源路径): 说明`。ERROR 必须修；WARN 是可疑的地方（比如字段没赋值），确认是有意为之可以不管
- 会检查：脚本文件名、预制体有没有、预制体名和类名是否一致、根节点、Missing Script、空引用和丢失的引用、运行时加载路径能不能解析到这个预制体（Resources、Addressables、项目注册的解析规则）

### 自定义加载器

项目的 `IPanelLoader` 不是 Resources 也不是 Addressables 地址等于加载路径的时候，要注册解析规则，否则每个面板都会报"找不到资源"。在项目的 Editor 程序集里：

```csharp
using RicKit.UI.Editor;
using UnityEditor;

internal static class PanelPathRules
{
    [InitializeOnLoadMethod]
    private static void Register()
    {
        // loadPath 是 assetPathPrefix + 类名，返回它在编辑器里对应的资源路径
        PanelValidator.RegisterResolver((string loadPath, out string assetPath) =>
        {
            assetPath = MyAssetLocations.UIPrefab(loadPath);
            return assetPath != null;
        });
    }
}
```

### 作为 EditMode 测试

```csharp
using System.Linq;
using NUnit.Framework;
using RicKit.UI.Editor;

public class UIPanelTests
{
    [Test]
    public void PanelsAreValid()
    {
        var errors = PanelValidator.Validate().Where(i => i.Severity == PanelIssueSeverity.Error).ToList();
        Assert.IsEmpty(errors, PanelValidator.FormatReport(errors));
    }
}
```

## 排查卡死

- 打开 `UISettings.verboseLog`，会打印每次导航调用、栈深、输入锁计数，以及同步接口吞掉的取消异常
- 常见原因：`OnAnimationIn` / `OnAnimationOut` 里 await 了一个永远不完成的任务；`SetLockInput(true)` 没有配对；在 `OnHide` 里按"当前面板还是自己"的假设写了逻辑
