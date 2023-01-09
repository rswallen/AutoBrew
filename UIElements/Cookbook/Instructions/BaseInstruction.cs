using PotionCraft.ObjectBased.InteractiveItem;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

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

            item.indexText = UIUtilities.MakeTMPTextObj<TextMeshProUGUI>("Caveat-Bold SDF");
            item.indexText.transform.SetParent(item.transform);
            item.indexText.transform.localPosition = new(0.2f, 0.5f);
            item.indexText.transform.localScale = new(0.1f, 0.1f);
            item.indexText.rectTransform.pivot = new(0f, 0.5f);
            item.indexText.rectTransform.sizeDelta = new(5f, 1f);
            item.indexText.alignment = TextAlignmentOptions.Left;
            item.indexText.enableWordWrapping = true;
            item.indexText.fontSize = 2.5f;
            item.indexText.text = "N/A";

            var collider = obj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(5f, 0.5f);
            item.thisCollider = collider;

            item.showOnlyFingerWhenInteracting = true;
            item.raycastPriorityLevel = -13500;

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
        private bool isActive = false;

        public bool IsVisible
        {
            get { return isVisible; }
            internal set
            {
                if (isVisible != value)
                {
                    isVisible = value;
                    UpdateVisibility(value);
                }
            }
        }
        private bool isVisible = true;

        public bool CanInteract
        {
            get { return canInteract; }
            internal set
            {
                if (canInteract != value)
                {
                    canInteract = value;
                    UpdateInteractive(value);
                }
            }
        }
        private bool canInteract = true;

        public Transform Anchor;
        public InstructionsPanel Parent;
        public InstructionsScrollView ScrollView;

        public BrewOrder Order
        {
            get { return order; }
            set { Apply(value); }
        }
        private BrewOrder order;

        public int IndexNum { get; private set; }
        private protected TMP_Text indexText;
        private protected BoxCollider2D thisCollider;

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

        public virtual void UpdateInteractive(bool newValue)
        {

        }

        public virtual void UpdateSize(float contentWidth)
        {

        }

        public void SetIndex(int index)
        {
            name = $"{GetType().Name}-{index}";
            IndexNum = index;
            indexText.text = (index + 1).ToString();
        }


        public abstract bool IsValid();

        public abstract Bounds GetBounds();

        public virtual void Apply(BrewOrder order)
        {
            this.order = order;
        }

        public virtual void OnAnchorGrabbed() { }
        public virtual void OnAnchorReleased() { }
    }
}
