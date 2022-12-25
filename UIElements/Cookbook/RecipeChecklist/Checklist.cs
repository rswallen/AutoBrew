using PotionCraft.LocalizationSystem;
using PotionCraft.ScriptableObjects.Talents;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook.RecipeChecklist
{
    internal class Checklist : MonoBehaviour
    {
        public static Checklist Create()
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
            list.potionBaseCheck.SetText("Potion Base: Oil");
            list.potionBaseCheck.IsTicked = false;

            list.saltAmountCheck = ChecklistItem.Create();
            list.saltAmountCheck.transform.SetParent(list.transform);
            list.saltAmountCheck.transform.localPosition = new(0f, -0.5f);
            list.saltAmountCheck.SetText("Void Salt: 1000/67");
            list.saltAmountCheck.IsTicked = true;

            list.ingredientCheck = ChecklistItem.Create();
            list.ingredientCheck.transform.SetParent(list.transform);
            list.ingredientCheck.transform.localPosition = new(0f, -1f);
            list.ingredientCheck.SetText("Ingredients: 24/36");
            list.ingredientCheck.IsTicked = false;

            list.instructionCheck = ChecklistItem.Create();
            list.instructionCheck.transform.SetParent(list.transform);
            list.instructionCheck.transform.localPosition = new(0f, -1.5f);
            list.instructionCheck.SetText("Instructions: 16/16");
            list.instructionCheck.IsTicked = true;

            obj.SetActive(true);
            return list;
        }

        private ChecklistItem potionBaseCheck;
        private ChecklistItem saltAmountCheck;
        private ChecklistItem ingredientCheck;
        private ChecklistItem instructionCheck;
    }
}
