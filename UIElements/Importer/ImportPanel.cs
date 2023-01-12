using AutoBrew.UIElements.Importer.Buttons;
using BepInEx.Logging;
using DarkScreenSystem;
using PotionCraft.InputSystem;
using PotionCraft.LocalizationSystem;
using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.Scroll;
using PotionCraft.ObjectBased.UIElements.Tooltip;
using TMPro;
using UnityEngine;

namespace AutoBrew.UIElements.Importer
{
    internal class ImportPanel : DarkScreenContent
    {
        private static ManualLogSource Log => AutoBrewPlugin.Log;

        internal static ImportPanel Create()
        {
            if (UIUtilities.SkinTemplate == null)
            {
                Log.LogDebug("no skin");
                return null;
            }

            if (UIUtilities.DescInputFieldCanvasTemplate == null)
            {
                Log.LogDebug("no inputfield");
                return null;
            }

            // panel object
            GameObject obj = new()
            {
                name = nameof(ImportPanel),
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(false);
            //obj.transform.SetParent(objAnchor.transform, false);

            var panel = obj.AddComponent<ImportPanel>();
            panel.thisCollider = obj.AddComponent<BoxCollider2D>();

            // background skin. can stretch to any size
            panel.skin = UIUtilities.SpawnSkin();
            panel.skin.transform.SetParent(panel.transform, false);

            // content anchor
            GameObject anchor = new()
            {
                name = "ContentAnchor",
                layer = LayerMask.NameToLayer("UI"),
            };
            anchor.SetActive(false);
            anchor.transform.SetParent(panel.transform, false);
            panel.contentAnchor = anchor.transform;

            // title text. important: text must not be null when it awakens
            panel.titleText = UIUtilities.SpawnDescLocalizedText();
            panel.titleText.transform.SetParent(panel.contentAnchor, false);
            panel.titleText.transform.localPosition = new(0f, 5f);
            panel.titleText.text.text = "";
            panel.titleText.text.fontSize = 5f;
            panel.titleText.text.fontWeight = FontWeight.Black;

            // description text. important: text must not be null when it awakens
            panel.descText = UIUtilities.SpawnDescLocalizedText();
            panel.descText.transform.SetParent(panel.contentAnchor, false);
            panel.descText.transform.localPosition = new(0f, 4.25f);
            panel.descText.text.text = "";
            panel.descText.text.fontSize = 3f;

            // big badass input field
            var canvas = UIUtilities.SpawnInputFieldCanvas();
            canvas.transform.SetParent(panel.contentAnchor, false);

            // hook up things from the canvas
            panel.inputField = canvas.GetComponentInChildren<TMP_InputField>();
            panel.scrollView = canvas.GetComponentInChildren<InputFieldScrollView>();

            // destroy the inputfield controller. it only works with description box
            var ifc = panel.inputField.GetComponent<InputFieldController>();
            ifc.inputField = null;
            ifc.tooltipContentProvider = null;
            UnityEngine.Object.Destroy(ifc, 0.001f);

            // add custom inputfieldcontroller and hook it up
            var go = panel.inputField.gameObject;
            ifc = go.AddComponent<ImportPanelInputFieldController>();
            ifc.inputField = panel.inputField;
            ifc.tooltipContentProvider = go.GetComponent<TooltipContentProvider>();
            ifc.tooltipContentProvider.interactiveItem = ifc;
            ifc.raycastPriorityLevel = -13015;
            (ifc as ImportPanelInputFieldController).Panel = panel;

            // create a dummy frame sprite (used for resizing the skin)
            panel.frameSpriteRenderer = UIUtilities.MakeRendererObj<SpriteRenderer>(panel.gameObject, "Dummy Frame Renderer", 220);
            panel.frameSpriteRenderer.transform.SetParent(panel.contentAnchor, false);
            panel.frameSpriteRenderer.sprite = UIUtilities.GetSpriteByName("DescriptionCustomization Frame");

            // create ok button
            panel.okButton = ImportPanelOkButton.Create(panel);
            panel.okButton.transform.SetParent(panel.contentAnchor, false);
            panel.okButton.transform.localPosition = new(-1.6f, -4.5f);

            // create cancel button
            panel.cancelButton = ImportPanelCancelButton.Create(panel);
            panel.cancelButton.transform.SetParent(panel.contentAnchor, false);
            panel.cancelButton.transform.localPosition = new(1.6f, -4.5f);

            panel.raycastPriorityLevel = -13015;
            panel.gameObject.SetActive(true);
            return panel;
        }

        private Vector4 padding = new(0.23f, 0.23f, 0.23f, 0.23f);
        private Rect windowRect = new(Vector2.zero, Vector2.one);
        public DarkScreenLayer Layer = DarkScreenLayer.Lower;

        private bool isActive;

        private Transform contentAnchor;
        private SeamlessWindowSkin skin;
        private BoxCollider2D thisCollider;
        private LocalizedText titleText;
        private LocalizedText descText;
        private SpriteRenderer frameSpriteRenderer;
        private ScrollView scrollView;
        private TMP_InputField inputField;

        private ImportPanelOkButton okButton;
        private ImportPanelCancelButton cancelButton;

        public string Data
        {
            get { return inputField.text; }
        }

        private void Start()
        {
            Show(false, Vector2.zero);
        }

        public void Toggle()
        {
            Show(!isActive, Vector2.zero);
        }

        private void UpdateContent(bool show)
        {
            thisCollider.enabled = show;
            if (show)
            {
                titleText.text.text = "Recipe Import";
                titleText.text.ForceMeshUpdate(true, true);
                descText.text.text = "Import a recipe by pasting either\nJSON data or a Potionous link";
                descText.text.ForceMeshUpdate(true, true);

                inputField.SetTextWithoutNotify("");
                scrollView?.verticalScrollPointer.SetPosition(0f, true, false);
                Bounds contentBounds = GetContentBounds();
                Vector3 a = transform.InverseTransformPoint(contentBounds.center);
                Vector2 size = contentBounds.size + (padding.x + padding.z) * Vector3.right + (padding.y + padding.w) * Vector3.up;
                contentAnchor.localPosition += -a + (padding.x - padding.z) * Vector3.right + (padding.w - padding.y) * Vector3.up;
                UpdateSize(size);
            }
        }

        private void UpdateSize(Vector2 size)
        {
            size += (padding.x + padding.z) * Vector2.right + (padding.y + padding.w) * Vector2.up;
            windowRect.size = size;
            thisCollider.size = size;
            skin.UpdateSize(size);
        }

        private void UpdatePosition(Vector2 position)
        {
            windowRect.position = position - 0.5f * windowRect.size;
            transform.localPosition = windowRect.position + 0.5f * windowRect.size;
        }

        private Bounds GetContentBounds()
        {
            Bounds textBounds = titleText.text.GetTextBounds();
            textBounds.Encapsulate(frameSpriteRenderer.bounds);
            textBounds.Encapsulate(okButton.spriteRenderer.bounds);
            return textBounds;
        }

        public override bool IsActive()
        {
            return isActive;
        }

        public override void Appear()
        {
            Debug.Log("This method does not work! Use Show instead.");
        }

        public override void Disappear(DarkScreenDeactivationType deactivationType)
        {
            Show(false, Vector2.zero);
            if (deactivationType == DarkScreenDeactivationType.ClickSubmit)
            {
                BrewMaster.ParseRecipe(Data);
            }
        }

        public override bool AreRoomNavigationHotkeysEnabled()
        {
            return false;
        }

        public override bool CanBeClosedWhenClickOnDarkScreen()
        {
            return true;
        }

        public override void OnJustDownedCommand(Command command)
        {
            if (command == Commands.submit)
            {
                Disappear(DarkScreenDeactivationType.ClickSubmit);
            }
            /*
            else if (command == Commands.close)
            {
                Disappear(DarkScreenDeactivationType.Other);
            }
            */
        }
        public void Show(bool show, Vector2 position)
        {
            DarkScreen darkScreen = DarkScreen.Get(Layer);
            if (show)
            {
                darkScreen.ShowObject(this, DarkScreenType.Scene, 0f, 0f, null);
            }
            else
            {
                darkScreen.HideObject(0f, null);
            }

            UpdateContent(show);
            if (show)
            {
                UpdatePosition(position);
            }
            isActive = show;
            contentAnchor.gameObject.SetActive(show);
            skin.gameObject.SetActive(show);
        }
    }
}