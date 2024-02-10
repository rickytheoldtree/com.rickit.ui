using System.Threading.Tasks;
using RicKit.UI.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;

#if ADDRESSABLES_SUPPORT
namespace RicKit.UI.Extensions.AddressablesExtension
{
    public class AddressablesLoader : IPanelLoader
    {
        public Task<GameObject> LoadPrefab(string path)
        {
            return Addressables.LoadAssetAsync<GameObject>(path).Task;
        }
    }
}
#endif