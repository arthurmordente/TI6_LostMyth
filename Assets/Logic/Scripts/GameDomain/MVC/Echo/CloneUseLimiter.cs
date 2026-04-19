using Logic.Scripts.Turns;

namespace Logic.Scripts.GameDomain.MVC.Echo {
	/// <summary>Limite de skills do Livro/clone: uma habilidade (slots 1–4) por turno do jogador; movimento e Dividir/TAB não passam aqui.</summary>
	public interface ICloneUseLimiter {
		bool CanUse();
		void MarkUsed();
		void ResetForPlayerTurn();
	}

	public class CloneUseLimiter : ICloneUseLimiter {
		private bool _usedThisPlayerTurn;

		public bool CanUse() {
			return !_usedThisPlayerTurn;
		}

		public void MarkUsed() {
			_usedThisPlayerTurn = true;
		}

		public void ResetForPlayerTurn() {
			_usedThisPlayerTurn = false;
		}
	}
}



