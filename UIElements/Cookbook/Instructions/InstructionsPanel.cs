using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.Scroll;
using PotionCraft.ObjectBased.UIElements.Scroll.Settings;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AutoBrew.UIElements.Cookbook.Instructions
{
    internal sealed class InstructionsPanel : MonoBehaviour
    {
        public static InstructionsPanel Create()
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

                panel.scrollview = scrollviewObj.AddComponent<InstructionsScrollView>();
                panel.scrollview.panel = panel;
                panel.scrollview.thisCollider = scrollviewObj.AddComponent<BoxCollider2D>();

                panel.scrollview.settings = ScriptableObject.CreateInstance<ScrollViewSettings>();
                panel.scrollview.settings.changeByScroll = 1.0f;
                panel.scrollview.settings.contentMoveAnimTime = 0.1f;
                panel.scrollview.settings.scrollByMouseWheelDeadZone = 0.1f;

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

                    panel.scrollview.verticalScroll = scrollObj;
                    panel.scrollview.verticalScrollPointer = scrollObj.GetComponentInChildren<ScrollPointer>();
                    panel.scrollview.verticalScrollPointer.scrollView = panel.scrollview;
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
                panel.contentAnchor = caObj.transform;

                // Make Content GameObject
                {
                    GameObject contentObj = new()
                    {
                        name = "Content",
                        layer = LayerMask.NameToLayer("UI"),
                    };
                    contentObj.SetActive(true);
                    panel.scrollview.content = contentObj.AddComponent<Content>();
                    panel.scrollview.content.transform.SetParent(panel.contentAnchor, false);
                    panel.scrollview.content.transform.localPosition = Vector2.zero;
                }
            }

            // Make ContentMask GameObject
            {
                var maskTexture = TextureCache.FindTexture("BoxMask");
                panel.contentMask = UIUtilities.MakeRendererObj<SpriteMask>(panel.gameObject, "ContentMask", 510);
                panel.contentMask.sprite = Sprite.Create(maskTexture, new(0f, 0f, maskTexture.width, maskTexture.height), new(0.5f, 0.5f));

                panel.maskOffset = new(-0.2f, -0.3f);
            }

            panel.contentMask2 = obj.AddComponent<RectMask2D>();

            // Make ContentFade GameObject
            {
                var fadeTexture = TextureCache.FindTexture("RecipeBook Recipe Substrate");
                panel.contentFade = UIUtilities.MakeRendererObj<SpriteRenderer>(panel.gameObject, "ContentFade", 500);
                panel.contentFade.drawMode = SpriteDrawMode.Sliced;
                panel.contentFade.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
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

            obj.SetActive(true);
            return panel;
        }

        private Transform contentAnchor;
        private InstructionsScrollView scrollview;
        private SpriteMask contentMask;
        private RectMask2D contentMask2;
        private SpriteRenderer contentFade;
        private SpriteRenderer contentFrame;

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
            get
            {
                return visibleArea.x - (padding[0] + padding[2]);
            }
        }

        private float scrollPadding = 0.6f;

        private Vector4 padding = new(0.4f, 1.0f, 0.4f, 1.0f);
        private Vector3 vertPadding = new(1.0f, 1.4f, 0.7f);
        
        private Vector2 maskOffset = new(-0.2f, -0.3f);
        private Vector2 fadeOffset = new(0f, -0.2f);

        public bool AddInstruction(InstructionDisplay pane, bool refill = true)
        {
            if ((pane == null) || instructions.Contains(pane))
            {
                return false;
            }
            instructions.Add(pane);
            pane.Anchor.SetParent(scrollview.content.transform, false);
            pane.ScrollView = scrollview;
            pane.Parent = this;
            pane.SetIndex(instructions.Count);

            if (refill)
            {
                Refill();
            }
            return true;
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
        }
        
        private float GetPanelLength(int numButtons)
        {
            int gaps = (numButtons < 1) ? 0 : numButtons - 1;
            return vertPadding[0] + (gaps * vertPadding[1]) + vertPadding[2];
        }

        private readonly List<InstructionDisplay> instructions = new();
        private InstructionEditor editor;

        public void Refill()
        {
            // start from the end of the list and work backwards
            int numButtons = 0;
            int position = 0;
            foreach (var pane in instructions)
            {
                if (pane != null)
                {
                    pane.Anchor.localPosition = (vertPadding[0] + (position++ * vertPadding[1])) * Vector2.down;
                    numButtons++;
                }
            }

            ContentLength = GetPanelLength(numButtons);
            UpdateSize(visibleArea);
        }

        public void UpdateSize(Vector2 newSize)
        {
            //contentAnchor.localPosition = new(0.1f, newSize.y / 2f);
            contentAnchor.localPosition = new((newSize.x / -2f) + 0.2f, newSize.y / 2f);

            float scrollX = (newSize.x - 0.45f) / 2;
            scrollview.verticalScroll.transform.localPosition = new(scrollX, 0f);

            float scrollLength = newSize.y - scrollPadding;

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

            contentMask2.rectTransform.sizeDelta = newSize;

            contentFade.size = new(newSize.x + fadeOffset.x, newSize.y + fadeOffset.y);

            contentFrame.size = newSize;

            // update scrollview collider
            scrollview.UpdateSize(newSize);
            scrollview.SetPositionTo(0f, false);
            pointer.SetPosition(0f, true, false);
        }
    }
}