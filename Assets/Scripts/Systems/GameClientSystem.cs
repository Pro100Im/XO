using Components;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Systems
{
    [UpdateAfter(typeof(GoInGameClientSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    partial struct GameClientSystem : ISystem
    {
        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var onConnectedEvent in SystemAPI.Query<OnConnectedEvent>())
            {
                var gameClientData = SystemAPI.GetSingletonRW<GameClientDataComponent>();

                gameClientData.ValueRW.PlayerType = (onConnectedEvent.ConnectionId == 1) ? PlayerType.Cross : PlayerType.Circle;
            }

            foreach(var (gameStartedRPC, entity) in SystemAPI.Query<RefRO<GameStartedRPC>>().WithEntityAccess())
            {
                Debug.Log("Game started event received on client.");

                DotsEventsMono.Instance.RaiseOnGameStartedEvent();

                entityCommandBuffer.DestroyEntity(entity);
            }

            entityCommandBuffer.Playback(state.EntityManager);
        }
    }
}