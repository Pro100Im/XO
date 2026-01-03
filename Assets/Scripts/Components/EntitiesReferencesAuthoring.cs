using Unity.Entities;
using UnityEngine;

namespace Components
{
    public class EntitiesReferencesAuthoring : MonoBehaviour
    {
        public GameObject CrossPrefab;
        public GameObject CirclePrefab;
        public GameObject LineWinnerPrefab;

        public class Baker : Baker<EntitiesReferencesAuthoring>
        {
            public override void Bake(EntitiesReferencesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new EntitiesReferencesComponent
                {
                    CrossPrefabEntity = GetEntity(authoring.CrossPrefab, TransformUsageFlags.Dynamic),
                    CirclePrefabEntity = GetEntity(authoring.CirclePrefab, TransformUsageFlags.Dynamic),
                    LineWinnerPrefabEntity = GetEntity(authoring.LineWinnerPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }

    public struct EntitiesReferencesComponent : IComponentData
    {
        public Entity CrossPrefabEntity;
        public Entity CirclePrefabEntity;
        public Entity LineWinnerPrefabEntity;
    }
}