using Zenject;
using Logic.Scripts.GameDomain.MVC.Boss;
using Logic.Scripts.GameDomain.MVC.Environment.Orb;

namespace Logic.Scripts.Turns
{
    public class TurnsInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<TurnStateService>().AsSingle();
            Container.BindInterfacesAndSelfTo<ActionPointsService>().AsSingle();
            Container.BindInterfacesAndSelfTo<EchoService>().AsSingle();
			Container.BindInterfacesAndSelfTo<Logic.Scripts.GameDomain.MVC.Echo.CloneUseLimiter>().AsSingle();

			Container.BindInterfacesAndSelfTo<EnvironmentActorsRegistry>().AsSingle();
            Container.BindInterfacesAndSelfTo<EnviromentActionService>().AsSingle();
			// OrbEnvironmentRule desabilitada: orb agora é executada como IEnvironmentTurnActor

			// Publicar registro globalmente para acesso cross-container
			var envReg = Container.Resolve<IEnvironmentActorsRegistry>();
			EnvironmentActorsRegistryService.Instance = envReg;

			Container.BindInterfacesAndSelfTo<Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips.LakiChipRuntimeService>().AsSingle();
            Container.BindInterfacesAndSelfTo<TurnFlowController>().AsSingle();
        }
    }
}


