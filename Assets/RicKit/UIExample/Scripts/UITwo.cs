using RicKit.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RicKit.UIExample.Scripts
{
    public class UITwo : PopUIPanel
    {
        [SerializeField] Button btnThree;
        protected override void Awake()
        {
            base.Awake();
            btnThree.onClick.AddListener(OnThreeClick);
        }

        private void OnThreeClick()
        {
            UI.CloseThenShowUI<UIThree>();
        }
    }
}