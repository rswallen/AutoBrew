using BepInEx.Logging;
using HarmonyLib;
using PotionCraft.ObjectBased.UIElements.PotionCustomizationPanel;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Events;

namespace AutoBrew.UI
{
    internal static class UserInput
    {
        static ManualLogSource Log => AutoBrewPlugin.Log;
        static Dictionary<string, UnityAction> _callbacks;

        [HarmonyPostfix, HarmonyPatch(typeof(PotionCustomizationPanel), "OnPanelContainerStart")]
        public static void OnPanelContainerStart_Postfix(PotionCustomizationPanel __instance)
        {
            _callbacks = new()
            {
                { "AutoBrew", BrewMaster.InitBrewFromJson },
                { "AutoBrew-Plotter", BrewMaster.InitBrewFromPlotterURL }
            };

            if (__instance.titleInputField != null)
            {
                __instance.titleInputField.onSubmit.AddListener(delegate (string text)
                {
                    if (!BrewMaster.Initialised || BrewMaster.Brewing)
                    {
                        return;
                    }

                    if (text.Length >= 20)
                    {
                        Log.LogDebug("UserInput: Title is too long, so not gonna process");
                        Log.LogDebug($"           Title Length - {text.Length}");
                        return;
                    }

                    if (!_callbacks.ContainsKey(text))
                    {
                        return;
                    }
                    _callbacks[text].Invoke();
                });
            }
        }
    }
}
