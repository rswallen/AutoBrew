using UnityEngine;

namespace AutoBrew.UIElements.Importer.Buttons
{
    internal sealed class ImportPanelOkButton : ImportPanelButton
    {
        public static ImportPanelOkButton Create(ImportPanel panel)
        {
            var button = Create<ImportPanelOkButton>(panel);
            button.spriteRenderer.sprite = UIUtilities.GetSpriteByName("Confirmation Yes Button");
            (button.thisCollider as BoxCollider2D).size = (button.spriteRenderer.size - new Vector2(0.3f, 0.3f));

            button.text.text.text = "Import";

            button.gameObject.SetActive(true);
            return button;
        }

        public override void OnButtonReleasedPointerInside()
        {
            base.OnButtonReleasedPointerInside();
            panel.Disappear(DarkScreenSystem.DarkScreenDeactivationType.ClickSubmit);
        }
    }
}