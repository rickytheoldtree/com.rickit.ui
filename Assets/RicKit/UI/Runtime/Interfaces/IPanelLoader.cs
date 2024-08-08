using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RicKit.UI.Interfaces
{
    public interface IPanelLoader
    {
        UniTask<GameObject> LoadPrefab(string path);
    }
}