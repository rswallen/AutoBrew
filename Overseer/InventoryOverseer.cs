using HarmonyLib;
using PotionCraft.LocalizationSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.PhysicalParticle;
using PotionCraft.ObjectBased.Potion;
using PotionCraft.ObjectBased.Salt;
using PotionCraft.ObjectBased.Stack;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.Ingredient;
using PotionCraft.ScriptableObjects.Salts;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AutoBrew.Overseer
{
    internal class InventoryOverseer : BaseOverseer
    {
        private static readonly Key _abortBadOrder = new("autobrew_brew_abort_invovsr_badorder");
        private static readonly Key _abortExtraIngAdded = new("autobrew_brew_abort_extraingadded");

        private Vector2 _ingredientSpawnPos;
        private Vector2 _saltItemSpawnPos;
        private Vector2 _cauldronOffset;
        private bool _alwaysDissolve;

        private BrewOrder _order;
        private bool _thingThrown;
        private int _saltAdded;

        public override void Reconfigure(Dictionary<string, string> data)
        {
            ABSettings.GetVector2(nameof(InventoryOverseer), data, "IngSpawnPos", out _ingredientSpawnPos, new Vector2(7.275f, 2.375f), false);
            ABSettings.GetVector2(nameof(InventoryOverseer), data, "SaltSpawnPos", out _saltItemSpawnPos, new Vector2(5.7f, -5.3f), false);
            ABSettings.GetVector2(nameof(InventoryOverseer), data, "CauldronOffset", out _cauldronOffset, new Vector2(0f, 5f), false);
            ABSettings.GetBool(nameof(InventoryOverseer), data, "AlwaysDissolve", out _alwaysDissolve, true, false);
        }

        public override void Reset()
        {
            _order = null;
            _thingThrown = false;
            _saltAdded = 0;

            Stage = OverseerStage.Idle;
        }

        public override void Setup(BrewOrder order)
        {
            switch (order.Stage)
            {
                case BrewStage.AddIngredient:
                case BrewStage.AddSalt:
                {
                    break;
                }
                default:
                {
                    Stage = OverseerStage.Failed;
                    return;
                }
            }
            _order = order;
            base.Setup(order);
        }

        public override void Process()
        {
            if (Stage != OverseerStage.Active)
            {
                return;
            }

            switch (_order.Stage)
            {
                case BrewStage.AddIngredient:
                {
                    if (!_thingThrown)
                    {
                        if (_order.Target == 0f)
                        {
                            if (!ThrowIngredient(_order.Item as Ingredient, StackThrowerTarget.ToCauldron))
                            {
                                Stage = OverseerStage.Failed;
                                return;
                            }
                        }
                        else
                        {
                            if (Managers.Ingredient.mortar.containedStack != null)
                            {
                                Log.LogError("AddIngredient: There is something in the mortar already. Aborting");
                                Stage = OverseerStage.Failed;
                                return;
                            }

                            if (!ThrowIngredient(_order.Item as Ingredient, StackThrowerTarget.ToMortar))
                            {
                                Stage = OverseerStage.Failed;
                                return;
                            }
                        }
                        _thingThrown = true;
                    }
                    else
                    {
                        if (Managers.Ingredient.mortar.containedStack == null)
                        {
                            // if there is nothing in the mortar, we continue to wait
                            // should probably add a time check - if we've waited X seconds and nothing has appeared, fail the stage
                            return;
                        }
                        Stage = OverseerStage.Complete;
                    }
                    return;
                }
                case BrewStage.AddSalt:
                {
                    int remaining = (int)_order.Target - _saltAdded;
                    if (remaining <= 0)
                    {
                        return;
                    }

                    SaltItem item = GetSaltShaker(_order.Item as Salt, _saltItemSpawnPos);
                    if (item == null)
                    {
                        Stage = OverseerStage.Failed;
                        return;
                    }

                    if (SpawnSaltParticle(item))
                    {
                        int count = item.Count;
                        item.Count = count - 1;
                        _saltAdded++;
                    }
                    return;
                }
                default:
                {
                    BrewMaster.Abort(_abortBadOrder);
                    return;
                }
            }
        }

        public override void LogStatus()
        {
            if (Idle)
            {
                Log.LogError("InventoryOverseer is inactive");
                return;
            }
            Log.LogInfo($"SaltStatus: {_saltAdded}/{_order.Target}");
        }

        public override double Accuracy
        {
            get
            {
                switch (_order.Stage)
                {
                    case BrewStage.AddIngredient:
                    {
                        return 1.0;
                    }
                    case BrewStage.AddSalt:
                    {
                        if (_order.Target == 0f)
                        {
                            return 1.0;
                        }
                        return _saltAdded / _order.Target;
                    }
                }
                return 0.0;
            }
        }

        public override double Precision
        {
            get
            {
                switch (_order.Stage)
                {
                    case BrewStage.AddIngredient:
                    {
                        return 0.0;
                    }
                    case BrewStage.AddSalt:
                    {
                        if (_order.Target == 0f)
                        {
                            return 0.0;
                        }
                        return Math.Abs(_order.Target - _saltAdded);
                    }
                }
                return 0.0;
            }
        }

        public bool CheckItemStock(ref Dictionary<InventoryItem, int> checklist)
        {
            bool haveEnough = true;
            foreach ((InventoryItem item, int count) in checklist.ToArray().Select(kvp => (kvp.Key, kvp.Value)))
            {
                int excess = Managers.Player.inventory.GetItemCount(item) - count;
                if (excess < 0)
                {
                    checklist[item] = 0;
                    haveEnough = false;
                }
            }
            return haveEnough;
        }

        public bool ThrowIngredient(Ingredient item, StackThrowerTarget target)
        {
            if (Managers.Player.inventory.GetItemCount(item) <= 0)
            {
                Log.LogError("Not enough of that ingredient");
                return false;
            }

            switch (target)
            {
                case StackThrowerTarget.ToMortar:
                    {
                        StackThrower.TryThrowToMortar(null, _ingredientSpawnPos, item, Managers.Player.inventoryPanel);
                        break;
                    }
                case StackThrowerTarget.ToCauldron:
                    {
                        StackThrower.TryThrowToCauldron(null, _ingredientSpawnPos, item, Managers.Player.inventoryPanel);
                        break;
                    }
                default: break;
            }
            
            Managers.Player.inventory.RemoveItem(item, 1);
            return true;
        }
        
        public SaltItem GetSaltShaker(Salt salt, Vector2 position)
        {
            SaltItem item = (SaltItem)Managers.Game.GetUniqueItemFromInventory(salt);
            if (item != null)
            {
                // if one exists, we're done
                return item;
            }

            // if not, make one
            item = SaltItem.SpawnNewItem(position, salt, Managers.Player.inventoryPanel);
            if (item != null)
            {
                // pick it up with the cursor, drop it, then reset the position
                // (for some reason, if we don't do this, it falls through the floor)
                Managers.Cursor.GrabItem(item, true);
                Managers.Cursor.ReleaseItem();
                item.transform.position = position;
                return item;
            }
            return null;
        }

        private bool SpawnSaltParticle(SaltItem item)
        {
            Vector2 cauldronPos = Managers.Ingredient.cauldron.transform.localPosition;
            Vector2 spawnPos = cauldronPos + _cauldronOffset;
            float randAngle = Random.Range(-150f, -30f);
            Vector2 impulse = item.GetSpawnVelocity(randAngle, 1f);
            PhysicalParticle.SpawnSaltParticle((Salt)item.inventoryItem, spawnPos, impulse);
            return true;
        }

        private void PutSaltItemAway()
        {
            SaltItem item = (SaltItem)Managers.Game.GetUniqueItemFromInventory(_order.Item);
            if (item != null)
            {
                item.PutInInventory();
            }
        }

        public void AddIngredientMark(Ingredient item, float grindStatus)
        {
            if ((Stage != OverseerStage.Active) || !_thingThrown)
            {
                return;
            }

            if ((item != _order.Item) || (grindStatus != 0.0))
            {
                BrewMaster.Abort(_abortExtraIngAdded);
                return;
            }
            
            Stage = OverseerStage.Complete;
        }

        public void AddSaltMark(List<SerializedRecipeMark> recipeMarksList, Salt salt)
        {
            if ((Idle) || (salt != _order.Item))
            {
                return;
            }

            SerializedRecipeMark mark = recipeMarksList.Last<SerializedRecipeMark>();
            if (mark.type != SerializedRecipeMark.Type.Salt)
            {
                return;
            }
            
            int value = mark.GetValueToDisplay();
            if (value > (int)_order.Target)
            {
                //we should never hit this, but JIC we do...
                Stage = OverseerStage.Failed;
                PutSaltItemAway();
                return;
            }
            
            if (value == (int)_order.Target)
            {
                Stage = OverseerStage.Complete;
                PutSaltItemAway();
            }
        }

        public void ParticleToPool(PhysicalParticleObject particle)
        {
            // ignore particles when not brewing and if they aren't salt
            if ((particle.type != PhysicalParticle.Type.Salt) || (particle == null) || !_alwaysDissolve)
            {
                return;
            }

            if ((Stage != OverseerStage.Active) || (_order.Stage != BrewStage.AddSalt))
            {
                // should probably abort here
                return;
            }

            // for whatever reason, not all salt particles trigger the dissolve function.
            // some end up with a positive y velocity (upward), and never pass the velocity check.
            // others die after 5 seconds without triggering the InsideCauldron check.
            // THIS HAPPENS EVEN IF WE SPAWN THEM DIRECTLY OVER THE CAULDRON AND MOVING DOWN!!
            // so if we find one that is killed inside the cauldron with a positive velocity,
            // manually dissolve it.
            // if we find one that died after 5 seconds without passing the InsideCauldron
            // check, manually dissolve it.
            // this could cause problems if another plugin spawns salt particles while
            // we are adding salt, but this will do for now

            bool inCauldron = particle.InsideCauldronCheck();
            bool goodVelocity = particle.thisRigidbody.velocity.y < 0f;
            // died with positive velocity
            if (inCauldron && (!goodVelocity))
            {
                particle.onCauldronDissolve?.Invoke();
                return;
            }

            // died of old age
            if (!inCauldron)
            {
                particle.onCauldronDissolve?.Invoke();
                return;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PhysicalParticle), "ToPool")]
        public static void ToPool_Prefix(PhysicalParticleObject particle)
        {
            if (!BrewMaster.Initialised || !BrewMaster.Brewing)
            {
                return;
            }
            BrewMaster.Larder.ParticleToPool(particle);
        }
    }
}
