using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.Pestle;
using PotionCraft.ObjectBased.Stack;
using PotionCraft.ObjectBased.Stack.StackItem;
using PotionCraft.ScriptableObjects.Ingredient;
using System.Collections.Generic;
using UnityEngine;


namespace AutoBrew.Overseer
{
    internal class MortarOverseer : BaseOverseer
    {
        private bool _showPath;
        private int _flourishMax;
        private int _minUpdates;
        private Vector2 _mortarOffset;

        private GrindStage _gStage;
        private float _grindTarget;
        private float _grindStep;
        private int _flourishNum;

        public enum GrindStage
        {
            Idle,
            Grinding,
            Flourish,
            WaitingForFlourish,
            WaitingForCauldron
        }

        public override void Reconfigure(Dictionary<string, string> data)
        {
            ABSettings.SetOrigin("MortarOverseer");
            ABSettings.GetBool(data, "ShowPath", out _showPath, false, false);
            ABSettings.GetVector2(data, "MortarOffset", out _mortarOffset, Vector2.zero, false);
            ABSettings.GetInt(data, "FlourishMax", out _flourishMax, 1, false);
            ABSettings.GetInt(data, "MinGrindUpdates", out _minUpdates, 200, false);
        }

        public override void Reset()
        {
            _grindTarget = 0f;
            _flourishNum = 0;

            Stage = OverseerStage.Idle;
        }

        public override void Setup(BrewOrder order)
        {
            Stack item = Managers.Ingredient.mortar.containedStack;
            if (item == null)
            {
                Log.LogInfo("MortarOverseer: Can't grind if there is nothing in the mortar");
                Stage = OverseerStage.Failed;
                return;
            }
            _grindStep = GetGrindStepRate(item);
            _grindTarget = (float)order.Target / 100f;
            _gStage = GrindStage.Grinding;
            
            Stage = OverseerStage.Active;
        }

        public override void Process()
        {
            if (Stage != OverseerStage.Active)
            {
                return;
            }

            if (_gStage == GrindStage.Grinding)
            {
                Stack item = Managers.Ingredient.mortar.containedStack;
                if (item == null)
                {
                    Stage = OverseerStage.Failed;
                    return;
                }

                if (item.overallGrindStatus >= _grindTarget)
                {
                    item.overallGrindStatus = (float)_grindTarget;
                    _gStage = GrindStage.Flourish;
                    return;
                }

                if (!Grind(item, 1, out _))
                {
                    Stage = OverseerStage.Failed;
                    return;
                }
                UpdateGrindStatus(item);

                if (_showPath)
                {
                    Ingredient mortarIngredient = item.inventoryItem as Ingredient;
                    Managers.RecipeMap.path.ShowPath(mortarIngredient, item.overallGrindStatus, 1f, 1f, true);
                }
                return;
            }

            if (_gStage == GrindStage.WaitingForFlourish)
            {
                if (Managers.Ingredient.mortar.containedStack == null)
                {
                    // if there is nothing in the mortar, we continue to wait
                    // should probably add a time check - if we've waited X seconds and nothing has appeared, fail the stage
                    return;
                }
                Log.LogDebug("Mortar has the ingredient for flourish");
                _gStage = GrindStage.Flourish;
                return;
            }

            if (_gStage == GrindStage.Flourish)
            {
                if (_flourishNum >= _flourishMax)
                {
                    if (!ThrowStack(StackThrowerTarget.ToCauldron))
                    {
                        Stage = OverseerStage.Failed;
                        return;
                    }
                    _gStage = GrindStage.WaitingForCauldron;
                    return;
                }
                if (!ThrowStack(StackThrowerTarget.ToMortar))
                {
                    Stage = OverseerStage.Failed;
                    return;
                }
                _flourishNum++;
                _gStage = GrindStage.WaitingForFlourish;
                return;
            }
        }

        public override void LogStatus()
        {
            Stack item = Managers.Ingredient.mortar.containedStack;
            if (item != null)
            {
                float total = item.overallGrindStatus;
                Log.LogInfo($"MortarOverseer - GrindTotal: {total:P2}");
            }
        }

