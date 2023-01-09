using AutoBrew.Overseer;
using PotionCraft.ManagersSystem;
using PotionCraft.ScriptableObjects;
using QFSW.QC;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook.RecipeChecklist
{
    internal class Checklist : MonoBehaviour
    {
        public static Checklist Create(CookbookPanel parent)
        {
            GameObject obj = new()
            {
                name = typeof(Checklist).Name,
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(false);

            var list = obj.AddComponent<Checklist>();
            list.potionBaseCheck = ChecklistItem.Create();
            list.potionBaseCheck.transform.SetParent(list.transform);
            list.potionBaseCheck.transform.localPosition = new(0f, 0f);

            list.saltAmountCheck = ChecklistItem.Create();
            list.saltAmountCheck.transform.SetParent(list.transform);
            list.saltAmountCheck.transform.localPosition = new(0f, -0.5f);

            list.ingredientCheck = ChecklistItem.Create();
            list.ingredientCheck.transform.SetParent(list.transform);
            list.ingredientCheck.transform.localPosition = new(0f, -1f);

            list.instructionCheck = ChecklistItem.Create();
            list.instructionCheck.transform.SetParent(list.transform);
            list.instructionCheck.transform.localPosition = new(0f, -1.5f);

            list.cookbook = parent;
            list.Clear();

            obj.SetActive(true);
            return list;
        }

        private CookbookPanel cookbook;

        private ChecklistItem potionBaseCheck;
        private ChecklistItem saltAmountCheck;
        private ChecklistItem ingredientCheck;
        private ChecklistItem instructionCheck;

        public bool Validate(bool forceCheck = false)
        {
            if (forceCheck)
            {
                VerifyAll();
            }
            return potionBaseCheck.IsTicked && saltAmountCheck.IsTicked && ingredientCheck.IsTicked && instructionCheck.IsTicked;
        }

        public void Clear()
        {
            potionBaseCheck.SetText("Potion Base: N/A");
            potionBaseCheck.IsTicked = false;

            saltAmountCheck.SetText("Void Salt: N/A");
            saltAmountCheck.IsTicked = false;

            ingredientCheck.SetText("Ingredients: N/A");
            ingredientCheck.IsTicked = false;

            instructionCheck.SetText("Instructions: N/A");
            instructionCheck.IsTicked = false;
        }

        public void VerifyAll()
        {
            potionBaseCheck.IsTicked = VerifyBaseUnlocked();
            potionBaseCheck.SetText($"PotionBase: {cookbook.Recipe.Base.GetName()}");

            saltAmountCheck.IsTicked = true;
            saltAmountCheck.SetText("Void Salt: 0/0");

            ingredientCheck.IsTicked = VerifyIngredientStock(out int wanted, out int count);
            ingredientCheck.SetText($"Ingredients: {count}/{wanted}");

            instructionCheck.IsTicked = VerifyInstructions(out count, out int valid);
            instructionCheck.SetText($"Instructions: {valid}/{count}");
        }

        public bool VerifyBaseUnlocked()
        {
            return Managers.RecipeMap.potionBaseSubManager.IsBaseUnlocked(cookbook.Recipe.Base);
        }

        public bool VerifyIngredientStock(out int wanted, out int actual)
        {
            wanted = 0;
            var items = cookbook.Recipe.GetItemsRequired();
            foreach ((InventoryItem item, int count) in items.Select(kvp => (kvp.Key, kvp.Value)))
            {
                wanted += count;
            }

            actual = 0;
            InventoryOverseer.CheckItemStock(ref items);
            foreach ((InventoryItem item, int count) in items.Select(kvp => (kvp.Key, kvp.Value)))
            {
                actual += count;
            }

            return (wanted != 0) && (actual == wanted);
        }

        public bool VerifyInstructions(out int count, out int valid)
        {
            count = valid = 0;

            foreach (var instruction in cookbook.Instructions.Instructions)
            {
                if (instruction != null)
                {
                    count++;
                    if (instruction.IsValid())
                    {
                        valid++;
                    }
                }
            }

            return (count != 0) && (valid == count);
        }
    }
}
