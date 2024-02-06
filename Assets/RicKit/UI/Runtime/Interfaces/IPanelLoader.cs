using System.Threading.Tasks;
using UnityEngine;

namespace RicKit.UI.Interfaces
{
    public interface IPanelLoader
    {
        Task<GameObject> LoadPrefab(string path);
    }
}