public class CheatController: ICheatController {
    private bool _imortal;
    private bool _infinityCast;
    private bool _infinityMove;

    public bool Imortal => _imortal;
    public bool InfinityCast => _infinityCast;
    public bool InfinityMove => _infinityMove;

    public CheatController() {
        _imortal = false;
        _infinityCast = false;
        _infinityMove = false;
    }

    public void SetImortal(bool isImortal) {
        _imortal = isImortal;
    }

    public void SetInifinityMove(bool CanMoveInifinity) {
        _infinityMove = CanMoveInifinity;
    }

    public void SetInfinityCast(bool CanCastInfinity) {
        _infinityCast = CanCastInfinity;
    }

}
