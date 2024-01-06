using RicKit.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RicKit.UITest.Editor
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