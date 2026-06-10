# Clips de animação sem hook de gameplay

Animações que **existem nos FBX** (ou em export) mas **não entram no loop de gameplay** atual — sem parâmetro de animator, sem chamada em runtime, ou sem `BossAttack` correspondente.

Para inventário completo do que **está ligado**, ver `AnimationInventory_StateMachines.md`.

---

## Menus Unity (resumo)

| Passo | Menu |
|-------|------|
| 1 — Exportar FBX | `TI6 → Animation → 1 Export FBX Clips → Erzahler / Laki / Hocari` |
| 2 — Rebuild controllers | `TI6 → Animation → 2 Build State Machines → All` (ou Erzahler / Laki / Hocari individual) |
| 3 — Hocari prefab (opcional) | `TI6 → Animation → 2 Build State Machines → Hocari — Assign to HokariBoss prefab` |

O build usa clips já exportados em `Exported/` (e pastas core de Erza/Laki). Estados opcionais (Death, Hit, Bet, ThrowDie, etc.) só aparecem no controller **se** o `.anim` existir após o export.

---

## Erzahler (`ErzahlerFinal.fbx`)

**Todos os clips do FBX têm destino** em pelo menos um dos controllers FINAL (`ERZ_ErzahlerBook_FINAL`, `ERZ_Erzahler_FINAL`, `ERZ_Book_FINAL`) ou em estados opcionais (Death, Hit, Bet, Conjuring Fail, Divide).

| Clip FBX | Situação |
|----------|----------|
| Locomoção / conjuring / book core | Pastas `erz+book`, `Erzahler`, `Book` (controllers core) |
| `Erzahler_Death`, `Hit`, `BetWon`, `BetLost`, `Conjuring_Fail` | Estados opcionais no player (se exportados) |
| `Book_CreateClone`, `Book_ReturnClone` | Divide ability (se exportados) |

**Não listado aqui:** clips faciais / viseme (outros assets, fora do pipeline de combate).

---

## Laki (`MadameLakiAnimations.fbx`)

| Clip | Motivo |
|------|--------|
| `Laki_Idle_Singing` | Performance alternativa; o boss usa sorteio `Idle_2` / `Idle_3` + `Ability`, sem hook para singing |

**Ligados ao gameplay:** `Laki_Idle_1`, `Idle_2/3` (Prep/Loop/Finish), `Laki_Ability`, `Laki_Death`, `Laki_Hit_LoseBet`, `Laki_BetWon`, `Laki_ThrowDie_*`.

---

## Hocari (`HocariAnimations1.fbx`, `HocariAnimations2.fbx`, `HOC_Hocari.fbx`)

### Sem `AttackId` / ataque no código

| Clips | Notas |
|-------|-------|
| `Hocari_Attack_Donut_Prep/Loop/Finish` (fase 1) | Exportados em `Phase1/`; **não** entram em `HOC_Hocari_FINAL` |
| `Hocari_Phase2_Attack_Donut_Prep/Loop/Finish` (fase 2) | Exportados em `Phase2/`; idem |
| Todo o FBX legado `HOC_Hocari.fbx` (prefixo `HOC_*`) | Rig antigo; só 2 clips de defesa direita P1 em `Legacy/`; resto substituído por Anim1/Anim2 |

### Idle / rest / defense fora do combat loop

| Clip | Motivo |
|------|--------|
| `Hocari_IdleDefaultRest_2` (Anim1) | Idle de repouso; combat usa `Hocari_CombatIdle_2` / `Hocari_Phase2_CombatIdle` |
| `HOC_IdleDefaultRest` (legacy) | Mesmo papel no rig antigo |
| `Hocari_CombatIdle_LeftDefense_*` (fase 1) | Defesa idle removida do `HOC_Hocari_FINAL` |
| `Hocari_Phase2_CombatIdle_LeftDefense_*` / `RightDefense_*` | Idem |
| `HOC_CombatIdle*Defense*` (legacy `HOC_Hocari.fbx`) | Rig antigo; sem `DefenseSide` no controller |

### Exportados mas não usados no controller unificado

| Clips | Motivo |
|-------|--------|
| Donut (6 clips acima) | Não há `BossAttack` Donut nem `AttackId` 5 em `BossController` |

---

## Como atualizar esta lista

1. Correr **Export FBX** da personagem.
2. Comparar pasta `Exported/` (ou `FromAnim1` / `FromAnim2`) com estados no builder (`*AnimatorControllerBuilder.cs`).
3. Se um clip novo no FBX não aparecer em nenhum builder nem em `AnimationInventory_StateMachines.md`, acrescentar aqui com o motivo.
