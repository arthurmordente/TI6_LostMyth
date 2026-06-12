using System;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Logic.Scripts.GameDomain.MVC.ExplorationLoadout
{
    /// <summary>
    /// Drop target for loadout equipped slots. Must live on a GameObject with a raycastable Graphic (e.g. img_Paint).
    /// </summary>
    public sealed class LoadoutSlotDropTarget : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        SkillLoadoutUnitType _unitType;
        int _slotIndex;
        Action<SkillLoadoutUnitType, int, SkillDataSO> _onDropped;
        Action<SkillLoadoutUnitType, int> _onClicked;

        public void Configure(
            SkillLoadoutUnitType unitType,
            int slotIndex,
            Action<SkillLoadoutUnitType, int, SkillDataSO> onDropped,
            Action<SkillLoadoutUnitType, int> onClicked)
        {
            _unitType = unitType;
            _slotIndex = slotIndex;
            _onDropped = onDropped;
            _onClicked = onClicked;
        }

        public void OnDrop(PointerEventData eventData)
        {
            SkillDataSO skill = ResolveDraggedSkill(eventData);
            if (skill == null) return;
            _onDropped?.Invoke(_unitType, _slotIndex, skill);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;
            _onClicked?.Invoke(_unitType, _slotIndex);
        }

        static SkillDataSO ResolveDraggedSkill(PointerEventData eventData)
        {
            if (LoadoutDragContext.IsDragging)
                return LoadoutDragContext.DraggingSkill;

            if (eventData.pointerDrag == null) return null;

            LoadoutSkillFrameView sourceFrame = eventData.pointerDrag.GetComponent<LoadoutSkillFrameView>()
                ?? eventData.pointerDrag.GetComponentInParent<LoadoutSkillFrameView>();
            return sourceFrame != null ? sourceFrame.BoundSkill : null;
        }
    }
}