        public override double Accuracy
        {
            get
            {
                return 1.0;
            }
        }

        public void AddIngredientMark(Ingredient item, float grindStatus)
        {
            if (Idle)
            {
                return;
            }
            switch (_gStage)
            {
                case GrindStage.WaitingForCauldron:
                {
                    Stage = OverseerStage.Complete;
                    return;
                }
                default:
                {
                    BrewMaster.Abort("Ingredient added without authorisation");
                    return;
                }
            }
        }

        public bool Grind(Stack item, int grinds, out int completed)
        {
            completed = 0;

            // Because one of the function calls updates the colour of the pestle, 
            // it looks better if we make sure its in the mortar. On the flipside, 
            // crystal grinding becomes slightly more dramatic.
            Pestle objPestle = Managers.Ingredient.pestle;
            if (objPestle == null)
            {
                return false;
            }
            
            if (!objPestle.PestleInMortar)
            {
                objPestle.MoveToSpawnPosition();
            }

            // if we just use MaxGrindState, things go quantum and fall through the mortar
            int maxState = item.Ingredient.MaxGrindState - 1;

            // First time rubbing a piece adds something extra to the item stack,
            // screwing over the enumerater. Solution:grab all the grindable pieces, 
            // add them to a temp list, then rub one at random
            List<IngredientFromStack> temp = new();
            foreach (StackItem piece in item.itemsFromThisStack)
            {
                if (piece is IngredientFromStack lump)
                {
                    if (lump.CooledDown() && lump.currentGrindState < maxState)
                    {
                        temp.Add(lump);
                    }
                }
            }

            System.Random randGen = new();
            for (int i = 0; i < grinds; i++)
            {
                completed++;
                if (temp.Count == 0)
                {
                    // If we've ground everything, play the animation instead, then break
                    // (we can only play it once)
                    if (item.grindedSubstanceInMortar)
                    {
                        item.grindedSubstanceInMortar.animator.Animate(0f);
                    }
                    break;
                }
                else
                {
                    // one grind per piece per func call
                    int num = randGen.Next(temp.Count);
                    objPestle.grindingZone.Rub(temp[num], 0f);
                    temp.RemoveAt(num);
                }
            }
            return true;
        }

        public void UpdateGrindStatus(Stack item)
        {
            item.UpdateGrindedSubstance();
            float grindStatus = Mathf.Clamp01(item.substanceGrinding.CurrentGrindStatus + _grindStep);
            item.substanceGrinding.CurrentGrindStatus = grindStatus;
        }

        public int GetTotalGrindUpdates(Stack item)
        {
            int[] prefabsPerState = item.GetPrefabsPerGrindStateCount();
            int last = prefabsPerState.Length - 1;
            if (last < 0)
            {
                return 0;
            }

            int maxGrinds = 0;
            for (int i = 0; i < last; i++)
            {
                maxGrinds += prefabsPerState[i];
            }
            return maxGrinds;
        }

        public float GetGrindStepRate(Stack item)
        {
            int totalUpdates = GetTotalGrindUpdates(item);
            return 1f / Mathf.Max(totalUpdates, _minUpdates);
        }

        public int GetCurrentGrindUpdates(Stack item)
        {
            int updates = 0;
            foreach (StackItem piece in item.itemsFromThisStack)
            {
                if (piece is IngredientFromStack)
                {
                    updates += (piece as IngredientFromStack).currentGrindState;
                }
            }
            return updates;
        }

        public bool ThrowStack(StackThrowerTarget target)
        {
            Stack item = Managers.Ingredient.mortar.containedStack;
            if (item == null)
            {
                return false;
            }

            Vector2 flourishPos = (Vector2)Managers.Ingredient.mortar.transform.position + _mortarOffset;
            switch (target)
            {
                case StackThrowerTarget.ToMortar:
                {
                    return StackThrower.TryThrowToMortar(item, flourishPos, item.Ingredient);
                }
                case StackThrowerTarget.ToCauldron:
                {
                    return StackThrower.TryThrowToCauldron(item, flourishPos, item.Ingredient);
                }
                default: return false;
            }
        }
    }
}