# New Skill System Audit

## Skill type classification (current assets)

- `Faca` -> `Damage`
- `Laser` -> `Damage` (uses `Faca` behavior)
- `PilarDeFogo` -> `Damage` (uses `DanoEmArea`)
- `PilarDeFogoUpgrade` -> `Damage` (uses `PilarDeFogo2`)
- `BolaDeFogo` -> `Damage` (asset uses `Faca` script in current setup)
- `AreaDeFogo` -> `Damage` (asset uses `DanoEmArea` in current setup)
- `Cura` -> `SelfBuff`
- `CuraPorTurno` -> `SelfBuff`
- `Escudo` -> `SelfBuff`
- `BarreiraProtetiva` -> `SelfBuff` (uses `Shield` behavior)

## SkillDataSO attribute audit

### Essential now

- `SkillName`: shown in lobby/exploration catalog and slots.
- `Description`: shown in skill details.
- `Icon`: shown in catalog and slot HUD/UI.
- `Cost`: AP cost in cast flow and UI.
- `Power`: damage/heal magnitude.
- `Range`: projectile limit and targeting data.
- `AreaOfEffect`: AoE hit detection and preview radius.
- `AoEPrefab`: targeting AoE visual in cast flow.
- `AttackPrefab`: projectile/attack spawn on cast.

### Preparatory or partial use

- `CoolDown`: shown in UI but not enforced in current cast runtime.
- `CastTime`: reserved for channeling/cast timing, not enforced yet.
- `AoEPrefabBaseRadius`: used to normalize AoE preview scaling.
- `Upgrade`: legacy/evolution reference used by old flow and some assets.

### Candidate for future review

- `Upgrade`: verify if migration to loadout/type-driven progression will replace it.
- `CastTime`: decide if cast-time system will be implemented or field deprecated.
- `CoolDown`: decide if cooldown enforcement stays only visual or becomes gameplay rule.
