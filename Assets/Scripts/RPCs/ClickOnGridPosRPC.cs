using Unity.NetCode;

namespace RPCs
{
    public struct ClickOnGridPosRPC : IRpcCommand
    {
        public int X;
        public int Y;
        public PlayerType PlayerType;
    }
}