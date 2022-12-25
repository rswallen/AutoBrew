using PotionCraft.ObjectBased.UIElements;
using System.Drawing;
using UnityEngine;

namespace AutoBrew.UIElements.Misc
{
    internal sealed partial class Dropdown : MonoBehaviour
    {
        internal sealed class DropdownTrigger : SpriteChangingButton
        {
            public static DropdownTrigger Create(Dropdown root)
            {
                GameObject obj = new()
                {
                    name = typeof(DropdownTrigger).Name,
                    layer = LayerMask.NameToLayer("UI"),
                };
                obj.SetActive(false);
                obj.transform.SetParent(root.transform, false);

                var trigger = obj.AddComponent<DropdownTrigger>();

                trigger.spriteRenderer = UIUtilities.MakeRendererObj<SpriteRenderer>(trigger, "Main Renderer", 100);
                trigger.spriteRenderer.sprite = UIUtilities.GetSpriteByName("InventorySorting Arrow Inactive");

                var collider = obj.AddComponent<BoxCollider2D>();
                collider.size = new(0.3f, 0.3f);
                trigger.thisCollider = collider;

                trigger.root = root;

                trigger.showOnlyFingerWhenInteracting = true;

                obj.SetActive(true);
                return trigger;
            }

            private Dropdown root;

            public override void OnButtonReleasedPointerInside()
            {
                base.OnButtonReleasedPointerInside();
                root.Toggle();
            }
        }
    }
}