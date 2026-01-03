using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
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

        [GhostField] public PlayerType CurrentPlayablePlayerType;
    }

    public struct GameServerDataArraysCmponent : IComponentData
    {
        public NativeArray<PlayerType> PlayerTypeArray;
        public NativeArray<Line> LineArray;
    }

    public enum GameState
    {
        WaitingForPlayers = 0,
        GameInProgress = 1,
    }

    public struct Line
    {
        public int2 First;
        public int2 Second;
        public int2 Third;

        public Orientation Orientation;
    }

    public enum Orientation
    {
        Horizontal,
        Vertical,
        DiagonalA,
        DiagonalB,
    }
}