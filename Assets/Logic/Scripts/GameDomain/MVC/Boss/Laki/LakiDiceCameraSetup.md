# Laki dice — Animation Event (Editor manual)

O spawn do dado da Laki espera um **Animation Event** no clip de throw antes de instanciar o dado e mover a câmera para o follow.

## Passos no Unity Editor

1. Abre o **Animator** da Laki (controller usado em combate).
2. Localiza o estado/clips **`Laki_ThrowDie_*`** (Prep / Loop / transição de release).
3. Seleciona o clip onde a mão **liberta o dado** (frame visível de release).
4. Na janela **Animation**, adiciona um **Animation Event** nesse frame.
5. Função: **`OnDiceReleaseAnimationEvent`**
6. O evento chama `LakiBossAnimationBridge.OnDiceReleaseAnimationEvent` no GameObject que tem o componente `LakiBossAnimationBridge` (normalmente no prefab/root do boss).

## Fallback

Se o evento não existir, `DiceAttackSession` spawna o dado após **3 segundos** e regista um warning no Console.

## Câmera

`LakiDiceCameraBridge` (bind no `GamePlayInstaller`):

| Fase | Comportamento |
|------|----------------|
| `OnDiceAttackBegan` | Foco na Laki (blend ~0.5s) |
| Spawn do dado do boss | Follow no `DiceActor` |
| Dado do boss aterra | Volta ao jogador (antes do turno dele) |
| Início do turno do jogador (`OnDicePlayerTurnOpening`) | Foco no jogador |
| Spawn do dado do jogador | Follow no `DiceActor` |
| Dado do jogador aterra | Após **0,3 s**, volta ao jogador |
| `OnDiceAttackEnded` / dismiss scoreboard | Restaura follow default |

Pan (MMB) e rotação (RMB) ficam bloqueados durante o lease cinemático.
