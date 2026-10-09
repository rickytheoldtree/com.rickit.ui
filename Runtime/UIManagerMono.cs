using UnityEngine;

namespace RicKit.UI
{
    public class UIManagerMono : MonoBehaviour
    {
        private UIManager ui;
        public void SetUIManager(UIManager manager) => ui = manager;
        private void Update() => ui?.Update();
        private void OnDestroy() => ui?.Dispose();
    }
}
