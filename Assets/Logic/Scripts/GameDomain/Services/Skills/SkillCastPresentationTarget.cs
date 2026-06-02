using Logic.Scripts.GameDomain.MVC.Book;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>
    /// Resolves where self-buff VFX/aim should appear (Erza when the book casts a SelfBuff).
    /// </summary>
    public static class SkillCastPresentationTarget
    {
        static INaraController _nara;

        public static void Bind(INaraController nara)
        {
            _nara = nara;
        }

        public static IPlayableUnit ResolvePlayable(IPlayableUnit caster, SkillDataSO skill)
        {
            if (caster == null) return null;
            if (SkillCastRules.IsRangelessSelfBuff(skill) && caster is IBookController && _nara != null)
                return _nara;
            return caster;
        }

        public static Vector3 GetSelfCastFootWorld(IPlayableUnit caster, SkillDataSO skill)
        {
            IPlayableUnit presentation = ResolvePlayable(caster, skill) ?? caster;
            if (presentation?.UnitViewGO == null) return Vector3.zero;
            return presentation.UnitViewGO.transform.position;
        }

        public static Transform GetSelfCastTransform(IPlayableUnit caster, SkillDataSO skill)
        {
            IPlayableUnit presentation = ResolvePlayable(caster, skill) ?? caster;
            return presentation?.UnitViewGO != null ? presentation.UnitViewGO.transform : null;
        }
    }
}
