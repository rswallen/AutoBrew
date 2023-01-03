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

            item.index = UIUtilities.MakeTextMeshProObj("Caveat-Bold SDF");
            item.index.transform.SetParent(item.transform);
            item.index.transform.localPosition = new(0.2f, 0.5f);
            item.index.rectTransform.pivot = new(0f, 0.5f);
            item.index.rectTransform.sizeDelta = new(5f, 1f);
            item.index.alignment = TextAlignmentOptions.Left;
            item.index.enableWordWrapping = true;
            item.index.fontSize = 2.5f;
            var tmp = item.index as TextMeshPro;
            tmp.sortingLayerID = SortingLayer.NameToID("DescriptionWindow");
            tmp.sortingOrder = 200;
            tmp.text = "N/A";

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
        
        private TMP_Text index;
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

        public void SetIndex(int newIndex)
        {
            index.text = newIndex.ToString();
        }
    }
}
