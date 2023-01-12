using DG.Tweening;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.InteractiveItem;
using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.Scroll;
using PotionCraft.ObjectBased.UIElements.Scroll.Settings;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AutoBrew.UIElements.Cookbook.Instructions
{
    internal sealed class InstructionsPanel : MonoBehaviour
    {
        public static InstructionsPanel Create(CookbookPanel parent)
        {
            GameObject obj = new()
            {
                name = typeof(InstructionsPanel).Name,
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(false);

            var panel = obj.AddComponent<InstructionsPanel>();

            // Make ScrollView GameObject
            {
                GameObject scrollviewObj = new()
                {
                    name = typeof(InstructionsScrollView).Name,
                    layer = LayerMask.NameToLayer("UI"),
                };
                scrollviewObj.SetActive(true);
                scrollviewObj.transform.SetParent(obj.transform, false);

                panel.Scrollview = scrollviewObj.AddComponent<InstructionsScrollView>();
                panel.Scrollview.panel = panel;
                panel.Scrollview.thisCollider = scrollviewObj.AddComponent<BoxCollider2D>();

                panel.Scrollview.settings = ScriptableObject.CreateInstance<ScrollViewSettings>();
                panel.Scrollview.settings.changeByScroll = 1.0f;
                panel.Scrollview.settings.contentMoveAnimTime = 0.1f;
                panel.Scrollview.settings.scrollByMouseWheelDeadZone = 0.1f;

                // make scroll object
                {
                    GameObject scrollObj = new()
                    {
                        name = "VerticalScroll",
                        layer = LayerMask.NameToLayer("UI"),
                    };
                    scrollObj.SetActive(true);
                    scrollObj.transform.SetParent(panel.transform, false);
                    scrollObj.transform.localPosition = new Vector2(2.4f, 0f);

                    var scroll = UIUtilities.SpawnScroll();
                    scroll.transform.SetParent(scrollObj.transform, false);

                    panel.Scrollview.verticalScroll = scrollObj;
                    panel.Scrollview.verticalScrollPointer = scrollObj.GetComponentInChildren<ScrollPointer>();
                    panel.Scrollview.verticalScrollPointer.scrollView = panel.Scrollview;
                }
            }

            // Make ContentAnchor GameObject
            {
                GameObject caObj = new()
                {
                    name = "ContentAnchor",
                    layer = LayerMask.NameToLayer("UI"),
                };
                caObj.SetActive(true);
                caObj.transform.SetParent(obj.transform);
                panel.contentMask2 = caObj.AddComponent<RectMask2D>();
                panel.contentAnchor = caObj.transform as RectTransform;
                panel.contentAnchor.pivot = new(0f, 1f);

                // Make Content GameObject
                {
                    GameObject contentObj = new()
                    {
                        name = "Content",
                        layer = LayerMask.NameToLayer("UI"),
                    };
                    contentObj.SetActive(true);
                    panel.Scrollview.content = contentObj.AddComponent<Content>();
                    panel.Scrollview.content.transform.SetParent(panel.contentAnchor, false);
                    panel.Scrollview.content.transform.localPosition = Vector2.zero;
                }
            }

            // Make ContentMask GameObject
            {
                var maskTexture = TextureCache.FindTexture("BoxMask");
                panel.contentMask = UIUtilities.MakeRendererObj<SpriteMask>(panel.gameObject, "ContentMask", 510);
                panel.contentMask.sprite = Sprite.Create(maskTexture, new(0f, 0f, maskTexture.width, maskTexture.height), new(0.5f, 0.5f));

                panel.maskOffset = new(-0.2f, -0.3f);
            }


            panel.canvas = obj.AddComponent<Canvas>();
            panel.canvas.sortingLayerID = SortingLayer.NameToID("DescriptionWindow");
            panel.canvas.sortingOrder = 150;

            // Make ContentFade GameObject
            {
                var fadeTexture = TextureCache.FindTexture("RecipeBook Recipe Substrate");
                panel.contentFade = UIUtilities.MakeRendererObj<SpriteRenderer>(panel.gameObject, "ContentFade", 500);
                panel.contentFade.drawMode = SpriteDrawMode.Sliced;
                panel.contentFade.sprite = Sprite.Create(fadeTexture, new(300, 210, 200, 780), new(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new(5, 30, 5, 30));

                panel.contentFade.transform.localPosition = new(0.15f, 0f);
            }

            // make ContentFrame object
            {
                // TODO: make 3 sprites (left|middle|right) so aspect ratio of rhombus in middle is preserved

                var frameTexture = TextureCache.FindTexture("sactx-0-2048x2048-BC7-PotionCustomization Atlas-ee9e517c");

                panel.contentFrame = UIUtilities.MakeRendererObj<SpriteRenderer>(panel, "Frame Renderer", 600);
                panel.contentFrame.sprite = Sprite.Create(frameTexture, new(647, 0, 634, 772), new(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new(8, 20, 8, 20));
                panel.contentFrame.drawMode = SpriteDrawMode.Sliced;
            }

            panel.cookbook = parent;
            obj.SetActive(true);
            return panel;
        }

        private CookbookPanel cookbook;

        private Canvas canvas;
        private RectTransform contentAnchor;
        private SpriteMask contentMask;
        private RectMask2D contentMask2;
        private SpriteRenderer contentFade;
        private SpriteRenderer contentFrame;

        public InstructionsScrollView Scrollview;
        public Transform Content
        {
            get { return Scrollview.content.transform; }
        }

        public Image instructSubstrate = null;

        public bool ItemOrderLocked { get; private set; }

        public float ContentLength { get; internal set; } = 1f;

        public Vector2 VisibleArea
        {
            get { return visibleArea; }
            set
            {
                visibleArea = value;
                Refill();
            }
        }
        private Vector2 visibleArea = new(7.25f, 6f);

        public float ContentWidth
        {
            get { return visibleArea.x - (padding[0] + padding[2]); }
        }

        private float scrollPadding = 0.6f;

        private Vector4 padding = new(0.4f, 1.0f, 0.4f, 1.0f);

        public Vector3 VertPadding
        {
            get { return vertPadding; }
            set
            {
                vertPadding = value;
                Refill(false);
            }
        }
        private Vector3 vertPadding = new(0.9f, 1.4f, 0.9f);

        private Vector2 maskOffset = new(-0.2f, -0.3f);

        public void Start()
        {
            BrewMaster.OnOrderStarted.AddListener(OnOrderStarted);
            BrewMaster.OnStateChanged.AddListener(OnBrewStateChanged);
        }

        public bool AddInstruction(InstructionDisplay pane, bool refill = true)
        {
            if ((pane == null) || instructions.Contains(pane))
            {
                return false;
            }
            instructions.Add(pane);
            pane.Anchor.SetParent(Scrollview.content.transform, false);
            pane.ScrollView = Scrollview;
            pane.Parent = this;
            pane.SetIndex(instructions.Count - 1);

            if (refill)
            {
                Refill();
            }
            return true;
        }

        public void MoveInstruction(int from, int to)
        {
            if ((from >= instructions.Count) || (to >= instructions.Count))
            {
                return;
            }
            var item = instructions[from];
            instructions.RemoveAt(from);
            instructions.Insert(to, item);
            UpdateIndices();
        }

        public void UpdateIndices()
        {
            for (int i = 0; i < instructions.Count; i++)
            {
                instructions[i].SetIndex(i);
            }
        }

        public void Clear()
        {
            foreach (var item in instructions)
            {
                if (item != null)
                {
                    Destroy(item.Anchor.gameObject);
                }
            }
            instructions.Clear();
            Refill();
        }

        private float GetPanelLength(int numButtons)
        {
            int gaps = (numButtons < 1) ? 0 : numButtons - 1;
            //return vertPadding[0] + (gaps * vertPadding[1]) + vertPadding[2];
            return (gaps * vertPadding[1]);
        }

        // return a copy
        public List<InstructionDisplay> Instructions
        {
            get { return instructions.ToList(); }
        }
        private readonly List<InstructionDisplay> instructions = new();
        private InstructionEditor editor;

        public void Refill(bool resetToStart = true)
        {
            float scrollPos = Scrollview.verticalScrollPointer.Value;

            // start from the end of the list and work backwards
            int numButtons = 0;
            for (int i = 0; i < instructions.Count; i++)
            {
                var pane = instructions[i];
                if (pane != null)
                {
                    if (!pane.Handle.IsPointerPressed)
                    {
                        pane.Anchor.localPosition = (vertPadding[0] + (i * vertPadding[1])) * Vector2.down;
                    }
                    numButtons++;
                }
            }

            Scrollview.content.increaseBy = new(0f, vertPadding[0] + vertPadding[2]);
            ContentLength = GetPanelLength(numButtons);
            UpdateSize(visibleArea);

            if (!resetToStart)
            {
                Scrollview.verticalScrollPointer.SetPosition(scrollPos, true, false);
            }
        }

        public void UpdateSize(Vector2 newSize)
        {
            //contentAnchor.localPosition = new(0.1f, newSize.y / 2f);
            contentAnchor.localPosition = new((newSize.x / -2f) + 0.2f, newSize.y / 2f);

            float scrollX = (newSize.x - 0.45f) / 2;
            Scrollview.verticalScroll.transform.localPosition = new(scrollX, 0f);

            float scrollLength = newSize.y - scrollPadding;

            var axis = Scrollview.verticalScrollPointer.axis;
            axis.spriteRenderer.size = new(0.1f, scrollLength);
            (axis.thisCollider as CapsuleCollider2D).size = new(0.15f, scrollLength);

            float scrollHalfLength = scrollLength / 2f;

            var pointer = Scrollview.verticalScrollPointer;
            pointer.startPosition = new(0f, scrollHalfLength);
            pointer.endPosition = new(0f, -scrollHalfLength);

            float maskScaleX = ((newSize.x + maskOffset.x) / contentMask.sprite.rect.width) * contentMask.sprite.pixelsPerUnit;
            float maskScaleY = ((newSize.y + maskOffset.y) / contentMask.sprite.rect.height) * contentMask.sprite.pixelsPerUnit;
            contentMask.transform.localScale = new(maskScaleX, maskScaleY);

            contentMask2.rectTransform.sizeDelta = newSize;
            contentFade.size = newSize;
            contentFrame.size = newSize;

            // update scrollview collider
            Scrollview.UpdateSize(newSize);
            Scrollview.SetPositionTo(0f, false);
            pointer.SetPosition(0f, true, false);
        }

        public void Scroll(float delta)
        {
            Scrollview.ScrollByMouseWheel(delta);
        }

        public int GetIndexOfY(float yPos)
        {
            for (int i = 0; i < instructions.Count; i++)
            {
                float indexPos = -vertPadding[0] - (i * vertPadding[1]);
                indexPos -= (0.5f * vertPadding[1]);
                if (indexPos < yPos)
                {
                    return i;
                }
            }
            return instructions.Count;
        }

        public void ScrollToIndex(int index)
        {
            if (!Scrollview.activeVertical)
            {
                return;
            }

            if ((index < 0) || (index >= instructions.Count))
            {
                return;
            }

            var item = instructions[index];
            if (item.Handle.IsPointerPressed)
            {
                return;
            }

            float yPos = index * vertPadding[1];
            float newPos = yPos / ContentLength;
            Scrollview.verticalScrollPointer.SetPosition(newPos, true);
        }

        private void OnOrderStarted(int index, BrewOrder order)
        {
            // if pointer is grabbed, don't move it
            var item = Managers.Cursor.grabbedInteractiveItem;
            if ((item != null) && (item == Scrollview.verticalScrollPointer))
            {
                return;
            }
            ScrollToIndex(index);
            AddOrMoveSubstrate(index);
            AutoBrewPlugin.Log.LogDebug("Order started: " + index);
        }

        private void OnBrewStateChanged(BrewState newMode)
        {
            int index = cookbook.Recipe.CurrentVisibleIndex;
            AddOrMoveSubstrate(index);

        }

        private void AddOrMoveSubstrate(int index)
        {
            bool animate = true;
            if (instructSubstrate == null)
            {
                instructSubstrate = CreateSubstrate();
                animate = false;
            }
            else
            {
                instructSubstrate.transform.DOKill(false);
            }
            
            instructSubstrate.transform.SetParent(instructions[index].Anchor, false);
            instructSubstrate.transform.SetAsFirstSibling();

            if (animate)
            {
                instructSubstrate.transform.DOLocalMove(new(3.5f, 0f), 0.2f);
            }
            else
            {
                instructSubstrate.transform.localPosition = new(3.5f, 0f);
            }

            switch (BrewMaster.State)
            {
                case BrewState.Paused:
                {
                    // pause = 1 1 0 0.4
                    instructSubstrate.color = new(1f, 1f, 0f, 0.4f);
                    break;
                }
                default:
                {
                    instructSubstrate.color = new(0.3f, 0.4f, 0.5f, 0.25f);
                    break;
                }
            }

            // error = 1 0 0 0.35
            instructSubstrate.gameObject.SetActive(true);
        }

        private Image CreateSubstrate()
        {
            var image = UIUtilities.MakeCanvasSpriteObj(this, "Instruction Substrate");
            image.sprite = UIUtilities.GetSpriteByName("MainMenu HoverSlot Large 5");
            image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 7f);
            image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 1.2f);

            
            return image;
        }
    }
}
