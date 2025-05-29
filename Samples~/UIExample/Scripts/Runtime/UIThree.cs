using RicKit.UI;
using RicKit.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace RicKit.UIExample.Scripts
{
    public class UIThree : PopUIPanel
    {
        [SerializeField] Button btnTwo;
        protected override void Awake()
        {
            base.Awake();
            btnTwo.onClick.AddListener(OnTwoClick);
        }

        private void OnTwoClick()
        {
            UI.ShowThenClosePrev<UITwo>();
        }
    }
}

