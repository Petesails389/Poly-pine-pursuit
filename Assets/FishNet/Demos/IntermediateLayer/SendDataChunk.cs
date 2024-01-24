using FishNet.Object;
using UnityEngine;

namespace FishNet.Example.IntermediateLayers
{
    public class SendDataChunk : NetworkBehaviour
    {
        private byte[] _data;

        private void Awake()
        {
            //Make a repeating large byte array.
            _data = new byte[20000];
            for (int i = 0; i < _data.Length; i++)
            {
                byte mod = (byte)(i % 256);
                _data[i] = mod;
            }
        }

        private void Update()
        {
            //Send RPCs every second on tick.
            if (!base.TimeManager.FrameTicked || (base.TimeManager.LocalTick % base.TimeManager.TickRate != 0))
                return;

            if (base.IsServerInitialized)
                ObsRpc();
            //if (base.IsClientInitialized)
                //SvrRpc(_data, "IntermediateLayers work both ways! This was sent to the server, while the first message was sent to clients.");
        }

        [ServerRpc(RequireOwnership = false)]
        private void SvrRpc(byte[] data, string txt)
        {
            Debug.Log($"Received SvrRpc. DataLen {data.Length}. Text {txt}");
        }

        [ObserversRpc]
        private void ObsRpc()
        {
            //Debug.Log($"Received ObsRpc. DataLen {data.Length}. Text {txt}");
            Debug.Log($"Received ObsRpc.");
        }

    }
}