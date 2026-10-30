// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Cysharp.Threading.Tasks;
// using Game.Infrastructure.Services.AssetProviderService;
// using Game.Infrastructure.StateMachines.BaseData;
// using NUnit.Framework;
// using UnityEngine.TestTools;
//
// namespace Game.Infrastructure.StateMachines.StateMachineTests
// {
//     public sealed class UIAsyncStateMachineBaseTests
//     {
//         private IAssetProvider _assetProviderMock;
//         private TestStateMachine _stateMachine;
//
//         [SetUp]
//         public void Setup()
//         {
//             _assetProviderMock = new AssetProvider();
//             _stateMachine = new TestStateMachine(_assetProviderMock);
//             
//             _stateMachine.States = new Dictionary<Type, IAsyncExitState>
//             {
//                 { typeof(MockAsyncState), _exitStateMock.Object }
//             };
//         }
//
//         [UnityTest]
//         public IEnumerator OpenOrEnter_StateIsEntered()
//         {
//             // Arrange
//             bool enteredCalled = false;
//             _exitStateMock.Setup(state => state.Enter()).Returns(UniTask.CompletedTask);
//
//             // Act
//             yield return _stateMachine.OpenOrEnter<MockAsyncState>(() => enteredCalled = true).ToCoroutine();
//
//             // Assert
//             _exitStateMock.Verify(state => state.Enter(), Times.Once);
//             
//             Assert.IsTrue(enteredCalled);
//         }
//
//         [UnityTest]
//         public IEnumerator EnterAsync_StateTransition()
//         {
//             // Arrange
//             _exitStateMock.Setup(state => state.Exit()).Returns(UniTask.CompletedTask);
//             _exitStateMock.Setup(state => state.Enter()).Returns(UniTask.CompletedTask);
//
//             // Act
//             yield return _stateMachine.EnterAsync<MockAsyncState>().ToCoroutine();
//
//             // Assert
//             _exitStateMock.Verify(state => state.Exit(), Times.Once);
//             _exitStateMock.Verify(state => state.Enter(), Times.Once);
//         }
//
//         [UnityTest]
//         public IEnumerator CloseStateMachine_StateIsClosed()
//         {
//             // Arrange
//             bool closedCalled = false;
//             _exitStateMock.Setup(state => state.Exit()).Returns(UniTask.CompletedTask);
//
//             // Act
//             yield return _stateMachine.CloseStateMachine(() => closedCalled = true).ToCoroutine();
//
//             // Assert
//             _exitStateMock.Verify(state => state.Exit(), Times.Once);
//             Assert.IsTrue(closedCalled);
//         }
//
//         [UnityTest]
//         public IEnumerator CloseStateMachine_OnStateMachineClosedEventTriggered()
//         {
//             // Arrange
//             bool eventTriggered = false;
//             _stateMachine.OnStateMachineClosed += () => eventTriggered = true;
//
//             // Act
//             yield return _stateMachine.CloseStateMachine().ToCoroutine();
//
//             // Assert
//             Assert.IsTrue(eventTriggered);
//         }
//
//         // Вспомогательный класс для тестирования
//         private sealed class TestStateMachine : UIAsyncStateMachineBase
//         {
//             protected override string MainViewPath => "path/to/view";
//
//             public TestStateMachine(IAssetProvider assetProvider) : base(assetProvider) { }
//
//             public override void SetUIStateView(UIAsyncPayloadView view) { }
//
//             protected override void SubscribeEvents() { }
//             protected override void UnSubscribeEvents() { }
//         }
//
//         // Вспомогательный класс для Mock
//         private class MockAsyncState : IAsyncState { }
//     }
// }
