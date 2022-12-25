using BepInEx.Logging;
using Toolbar.UIElements.Buttons;

namespace AutoBrew.Toolbar.Buttons
{
    internal class BaseAutoBrewButton : BaseToolbarButton
    {
        private protected static ManualLogSource Log => AutoBrewPlugin.Log;
    }
}
