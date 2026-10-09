using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RicKit.UI.Interfaces
{
    public interface IPanelLoader
    {
        UniTask<GameObject> LoadPrefabAsync(string path);
        GameObject LoadPrefab(string path);
    }

    /// <summary>Optional cancellation. Implementations must clean up their own cancelled loads.</summary>
    public interface ICancellablePanelLoader : IPanelLoader
    {
        UniTask<GameObject> LoadPrefabAsync(string path, CancellationToken cancellationToken);
    }

    /// <summary>Called once per successful load, after its instance has actually been destroyed.</summary>
    public interface IReleasablePanelLoader : IPanelLoader
    {
        void ReleasePrefab(string path, GameObject prefab);
    }
}
