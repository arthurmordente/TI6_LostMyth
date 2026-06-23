# UI uGUI — passos no Unity Editor

Scripts já migram para uGUI. Falta ligar prefabs nas cenas (sem alterar YAML automaticamente).

## 1. CoreScene

- Remover instâncias de `Canvas_MainMenu` e `Canvas_PauseMenu` (se existirem).
- No `CoreLoadingScreen`: componente `LoadingScreenCanvasView` + filho `TipContainer`.
- Arrastar referências em `CoreInstaler`:
  - **Loading Screen View** → `CoreLoadingScreen`
  - **Loading Tip Pool** → `Assets/GameDesign/GameData/Loading/LoadingTipPool.asset`

### Loading screen com dicas (transições de cena)

Assets gerados automaticamente na primeira abertura do Unity (ou via menu **TI6 → Loading Screen → Create Tip Template Assets**):

| Asset | Caminho |
|-------|---------|
| Template de dica | `Assets/GameDesign/Prefabs/Ui/LoadingTips/LoadingTip_Template.prefab` |
| Pool de dicas | `Assets/GameDesign/GameData/Loading/LoadingTipPool.asset` |
| Host overlay | `Assets/GameDesign/Prefabs/Ui/CoreLoadingScreen.prefab` |

**Estrutura obrigatória de cada prefab de dica** (duplicar o template):

```
LoadingTip_Template          ← LoadingTipCanvasView
└── Panel                    ← fundo / moldura; pode ter Image e filhos decorativos
    ├── TipContent           ← content root modular (texto, imagens, layout groups…)
    │   ├── img_Icon         ← exemplo (opcional)
    │   └── txt_Tip          ← exemplo
    └── ContinuePrompt       ← INATIVO no prefab; ativado só quando a cena está pronta
        └── ContinueContent  ← content root do prompt (texto, ícones…)
            └── txt_Continue
```

`Panel`, `TipContent` e `ContinuePrompt` são **GameObjects** — podes combinar à vontade `Image`, `TextMeshProUGUI`, `Horizontal/VerticalLayoutGroup`, etc. O runtime só ativa/desativa `ContinuePrompt`.

**Workflow do artista**

1. Duplicar `LoadingTip_Template.prefab` (ex. `LoadingTip_Combos.prefab`).
2. Editar `TipContent` e/ou `Panel` (adicionar/remover imagens, textos, layouts).
3. Manter `ContinuePrompt` desativado e não remover `LoadingTipCanvasView`.
4. Adicionar o novo prefab à lista em `LoadingTipPool.asset`.

Para atualizar o template base após mudanças no gerador: **TI6 → Loading Screen → Regenerate Tip Template** (sobrescreve o template).

**Comportamento**

- Boot inicial (`CoreInitiator`): carrega `GameScene` em background, sem loading screen.
- Transições Lobby ↔ Exploration ↔ Gameplay: mostra dica aleatória do pool; carrega cena em background; quando pronta, ativa `ContinuePrompt`; input só é aceite depois do prompt visível.


## 2. LobbyScene

- Instanciar `Assets/Ui/UI_Jordan/Canvas_MainMenu.prefab`.
- Adicionar componente `LobbyMainMenuCanvasView` no root do canvas.
- `LobbyInstaller` → campo `Lobby Menu View`.

- Desativar/remover `LobbyView` (`UIDocument` legado).

## 3. ExplorationScene

No GameObject do **SceneContext** (com `ExplorationInstaller`):

| Campo no Inspector | O que arrastar |
|--------------------|----------------|
| **Loadout Menu View** | Root do canvas de loadout com componente `ExplorationLoadoutCanvasView` |
| **Pause Menu View** | Root do `Canvas_PauseMenu` com `PauseMenuCanvasView` |

### Canvas de loadout (NPC Oganjdan)

1. Instancia o teu prefab/canvas de loadout na `ExplorationScene`.
2. No **root do canvas**, adiciona o script **`ExplorationLoadoutCanvasView`** (substitui `ExplorationLoadoutUIView` no mesmo objeto, se já existia).
3. No Inspector do `ExplorationLoadoutUIView` / canvas, define **`Root Panel`** = painel principal do menu (fica `SetActive(false)` ao iniciar).
4. Arrasta esse root para **Loadout Menu View** no `ExplorationInstaller`.

O menu **inicia oculto** (`Awake` + `Init`). Só abre quando o jogador interage com o NPC (`OganjdanInteractable` → `OnSkillLoadoutInteractionCommand` → `Show()`).

**Drag-and-drop (equipar skills):** implementado em código — não é preciso prefab extra. Opcionalmente arrasta o **ScrollRect** do catálogo para `_catalogScrollRect` no `ExplorationLoadoutUIView` (desactiva scroll durante drag). Interacção: pointer down/click mostra detalhes; arrastar para slot Erza/Livro equipa; slots inválidos escurecem durante o arrasto.

**Painel de detalhes — custo:** no `ExplorationLoadoutUIView` / `ExplorationLoadoutCanvasView`, secção **Details — custo**:

