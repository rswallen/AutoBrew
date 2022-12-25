using PotionCraft.ObjectBased.UIElements;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace AutoBrew.UIElements.Importer.Buttons
{
    internal class ImportPanelCancelButton : ImportPanelButton
    {
        public static ImportPanelCancelButton Create(ImportPanel panel)
        {
            var button = Create<ImportPanelCancelButton>(panel);
            button.spriteRenderer.sprite = UIUtilities.GetSpriteByName("Confirmation No Button");

            (button.thisCollider as BoxCollider2D).size = button.spriteRenderer.size;

            button.text.text.text = "Cancel";
            
            button.gameObject.SetActive(true);
            return button;
        }

        public override void OnButtonReleasedPointerInside()
        {
            base.OnButtonReleasedPointerInside();
            panel.Disappear(DarkScreenSystem.DarkScreenDeactivationType.Other);
        }
    }
}