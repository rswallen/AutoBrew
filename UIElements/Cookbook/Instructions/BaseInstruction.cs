using PotionCraft.ObjectBased.InteractiveItem;
using TMPro;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook.Instructions
{
    internal abstract class BaseInstruction : InteractiveItem
    {
        public static T Create<T>() where T : BaseInstruction
        {
            GameObject obj = new()
            {
                name = $"{typeof(T).Name}",
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(false);
            
            var item = obj.AddComponent<T>();



            var collider = obj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(5f, 0.5f);
            item.thisCollider = collider;

            item.showOnlyFingerWhenInteracting = true;
            item.raycastPriorityLevel = -13500;

            obj.SetActive(true);
            return item;
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
                    Anchor?.gameObject.SetActive(value);
                }
            }
        }

        public bool IsVisible
        {
            get { return isVisible; }
            internal set { UpdateVisibility(value); }
        }

        private bool isActive = false;
        private bool isVisible = true;
        private bool canInteract = true;

        public Transform Anchor;
        public InstructionsPanel Parent;
        public InstructionsScrollView ScrollView;
        public TMP_Text index; 

        private BoxCollider2D thisCollider;

        public void LateUpdate()
        {
            DisableWhenOutOfBounds();
        }

        private void DisableWhenOutOfBounds()
        {
            if (ScrollView == null)
            {
                return;
            }

            canInteract = (transform.position.y < ScrollView.MaxInteractPos.y) && (transform.position.y > ScrollView.MinInteractPos.y);
            IsVisible = (transform.position.y < ScrollView.MaxVisiblePos.y) && (transform.position.y > ScrollView.MinVisiblePos.y);
        }

        public override bool CanBeInteractedNow()
        {
            return canInteract && base.CanBeInteractedNow();
        }

        public virtual void UpdateVisibility(bool newValue)
        {
            isVisible = newValue;
        }

        public virtual void UpdateSize(float contentWidth)
        {

        }
    }
}
