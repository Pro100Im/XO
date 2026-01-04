using Components;
using RPCs;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

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

            foreach (var (gameStartedRPC, entity) in SystemAPI.Query<RefRO<GameStartedRPC>>().WithEntityAccess())
            {
                DotsEventsMono.Instance.RaiseOnGameStartedEvent();

                entityCommandBuffer.DestroyEntity(entity);
            }

            foreach (var (gameWinRPC, entity) in SystemAPI.Query<RefRO<GameWinRPC>>().WithEntityAccess())
            {
                DotsEventsMono.Instance.RaiseOnGameWinEvent(gameWinRPC.ValueRO.Winner);

                entityCommandBuffer.DestroyEntity(entity);
            }

            foreach (var (rematchRPC, entity) in SystemAPI.Query<RefRO<RematchRPC>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                DotsEventsMono.Instance.RaiseOnGameRematchEvent();

                entityCommandBuffer.DestroyEntity(entity);
            }

            foreach (var (gameTieRPC, entity) in SystemAPI.Query<RefRO<GameTieRPC>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                DotsEventsMono.Instance.RaiseOnGameTieEvent();

                entityCommandBuffer.DestroyEntity(entity);
            }

            entityCommandBuffer.Playback(state.EntityManager);
        }
    }
}