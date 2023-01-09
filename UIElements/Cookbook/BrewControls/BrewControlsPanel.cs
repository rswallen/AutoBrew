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

            panel.pause = PauseBrewButton.Create(panel);
            panel.pause.transform.localPosition = new(0f, 0.625f);

            panel.step = StepBrewButton.Create(panel);
            panel.step.transform.localPosition = new(-0.625f, -0.625f);

            panel.abort = AbortBrewButton.Create(panel);
            panel.abort.transform.localPosition = new(0.625f, -0.625f);

            panel.cookbook = parent;
            obj.SetActive(true);
            return panel;
        }

        private CookbookPanel cookbook;
        private StartBrewButton start;
        private PauseBrewButton pause;
        private StepBrewButton step;
        private AbortBrewButton abort;

        private void Awake()
        {
            AbortBrew();
        }

        public void StartBrew()
        {
            start.IsActive = false;
            pause.IsActive = true;
            step.IsActive = true;
            abort.IsActive = true;
        }

        public void PauseBrew()
        {

        }

        public void ContinueBrew()
        {

        }

        public void AbortBrew()
        {
            start.IsActive = true;
            pause.IsActive = false;
            step.IsActive = false;
            abort.IsActive = false;
        }
    }
}
