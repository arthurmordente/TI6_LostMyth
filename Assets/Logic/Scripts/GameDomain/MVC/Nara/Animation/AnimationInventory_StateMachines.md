# Inventario de Animacoes e Maquinas de Estado (Erza/Nara, Book, Laki)

Este documento descreve o inventario atual de animacoes e como as maquinas de estado sao dirigidas pelo codigo.

## Nota de nomenclatura

- `Nara` e o nome legado do protagonista no codigo.
- `Erza/Erzahler` e o nome atual de gameplay/art.
- Neste documento, `NaraController/NaraView` representam a entidade `Erza`.

## Assets e Controllers

### Player Erza (sem clone ativo)

- Controller: `Assets/Art/Animations/erz+book/ERZ_ErzahlerBook.controller`
- Clips (pasta `erz+book`):
  - `ErzahlerArmature_Erzahler+Book_Walk_1.anim`
  - `ErzahlerArmature_Erzahler+Book_Walk_2.anim`
  - `ErzahlerArmature_Erzahler+Book_FastConjuringWithTwoHands.anim`
  - `ErzahlerArmature_Erzahler+Book_SlowConjuring_Prep.anim`
  - `ErzahlerArmature_Erzahler+Book_SlowConjuring_Loop.anim`
  - `ErzahlerArmature_Erzahler+Book_SlowConjuring_Finish.anim`

### Player Erza (com clone ativo)

- Controller: `Assets/Art/Animations/Erzahler/ERZ_Erzahler.controller`
- Clips (pasta `Erzahler`):
  - `ErzahlerArmature_Erzahler_Idle_1.anim`
  - `ErzahlerArmature_Erzahler_Idle_2.anim`
  - `ErzahlerArmature_Erzahler_Walk.anim`
  - `ErzahlerArmature_Erzahler_Jog.anim`
  - `ErzahlerArmature_Erzahler_FastConjuring.anim`
  - `ErzahlerArmature_Erzahler_SlowConjuring_Prep.anim`
  - `ErzahlerArmature_Erzahler_SlowConjuring_Loop.anim`
  - `ErzahlerArmature_Erzahler_SlowConjuring_Finish.anim`

### Clone Book (Book of Cagliostro)

- Controller: `Assets/Art/Animations/Book/ERZ_Book.controller`
- Clips (pasta `Book`):
  - `ErzahlerArmature_Book_Idle_1.anim`
  - `ErzahlerArmature_Book_Idle_2.anim`
  - `ErzahlerArmature_Book_Idle_3.anim`
  - `ErzahlerArmature_Book_Walk_1.anim`
  - `ErzahlerArmature_Book_Walk_2.anim`
  - `ErzahlerArmature_Book_Ability.anim`

### Laki

- Controller: `Assets/Art/Animations/MadamLaki/LKI_Animator.controller`
- Clips:
  - `Laki_Idle_1.anim`
  - `Laki_Idle_2_Prep.anim`, `Laki_Idle_2_Loop.anim`, `Laki_Idle_2_Finish.anim`
  - `Laki_Idle_3_Prep.anim`, `Laki_Idle_3_Loop.anim`, `Laki_Idle_3_Finish.anim`
  - `Laki_Ability.anim`

## Parametros e Tags de Animator

Fonte: `Assets/Logic/Scripts/GameDomain/MVC/Nara/Animation/ErzahlerAnimatorParams.cs`

### Erza (player)

- Params:
  - `Moving` (bool)
  - `Running` (bool)
  - `WalkVariant` (int)
  - `IdleVariant` (int)
  - `ConjuringFast` (trigger)
  - `ConjuringPrep` (trigger)
  - `ConjuringLoop` (bool)
  - `ConjuringFinish` (trigger)
- Tags:
  - `Idle`
  - `Locomotion`
  - `ConjuringLoop`

### Book clone

- Params:
  - `Moving` (bool)
  - `IdleVariant` (int)
  - `WalkVariant` (int)
  - `Ability` (trigger)
- Tags:
  - `Idle`
  - `Locomotion`
  - `Ability`

