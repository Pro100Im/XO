using RPCs;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientTestSystem : ISystem
    {
        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if(Keyboard.current.cKey.wasPressedThisFrame)
            {
                Debug.Log("ClientTestSystem: C key was pressed.");

                var rpcEntity = state.EntityManager.CreateEntity();

                state.EntityManager.AddComponentData(rpcEntity, new SimpleRPC { Value = 42 });
                state.EntityManager.AddComponent<SendRpcCommandRequest>(rpcEntity);
            }
        }
    }
}