using RicKit.UI.Panels;
using UnityEngine;
using UnityEngine.Rendering;

namespace RicKit.UI.Component
{
    /// <summary>
    /// 让面板里的粒子、SortingGroup、子 Canvas 自动跟随面板的层级：sortingOrder = 面板 OrderInLayer + offset，
    /// sortingLayer 与面板一致。挂在 SortingGroup / Canvas / Renderer（含 ParticleSystemRenderer）所在的物体上，
    /// 优先级 SortingGroup > Canvas > Renderer
    /// </summary>
    [DisallowMultipleComponent]
    public class UISortingFollower : MonoBehaviour
    {
        [SerializeField]
        private int offset = 1;

        public int Offset
        {
            get => offset;
            set
            {
                offset = value;
                Refresh();
            }
        }

        private void OnEnable()
        {
            // 运行时实例化到面板下的物体，以及在未激活时被设置过的子 Canvas，都在这里补一次
            Refresh();
        }

        public void Refresh()
        {
            var panel = FindOwnerPanel();
            if (panel) Apply(panel);
        }

        internal void Follow(AbstractUIPanel panel)
        {
            // 嵌套面板时只跟最近的那个
            if (FindOwnerPanel() != panel) return;
            Apply(panel);
        }

        private void Apply(AbstractUIPanel panel)
        {
            var order = panel.OrderInLayer + offset;
            var layer = panel.SortingLayerName;
            if (TryGetComponent(out SortingGroup sortingGroup))
            {
                sortingGroup.sortingLayerName = layer;
                sortingGroup.sortingOrder = order;
            }
            else if (TryGetComponent(out Canvas canvas))
            {
                canvas.overrideSorting = true;
                canvas.sortingLayerName = layer;
                canvas.sortingOrder = order;
            }
            else if (TryGetComponent(out Renderer rendererComponent))
            {
                rendererComponent.sortingLayerName = layer;
                rendererComponent.sortingOrder = order;
            }
        }

        private AbstractUIPanel FindOwnerPanel()
        {
            for (var t = transform; t; t = t.parent)
            {
                if (t.TryGetComponent(out AbstractUIPanel panel)) return panel;
            }
            return null;
        }
    }
}