### Laki

- Params:
  - `PerformanceId` (int)
  - `PerformancePrep` (trigger)
  - `PerformanceLoop` (bool)
  - `PerformanceFinish` (trigger)
  - `Ability` (trigger)
  - `Spotlight` (trigger, legado)
- Tags:
  - `Idle`
  - `PerformanceLoop`
  - `PerformancePrep`
  - `Ability`

## Maquinas de estado e transicoes (builder)

Fonte: `Assets/Logic/Scripts/GameDomain/MVC/Nara/Editor/ErzahlerAnimatorControllerBuilder.cs`

### ERZ_ErzahlerBook (player default, sem clone)

- Estados principais:
  - `Idle` (usa `Walk_1` com `speed = 0`)
  - `Walk_1`
  - `Walk_2`
  - `FastConjuring`
  - Sub-state machine `SlowConjuring` (`Prep -> Loop -> Finish`)
- Transicoes:
  - `Idle -> Walk_1` quando `Moving = true`
  - `Idle -> Walk_2` quando `Moving = true` e `WalkVariant == 2`
  - `Walk_1/Walk_2 -> Idle` quando `Moving = false`
  - `Walk_1 <-> Walk_2` por `WalkVariant` (1/2)
  - `AnyState -> FastConjuring` por trigger `ConjuringFast`
  - `AnyState -> SlowConjuring` por trigger `ConjuringPrep`
  - `FastConjuring -> Idle` por exit time
  - `SlowConjuring`: `Prep -> Loop` por `ConjuringLoop = true`, `Loop -> Finish` por `ConjuringFinish`, `Finish` sai por exit transition

### ERZ_Erzahler (player com clone ativo)

- Estados principais:
  - `Idle_1`, `Idle_2`
  - `Walk`, `Jog`
  - `FastConjuring`
  - Sub-state machine `SlowConjuring` (`Prep -> Loop -> Finish`)
- Transicoes:
  - `Idle_1 <-> Idle_2` por `IdleVariant` (1/2)
  - `Idle_* -> Walk` quando `Moving = true` e `Running = false`
  - `Idle_* -> Jog` quando `Moving = true` e `Running = true`
  - `Walk/Jog -> Idle_1` quando `Moving = false`
  - `Walk <-> Jog` por `Running`
  - `AnyState -> FastConjuring` por `ConjuringFast`
  - `AnyState -> SlowConjuring` por `ConjuringPrep`
  - `FastConjuring -> Idle_1` por exit time
  - `SlowConjuring`: `Prep -> Loop` por `ConjuringLoop = true`, `Loop -> Finish` por `ConjuringFinish`, `Finish` sai por exit transition

### ERZ_Book (clone deployado)

- Estados principais:
  - `Idle_1`, `Idle_2`, `Idle_3`
  - `Walk_1`, `Walk_2`
  - `Ability`
- Transicoes:
  - `Idle_1 -> Idle_2` por `IdleVariant == 2`
  - `Idle_1 -> Idle_3` por `IdleVariant == 3`
  - `Idle_2/Idle_3 -> Idle_1` por `IdleVariant == 1`
  - `Idle_* -> Walk_1` por `Moving = true`
  - `Idle_1 -> Walk_2` por `Moving = true` e `WalkVariant == 2`
  - `Walk_1/Walk_2 -> Idle_1` por `Moving = false`
  - `Walk_1 <-> Walk_2` por `WalkVariant` (1/2)
  - `AnyState -> Ability` por trigger `Ability`
  - `Ability -> Idle_1` por exit time

### LKI_Animator (Laki)

- Estados principais:
  - `Idle_1`
  - Sub-state machine `PerfIdle_2` (`Prep -> Loop -> Finish`)
  - Sub-state machine `PerfIdle_3` (`Prep -> Loop -> Finish`)
  - `Ability`