| Campo | O que arrastar |
|-------|----------------|
| **Detail Cost Label Text** | TMP ao lado (`Custo:` / `Sem Custo`) |
| **Detail Cost Badge Image** | `Image` do ícone de mana (o sprite já traz o número desenhado) |
| **Detail Cost Badge Sprites** | Array de 4 sprites: `[0]` = custo 0, `[1]` = 1, `[2]` = 2, `[3]` = 3 |

Custo **0** (passivas incluídas — no `SkillDataSO` passivas têm sempre custo 0): rótulo **Sem Custo** + sprite `[0]`. Custo **1–3**: rótulo **Custo:** + sprite correspondente. Sem skill seleccionada: badge oculto.

### Canvas de pause

- Instanciar `Canvas_PauseMenu.prefab`, `PauseMenuCanvasView` no root, ligar em **Pause Menu View**.

- Remover qualquer `UIDocument` / Customize legado se ainda existir na cena.

## 4. GameplayScene

- Instanciar `Canvas_PauseMenu.prefab` + `PauseMenuCanvasView`.
- `GamePlayInstaller` → `Pause Menu View`, `Game Over View` (com `GameOverCanvasView`).

- Remover `PauseUi` e `GameOver` com `UIDocument`.

### HUD de combate — tinta + frame por skill

O runtime adiciona `SkillSlotVisualView` em cada `btn_Skill` e resolve ícone/frame/tinta via `SkillVisualCatalog` (GameInstaller). Para a **tinta** aparecer, cada botão de skill precisa de um filho `img_Paint`:

```
btn_Skill (Button — Image = frame)
├── img_Paint          ← Image, primeiro filho, stretch full rect
├── icon_Skill
├── icon_Mana
└── icon_Keybind
```

Repetir nos **8** botões (4 Erza + 4 Livro) em `Assets/Ui/UI_Jordan/Canvas_Gameplay.prefab`. Duplica a estrutura de `SkillFrame.prefab` (`img_Paint` atrás do frame) ou copia sprites do `SkillVisualCatalog`. Sem `img_Paint`, o HUD actualiza ícone + frame (Image do botão); a tinta fica omitida até existir a camada.

**Affordance (mana / cast único):** em runtime cada `btn_Skill` recebe `CanvasGroup` se ainda não existir. Slots sem recurso ficam com alpha `0.35` (igual ao loadout em drag inválido). Erza: mana insuficiente; Livro: cast único já gasto. Ao tentar castar sem recurso (click ou atalho), tremem o **icon_Mana** do slot e o **ícone de mana do jogador** (`ManaFlask` / frasco do Livro) — não o fill líquido nem o botão de skill inteiro.

**Passivas:** `icon_Keybind` / `img_Keybind` fica oculto (`IsCastable == false`). Opcionalmente preenche **Erza/Book Skill Keybind Display Roots** no `GamePlayUiCanvasView`; senão auto-resolve por nome dentro de cada `btn_Skill`.

## 5. GameScene

- No `GameInstaller` (SceneContext): adicionar `UniversalUiSceneViews` no mesmo GameObject ou filho.
- Overlays Options/Credits/Load/Guide/Cheats são criados em runtime se não houver refs; **iniciam sempre ocultos** até `ShowOptionsScreen`, `ShowCreditsScreen`, etc.
- Se usares canvases na cena, atribui em `UniversalUiSceneViews` e define **Root Panel** no Inspector de cada `*CanvasView` (filho `Panel`, não o root do canvas).

### Hierarquia recomendada (`UniversalUIViews.prefab`)

| Objeto | Estado inicial no prefab | Notas |
|--------|--------------------------|--------|
| `*CanvasView` (root do overlay) | **Ativo**, `localScale = (1,1,1)` | O script desliga o GO em `InitEntryPoint`; precisa de estar ativo para Awake/serialização. |
| Filho `Panel` (conteúdo) | **Ativo** (o código oculta via `Hide`) | Atribuir em **Root Panel** no `*CanvasView`. |
| Botões fora do `Panel` (ex. Close solto) | Evitar — meter tudo dentro de `Panel` | Se Close for irmão do Panel, ao esconder só o Panel o X fica visível. |

**Options:** no prefab atual o GO `OptionsCanvasView` costuma começar **inativo** com scale 0 — no Editor, passa a **ativo + scale 1**; o `HideUntilOpened` no arranque trata da visibilidade.

**Credits:** se **Root Panel** apontar para o próprio canvas, o código trata como overlay inteiro (equivalente a não ter filho Panel).

- Remover GameObjects com `OptionsUIView`, `CreditsUIView`, `LoadUIView`, `GuideUIView`, `CheatsUIView`.

## 6. Canvas sorting

- Pause: `sortingOrder` ≥ 100
- Overlays universais (GameScene): ≥ 200
- HUD combate: default

## 7. Teste rápido

1. Lobby → menu uGUI → Iniciar → Exploration
2. Exploration ESC → pause → Retornar ao Lobby → LobbyScene
3. Combate ESC → pause → Retornar ao Lobby → ExplorationScene
4. Config / Créditos / Fechar em cada contexto
