# Hocari — Export + Animator Unificado

Scripts criados.

### Menus Unity

| Passo | Menu |
|-------|------|
| Export FBX | `TI6 → Animation → 1 Export FBX Clips → Hocari` |
| Build controller | `TI6 → Animation → 2 Build State Machines → Hocari` |
| Assign prefab | `TI6 → Animation → 2 Build State Machines → Hocari — Assign HOC_Hocari_FINAL to HokariBoss prefab` |

Clips sem hook de gameplay (Donut, idle rest, legacy `HOC_*`): `AnimationInventory_UnwiredClips.md`

## Passos no Unity (tu)

1. Deixa o Unity recompilar os scripts.
2. **`TI6 → Animation → 1 Export FBX Clips → Hocari`**  
   - Fontes: `HocariAnimations1.fbx`, `HocariAnimations2.fbx`  
   - Dump completo: `Exported/FromAnim1`, `FromAnim2`, `Legacy`  
   - Pastas para o builder: `Phase1`, `Phase2`, `Shared`
3. **`TI6 → Animation → 2 Build State Machines → Hocari`**  
   - Gera `Assets/Art/Animations/Hocari/HOC_Hocari_FINAL.controller`  
   - **Obrigatório após alterações ao FSM no builder** (`HocariAnimatorControllerBuilder.cs`): volta a correr este menu para regenerar o `.controller` (não editar o `.controller` à mão).
4. **`TI6 → Animation → 2 Build State Machines → Hocari — Assign HOC_Hocari_FINAL to HokariBoss prefab`**
5. Abre a cena do boss Hokari e testa no Play mode.

---

## Ficheiros a criar

| Ficheiro | Papel |
|---|---|
| `Animation/HocariAnimatorParams.cs` | Constantes de params/tags |
| `Animation/HocariPhaseTransitionBehaviour.cs` | `BossPhase=1` no exit de `Finish_2` |
| `Editor/HocariAnimationClipExporter.cs` | Export `.anim` dos FBX |
| `Editor/HocariAnimatorControllerBuilder.cs` | Monta o controller unificado |

---

## Mapa de clips exportados

### Phase1 (`HocariAnimations2.fbx` + 1 clip de Anim1)

- Idle: `Hocari_CombatIdle_2`
- Ataques: Protean_2, Circle, Donut, SwordLines (+ Prep de Anim1), Wing L/R
- Hit: `Hocari_Hit`

### Phase2 (`HocariAnimations1.fbx` + Wing R Prep de Anim2)

- Idle: `Hocari_Phase2_CombatIdle`
- Ataques: Protean, Circle, Donut, SwordLines, Wing L/R (+ Right Prep de Anim2)
- Hit/Death: `Hocari_Phase2_Hit`, `Hocari_Phase2_Death`

### Shared (`HocariAnimations1.fbx`)

- `Hocari_PhaseTransition_Prep/Loop/Finish/Finish_2`
- `Hocari_Movement_Prep/Loop/Loop_2/Loop_3/Finish`

---

## Controller `HOC_Hocari_FINAL`

Legado a ignorar: `1HOC_Hocari.controller`, `HOC_AnimatorController.controller`, `HOC_Hocari_Unified.controller`.

### Parâmetros

**Existentes (BossView):** `AttackId`, `AttackPrep`, `AttackLoop`, `AttackFinish`, `Moving`, `MovePrep`, `MoveFinish`, `Idle`

**Novos:** `BossPhase` (int 0/1), `PhaseTransition`, `Hit`, `Death`

### AttackId

| Id | Ataque |
|---:|---|
| 0 | Protean |
| 1 | Circle |
| 2 | SwordLines |
| 3 | WingSlash Left |
| 4 | WingSlash Right |
| — | Donut (clips exportados; sem `AttackId` — não há ataque Donut no código) |

Cada ataque tem sub-SM com estados **P1_Prep/Loop/Finish** e **P2_Prep/Loop/Finish**; o `HOC_AttackChooser` encaminha por `AttackId` + `BossPhase`.

### Estrutura base

```
Base Layer
├── P1_CombatIdle (BossPhase==0, tag Idle)  ← default
├── P2_CombatIdle (BossPhase==1, tag Idle)
├── Movement_SM (MovePrep/MoveLoop tags)
├── PhaseTransition_SM → Finish_2 + HocariPhaseTransitionBehaviour
├── Attacks_SM
│   ├── HOC_AttackChooser (Route → 12 attack phase SMs; só entra via AttackPrep)
│   └── Prep → Loop (AttackLoop) → Finish (AttackFinish) → P1/P2_CombatIdle
│       (PlayAttackFinish limpa AttackId; sem reentrada até próximo AttackPrep)
├── Hit (AnyState, clip por BossPhase)
│   ├── AttackLoop → HOC_Attacks (reentra no telegraph)
│   └── !AttackLoop → combat idle
└── Death (AnyState)
```

---

## Código — HocariAnimatorParams.cs

```csharp
namespace Logic.Scripts.GameDomain.MVC.Boss.Hocari.Animation
{
    public static class HocariAnimatorParams
    {
        public const string AttackId = "AttackId";
        public const string AttackPrep = "AttackPrep";
        public const string AttackLoop = "AttackLoop";
        public const string AttackFinish = "AttackFinish";
        public const string Moving = "Moving";
        public const string MovePrep = "MovePrep";
        public const string MoveFinish = "MoveFinish";
        public const string Idle = "Idle";
        public const string BossPhase = "BossPhase";
        public const string PhaseTransition = "PhaseTransition";
        public const string Hit = "Hit";
        public const string Death = "Death";
        public const string TagIdle = "Idle";
        public const string TagAttackPrep = "AttackPrep";
        public const string TagAttackLoop = "AttackLoop";
        public const string TagMovePrep = "MovePrep";
        public const string TagMoveLoop = "MoveLoop";
        public const string TagDeath = "Death";
        public const int PhaseOne = 0;
        public const int PhaseTwo = 1;
        public const int AttackProtean = 0;
        public const int AttackCircle = 1;
        public const int AttackSwordLines = 2;
        public const int AttackWingLeft = 3;
        public const int AttackWingRight = 4;
    }
}
```

---

## Código — HocariPhaseTransitionBehaviour.cs

```csharp
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Hocari.Animation
{
    public sealed class HocariPhaseTransitionBehaviour : StateMachineBehaviour
    {
        [SerializeField] private string _bossPhaseParam = HocariAnimatorParams.BossPhase;
        [SerializeField] private int _targetPhase = HocariAnimatorParams.PhaseTwo;

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!string.IsNullOrEmpty(_bossPhaseParam))
                animator.SetInteger(_bossPhaseParam, _targetPhase);
        }
    }
}
```

---

## Próximo passo

**Activa Agent mode** e pede: *"cria os scripts Hocari do HOCARI_ANIMATION_BUILD.md"* — o agent escreve os 4 ficheiros `.cs` completos (exporter + builder ~500 linhas) e podes correr o menu no Unity.

O prefab `GameDesign/Prefabs/Bosses/Hocari/HokariBoss.prefab` será ligado pelo menu Assign (com a tua confirmação já dada).
