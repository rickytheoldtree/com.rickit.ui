# RicKit UI
依赖UniTask的UI管理插件
## 简介
支持入场动画出场动画，动画时默认无法输入

支持自定义Panel加载，默认从Resources/UIPanels下加载；自定义时自己实现一个IPanelLoader并在Conifg中选择

使用前在Resources/UIManagerConfig下设置所有参数

UIManager为懒加载，会在第一次调用时创建，包括UICam，Blocker等重要组成部分

UIPanel缓存为dictionary，显示顺序用stack实现

所有自定义的面包需要继承AbstractUIPanel，继承了AbstractUIPanel的面板可以在Rickit => UI => 界面编辑器 中创建/打开
## 安装方式
1.导入前通过UPM(Package Manager) 使用url导入UniTask https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask, 或直接在 https://github.com/Cysharp/UniTask/releases 下载.unitypackage. 

2.通过UPM url  https://github.com/rickytheoldtree/com.rickit.rui.git#RUI 导入该插件，或从Release处下载.
