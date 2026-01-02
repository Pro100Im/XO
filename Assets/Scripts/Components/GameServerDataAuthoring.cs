using Unity.Entities;
using UnityEngine;

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
    public PlayerType CurrentPlayablePlayerType;
}
