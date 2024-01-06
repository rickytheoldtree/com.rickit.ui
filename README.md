# RicKit UI
- 异步的UI管理插件

## 简介
- 支持入场动画出场动画，动画时默认无法输入
- 对Esc返回默认支持
- 支持自定义Panel加载，默认从Resources/UIPanels下加载；自定义时自己实现一个IPanelLoader并在Resources/UIManagerConifg中设置
- 使用前在Resources/UIManagerConfig下设置所有参数，包括CurvingMasks, SortingLayerName, 依赖分辨率等关键设置
- UIManager为懒加载，会在第一次调用时创建，包括UICam，Blocker等重要组成部分
- 所有实现的ui需要继承AbstractUIPanel，继承了AbstractUIPanel的窗口预制体可以在RicKit => UI => 界面编辑器 中创建/打开 然后编辑
## 示例
![gif](https://github.com/rickytheoldtree/com.rickit.rui/blob/main/Gif/0.gif)
- 该示例项目可以在release中的.unitypackage中找到
## RUI安装方式
- 可以通过 UPM url导入 https://github.com/rickytheoldtree/com.rickit.rui.git#rui
- 或从Release处直接下载 .unitypackage
## 反馈与交流
- Q群: 85024152
- 有效反馈会记录到Contributor
