using PotionCraft.ObjectBased.UIElements;
using TMPro;
using UnityEngine;


namespace AutoBrew.UIElements.Misc
{
    internal sealed partial class Dropdown : MonoBehaviour
    {
        internal sealed class DropdownOption : SpriteChangingButton
        {
            public static DropdownOption Create(string localeKey, Vector2 size, Dropdown root)
            {
                GameObject obj = new()
                {
                    name = $"{typeof(DropdownOption).Name}",
                    layer = LayerMask.NameToLayer("UI"),
                };
                obj.SetActive(false);
                
                var option = obj.AddComponent<DropdownOption>();

                var collider = obj.AddComponent<BoxCollider2D>();
                collider.size = size;
                option.thisCollider = collider;

                option.localizedText = UIUtilities.SpawnDescLocalizedText();
                option.localizedText.transform.SetParent(option.transform, false);
                option.localizedText.transform.localPosition = new(0f, 0f);
                //option.localizedText.SetText(localeKey);
                option.localizedText.text.text = localeKey;
                option.localizedText.text.fontSize = 3f;
                //option.localizedText.text.overflowMode = TextOverflowModes.Ellipsis;

                option.root = root;

                option.showOnlyFingerWhenInteracting = true;
                option.raycastPriorityLevel = -13500;

                obj.SetActive(true);
                return option;
            }

            private Dropdown root;
            public DropdownScrollView ScrollView;

            public void LateUpdate()
            {
                DisableWhenOutOfBounds();
            }

            public override void OnButtonReleasedPointerInside()
            {
                base.OnButtonReleasedPointerInside();
                root.Select(this);
            }

            private void DisableWhenOutOfBounds()
            {
                if ((ScrollView == null) || (root == null))
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

            public bool IsVisible
            {
                get { return isVisible; }
                internal set
                {
                    if (isVisible != value)
                    {
                        isVisible = value;
                        localizedText.gameObject.SetActive(value);
                    }
                }
            }
            private bool isVisible = true;
            private bool canInteract = true;
        }
    }
}
