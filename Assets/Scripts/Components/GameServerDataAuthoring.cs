using Unity.Entities;
using UnityEngine;

namespace Components
{
    public class GameServerDataAuthoring : MonoBehaviour
    {
        public class Baker : Baker<GameServerDataAuthoring>
        {
            public override void Bake(GameServerDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new GameServerDataCmponent());
            }
        }
    }

    public struct GameServerDataCmponent : IComponentData
    {
        public GameState CurrentGameState;
        public PlayerType CurrentPlayablePlayerType;
    }

    public enum GameState
    {
        WaitingForPlayers = 0,
        GameInProgress = 1,
    }
}