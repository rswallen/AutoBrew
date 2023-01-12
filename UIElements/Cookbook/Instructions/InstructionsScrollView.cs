using DG.Tweening;
using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.Scroll;
using PotionCraft.ObjectBased.UIElements.Scroll.Settings;
using UnityEngine;

namespace AutoBrew.UIElements.Cookbook.Instructions
{
    internal class InstructionsScrollView : ScrollView
    {
        public void Awake()
        {
            settings = ScriptableObject.CreateInstance<ScrollViewSettings>();
            settings.changeByScroll = 1.0f;
            settings.contentMoveAnimTime = 0.1f;
            settings.scrollByMouseWheelDeadZone = 0.1f;
        }

        public override void Start()
        {
            base.Start();
            contentColliderSize = thisCollider.size;
        }

        public override void CalculateOversize()
        {
            oversize = Vector2.zero;
            if (verticalScroll != null)
            {
                oversize += (contentHeight - contentColliderSize.y) * Vector2.up;
            }
        }

        public override void SetPositionTo(float value, bool animated = false)
        {
            // source: InventoryScrollView.SetPositionTo
            content.transform.DOKill(false);
            if (animated)
            {
                content.transform.DOLocalMove(new(0f, value * oversize.y), 0.2f, false);
                return;
            }
            content.transform.localPosition = new(0f, value * oversize.y);

        }

        public override float GetContentHeight(bool withCustomIncrease = true)
        {
            return Mathf.Max(panel.ContentLength, 1f) + (withCustomIncrease ? content.increaseBy.y : 0f);
        }

        public void UpdateSize(Vector2 newSize)
        {
            if (thisCollider != null)
            {
                thisCollider.size = newSize;
                contentColliderSize = thisCollider.size;
            }
        }

        public Content content;
        public BoxCollider2D thisCollider;
        public InstructionsPanel panel;

        private Vector2 minInteractPos;
        private Vector2 minVisiblePos;

        private Vector2 maxInteractPos;
        private Vector2 maxVisiblePos;

        public Vector2 MinInteractPos
        {
            get
            {
                UpdateMinMaxCoordinates();
                return minInteractPos;
            }
        }

        public Vector2 MaxInteractPos
        {
            get
            {
                UpdateMinMaxCoordinates();
                return maxInteractPos;
            }
        }

        public Vector2 MinVisiblePos
        {
            get
            {
                UpdateMinMaxCoordinates();
                return minVisiblePos;
            }
        }

        public Vector2 MaxVisiblePos
        {
            get
            {
                UpdateMinMaxCoordinates();
                return maxVisiblePos;
            }
        }

        private int lastFrameUpdate;

        private void UpdateMinMaxCoordinates()
        {
            if (lastFrameUpdate == Time.frameCount)
            {
                return;
            }
            lastFrameUpdate = Time.frameCount;

            Vector2 vector = transform.position;
            vector += thisCollider.offset;
            Vector2 vector2 = 0.5f * thisCollider.bounds.size;

            Vector2 minBasePos = vector - vector2;
            Vector2 maxBasePos = vector + vector2;

            Vector2 interactOffset = 0.55f * (Vector2)transform.lossyScale;
            minInteractPos = minBasePos + interactOffset;
            maxInteractPos = maxBasePos - interactOffset;

            Vector2 visibleOffset = 0.3f * (Vector2)transform.lossyScale;
            minVisiblePos = minBasePos + visibleOffset;
            maxVisiblePos = maxBasePos - visibleOffset;

        }
    }
}
