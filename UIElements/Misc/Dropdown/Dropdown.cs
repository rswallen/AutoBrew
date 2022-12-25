using BepInEx.Logging;
using PotionCraft.LocalizationSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AutoBrew.UIElements.Misc
{
    internal sealed partial class Dropdown : MonoBehaviour
    {
        internal static ManualLogSource Log => AutoBrewPlugin.Log;

        public static Dropdown Create()
        {
            GameObject obj = new()
            {
                name = $"{typeof(Dropdown).Name}",
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(false);

            var dropdown = obj.AddComponent<Dropdown>();

            dropdown.label = UIUtilities.SpawnDescLocalizedText();
            dropdown.label.transform.SetParent(dropdown.transform, false);
            dropdown.label.transform.localPosition = new(-2f, 0f);
            dropdown.label.text.text = "";

            dropdown.trigger = DropdownTrigger.Create(dropdown);
            dropdown.trigger.transform.localPosition = new(0f, 0f);

            dropdown.panel = DropdownPanel.Create(dropdown);
            dropdown.panel.transform.localPosition = new(0f, -3f);

            dropdown.sortinggroup = obj.AddComponent<SortingGroup>();
            dropdown.sortinggroup.sortingLayerID = SortingLayer.NameToID("DescriptionWindow");
            dropdown.sortinggroup.sortingOrder = 100;

            dropdown.background = UIUtilities.MakeRendererObj<SpriteRenderer>(dropdown, "Background", 100);
            dropdown.background.sprite = UIUtilities.GetSpriteByName("Talent Upgraded 2");

            obj.SetActive(true);
            return dropdown;
        }

        private LocalizedText label;
        private DropdownTrigger trigger;
        private DropdownPanel panel;
        private SortingGroup sortinggroup;
        private SpriteRenderer background;

        public void Select(DropdownOption option)
        {
            label.text.text = option.localizedText.text.text;
        }

        public void Toggle()
        {
            panel.Toggle();
        }

        public void AddOptions(IEnumerable<string> options)
        {
            foreach (string option in options)
            {
                panel.AddOption(option, false);
            }
            panel.Refill();
        }


        /// update size:
        ///  - get height of label
        ///  - add padding to lable height
        ///  - if new height < label height, update with label height
    }
}
