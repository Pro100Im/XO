using Components;
using RPCs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    partial struct GoInGameClientSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame>();

            state.RequireForUpdate(state.GetEntityQuery(entityQueryBuilder));
            entityQueryBuilder.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (networkId, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithEntityAccess())
            {
                entityCommandBuffer.AddComponent<NetworkStreamInGame>(entity);

                var rpcEntity = state.EntityManager.CreateEntity();

                entityCommandBuffer.AddComponent<GoInGameRequestRPC>(rpcEntity);
                entityCommandBuffer.AddComponent<SendRpcCommandRequest>(rpcEntity);

                var onConnectedEventEntity = entityCommandBuffer.CreateEntity();
                entityCommandBuffer.AddComponent(onConnectedEventEntity, new OnConnectedEvent { ConnectionId = networkId.ValueRO.Value });
            }

            entityCommandBuffer.Playback(state.EntityManager);
        }
    }
}