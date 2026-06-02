/// <summary>
/// Componente no root do Canvas de loadout na <c>ExplorationScene</c>.
/// Arrasta este script para o mesmo GameObject do canvas (ou substitui <see cref="ExplorationLoadoutUIView"/> no Inspector).
/// </summary>
/// <remarks>
/// Campos de detalhes expostos no Inspector:
/// <list type="bullet">
/// <item><description>Nome, divindade, tipo (Dano / Buff / Passiva / Movimento), descrição, poder, custo, alcance</description></item>
/// <item><description>Preview visual (background + frame + ícone)</description></item>
/// </list>
/// Seleção: outline dourado no <c>SkillFrame</c> selecionado (catálogo ou slot).
/// Assign inválido (buff no player, passiva no book): tremor leve no painel + slot.
/// </remarks>
public sealed class ExplorationLoadoutCanvasView : ExplorationLoadoutUIView, IExplorationLoadoutView
{
}
