using Components;
using RPCs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct GameServerSystem : ISystem
{
    private const float GridStep = 3.1f;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntitiesReferencesComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var entitiesReferences = SystemAPI.GetSingleton<EntitiesReferencesComponent>();
        var entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
        var networkStreamInGameQuery = state.EntityManager.CreateEntityQuery(typeof(NetworkStreamInGame));
        var gameServerData = SystemAPI.GetSingletonRW<GameServerDataCmponent>();

        if (gameServerData.ValueRO.CurrentGameState == GameState.WaitingForPlayers && networkStreamInGameQuery.CalculateEntityCount() == 2)
        {
            Debug.Log("Both players connected. Starting the game...");

            gameServerData.ValueRW.CurrentPlayablePlayerType = PlayerType.Cross;
            gameServerData.ValueRW.CurrentGameState = GameState.GameInProgress;

            state.EntityManager.CreateEntity(typeof(GameStartedRPC), typeof(SendRpcCommandRequest));
        }

        foreach (var (clickOnGridPosRPC, receiveRpcCommandRequest, entity) in SystemAPI.Query<RefRO<ClickOnGridPosRPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            if (gameServerData.ValueRO.CurrentPlayablePlayerType != clickOnGridPosRPC.ValueRO.PlayerType)
            {
                entityCommandBuffer.DestroyEntity(entity);

                continue;
            }

            if (gameServerData.ValueRO.CurrentPlayablePlayerType == PlayerType.Cross)
                gameServerData.ValueRW.CurrentPlayablePlayerType = PlayerType.Circle;
            else
                gameServerData.ValueRW.CurrentPlayablePlayerType = PlayerType.Cross;

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
