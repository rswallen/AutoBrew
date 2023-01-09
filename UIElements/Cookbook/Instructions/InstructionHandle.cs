using PotionCraft.InputSystem;
using PotionCraft.ObjectBased.UIElements;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook.Instructions
{
    internal class InstructionHandle : MovableUIItem
    {
        public static InstructionHandle Create()
        {
            GameObject obj = new()
            {
                name = typeof(InstructionHandle).Name,
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(false);

            var handle = obj.AddComponent<InstructionHandle>();
            handle.thisCollider = obj.AddComponent<BoxCollider2D>();
            handle.thisCollider.size = new(0.08f, 1.3f);

            handle.symbol = obj.AddComponent<SpriteRenderer>();
            handle.symbol.sprite = SymbolIcon;
            handle.symbol.drawMode = SpriteDrawMode.Sliced;
            handle.symbol.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            handle.symbol.size = new(0.08f, 1.3f);
            handle.symbol.sortingLayerID = SortingLayer.NameToID("DescriptionWindow");
            handle.symbol.sortingOrder = 400;

            handle.disableXPositionChanging = true;
            handle.raycastPriorityLevel = -20000;
            handle.IgnoreRotationForPivot = true;

            obj.SetActive(true);
            return handle;
        }

        public static Sprite SymbolIcon
        {
            get
            {
                symbolIcon ??= UIUtilities.GetSpriteByName("Talent Divider CannotUpgrade");
                return symbolIcon;
            }
        }
        private static Sprite symbolIcon;

        private static float scrollAmount = 0.15f;

        private BoxCollider2D thisCollider;
        private SpriteRenderer symbol;

        public bool IsPointerPressed { get; private set; }

        public bool IsActive
        {
            get { return isActive; }
            set { ChangeActiveState(value); }
        }
        private bool isActive;

        public BaseInstruction Instruction
        {
            get { return instruction; }
            set { UpdateInstruction(value); }
        }
        private BaseInstruction instruction;

        private void Update()
        {
            if (!IsActive)
            {
                return;
            }

            if (IsPointerPressed && !Commands.cursorPrimary.InUse())
            {
                IsPointerPressed = false;
            }

            if (!IsPointerPressed)
            {
                return;
            }

            var panelTransform = Instruction.Parent.transform;
            //Vector2 relPos = panelTransform.InverseTransformPoint(transform.position);
            Vector2 relPos = transform.localPosition;
            if (relPos.y > 2f)
            {
                Instruction.Parent.Scroll(scrollAmount);
                relPos.y = 2f;
                transform.localPosition = relPos;
            }
            else if (relPos.y <= -2f)
            {
                Instruction.Parent.Scroll(-scrollAmount);
                relPos.y = -2f;
                transform.localPosition = relPos;
            }

            relPos = Instruction.Parent.Content.InverseTransformPoint(transform.position);
            int posIndex = Instruction.Parent.GetIndexOfY(relPos.y);
            if (posIndex != Instruction.IndexNum)
            {
                Instruction.Parent.MoveInstruction(Instruction.IndexNum, posIndex);
                Instruction.Parent.Refill(false);
            }
        }

        public override Vector3 GetCenterOfItem()
        {
            return transform.position;
        }

        public override void OnGrabPrimary()
        {
            base.OnGrabPrimary();
            IsPointerPressed = true;
            transform.SetParent(Instruction.Parent.transform, true);
            Instruction.OnAnchorGrabbed();
        }

        public override void OnReleasePrimary()
        {
            base.OnReleasePrimary();
            IsPointerPressed = false;
            transform.SetParent(Instruction.Parent.Content, true);
            Instruction.OnAnchorReleased();
        }

        public override bool CanBeInteractedNow()
        {
            return base.CanBeInteractedNow() && IsActive;
        }

        private void ChangeActiveState(bool enabled)
        {
            if (isActive == enabled)
            {
                return;
            }
            isActive = enabled;
            symbol.enabled = enabled;
            thisCollider.enabled = enabled;
        }

        private void UpdateInstruction(BaseInstruction value)
        {
            instruction = value;
            if (instruction != null)
            {
                // resize based on instruction size
            }
        }
    }
}
