using BepInEx.Logging;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.Potion;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraft.ScriptableObjects.Salts;
using UnityEngine;

namespace AutoBrew.Overseer
{
    internal abstract class BaseOverseer
    {
        private protected static ManualLogSource Log => AutoBrewPlugin.Log;

        private OverseerStage _stage;
        private double _gtStart;
        private double _gtEnd;

        public bool Idle
        {
            get { return _stage == OverseerStage.Idle; }
        }

        public double Duration
        {
            get
            {
                if ((_stage != OverseerStage.Complete) && (_stage != OverseerStage.Failed))
                {
                    return 0.0;
                }
                return _gtEnd - _gtStart;
            }
        }

        public abstract double Accuracy { get; }

        public abstract double Precision { get; }

        public OverseerStage Stage
        {
            get { return _stage; }
            set
            {
                switch (value)
                {
                    case OverseerStage.Idle:
                    {
                        SetIdle();
                        return;
                    }
                    case OverseerStage.Active:
                    {
                        SetActive();
                        return;
                    }
                    case OverseerStage.Complete:
                    {
                        SetComplete();
                        return;
                    }
                    case OverseerStage.Failed:
                    {
                        SetFailed();
                        return;
                    }
                }
            }
        }

        public void Update(BrewOrder order)
        {
            if (Idle)
            {
                Setup(order);
            }

            Process();
            if (_stage == OverseerStage.Complete)
            {
                Log.LogInfo($"Order '{order.Stage}' completed in {Duration:N2}s, {Accuracy:P4} of target, error of {Precision:N4}u");
                BrewMaster.PrintRecipeMapMessage($"Order '{order.Stage}' completed", BrewMaster.OrderCompleteOffset);
                
                Reset();
                BrewMaster.AdvanceOrder();
            }
            else if (_stage == OverseerStage.Failed)
            {
                BrewMaster.LogFailedOrder(order);
                BrewMaster.Abort(order.GetFailKey());
            }
        }

        public abstract void Reset();

        public virtual void Setup(BrewOrder order)
        {
            ConsumeVoidSalt(order.SaltCost);
            SetActive();
        }

        public abstract void Process();

        public abstract void LogStatus();

        private void SetIdle()
        {
            _stage = OverseerStage.Idle;
            _gtStart = 0.0;
            _gtEnd = 0.0;
        }

        private void SetActive()
        {
            _stage = OverseerStage.Active;
            _gtStart = Time.timeAsDouble;
        }

        private void SetComplete()
        {
            _gtEnd = Time.timeAsDouble;
            _stage = OverseerStage.Complete;
        }

        private void SetFailed()
        {
            _gtEnd = Time.timeAsDouble;
            _stage = OverseerStage.Failed;
        }

        private bool ConsumeVoidSalt(int amount)
        {
            Salt voidSalt = Salt.GetByName("Void Salt", false, false);
            if (voidSalt == null)
            {
                // if salt don't exist, we can't do anything
                Log.LogError("ConsumeVoidSalt: 'Void Salt' does not exist");
                return false;
            }

            if (Managers.Player.inventory.GetItemCount(voidSalt) < amount)
            {
                Log.LogError("ConsumeVoidSalt: Not enough void salt");
                return false;
            }

            for (int i = 0; i < amount; i++)
            {
                PotionUsedComponent.AddToList(Managers.Potion.usedComponents, voidSalt, true);
            }
            PotionManager.RecipeMarksSubManager.AddSaltMark(Managers.Potion.recipeMarks.GetMarksList(), voidSalt, amount);
            Managers.Potion.potionCraftPanel.onPotionUpdated?.Invoke(true);
            Managers.Player.inventory.RemoveItem(voidSalt, amount);
            return true;
        }
    }

    public enum OverseerStage
    {
        Idle,
        Active,
        Complete,
        Failed
    }
}
