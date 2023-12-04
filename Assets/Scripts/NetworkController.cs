using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Networking;

public class NetworkController : NetworkBehaviour
{
    private NetworkVariable<int> seed = new NetworkVariable<int>(0);

    void Start() {
        if (IsHost) {
            seed.Value = 5;
        }
        Debug.Log("Start:" + seed.Value);
    }

    public override void OnNetworkSpawn() {
        Debug.Log("OnNetworkSpawn:" + seed.Value);
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.T)) {
            seed.Value = Random.Range(0,10);
        }
        //Debug.Log(OwnerClientId + ": " + seed.Value);
    }
}

