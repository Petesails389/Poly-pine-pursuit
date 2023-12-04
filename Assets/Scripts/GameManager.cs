using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;

    private NetworkVariable<int> seed = new NetworkVariable<int>(default,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Start() {
        //makes this game object persistant throughout scene changes
        DontDestroyOnLoad(this.gameObject);
    }

    public override void OnNetworkSpawn() {
        //subscirbes to scene events
        NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;

        if (IsHost) {
            //randomises the seed if this person is hosting
            seed.Value = Random.Range(-100000,100000);
            //Starts the main game scene
            NetworkManager.Singleton.SceneManager.LoadScene("MainGame", LoadSceneMode.Single);

            //calls the base
            base.OnNetworkSpawn();
        }
    }

    //this is called by the netwwork sceneManager antime there is a scene event
    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
    {
        //runs on host when the game scene first loads:
        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted && IsHost) {
            GameObject.Find("Terrain").GetComponent<TerrainGeneration>().GenerateTerrain();
            SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
        }
        //runs on client when the game scene is synced
        if (sceneEvent.SceneEventType == SceneEventType.SynchronizeComplete && !IsHost) {
            GameObject.Find("Terrain").GetComponent<TerrainGeneration>().GenerateTerrain();
            SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    //spawns a player for a newly joined client as a player object
    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(ulong clientId){
        GameObject player = Instantiate(playerPrefab);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }

    //returns the seed for this game
    public int GetSeed() {
        return seed.Value;
    }
}
