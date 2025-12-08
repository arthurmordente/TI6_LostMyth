public interface ICheatController {
    bool Imortal { get; }
    bool InfinityCast { get; }
    bool InfinityMove { get; }
    void SetImortal(bool isImortal);
    void SetInfinityCast(bool CanMoveInifinity);
    void SetInifinityMove(bool CanMoveInifinity);
}
