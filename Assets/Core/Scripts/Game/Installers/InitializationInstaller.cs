using Core.Scripts.Game.Infrastructure.Bootstrapper;
using Core.Scripts.Game.Infrastructure.Services.AssetProviderService;
using Core.Scripts.Game.Infrastructure.StateMachines.GameStateMachineMain;
using Core.Scripts.Game.Infrastructure.StateMachines.GameStateMachineMain.States;
using Zenject;

namespace Core.Scripts.Game.Installers
{
    public sealed class InitializationInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindHelpers();
            BindGameStateMachine();
            BindBootstrap();
        }

        private void BindHelpers()
        {
            Container.Bind<IAssetProvider>().To<AssetProvider>().AsSingle().NonLazy();
        }

        private void BindGameStateMachine()
        {
            Container.Bind<BootStrapState>().AsSingle();
            Container.Bind<LoadLevelState>().AsSingle();
            Container.Bind<GamePlayState>().AsSingle();

            Container.BindFactory<GameStateMachine, BootStrapState, BootStrapState.Factory>().AsSingle();
            Container.BindFactory<GameStateMachine, LoadLevelState, LoadLevelState.Factory>().AsSingle();
            Container.BindFactory<GameStateMachine, GamePlayState, GamePlayState.Factory>().AsSingle();

            Container.Bind<GameStateMachine>().AsSingle().NonLazy();
        }

        private void BindBootstrap()
        {
            Container.BindInterfacesTo<GameBootstrapper>().AsSingle().NonLazy();
        }
    }
}