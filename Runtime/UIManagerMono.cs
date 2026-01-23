using UnityEngine;

namespace RicKit.UI
{
    public class UIManagerMono : MonoBehaviour
    {
        private UIManager ui;
        public void SetUIManager(UIManager ui)
        {
            this.ui = ui;
        }
        private void Update()
        {
            ui.Update();
        }
    }
}