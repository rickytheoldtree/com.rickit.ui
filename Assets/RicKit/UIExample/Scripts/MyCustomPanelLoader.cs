using RicKit.UI;
using UnityEngine;

namespace RicKit.UITest.Editor
{
    public class MyCustomPanelLoader : IPanelLoader
    {
        private const string PrefabPath = "UIPanels/";

        public GameObject LoadPrefab(string name)
        {
            return Resources.Load<GameObject>(PrefabPath + name);
        }
    }
}

