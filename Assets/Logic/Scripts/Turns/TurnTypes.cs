using Zenject;

namespace Logic.Scripts.Turns
{
    public enum TurnMode
    {
        Inactive = 0,
        Active = 1
    }

    public enum TurnPhase
    {
        None = 0,
        BossAct = 1,
        PlayerAct = 2,
        EnviromentAct = 4
    }
}
