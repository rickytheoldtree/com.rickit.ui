# RicKit UI
依赖UniTask的UI管理插件
## 简介
支持入场动画出场动画，动画时默认无法输入

支持自定义Panel加载，默认从Resources/UIPanels下加载；自定义时自己实现一个IPanelLoader并在Resources/UIManagerConifg中设置

使用前在Resources/UIManagerConfig下设置所有参数，包括CurvingMasks, SortingLayerName, 依赖分辨率等关键设置

UIManager为懒加载，会在第一次调用时创建，包括UICam，Blocker等重要组成部分

所有实现的ui需要继承AbstractUIPanel，继承了AbstractUIPanel的窗口预制体可以在RicKit => UI => 界面编辑器 中创建/打开 然后编辑
## UniTask安装方式
1. 可以通过UPM(Package Manager) 使用url导入UniTask https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask,
2. 或直接在 https://github.com/Cysharp/UniTask/releases 下载.unitypackage.
3. 或直接在该项目根目录下载.unitypackage.
## RUI安装方式
1. 确保UniTask已经导入
2. 可以通过 UPM url导入 https://github.com/rickytheoldtree/com.rickit.rui.git#rui
3. 或从Release处直接下载 .unitypackage
