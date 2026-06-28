using Logic.Scripts.GameDomain.Services.Skills;
using Logic.Scripts.Turns;
using UnityEngine;
using Zenject;

[CreateAssetMenu(
    fileName = "GrantTemporaryApTurnGainEffect",
    menuName = "ScriptableObjects/Skills/Effects/GrantTemporaryActionPointsTurnGain")]
public class GrantTemporaryActionPointsTurnGainSkillEffectSO : SkillEffectSO
{
    [SerializeField] int _bonusPerTurn = 1;
    [SerializeField] int _durationPlayerTurns = 2;

    public override void Execute(in SkillExecutionContext context)
    {
        IActionPointsService actionPoints = ResolveActionPointsService();
        actionPoints?.GrantTemporaryGainPerTurnBonus(_bonusPerTurn, _durationPlayerTurns);
    }

    static IActionPointsService ResolveActionPointsService()
    {
        var sceneCtxs = Object.FindObjectsByType<SceneContext>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneCtxs.Length; i++)
        {
            SceneContext sceneContext = sceneCtxs[i];
            if (sceneContext == null) continue;
            try
            {
                return sceneContext.Container.Resolve<IActionPointsService>();
            }
            catch
            {
            }
        }

        return null;
    }
}
