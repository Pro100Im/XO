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
    private const int GridWidthHeight = 3;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntitiesReferencesComponent>();
        state.RequireForUpdate<GameServerDataCmponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var entitiesReferences = SystemAPI.GetSingleton<EntitiesReferencesComponent>();
        var entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
        var networkStreamInGameQuery = state.EntityManager.CreateEntityQuery(typeof(NetworkStreamInGame));
        var gameServerData = SystemAPI.GetSingletonRW<GameServerDataCmponent>();

        if (gameServerData.ValueRO.CurrentGameState == GameState.WaitingForPlayers && networkStreamInGameQuery.CalculateEntityCount() == 2)
        {
            gameServerData.ValueRW.CurrentPlayablePlayerType = PlayerType.Cross;
            gameServerData.ValueRW.CurrentGameState = GameState.GameInProgress;

            var gameServerDataEntity = SystemAPI.GetSingletonEntity<GameServerDataCmponent>();
            var lineArray = new NativeArray<Line>(8, Allocator.Persistent);

            lineArray[0] = new Line
            {
                First = new int2(0, 0),
                Second = new int2(1, 0),
                Third = new int2(2, 0),
                Orientation = Orientation.Horizontal
            };

            lineArray[1] = new Line
            {
                First = new int2(0, 1),
                Second = new int2(1, 1),
                Third = new int2(2, 1),
                Orientation = Orientation.Horizontal
            };

            lineArray[2] = new Line
            {
                First = new int2(0, 2),
                Second = new int2(1, 2),
                Third = new int2(2, 2),
                Orientation = Orientation.Horizontal
            };

            lineArray[3] = new Line
            {
                First = new int2(0, 0),
                Second = new int2(0, 1),
                Third = new int2(0, 2),
                Orientation = Orientation.Vertical
            };

            lineArray[4] = new Line
            {
                First = new int2(1, 0),
                Second = new int2(1, 1),
                Third = new int2(1, 2),
                Orientation = Orientation.Vertical
            };

            lineArray[5] = new Line
            {
                First = new int2(2, 0),
                Second = new int2(2, 1),
                Third = new int2(2, 2),
                Orientation = Orientation.Vertical
            };

            lineArray[6] = new Line
            {
                First = new int2(0, 0),
                Second = new int2(1, 1),
                Third = new int2(2, 2),
                Orientation = Orientation.DiagonalA
            };

            lineArray[7] = new Line
            {
                First = new int2(0, 2),
                Second = new int2(1, 1),
                Third = new int2(2, 0),
                Orientation = Orientation.DiagonalB
            };

            state.EntityManager.AddComponentData(gameServerDataEntity, new GameServerDataArraysCmponent
            {
                PlayerTypeArray = new NativeArray<PlayerType>(GridWidthHeight * GridWidthHeight, Allocator.Persistent),
                LineArray = lineArray
            });

            state.EntityManager.CreateEntity(typeof(GameStartedRPC), typeof(SendRpcCommandRequest));
        }

        foreach (var (clickOnGridPosRPC, receiveRpcCommandRequest, entity) in SystemAPI.Query<RefRO<ClickOnGridPosRPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            if (gameServerData.ValueRO.CurrentPlayablePlayerType != clickOnGridPosRPC.ValueRO.PlayerType)
            {
                entityCommandBuffer.DestroyEntity(entity);

                continue;
            }

            var gameServerDataArrays = SystemAPI.GetSingletonRW<GameServerDataArraysCmponent>();

            if (gameServerDataArrays.ValueRO.PlayerTypeArray[GetFlatGridIndex(clickOnGridPosRPC.ValueRO.X, clickOnGridPosRPC.ValueRO.Y)] != PlayerType.None)
            {
                entityCommandBuffer.DestroyEntity(entity);

                continue;
            }

            gameServerDataArrays.ValueRW.PlayerTypeArray[GetFlatGridIndex(clickOnGridPosRPC.ValueRO.X, clickOnGridPosRPC.ValueRO.Y)] = clickOnGridPosRPC.ValueRO.PlayerType;


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

            CheckForWinner(gameServerDataArrays.ValueRO, gameServerData, entityCommandBuffer, entitiesReferences);
        }

        entityCommandBuffer.Playback(state.EntityManager);
    }

    private float3 GetPositionFromGridPos(int x, int y)
    {
        return new float3(-GridStep + x * GridStep, -GridStep + y * GridStep, 0);
    }

    private int GetFlatGridIndex(int x, int y)
    {
        return x + y * GridWidthHeight;
    }

    private bool IsWinningLine(PlayerType first, PlayerType second, PlayerType third)
    {
        return first != PlayerType.None && first == second && second == third;
    }

    private void CheckForWinner(GameServerDataArraysCmponent data, RefRW<GameServerDataCmponent> gameServerDataCmponent, EntityCommandBuffer entityCommandBuffer, EntitiesReferencesComponent entitiesReferencesComponent)
    {
        foreach (var line in data.LineArray)
        {
            if (IsWinningLine(
                data.PlayerTypeArray[GetFlatGridIndex(line.First.x, line.First.y)], 
                data.PlayerTypeArray[GetFlatGridIndex(line.Second.x, line.Second.y)], 
                data.PlayerTypeArray[GetFlatGridIndex(line.Third.x, line.Third.y)]))
            {
                gameServerDataCmponent.ValueRW.CurrentPlayablePlayerType = PlayerType.None;

                var entityLineWinner = entityCommandBuffer.Instantiate(entitiesReferencesComponent.LineWinnerPrefabEntity);
                var wordlPos = GetPositionFromGridPos(line.Second.x, line.Second.y);
                var eluerZ = 0f;

                wordlPos.z = -0.1f;

                switch(line.Orientation)
                {
                    case Orientation.Horizontal:

                        eluerZ = 0f;
                        break;
                    case Orientation.Vertical:
                        eluerZ = 90f;
                        break;
                    case Orientation.DiagonalA:
                        eluerZ = 45f;
                        break;
                    case Orientation.DiagonalB:
                        eluerZ = -45f;
                        break;
                }

                entityCommandBuffer.SetComponent(entityLineWinner, LocalTransform.FromPositionRotation(wordlPos, quaternion.RotateZ(eluerZ * math.TORADIANS)));
                //entityCommandBuffer.SetComponent(entityLineWinner, new LocalTransform { Position = wordlPos, Rotation = quaternion.RotateZ(eluerZ * math.TORADIANS), Scale = 1f });
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (SystemAPI.HasSingleton<GameServerDataArraysCmponent>())
        {
            var gameServerDataArrays = SystemAPI.GetSingleton<GameServerDataArraysCmponent>();

            gameServerDataArrays.PlayerTypeArray.Dispose();
            gameServerDataArrays.LineArray.Dispose();
        }
    }
}
