# RicKit UI
[![openupm](https://img.shields.io/npm/v/com.rickit.ui?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.rickit.ui/)
- 异步的UI管理插件
- ## 示例
![gif](https://github.com/rickytheoldtree/com.rickit.rui/blob/main/Gif/0.gif)
- 该示例项目可以在Release中的.unitypackage中找到
## 安装方式
- 通过PackageManager安装
    - 打开管理界面 Edit/Project Settings/Package Manager
    - 添加scope(国际, 实时更新)
      - Name: package.openupm.com
      - URL: https://package.openupm.com
      - Scope(s): com.rickit.ui
    - 添加scope(CN, 更新滞后)
      - Name: package.openupm.cn
      - URL: https://package.openupm.cn
      - Scope(s): com.rickit.ui

    - 在 Window/PackageManger 左上角选择 `My Registries` 刷新列表后, 找到`RicKit UI`下载
## 简介
- 支持入场动画出场动画，动画时默认无法输入
- 对`Esc`返回默认支持
- 支持自定义Panel加载，默认从 Resources/UIPanels 下加载; 自定义时自己实现一个`IPanelLoader`并在 Resources/UIManagerConifg 中设置
- 使用前在 Resources/UIManagerConfig 下设置所有参数，包括`CurvingMasks`, `SortingLayerName`, 依赖分辨率等关键设置
- `UIManager`为懒加载，会在第一次调用时创建，包括`UICam`, `Blocker`等重要组成部分
- 所有自己实现的UIPanel需要继承`AbstractUIPanel`，继承了`AbstractUIPanel`的窗口预制体可以在 RicKit => UI => 界面编辑器 中创建/打开 然后编辑
- `IPanelLoader`的`Task<GameObject> LoadPrefab()` 允许异步实现, 如需要同步实现则直接返回`Task.FromResult(GameObject obj)`即可(不用添加async关键字)
## TO-DO
- 解决`Task.Yield()`性能问题
- 支持更多Ease
- 解决创建`IPanelLoader`时遍历全程序集进行反射的问题
- 更多的Editor脚本?
## 交流与反馈
- Q群: 851024152
- 欢迎问题反馈, 以及任何建议
