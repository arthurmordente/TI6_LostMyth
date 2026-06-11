# Laki Arena Tiles — Implementação e overlay de ícones

Referência da roleta de tiles da Laki (16 sectores × 2 bandas). Complementa `AnimationInventory_StateMachines.md`.

---

## Visão geral

Cada tile tem:

1. **Superfície 3D** — mesh sector anular + material `SHDRMAT_Roulette{Positive,Neutral,Negative}`
2. **Overlay uGUI world-space** — ícones empilhados (`AbilityEffect.TileIcon`), montados em runtime após cada sorteio

```
Tile_XX
  ├── TileSurface (mesh 3D)
  └── TileInfoCanvas (World Space Canvas)
        └── SlotsContainer (VerticalLayoutGroup, centrado)
              └── Icon × N (Image only, empilhados)
```

---

## Ficheiros-chave

| Papel | Ficheiro |
|-------|----------|
| Bootstrap | `LakiArenaBossBootstrap.cs` |
| Estado + reroll + apply | `RouletteArenaService.cs` |
| Render + UI | `LakiRouletteArenaView.cs` |
| Fim de turno | `LakiRouletteArenaActor.cs` + `TurnFlowController.cs` |
| Contagem por fase | `LakiArenaPhaseTileDispositionSO.cs` |
| Layouts weighted | `TileLayoutDef.cs` |
| AP adiado | `LakiArenaTileActionPointsBridge.cs` |
| Ícone por efeito | `AbilityEffect.TileIcon` |

Prefab boss: `GameDesign/Prefabs/Bosses/Laki/LKI_LakiPrefabBoss.prefab`

---

## Lógica de efeitos

1. **Cor** (`TileEffectType`) — shuffle em `RerollTiles`; fase boss via `LakiArenaPhaseTileDispositionSO`
2. **Efeitos** — pools no bootstrap + `TileTypeLayoutConfig` por cor; sorteio → `_assignedEffects[tileIndex][]`
3. **Visual** — `RefreshTileCanvas` itera `GetTileAssignedEffects(i)` e empilha `TileIcon` de cada efeito

### Fluxo de turno

```
Jogador termina → Echoes → Laki EOT
  → ExecuteApplyPhaseAsync (emphasis + apply player/book)
  → delay → Boss resolve → delay
  → ExecuteRerollPhaseAsync (shuffle + RerollTiles + RefreshFrom)
  → próximo turno
```

---

## Overlay de ícones (implementado)

| Caso | Visual |
|------|--------|
| 1 efeito | 1 ícone centrado |
| 2 efeitos | 2 ícones empilhados, spacing `_stackSpacing` |
| Sem `TileIcon` | Slot omitido + `Debug.LogWarning` |

Parâmetros em `LakiRouletteArenaView` (Inspector quando expostos no bootstrap):

- `_stackedIconSize` (~420) — tamanho base do ícone
- `_stackSpacing` (~36) — gap vertical entre ícones
- `_iconSizeOuterScale` — multiplicador na banda outer (band 1)

Ordem vertical: primeiro slot do layout **em cima**, segundo **embaixo**.

---

## Art — TileIcon no prefab boss

Cada efeito nos pools de `LakiArenaBossBootstrap` deve ter `TileIcon` assignado.

| Efeito (exemplo) | Sprite sugerido |
|------------------|-----------------|
| Heal (+10/+20 HP) | `Ui/Images/Luana/Icons_Skills/iconSkill_Heal.png` |
| Damage (-10/-20 HP) | `Ui/Images/Luana/Icons_Skills/DanoCaster_Icon.png` |
| AP +/- | `Ui/Images/Luana/Icons_Skills/Mana_Icon.png` |

**GUIDs quebrados no prefab (verificar no Editor):**

- `856d3e726be9055409ababe2a81d8ed2` — AP (missing) → assign `Mana_Icon`
- `995624d19f0faec47ae51b40b76485de` — damage (missing) → assign `DanoCaster_Icon`

Heal (`f18e00465fefb7b4db6702a67ed18621`) → `iconSkill_Heal.png` OK.

Abrir `LKI_LakiPrefabBoss` → `LakiArenaBossBootstrap` → pools de efeitos → campo **Tile Icon** em cada entrada.

Validar no Unity: **TI6 → Laki → Validate Tile Icons on Boss Prefab** (lista efeitos sem ícone).

---

## Superfície 3D (inalterada)

- Prefabs: `Boss/Visuals/GeneratedPrefabs/LakiArenaTiles/LakiRoulette_{Inner|Outer}_{Neutral|Positive|Negative}`
- Regenerar: **Tools → Boss → Generate Laki Arena Roulette Meshes & Prefabs Only**

---

## Checklist de teste

1. Tile 1 efeito — ícone centrado, sem label
2. Tile 2 efeitos — stack compacto
3. Reroll — ícones reflectem efeitos sorteados
4. Inner vs outer — legível (ajustar `_iconSizeOuterScale` se necessário)
5. Gameplay — todos os efeitos assignados aplicam-se ao pisar a tile
