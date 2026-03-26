using System;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Abilitys {
    [Serializable]
    public abstract class AbilityEffect {
        public string Name;
        public string Description;
        public bool IsAutoCast;
        [Tooltip("Icon shown on the tile canvas when this effect is one of the possible outcomes for that tile.")]
        public Sprite TileIcon;
        protected AbilityData Data;
        public virtual void SetUp(Vector3 point) { }
        public virtual void Execute(AbilityData data, IEffectable caster, IEffectable target) { }
        public virtual void Execute(AbilityData data, IEffectable caster) { }
        public virtual void Execute(IEffectable caster, IEffectable target) { }
        public virtual void Cancel(IEffectable caster, IEffectable target) { }
    }
}