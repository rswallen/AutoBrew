using PotionCraft.InputSystem;
using PotionCraft.ManagersSystem.Debug;
using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.Tooltip;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBrew.UIElements.Misc
{
    internal class MoveUIHandle : MovableUIItem
    {
        public static MoveUIHandle Create()
        {
            GameObject obj = new()
            {
                name = typeof(MoveUIHandle).Name,
                layer = LayerMask.NameToLayer("UI"),
            };
            obj.SetActive(false);

            var handle = obj.AddComponent<MoveUIHandle>();
            handle.thisCollider = obj.AddComponent<CircleCollider2D>();
            handle.thisCollider.offset = Vector3.zero;
            handle.thisCollider.radius = 0.2f;

            s_symbol ??= MakeSymbolSprite();

            handle.symbol = obj.AddComponent<SpriteRenderer>();
            handle.symbol.sprite = s_symbol;
            handle.symbol.drawMode = SpriteDrawMode.Sliced;
            handle.symbol.size = new(0.3f, 0.3f);

            handle.tooltipContentProvider = obj.AddComponent<TooltipContentProvider>();
            handle.tooltipContentProvider.fadingType = TooltipContentProvider.FadingType.UIElement;
            handle.tooltipContentProvider.tooltipCollider = handle.thisCollider;
            handle.tooltipContentProvider.positioningSettings = new List<PositioningSettings>()
            {
                new PositioningSettings()
                {
                    bindingPoint = PositioningSettings.BindingPoint.TransformPosition,
                    freezeX = false,
                    freezeY = true,
                    position = new Vector2(0.45f, 0.4f),
                    tooltipCorner = PositioningSettings.TooltipCorner.LeftTop,
                }
            };
            
            handle.raycastPriorityLevel = -20000;
            handle.IgnoreRotationForPivot = true;

            DebugManager.onDeveloperModeChanged.AddListener(handle.OnDevModeChanged);

            obj.SetActive(true);
            return handle;
        }

        public static MoveUIHandle Create(string sortingLayer, int sortingOrder)
        {
            var handle = Create();
            handle.symbol.sortingLayerID = SortingLayer.NameToID(sortingLayer);
            handle.symbol.sortingOrder = sortingOrder;
            return handle;
        }

        private static Sprite s_symbol;
        private static Sprite MakeSymbolSprite()
        {
            var texture = TextureCache.LoadTexture("UI", "move-ui-symbol.png");
            var rect = new Rect(0f, 0f, texture.width, texture.height);
            var pivot = new Vector2(0.5f, 0.5f);
            return Sprite.Create(texture, rect, pivot, 100, 0, SpriteMeshType.FullRect, new(0f,0f,0f,0f));
        }

        private bool IsPointerPressed { get; set; }

        public bool IsActive
        {
            get { return isActive; }
            set { ChangeActiveState(value); }
        }

        private void Update()
        {
            if (!IsActive)
            {
                return;
            }
            Vector2 vector = transform.localPosition;
            
            if (IsPointerPressed && !Commands.cursorPrimary.InUse())
            {
                IsPointerPressed = false;
            }
        }

        public override Vector3 GetCenterOfItem()
        {
            return base.transform.position;
        }

        public override void OnGrabPrimary()
        {
            if (!Commands.cursorAlternativeActionModifier.InUse())
            {
                Relink();
                linked = true;
            }

            base.OnGrabPrimary();
            SetActive();
        }

        public override void OnReleasePrimary()
        {
            if (linked)
            {
                Unlink();
                linked = false;
            }
            
            base.OnReleasePrimary();
        }

        public override bool CanBeInteractedNow()
        {
            return base.CanBeInteractedNow() && IsActive;
        }

        public override TooltipContent GetTooltipContent()
        {
            if (linkChild == null)
            {
                return null;
            }

            Vector2 hPos = (linkChild.localPosition * -1f) + transform.localPosition;
            Vector2 cPos = linkChild.localPosition;
            if (linkParent != null)
            {
                Vector2 pPos = linkParent.localPosition;
                return new()
                {
                    header = "Object Info",
                    textBelowHeader = $"Handle:\n - Offset: {hPos}",
                    description1 = $"Parent:\n - Name: {linkParent.name}\n - LocalPos: {pPos}",
                    description2 = $"Child:\n - Name: {linkChild.name}\n - LocalPos: {cPos}",
                    
                };
            }
            else
            {
                return new()
                {
                    header = "Object Info",
                    textBelowHeader = $"Handle:\n - Offset: {hPos}",
                    description1 = $"Parent:\n - Name: {{null}}\n - LocalPos: {{null}}",
                    description2 = $"Child:\n - Name: {linkChild.name}\n - LocalPos: {cPos}",
                };
            }
        }

        public CircleCollider2D thisCollider;

        public SpriteRenderer symbol;

        private Transform linkParent;
        private Transform linkChild;

        private bool isActive;
        private bool linked;

        public bool DevModeToggle = true;

        public void SetActive()
        {
            IsPointerPressed = true;
        }

        private void ChangeActiveState(bool value)
        {
            if (isActive == value)
            {
                return;
            }
            isActive = value;
        }

        public void SetSortingLayer(string sortingLayer, int sortingOrder)
        {
            symbol.sortingLayerID = SortingLayer.NameToID(sortingLayer);
            symbol.sortingOrder = sortingOrder;
        }

        public void ReplaceLink(MonoBehaviour parent, MonoBehaviour child)
        {
            ReplaceLink(parent.transform, child.transform, new(0f, 0f));
        }

        public void ReplaceLink(MonoBehaviour parent, MonoBehaviour child, Vector2 childLocPosOffset)
        {
            ReplaceLink(parent.transform, child.transform, childLocPosOffset);
        }

        public void ReplaceLink(Transform parent, Transform child, Vector2 childLocPosOffset)
        {
            if ((parent == null) || (child == null) || (child.parent != parent))
            {
                AutoBrewPlugin.Log.LogDebug("MoveUIHandle.ReplaceLink: a transform is null or there isn't a link");
                return;
            }

            transform.SetParent(parent, true);
            transform.localPosition = child.transform.localPosition + (Vector3)childLocPosOffset;

            linkParent = parent;
            linkChild = child;
        }

        public void Unlink()
        {
            if (linkChild == null)
            {
                return;
            }

            linkChild.SetParent(linkParent, true);
        }

        public void Relink()
        {
            if (linkChild == null)
            {
                return;
            }

            linkChild.SetParent(transform, true);
        }

        public void RepairLink()
        {
            if (linkChild == null)
            {
                return;
            }

            linkChild.SetParent(linkParent, true);
            transform.SetParent(null, true);

            linkParent = null;
            linkChild = null;
        }

        private void OnDevModeChanged(bool devModeOn)
        {
            if (DevModeToggle)
            {
                symbol.enabled = devModeOn;
                //backdrop.enabled = devModeOn;
                thisCollider.enabled = devModeOn;
            }
        }
    }
}
