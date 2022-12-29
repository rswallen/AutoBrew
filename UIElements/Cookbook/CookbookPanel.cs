using AutoBrew.UIElements.Cookbook.BrewControls;
using AutoBrew.UIElements.Cookbook.Instructions;
using AutoBrew.UIElements.Cookbook.RecipeChecklist;
using AutoBrew.UIElements.Misc;
using PotionCraft.LocalizationSystem;
using PotionCraft.ObjectBased.InteractiveItem;
using PotionCraft.ObjectBased.UIElements;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook
{
    internal class CookbookPanel : InteractiveItem
    {
        public static CookbookPanel Create()
        {
            GameObject obj = new()
            {
                name = $"{typeof(CookbookPanel).Name}",
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(false);

            var panel = obj.AddComponent<CookbookPanel>();
            panel.thisCollider = obj.AddComponent<BoxCollider2D>();

            panel.skin = UIUtilities.SpawnSkin();
            panel.skin.transform.SetParent(panel.transform);

            panel.heading = UIUtilities.SpawnDescLocalizedText();
            panel.heading.transform.SetParent(panel.transform);
            panel.heading.transform.localPosition = new(0f, 4.3f);
            panel.heading.text.fontSize = 5f;
            panel.heading.text.text = "Recipe Preview";

            panel.controller = BrewControlsPanel.Create();
            panel.controller.transform.SetParent(panel.transform);
            panel.controller.transform.localPosition = new(-2.3f, 2.7f);

            panel.checklist = Checklist.Create();
            panel.checklist.transform.SetParent(panel.transform);
            panel.checklist.transform.localPosition = new(-0.7f, 3.5f);

            panel.instructions = InstructionsPanel.Create();
            panel.instructions.transform.SetParent(panel.transform);
            panel.instructions.transform.localPosition = new(0f, -1.7f);

            InstructionDisplay.UpdateSprites(200);
            panel.instructions.Refill();

            // TEMP HANDLES - REMOVE!!
            var headingHandle = MoveUIHandle.Create("DescriptionWindow", 1000);
            headingHandle.ReplaceLink(panel, panel.heading, new(0f, 0f));
            headingHandle.IsActive = true;

            var controllerHandle = MoveUIHandle.Create("DescriptionWindow", 1000);
            controllerHandle.ReplaceLink(panel, panel.controller, new(0f, 1f));
            controllerHandle.IsActive = true;

            var checklistHandle = MoveUIHandle.Create("DescriptionWindow", 1000);
            checklistHandle.ReplaceLink(panel, panel.checklist, new(0.25f, -0.75f));
            checklistHandle.IsActive = true;

            var instructionsHandle = MoveUIHandle.Create("DescriptionWindow", 1000);
            instructionsHandle.ReplaceLink(panel, panel.instructions, new(0f, 1f));
            instructionsHandle.IsActive = true;

            panel.IsActive = true;
            return panel;
        }

        private SeamlessWindowSkin skin;

        private LocalizedText heading;
        private Checklist checklist;
        private BrewControlsPanel controller;
        private InstructionsPanel instructions;

        //private LocalizedText manualText;

        public override void Awake()
        {
            base.Awake();
            UpdateSize(new(8f, 10f));
            IsActive = false;
        }

        public bool IsActive
        {
            get { return isActive; }
            set
            {
                if (isActive != value)
                {
                    isActive = value;
                    gameObject.SetActive(value);
                }
            }
        }
        
        private bool isActive = false;
        private BoxCollider2D thisCollider;

        private void UpdateSize(Vector2 newSize)
        {
            skin.UpdateSize(newSize);
            thisCollider.size = newSize;
        }

        public void Toggle()
        {
            IsActive = !IsActive;
        }

        public void Reset()
        {
            instructions.Clear();
        }

        public void LoadMethod(BrewMethod recipe)
        {
            foreach (var order in recipe.OrderList)
            {
                switch (order.Stage)
                {
                    case BrewOrderType.GrindPercent:
                    {
                        break;
                    }
                    default:
                    {
                        var item = InstructionDisplay.Create();
                        item.ApplyOrder(order);
                        instructions.AddInstruction(item, false);
                        break;
                    }
                }
            }
            instructions.Refill();
        }
    }
}