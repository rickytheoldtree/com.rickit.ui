using RicKit.UI;
using UnityEngine;

namespace RicKit.UIExample.Scripts
{
    public class ExampleLauncher : MonoBehaviour
    {
        private void Start()
        {
            UIManager.Init();
            UIManager.I.ShowUIUnmanagable<UIUnmanagable>();
            UIManager.I.ShowUI<UIOne>();
        }
    }
}