- Transicoes:
  - `AnyState -> PerfIdle_2` por `PerformancePrep` e `PerformanceId == 2`
  - `AnyState -> PerfIdle_3` por `PerformancePrep` e `PerformanceId == 3`
  - `AnyState -> Ability` por trigger `Ability`
  - Em cada sub-state machine:
    - `Prep -> Loop` por `PerformanceLoop = true`
    - `Loop -> Finish` por `PerformanceFinish`
    - `Finish` sai por exit transition
  - `Ability -> Idle_1` por exit time

## Gatilhos via codigo (runtime)

## 1) Swap de controller do player (Erza)

Fluxo do clone:

- Deploy clone:
  - `DivideAbilityHandler.DeployBook()` chama:
    - `_bookController.CreateBook(position)`
    - `_naraController.SetBookCloneDeployed(true)`
- Recall clone:
  - `DivideAbilityHandler.RecallBook()` chama:
    - `_bookController.DestroyBook()`
    - `_naraController.SetBookCloneDeployed(false)`

Implementacao do swap:

- `NaraController.SetBookCloneDeployed(bool)` -> `NaraView.SetBookCloneDeployed(bool)`
- `NaraView.SetBookCloneDeployed(bool)` -> `ErzahlerPlayerAnimatorDriver.SetBookCloneActive(bool)`
- `ErzahlerPlayerAnimatorDriver.ApplyControllerSwap()` troca:
  - `ErzahlerWithBook` quando `cloneDeployed = false`
  - `ErzahlerSolo` quando `cloneDeployed = true`

## 2) Gatilhos de ataque/conjuring do player

- Aim/prepare:
  - `NaraView.SetAttackType(type > 0)` chama `PlayConjuringSlowPrep()` no driver Erza
- Commit:
  - `NaraView.TriggerExecute()` seta `ConjuringLoop = true`
- Resolve:
  - `NaraController.OnAbilityExecuted()` chama `NaraView.ReleaseConjuring()`
  - `ReleaseConjuring()` dispara trigger `ConjuringFinish`
- Cancel:
  - `NaraView.TriggerCancel()` seta `ConjuringLoop = false` e reseta estado de ataque

## 3) Movimento do player

- `NaraController.ManagedFixedUpdate()` chama `NaraView.SetMoving(willMove)`
- Em modo Erza, `NaraView` delega para `ErzahlerPlayerAnimatorDriver.SetMoving()`, que atualiza:
  - `Moving`
  - `Running` (quando fornecido)

## 3b) Alternancia de idle (player)

- `ErzahlerPlayerIdleController` (auto no `NaraView` junto do driver)
- A cada `_intervalSeconds` (default 8s), se **parado**, **sem cast** e com controllers Erza:
  - `IdleVariant` alterna `1 <-> 2` via `ErzahlerPlayerAnimatorDriver.SetIdleVariant`
- **So tem efeito** no controller solo `ERZ_Erzahler` (`Idle_1` / `Idle_2`)
- Com livro (`ERZ_ErzahlerBook`) ha um unico estado Idle (sem segunda variante no builder)

## 4) Clone Book

- Setup controller:
  - `BookController.CreateBook()` -> `BookView.ConfigureBookAnimation(_erzahlerAnimatorControllers.BookClone)`
- Movimento:
  - `BookController.ManagedFixedUpdate()` -> `BookView.SetMoving(...)`
- Ability:
  - `BookView.TriggerExecute()` e `SetAttackType(type > 0)` disparam trigger `Ability`

## 5) Laki (dados + ataques comuns apenas)

Controller: `LKI_Animator` (`Idle_1`, performance `Idle_2`/`Idle_3`, `Ability`).

**Nao usar** `MinigameRuntimeService` / Suit / Naipe para animacao da Laki.

### Fases de animacao (Laki)

**Prepare** (`OnBossPrepareTurnStartedAsync`):
1. `PerformanceFinish` se ainda em performance → espera tag `Idle`
2. Sorteia idle 2/3 → `PerformancePrep` + `PerformanceLoop`
3. Telegraphs (sem `Ability`)

**PlayerAct:** mantém o idle sorteado no prepare.

