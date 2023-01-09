using PotionCraft.ObjectBased.InteractiveItem;
using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.Scroll;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AutoBrew.UIElements.Misc
{
    internal sealed partial class Dropdown : MonoBehaviour
    {
        internal sealed class DropdownPanel : InteractiveItem
        {
            public static DropdownPanel Create(Dropdown root)
            {
                if (UIUtilities.SkinTemplate == null)
                {
                    return default;
                }

                GameObject obj = new()
                {
                    name = typeof(DropdownPanel).Name,
                    layer = LayerMask.NameToLayer("UI"),
                };
                obj.SetActive(false);

                var panel = obj.AddComponent<DropdownPanel>();
                panel.root = root;

                // create the skin that serves as the background
                panel.skin = UIUtilities.SpawnSkin();
                panel.skin.transform.SetParent(obj.transform, false);

                // sorting group stuff (from InteractiveItem)
                panel.sortingGroup = obj.AddComponent<SortingGroup>();
                panel.sortingGroup.sortingLayerID = UIUtilities.SortingLayerID;
                panel.sortingGroup.sortingOrder = 0;

                // Make ScrollView GameObject
                {
                    GameObject scrollviewObj = new()
                    {
                        name = typeof(DropdownScrollView).Name,
                        layer = LayerMask.NameToLayer("UI"),
                    };
                    scrollviewObj.SetActive(true);
                    scrollviewObj.transform.SetParent(obj.transform, false);

                    panel.scrollview = scrollviewObj.AddComponent<DropdownScrollView>();
                    panel.scrollview.thisCollider = scrollviewObj.AddComponent<BoxCollider2D>();
                    panel.scrollview.panel = panel;

                    // make vertical scroll object
                    {
                        GameObject scrollObj = new()
                        {
                            name = "VerticalScroll",
                            layer = LayerMask.NameToLayer("UI"),
                        };
                        scrollObj.SetActive(true);
                        scrollObj.transform.SetParent(panel.transform, false);
                        scrollObj.transform.localPosition = new Vector2(2f, 0f);

                        var scroll = UIUtilities.SpawnScroll();
                        scroll.transform.SetParent(scrollObj.transform, false);

                        panel.scrollview.verticalScroll = scrollObj;
                        panel.scrollview.verticalScrollPointer = scrollObj.GetComponentInChildren<ScrollPointer>();
                        panel.scrollview.verticalScrollPointer.scrollView = panel.scrollview;
                    }
                }

                // Make ContentAnchor GameObject
                {
                    panel.contentAnchor = new()
                    {
                        name = "ContentAnchor",
                        layer = LayerMask.NameToLayer("UI"),
                    };
                    panel.contentAnchor.SetActive(true);
                    panel.contentAnchor.transform.SetParent(obj.transform, false);

                    // Make Content GameObject
                    {
                        GameObject contentObj = new()
                        {
                            name = "Content",
                            layer = LayerMask.NameToLayer("UI"),
                        };
                        contentObj.SetActive(true);
                        panel.scrollview.content = contentObj.AddComponent<Content>();
                        panel.scrollview.content.transform.SetParent(panel.contentAnchor.transform, false);
                        panel.scrollview.content.transform.localPosition = Vector2.zero;
                    }
                }

                // Make ContentMask GameObject
                {
                    var maskTexture = TextureCache.FindTexture("BoxMask");
                    panel.contentMask = UIUtilities.MakeRendererObj<SpriteMask>(panel.gameObject, "ContentMask", 510);
                    panel.contentMask.sprite = Sprite.Create(maskTexture, new(0f, 0f, maskTexture.width, maskTexture.height), new(0.5f, 0.5f));
                }

                // Make ContentFadse GameObject
                {
                    var fadeTexture = TextureCache.FindTexture("RecipeBook Recipe Substrate");
                    panel.contentFade = UIUtilities.MakeRendererObj<SpriteRenderer>(panel.gameObject, "ContentFade", 500);
                    panel.contentFade.transform.localPosition = new(0.15f, 0f);
                    panel.contentFade.drawMode = SpriteDrawMode.Sliced;
                    panel.contentFade.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    panel.contentFade.sprite = Sprite.Create(fadeTexture, new(300, 210, 200, 780), new(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new(5, 30, 5, 30));
                }

                panel.maskOffset = new(-0.2f, -0.3f);

                panel.showOnlyFingerWhenInteracting = true;
                panel.raycastPriorityLevel = -13015;

                panel.IsOpen = true;
                obj.transform.SetParent(root.transform, false);
                return panel;
            }

            private Dropdown root;

            public float ContentLength { get; internal set; } = 1f;

            private readonly List<DropdownOption> options = new();

            private GameObject contentAnchor;

            private SeamlessWindowSkin skin;

            private DropdownScrollView scrollview;

            private SpriteMask contentMask;

            private SpriteRenderer contentFade;

            public int MaxVisibleOptions
            {
                set { UpdateMinMaxLength(value); }
            }

            private Vector3 padding = new(0.6f, 0.5f, 0.6f);
            private float minLength;
            private float maxLength;
            private float panelWidth = 5f;
            private Vector2 maskOffset = new(-0.2f, -0.3f);
            private Vector2 fadeOffset = new(0f, -0.2f);

            /// <summary>
            /// Open or close the panel by setting this to true or false.
            /// </summary>
            public bool IsOpen
            {
                get => isOpen;
                set
                {
                    if (isOpen != value)
                    {
                        isOpen = value;
                        gameObject.SetActive(value);
                    }
                }
            }
            private bool isOpen = false;

            public bool AddOption(string localeKey, bool refill = true)
            {
                if (string.IsNullOrEmpty(localeKey))
                {
                    return false;
                }

                var option = DropdownOption.Create(localeKey, new Vector2(10f, 0.5f), root);
                if (option == null)
                {
                    return false;
                }
                options.Add(option);

                option.transform.SetParent(scrollview.content.transform, false);
                option.ScrollView = scrollview;

                if (refill)
                {
                    Refill();
                }
                UpdateParentButtonLocked();
                return true;
            }

            public bool RemoveOption(DropdownOption option, bool refill = true)
            {
                if ((option == null) || !options.Contains(option))
                {
                    return false;
                }
                options.Remove(option);

                option.transform.SetParent(null, false);
                option.ScrollView = scrollview;

                if (refill)
                {
                    Refill();
                }
                UpdateParentButtonLocked();
                return true;
            }

            public void Toggle()
            {
                if (!IsOpen)
                {
                    Refill();
                }
                IsOpen = !IsOpen;
            }

            private float GetPanelLength(int numButtons)
            {
                int gaps = (numButtons < 1) ? 0 : numButtons - 1;
                return padding[0] + (gaps * padding[1]) + padding[2];
            }

            internal void UpdateMinMaxLength(int maxButtons)
            {
                minLength = GetPanelLength(1);
                maxLength = GetPanelLength(maxButtons);
                Refill();
            }

            public void UpdateParentButtonLocked()
            {
                if (root != null)
                {
                    //root.Locked = (options.Count == 0);
                }
            }

            public override void Awake()
            {
                base.Awake();
                UpdateMinMaxLength(5);
                IsOpen = false;
            }

            public void Refill()
            {
                // start from the end of the list and work backwards
                int numButtons = 0;
                int position = 0;
                foreach (var option in options)
                {
                    if (option != null)
                    {
                        option.transform.localPosition = (padding[0] + (position++ * padding[1])) * Vector2.down;
                        numButtons++;
                    }
                }

                ContentLength = GetPanelLength(numButtons);
                var length = Mathf.Clamp(ContentLength, minLength, maxLength);
                UpdateSize(new Vector2(panelWidth, length));
            }

            public void UpdateSize(Vector2 newSize)
            {
                transform.localPosition = new Vector2(0f, -0.5f - (newSize.y / 2f));

                skin.UpdateSize(newSize);

                contentAnchor.transform.localPosition = new(0.1f, newSize.y / 2f);

                float scrollLength = newSize.y - 0.4f;

                var axis = scrollview.verticalScrollPointer.axis;
                axis.spriteRenderer.size = new(0.1f, scrollLength);
                (axis.thisCollider as CapsuleCollider2D).size = new(0.15f, scrollLength);

                float scrollHalfLength = scrollLength / 2f;

                var pointer = scrollview.verticalScrollPointer;
                pointer.startPosition = new(0f, scrollHalfLength);
                pointer.endPosition = new(0f, -scrollHalfLength);

                float maskScaleX = ((newSize.x + maskOffset.x) / contentMask.sprite.rect.width) * contentMask.sprite.pixelsPerUnit;
                float maskScaleY = ((newSize.y + maskOffset.y) / contentMask.sprite.rect.height) * contentMask.sprite.pixelsPerUnit;
                contentMask.transform.localScale = new(maskScaleX, maskScaleY);

                contentFade.size = new(newSize.x + fadeOffset.x, newSize.y + fadeOffset.y);

                // update scrollview collider
                scrollview.UpdateSize(newSize);
                scrollview.SetPositionTo(0f, false);
                pointer.SetPosition(0f, true, false);
            }
        }
    }
}