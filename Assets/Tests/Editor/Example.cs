using UnityEngine;

namespace RicKit.UI.Example
{
    public class Example : MonoBehaviour
    {
        private void Start()
        {
            UIManager.I.ShowUI<UIOne>();
        }
    }
}

