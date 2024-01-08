using System.Threading.Tasks;
using RicKit.UI;
using UnityEngine;

namespace RicKit.UIExample.Scripts
{
    public class MyCustomPanelLoader : IPanelLoader
    {
        private const string PrefabPath = "UIPanels/";

        public async Task<GameObject> LoadPrefab(string name)
        {
            // 模拟耗时加载 Prefab
            Debug.Log($"模拟耗时加载 {name} 中");
            await Task.Delay(1000);
            Debug.Log($"模拟耗时加载 {name} 完成");
            return Resources.Load<GameObject>(PrefabPath + name);
        }
    }
}

