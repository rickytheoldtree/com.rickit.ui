using RicKit.UI;
using RicKit.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace RicKit.UIExample.Scripts
{
    public class UIOne : FadeUIPanel
    {
        [SerializeField] private Button btn1, btn2;
        protected override void Awake()
        {
            base.Awake();
            btn1.onClick.AddListener(OnBtn1Click);
            btn2.onClick.AddListener(OnBtn2Click);
        }

        private void OnBtn2Click()
        {
            UI.HideThenShowUI<UITwo>();
        }

        private void OnBtn1Click()
        {
            UI.ShowUI<UITwo>();
        }

        public override void OnESCClick()
        {
            
        }
    }
}