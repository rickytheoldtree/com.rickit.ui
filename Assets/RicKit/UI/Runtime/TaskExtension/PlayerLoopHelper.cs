using System;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace RicKit.UI.TaskExtension
{
    public static class PlayerLoopHelper
    {
        private static Action onAfterUpdate;

        public static void Initialize()
        {
            var playerLoop = PlayerLoop.GetDefaultPlayerLoop();

            for (var i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                if (playerLoop.subSystemList[i].type == typeof(Update))
                {
                    var updateList = playerLoop.subSystemList[i].subSystemList;
                    var newUpdateList = new PlayerLoopSystem[updateList.Length + 1];

                    var customSystem = new PlayerLoopSystem
                    {
                        type = typeof(PlayerLoopHelper),
                        updateDelegate = AfterUpdate
                    };

                    newUpdateList[0] = customSystem;
                    updateList.CopyTo(newUpdateList, 1);
                    playerLoop.subSystemList[i].subSystemList = newUpdateList;
                    break;
                }
            }

            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        public static void InsetAfterUpdate(Action action)
        {
            onAfterUpdate += action;
        }

        private static void AfterUpdate()
        {
            if (onAfterUpdate != null)
            {
                var action = onAfterUpdate;
                onAfterUpdate = null;
                action();
            }
        }
    }
}