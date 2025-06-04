# RicKit RDebug

[![openupm](https://img.shields.io/npm/v/com.rickit.rdebug?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.rickit.rdebug/)

> 🌐 [English Documentation](./README.md)

---

## 简介

RicKit RDebug 是一个基于 Unity 的调试面板工具，可以快速创建自定义运行时调试 UI。通过继承抽象类 `RDebug`，你可以轻松添加按钮、输入框等控件，实现运行时调试和参数调整。

---

## 主要特性

- 一键生成调试面板
- 支持常用控件如按钮、输入框
- 灵活的布局选项（垂直/水平）
- 可自定义按钮与输入框的样式（颜色、字体等）
- 适用于 Unity MonoBehaviour 工作流

---

## 快速开始

1. 新建一个类继承 `RDebug`，实现 `OnShow()` 方法。你也可以重写属性进行自定义。

```csharp
using RicKit.RDebug;
using UnityEngine;

public class MyDebugPanel : RDebug
{
    protected override void Awake()
    {
        // 可在Awake中自定义样式
        TextColor = Color.yellow;
        BgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        // BgSprite = ... // 可自定义背景图片
        base.Awake();
    }

    protected override void OnShow()
    {
        UsingHorizontalLayoutGroup(() =>
        {
            CreateButton("customBtn", "我的按钮", () => Debug.Log("按钮被点击！"));
            CreateInputField("customInput", "输入框", value => Debug.Log($"输入: {value}"));
        });
    }
}
```

---

## API 参考

### 继承点

- `protected abstract void OnShow()`
  - 实现此方法以定义你的调试面板内容。

### 常用方法

- `protected Button CreateButton(string key, string name, UnityAction onClick, int width = 100, int height = 100, int fontSize = 30)`
  - 添加按钮。
  - `key`：按钮唯一标识
  - `name`：显示文本
  - `onClick`：点击回调

- `protected InputField CreateInputField(string key, string name, UnityAction<string> onValueChanged, int width = 100, int height = 100, int fontSize = 30, string defaultValue = "")`
  - 添加输入框。
  - `key`：唯一标识
  - `name`：标签文本
  - `onValueChanged`：内容变化回调

- `protected GameObject CreateLabel(string key, string name, int width = 100, int height = 100, int fontSize = 30)`
  - 添加标签（仅显示文本）。

- `protected void UsingHorizontalLayoutGroup(Action action, int height = 100)`
  - 以横向方式组织一组控件。

- `public void OnHide()`
  - 手动隐藏调试面板并清除控件。

### 字段与属性

- `protected Dictionary<string, GameObject> Components { get; }`
  - 存储所有已创建 UI 元素（按钮、输入框、标签等）及其对应 key，方便后续访问和管理。

### 样式自定义

- `protected Color TextColor { get; set; }`
- `protected Color BgColor { get; set; }`
- `protected Sprite BgSprite { get; set; }`

---

## 注意事项

- 需在 Unity 工程中使用
- 需将自定义调试类挂载到场景中的 GameObject 上
- 样式与布局均可自定义

---

## 开源协议

Apache License 2.0

---

## 相关链接

- [GitHub 仓库](https://github.com/rickytheoldtree/com.rickit.rdebug)
- [OpenUPM 页面](https://openupm.com/packages/com.rickit.rdebug/)

---

## 更新日志

请参阅 [`Assets/RicKit/RDebug/CHANGELOG.md`](Assets/RicKit/RDebug/CHANGELOG.md) 获取最新变更信息。

近期更新（v1.1.0）：
- 重构 `RDebug` 类以更高效地管理 UI 组件
- API 变动：  
  - 所有控件创建方法（如 `CreateButton`、`CreateInputField` 等）现在第一个参数为唯一 `key`
  - 新增 `CreateLabel` 用于只读文本
  - 优化面板清理和布局组管理
  - 新增 `Components` 字典，便于管理与访问所有创建的 UI 元素
