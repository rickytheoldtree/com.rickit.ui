# RicKit UI
[![openupm](https://img.shields.io/npm/v/com.rickit.ui?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.rickit.ui/)
- 异步的UI管理插件
- ## 示例
![gif](https://github.com/rickytheoldtree/com.rickit.rui/blob/main/Gif/0.gif)
- 该示例项目可以在 Package Manager 中 RicKit UI 下的 Samples 中找到
- [WebGL DEMO](https://rickytheoldtree.github.io/com.rickit.ui/)
## 安装方式
- 通过PackageManager安装
    - 打开管理界面 Edit/Project Settings/Package Manager
    - 添加scope(国际, 实时更新)
      - Name: package.openupm.com
      - URL: https://package.openupm.com
      - Scope(s): `com.rickit.ui`, `com.cysharp.unitask`(依赖)

    - 在 Window/PackageManger 左上角选择 `My Registries` 刷新列表后, 找到`RicKit UI`下载
## 简介
- 用栈维护界面顺序
- 支持入场动画出场动画，动画时默认无法输入
- 对`Esc`返回默认支持，动画期间默认忽视Esc点击
- 使用前在 Resources/UISettings 下设置所有参数，包括`CurvingMasks`, `SortingLayerName`, 依赖分辨率等关键设置
- 通过`Rickit => UI => Create UISettings` 来创建 `UISettings`
- `UIManager`必须手动Init，会在第一次调用时创建包括`UICam`, `Blocker`等重要组成部分
- 所有自己实现的UIPanel需要继承`AbstractUIPanel`，继承了`AbstractUIPanel`的窗口预制体可以在 RicKit => UI => 界面编辑器 中创建/打开 然后编辑
## 资源加载自定义
- 初始化时可选传入自定义`IPanelLoader`, 默认为Resource的同步加载

```csharp
    //Resources
    public class DefaultPanelLoader : IPanelLoader
    {
        //同步
        public UniTask<GameObject> LoadPrefab(string path)
        {
            return UniTask.FromResult(Resources.Load<GameObject>(path));
        }

        //异步
        /*public async UniTask<GameObject> LoadPrefab(string path)
        {
            return await Resources.LoadAsync<GameObject>(path) as GameObject;
        }*/
    }

    //Addressables
    public class AddressablesPanelLoader : IPanelLoader
    {
        //同步
        public UniTask<GameObject> LoadPrefab(string path)
        {
            return UniTask.FromResult(Addressables.LoadAssetAsync<GameObject>(path).WaitForCompletion());
        }

        //异步
        /*public async UniTask<GameObject> LoadPrefab(string path)
        {
            return await Addressables.LoadAssetAsync<GameObject>(path);
        }*/
    }
```

## Universal RP 支持
- 在需要叠加`UI相机`的`游戏相机`上添加`UniversalRenderPipelineCamStackUICam`脚本
- 如果只使用UI相机，则不需要做额外操作

## 交流与反馈
- Q群: 851024152
- 欢迎问题反馈, 以及任何建议
