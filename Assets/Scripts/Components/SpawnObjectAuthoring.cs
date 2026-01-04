using Unity.Entities;
using UnityEngine;

public class SpawnObjectAuthoring : MonoBehaviour
{
    public class Baker : Baker<SpawnObjectAuthoring>
    {
        public override void Bake(SpawnObjectAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<SpawnObjectComponent>(entity);
        }
    }
}

public struct SpawnObjectComponent : IComponentData
{
   
}
