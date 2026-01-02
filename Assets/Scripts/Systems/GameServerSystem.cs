using Components;
using RPCs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct GameServerSystem : ISystem
{
    private const float GridStep = 3.1f;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntitiesReferencesComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entitiesReferences = SystemAPI.GetSingleton<EntitiesReferencesComponent>();
        var entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (clickOnGridPosRPC, receiveRpcCommandRequest, entity) in SystemAPI.Query<RefRO<ClickOnGridPosRPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            var playerPrefab = clickOnGridPosRPC.ValueRO.PlayerType == PlayerType.Circle
                ? entitiesReferences.CirclePrefabEntity
                : entitiesReferences.CrossPrefabEntity;
            var playerObjectEntity = entityCommandBuffer.Instantiate(playerPrefab);
            var worldPos = GetPositionFromGridPos(clickOnGridPosRPC.ValueRO.X, clickOnGridPosRPC.ValueRO.Y);

            entityCommandBuffer.SetComponent(playerObjectEntity, LocalTransform.FromPosition(worldPos));
            entityCommandBuffer.DestroyEntity(entity);
        }

        entityCommandBuffer.Playback(state.EntityManager);
    }

    private float3 GetPositionFromGridPos(int x, int y)
    {
        return new float3(-GridStep + x * GridStep, -GridStep + y * GridStep, 0);
    }
}
