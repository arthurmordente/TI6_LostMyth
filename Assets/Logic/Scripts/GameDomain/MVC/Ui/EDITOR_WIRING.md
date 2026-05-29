# UI uGUI — passos no Unity Editor

Scripts já migram para uGUI. Falta ligar prefabs nas cenas (sem alterar YAML automaticamente).

## 1. CoreScene

- Remover instâncias de `Canvas_MainMenu` e `Canvas_PauseMenu` (se existirem).
- No `CoreLoadingScreen` (ou equivalente): substituir `LoadingScreenView` + `UIDocument` por `LoadingScreenCanvasView`.
- Arrastar referência em `CoreInstaler` → `Loading Screen View`.

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

### Canvas de pause

- Instanciar `Canvas_PauseMenu.prefab`, `PauseMenuCanvasView` no root, ligar em **Pause Menu View**.

- Remover qualquer `UIDocument` / Customize legado se ainda existir na cena.

## 4. GameplayScene

- Instanciar `Canvas_PauseMenu.prefab` + `PauseMenuCanvasView`.
- `GamePlayInstaller` → `Pause Menu View`, `Game Over View` (com `GameOverCanvasView`).

- Remover `PauseUi` e `GameOver` com `UIDocument`.

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
