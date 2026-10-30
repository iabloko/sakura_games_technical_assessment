using Core.Scripts.Game.Gameplay.Animals;
using Core.Scripts.Game.Infrastructure.Bootstrapper;
using Core.Scripts.Game.Infrastructure.Loader;
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
            Container.BindInterfacesTo<AssetProvider>().AsSingle().NonLazy();
            Container.Bind<SceneLoader>().AsSingle();
            Container.Bind<IAnimalFactory>().To<AnimalFactory>().AsSingle();
        }

        private void BindGameStateMachine()
        {
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
