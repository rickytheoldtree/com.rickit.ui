#if ADDRESSABLES_SUPPORT
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using RicKit.UI.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RicKit.UI.Extensions.AddressablesExtension
{
    public class AddressablesLoader : IPanelLoader
    {
        public UniTask<GameObject> LoadPrefab(string path)
        {
            return Addressables.LoadAssetAsync<GameObject>(path).Task.AsUniTask();
        }
    }
}
#endif