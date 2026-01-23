using System;
using RicKit.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace RicKit.UIExample.Scripts
{
    public class UIUnmanagable : FadeUIPanel
    {
        [SerializeField] private Text txtCanInteract;
        public override void OnESCClick()
        {
            
        }

        private void Update()
        {
            txtCanInteract.text = "Can Interact: " + (!UI.IsLockInput() ? "Yes" : "No");
        }
    }
}