using Unity.NetCode;

namespace RPCs
{
    public struct GameWinRPC : IRpcCommand
    {
        public PlayerType Winner;
    }
}