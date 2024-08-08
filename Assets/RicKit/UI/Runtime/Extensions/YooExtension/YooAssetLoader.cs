#if YOO_SUPPORT
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using RicKit.UI.Interfaces;
using UnityEngine;
using YooAsset;

namespace RicKit.UI.Extensions.YooExtension
{
    public class YooAssetLoader : IPanelLoader
    {
        private readonly string packageName;
        private readonly bool sync;
        public YooAssetLoader(string packageName, bool sync)
        {
            this.packageName = packageName;
            this.sync = sync;
        }

        private ResourcePackage Package => package ??= YooAssets.GetPackage(packageName);
        private ResourcePackage package;

        public async UniTask<GameObject> LoadPrefab(string path)
        {
            if (sync)
            {
                return Package.LoadAssetSync<GameObject>(path).AssetObject as GameObject;
            }
            var handle = Package.LoadAssetAsync<GameObject>(path);
            await handle.Task;
            return handle.AssetObject as GameObject;
        }
    }
    
}
#endif