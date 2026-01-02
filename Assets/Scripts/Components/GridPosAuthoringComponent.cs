using Unity.Entities;
using UnityEngine;

namespace Components
{
    public class GridPosAuthoringComponent : MonoBehaviour
    {
        public int X;
        public int Y;

        public class Baker : Baker<GridPosAuthoringComponent>
        {
            public override void Bake(GridPosAuthoringComponent authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var gridPos = new GridPosComponent
                {
                    X = Mathf.RoundToInt(authoring.X),
                    Y = Mathf.RoundToInt(authoring.Y)
                };

                AddComponent(entity, gridPos);
            }
        }
    }

    public struct GridPosComponent : IComponentData
    {
        public int X;
        public int Y;
    }
}