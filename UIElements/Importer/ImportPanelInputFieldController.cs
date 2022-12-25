﻿using DarkScreenSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.PotionDescriptionWindow;
using System.Linq;

namespace AutoBrew.UIElements.Importer
{
    internal class ImportPanelInputFieldController : InputFieldController
    {
        public ImportPanel Panel;

        public override bool MustBeEnabled()
        {
            if (!Panel.IsActive() || !base.MustBeEnabled())
            {
                return false;
            }

            return !DarkScreen.Entities.Values.Any((DarkScreen darkScreen) => darkScreen.layer != Panel.Layer && !darkScreen.IsActiveObjectNull());
        }
    }
}
