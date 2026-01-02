using Components;
using RPCs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    partial struct GridPosClickClientSystem : ISystem
    {
        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
                var collisionWorld = physicsWorld.CollisionWorld;

                var mousePosition = Mouse.current.position;
                var worldPosition = (float3)Camera.main.ScreenToWorldPoint(mousePosition.value);
                var raycastInput = new RaycastInput
                {
                    Start = worldPosition,
                    End = worldPosition + new float3(0, 0, 100),
                    Filter = CollisionFilter.Default
                };

                if (!collisionWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit raycastHit))
                    return;

                if (SystemAPI.HasComponent<GridPosComponent>(raycastHit.Entity))
                {
                    var gameClientData = SystemAPI.GetSingleton<GameClientData>();
                    var gridPos = SystemAPI.GetComponent<GridPosComponent>(raycastHit.Entity);
                    var rpcEntity = state.EntityManager.CreateEntity(typeof(ClickOnGridPosRPC), typeof(SendRpcCommandRequest));

                    state.EntityManager.SetComponentData(rpcEntity, new ClickOnGridPosRPC
                    {
                        X = gridPos.X,
                        Y = gridPos.Y,
                        PlayerType = gameClientData.PlayerType
                    });
                }
            }
        }
    }
}