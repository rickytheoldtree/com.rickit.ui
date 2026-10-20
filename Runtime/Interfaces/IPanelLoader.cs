using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RicKit.UI.Interfaces
{
    public interface IPanelLoader
    {
        UniTask<GameObject> LoadPrefabAsync(string path);
        
        GameObject LoadPrefab(string path);
    }

    /// <summary>
    /// 需要释放资源的加载器（Addressables、自建 AssetBundle 引用计数等）实现这个接口。
    /// UIManager 每成功加载一次就对应调用一次 ReleasePrefab，时机是该次加载出的面板实例被销毁之后
    /// （ClearAll、destroy 关闭、SafeDestroy、外部直接 Destroy 都算）；加载出来但实例化失败也会立刻释放。
    /// 面板运行时从自己预制体里实例化、又挂到面板外面的物体，要在面板销毁前一起处理掉，否则资源被卸载后会丢贴图
    /// </summary>
    public interface IReleasablePanelLoader : IPanelLoader
    {
        void ReleasePrefab(string path, GameObject prefab);
    }
}