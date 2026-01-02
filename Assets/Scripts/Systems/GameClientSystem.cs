using Components;
using Unity.Burst;
using Unity.Entities;

namespace Systems
{
    [UpdateAfter(typeof(GoInGameClientSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    partial struct GameClientSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach(var onConnectedEvent in SystemAPI.Query<OnConnectedEvent>())
            {
                var gameClientData = SystemAPI.GetSingletonRW<GameClientData>();

                gameClientData.ValueRW.PlayerType = (onConnectedEvent.ConnectionId == 1) ? PlayerType.Cross : PlayerType.Circle;
            }
        }
    }
}