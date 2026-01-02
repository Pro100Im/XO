using RPCs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct ServerTestSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (simpleRPC, receiveRpcCommandReques, entity) in SystemAPI.Query<RefRO<SimpleRPC>, RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess())
        {
            Debug.Log($"simpleRPC = {simpleRPC.ValueRO.Value} :: entity = {entity.Index} :: receiveRpcCommandRequest {receiveRpcCommandReques.ValueRO.SourceConnection.Index}");

            entityCommandBuffer.DestroyEntity(entity);
        }

        entityCommandBuffer.Playback(state.EntityManager);
    }
}