**Resolve** (`BeginResolveAttackAnimation` antes de `ExecuteAsync`):
1. `PerformanceFinish` → `Idle_1`
2. `Ability` uma vez enquanto os ataques aplicam dano
3. Fica em `Idle_1` (sem novo sorteio aqui)

**Proximo prepare:** novo sorteio (passo 1–2).

### Dice Attack

- So UI/turn gates — **nao** dispara `BeginResolveAttackAnimation`

### Rebuild Laki (menu Unity)

- `TI6 > Animation > Build > LKI_Animator (Laki boss)` — so este controller + slot `LakiBoss` no SO
- Outros controllers têm entradas separadas em `TI6 > Animation > Build/...`

Fonte: `LakiBossAnimatorView.cs`, `LakiBossAnimationBridge.cs`

## 6) Hocari (boss legado — `1HOC_Hocari.controller`)

Controller em `Assets/Art/Animations/Hocari/1HOC_Hocari.controller` (nao gerado pelo builder Erza/Laki).

### Parametros (Animator)

| Parametro | Tipo | Uso no codigo (`BossView` / `BossController`) |
|-----------|------|--------------------------------------------------|
| `AttackId` | int | Escolhe sub-ataque (0 Protean, 1 Circle, 2 FeatherLines, 3/4 Wing L/R) |
| `AttackPrep` | trigger | Inicio do telegraph de ataque |
| `AttackLoop` | bool | Mantem loop do telegraph ate resolver |
| `AttackFinish` | trigger | Fecha ataque no turno de resolucao |
| `Moving` | bool | Locomotion |
| `MovePrep` / `MoveFinish` | trigger | Deslocamento por turno |
| `Idle` | trigger | Volta ao idle (`BossResetStateBehaviour` no fim dos ataques) |

### Estrutura da maquina (alto nivel)

- **Idle de combate**: `HOC_CombatIdle` (+ variantes Phase2 / defesa L/R com Prep-Loop-Finish)
- **Movimento**: sub-FSM `HOC_Movement` (Prep / Loop / Finish)
- **Ataques**: sub-FSM `HOC_Attacks` → `HOC_AttackChooser` → por `AttackId`:
  - `HOC_Protean`, `HOC_Circle`, `HOC_Swords` (FeatherLines), `HOC_WingsLeft`, `HOC_WingsRight`
  - Cada ataque: Prep → Loop (`AttackLoop`) → Finish (`AttackFinish`) → reset
- **Reset**: estado com `BossResetStateBehaviour` limpa flags e dispara `Idle`
- Clips extras: `HOC_IdleDefaultRest`, `HOC_PhaseTransition*`, `HOC_Phase2*` (fase 2 no asset, wiring no controller)

### Diferenca vs Laki

| | Hocari | Laki (`LKI_Animator`) |
|---|--------|----------------------|
| Telegraph ataque | `AttackId` + Prep/Loop/Finish | `Ability` one-shot |
| Idle de turno | `CombatIdle` fixo no controller | Sorteio `Idle_2` / `Idle_3` por turno |
| Movimento | Sub-FSM Movement | (sem movimento no LKI atual) |

## Bootstrap e carregamento automatico

- Builder/editor:
  - `TI6/Animation/Build Erzahler & Laki Animator Controllers`
  - Auto-build em load se `ERZ_ErzahlerBook.controller` nao existir
- SO de controllers:
  - `ErzahlerAnimatorControllers.asset`
  - copia em `Assets/Resources/ErzahlerAnimatorControllers.asset`
- Runtime:
  - `ErzahlerPlayerAnimatorDriver.InitializeDefaultController()` carrega `ErzahlerAnimatorControllersSO.LoadDefault()` se necessario

## Observacoes importantes

- `Laki_Ability` e corrigido para one-shot no builder (`loopTime = false`).
- O rig do livro do player (`ROOTBook`) pode ser ocultado quando clone esta ativo (`ErzahlerPlayerAnimatorDriver`).
- O sistema legado `AKY_AttackType/Execute/Cancel` continua com fallback quando drivers Erza nao estao presentes.
