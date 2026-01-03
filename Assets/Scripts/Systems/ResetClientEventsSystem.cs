using Unity.Burst;
using Unity.Entities;

namespace Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup), OrderLast = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    partial struct ResetClientEventsSystem : ISystem
    {
        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (onConnectedEvent, entity) in SystemAPI.Query<RefRO<OnConnectedEvent>>().WithEntityAccess())
            {
                DotsEventsMono.Instance.RaiseOnClientConnectedEvent(onConnectedEvent.ValueRO.ConnectionId);

                entityCommandBuffer.DestroyEntity(entity);
            }

            entityCommandBuffer.Playback(state.EntityManager);
        }
    }
}