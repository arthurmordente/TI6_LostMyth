using UnityEngine;

public class GameOverCommandData
{
    public readonly bool IsWin;
    public readonly Animator DeathAnimator;

    public GameOverCommandData(bool isWin, Animator deathAnimator = null) {
        IsWin = isWin;
        DeathAnimator = deathAnimator;
    }
}
