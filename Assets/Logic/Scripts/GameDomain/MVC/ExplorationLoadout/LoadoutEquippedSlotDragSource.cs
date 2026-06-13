using System;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Logic.Scripts.GameDomain.MVC.ExplorationLoadout
{
    /// <summary>
    /// Drag source for equipped loadout slots (build screen only). Shows the same ghost as catalog drag.
    /// </summary>
    public sealed class LoadoutEquippedSlotDragSource : MonoBehaviour,
        IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        const float DragThresholdPixels = 10f;

        [SerializeField] private LoadoutSkillFrameView _frameView;

        Action<SkillDataSO> _onPreview;
        Action<SkillDataSO> _onDragBegin;
        Action<Vector2> _onDragMove;
        Action _onDragEnd;

        Vector2 _pointerDownScreenPosition;
        bool _dragStarted;

        public void Configure(
            LoadoutSkillFrameView frameView,
            Action<SkillDataSO> onPreview,
            Action<SkillDataSO> onDragBegin,
            Action<Vector2> onDragMove,
            Action onDragEnd)
        {
            _frameView = frameView;
            _onPreview = onPreview;
            _onDragBegin = onDragBegin;
            _onDragMove = onDragMove;
            _onDragEnd = onDragEnd;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_frameView == null || _frameView.BoundSkill == null) return;
            _pointerDownScreenPosition = eventData.position;
            _dragStarted = false;
            _onPreview?.Invoke(_frameView.BoundSkill);
        }

        public void OnBeginDrag(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            if (_frameView == null || _frameView.BoundSkill == null) return;

            if (!_dragStarted)
            {
                float distance = Vector2.Distance(_pointerDownScreenPosition, eventData.position);
                if (distance < DragThresholdPixels) return;

                _dragStarted = true;
                _frameView.SetBlocksRaycasts(false);
                LoadoutDragContext.Begin(_frameView.BoundSkill);
                _onDragBegin?.Invoke(_frameView.BoundSkill);
            }

            _onDragMove?.Invoke(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_frameView != null)
                _frameView.SetBlocksRaycasts(true);

            if (_dragStarted)
            {
                _onDragEnd?.Invoke();
                LoadoutDragContext.Clear();
            }

            _dragStarted = false;
        }
    }
}
