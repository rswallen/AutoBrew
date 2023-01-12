using BepInEx.Logging;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook.BrewControls
{
    internal sealed class BrewControlsPanel : MonoBehaviour
    {
        private static ManualLogSource Log => AutoBrewPlugin.Log;

        public static BrewControlsPanel Create(CookbookPanel parent)
        {
            GameObject obj = new()
            {
                name = $"{typeof(BrewControlsPanel).Name}",
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(false);

            var panel = obj.AddComponent<BrewControlsPanel>();

            panel.start = StartBrewButton.Create(panel);
            panel.start.transform.localPosition = new(0f, 0f);

            panel.pause = ContinueBrewButton.Create(panel);
            panel.pause.transform.localPosition = new(0f, 0.625f);

            panel.step = ModeBrewButton.Create(panel);
            panel.step.transform.localPosition = new(-0.625f, -0.625f);

            panel.abort = AbortBrewButton.Create(panel);
            panel.abort.transform.localPosition = new(0.625f, -0.625f);

            panel.Cookbook = parent;
            obj.SetActive(true);
            return panel;
        }

        public CookbookPanel Cookbook { get; private set; }
        private StartBrewButton start;
        private ContinueBrewButton pause;
        private ModeBrewButton step;
        private AbortBrewButton abort;

        private void Start()
        {
            ShowButtons(false);
            start.IsActive = true;
        }

        public void ShowButtons(bool brewActive)
        {
            start.IsActive = !brewActive;
            pause.IsActive = brewActive;
            step.IsActive = brewActive;
            abort.IsActive = brewActive;
        }
    }
}
