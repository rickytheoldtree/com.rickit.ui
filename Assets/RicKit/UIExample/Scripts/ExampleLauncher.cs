using RicKit.UI;
using RicKit.UITest.Editor;
using UnityEngine;

namespace RicKit.UIExample
{
    public class ExampleLauncher : MonoBehaviour
    {
        private void Start()
        {
            UIManager.I.ShowUI<UIOne>();
        }
    }
}

