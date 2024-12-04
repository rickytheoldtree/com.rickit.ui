using System.Collections.Generic;
using UnityEngine;

namespace RicKit.UI.SubUI
{
    public class SubUICtrl : MonoBehaviour
    {
        [SerializeField] private AbstractSubPanel[] subPanels;

        private readonly Dictionary<string, AbstractSubPanel> subPanelPrefabDic =
            new Dictionary<string, AbstractSubPanel>();

        private readonly Dictionary<string, AbstractSubPanel> subPanelDic = new Dictionary<string, AbstractSubPanel>();

        private void Awake()
        {
            foreach (var subPanel in subPanels)
            {
                RegisterSubPanel(subPanel);
            }
        }

        private void RegisterSubPanel<T>(T subPabel) where T : AbstractSubPanel
        {
            subPanelPrefabDic.Add(subPabel.GetType().Name, subPabel);
        }

        public void Show<T>()
        {
            if (!subPanelPrefabDic.TryGetValue(typeof(T).Name, out var prefab)) return;
            if (!subPanelDic.TryGetValue(typeof(T).Name, out var p))
            {
                p = Instantiate(prefab, transform);
                subPanelDic[typeof(T).Name] = p;
            }
            p.SubUICtrl = this;
            p.OnShow();
        }

        public void Hide<T>()
        {
            if (!subPanelDic.TryGetValue(typeof(T).Name, out var p)) return;
            subPanelDic.Remove(typeof(T).Name);
            p.OnHide(() =>
            {
                p.SubUICtrl = null;
                Destroy(p.gameObject);
            });
        }
    }
}