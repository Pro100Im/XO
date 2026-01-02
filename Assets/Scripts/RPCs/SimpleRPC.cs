using Unity.NetCode;
using UnityEngine;

namespace RPCs
{
    public struct SimpleRPC : IRpcCommand
    {
        public int Value;
    }
}