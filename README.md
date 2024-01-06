# RicKit UI
- 异步的UI管理插件

## 简介
- 支持入场动画出场动画，动画时默认无法输入
- 对`Esc`返回默认支持
- 支持自定义Panel加载，默认从`Resources/UIPanels`下加载；自定义时自己实现一个`IPanelLoader`并在`Resources/UIManagerConifg`中设置
- 使用前在`Resources/UIManagerConfig`下设置所有参数，包括`CurvingMasks`, `SortingLayerName`, 依赖分辨率等关键设置
- `UIManager`为懒加载，会在第一次调用时创建，包括`UICam`，`Blocker`等重要组成部分
- 所有实现的ui需要继承`AbstractUIPanel`，继承了`AbstractUIPanel`的窗口预制体可以在RicKit => UI => 界面编辑器 中创建/打开 然后编辑
## 示例
![gif](https://github.com/rickytheoldtree/com.rickit.rui/blob/main/Gif/0.gif)
- 该示例项目可以在release中的.unitypackage中找到
## RUI安装方式
- 可以通过 UPM url导入 https://github.com/rickytheoldtree/com.rickit.rui.git#rui
- 或从Release处直接下载 .unitypackage
## 反馈与交流
- Q群: 85024152
- 有效反馈会记录到Contributor
