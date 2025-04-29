using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RicKit.UI.Interfaces
{
    public interface IPanelLoader
    {
        UniTask<GameObject> LoadPrefabAsync(string path);
        
        GameObject LoadPrefab(string path);
    }
}