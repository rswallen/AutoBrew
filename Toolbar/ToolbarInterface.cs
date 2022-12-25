using AutoBrew.Toolbar.Buttons;
using Toolbar;
using Toolbar.UIElements.Buttons;

namespace AutoBrew.Toolbar
{
    internal static class ToolbarInterface
    {
        public static void Setup()
        {
            if (!ToolbarAPI.IsInitialised)
            {
                return;
            }

            BaseToolbarButton button = MainSubPanelButton.Create<MainSubPanelButton>("autobrew.mainpanel", "autobrew.mainpanel");
            ToolbarAPI.AddButtonToRootPanel(button);
            var panel = (button as SubPanelToolbarButton).SubPanel;

            button = ImportPotionousButton.Create("autobrew.mainpanel.loadpotionous");
            panel.AddButton(button);

            button = ToggleCookbookButton.Create("autobrew.mainpanel.togglecookbook");
            panel.AddButton(button);
        }
    }
}
