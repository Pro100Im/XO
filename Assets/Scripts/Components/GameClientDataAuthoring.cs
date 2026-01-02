using Unity.Entities;
using UnityEngine;

namespace Components
{
    public class GameClientDataAuthoring : MonoBehaviour
    {
        public class Baker : Baker<GameClientDataAuthoring>
        {
            public override void Bake(GameClientDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new GameClientData());
            }
        }
    }

    public struct GameClientData : IComponentData
    {
        public PlayerType PlayerType;
    }
}