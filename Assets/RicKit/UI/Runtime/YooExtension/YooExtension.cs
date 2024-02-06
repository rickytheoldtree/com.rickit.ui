#if YOO_SUPPORT
using System.Threading.Tasks;
using RicKit.UI.Interfaces;
using UnityEngine;
using YooAsset;

namespace RicKit.UI.YooExtension
{
    public class YooAssetLoader : IPanelLoader
    {
        private readonly string packageName;

        public YooAssetLoader(string packageName)
        {
            this.packageName = packageName;
        }

        private ResourcePackage Package => package ??= YooAssets.GetPackage(packageName);
        private ResourcePackage package;

        public async Task<GameObject> LoadPrefab(string path)
        {
            var handle = Package.LoadAssetAsync<GameObject>(path);
            await handle.Task;
            return handle.AssetObject as GameObject;
        }
    }
    
}
#endif