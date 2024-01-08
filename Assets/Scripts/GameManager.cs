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

    private bool localPause = true;
    private NetworkVariable<bool> globalPause = new NetworkVariable<bool>(true,
    NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private GameUI gameUI; //reference to the gameUI script

    void Start() {
        //makes this game object persistant throughout scene changes
        DontDestroyOnLoad(this.gameObject);
    }

    public override void OnNetworkSpawn() {
        //subscirbes to scene events
        NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
        //subscribes to global pause changes
        globalPause.OnValueChanged += OnGlobalPauseChange;

        //insures all variables are as they should be
        gameUI = null;

        if (IsHost) {
            //randomises the seed if this person is hosting
            seed.Value = Random.Range(-100000,100000);
            //Starts the main game scene
            NetworkManager.Singleton.SceneManager.LoadScene("MainGame", LoadSceneMode.Single);

            //calls the base
            base.OnNetworkSpawn();
        }

        if (IsClient) {
            
        }
    }

    //this is called by the netwwork sceneManager antime there is a scene event
    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent)
    {
        //runs on host when the game scene first loads:
        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted && IsHost) {
            gameUI = GameObject.Find("GameUI").GetComponent<GameUI>(); //find the UI
            GameObject.Find("Terrain").GetComponent<TerrainGeneration>().GenerateTerrain(); //Generate the Terrain

            //places the player, spawns its netowrk object and keeps a reference to it's controller
            GameObject player = Instantiate(playerPrefab);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);

            globalPause.Value = true;
            ToggleGlobalPauseServerRpc(); //unpauses the game
            ToggleLocalPause();
        }
        //runs on the server when the game scene is loaded
        if (sceneEvent.SceneEventType == SceneEventType.SynchronizeComplete && IsHost) {
            GameObject player = Instantiate(playerPrefab);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(sceneEvent.ClientId);

            //calls a client RPC on the client that just joined to do it's startup things 
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{sceneEvent.ClientId}
            }
            };

            OnPlayerSpawnedClientRpc(clientRpcParams);

        }
        //runs on client when the game scene is synced
        if (sceneEvent.SceneEventType == SceneEventType.SynchronizeComplete && !IsHost) {
            GameObject.Find("Terrain").GetComponent<TerrainGeneration>().GenerateTerrain(); //Generate the terrain
        }
    }

    [ClientRpc]
    //runs on client once the player has been spawned - this is when everything is now ready for the client to start - should be shortly after the scene sync completes
    public void OnPlayerSpawnedClientRpc(ClientRpcParams clientRpcParams = default) {
        if (IsOwner) return;

        gameUI = GameObject.Find("GameUI").GetComponent<GameUI>(); //find the UI
        ToggleLocalPause(); //unpauses the game
    }

    //returns if the game should be paused or not
    public bool IsPaused() {
        return globalPause.Value || localPause;
    }

    //toggles local pause
    public void ToggleLocalPause() {
        localPause = !localPause;
        gameUI.SetPause(IsPaused());
        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>().CursorLockUpdate();
    }

    //toggles globalPause
    [ServerRpc]
    private void ToggleGlobalPauseServerRpc() {
        globalPause.Value = !globalPause.Value;
    }

    private void OnGlobalPauseChange(bool previous, bool current) {
        gameUI.SetPause(IsPaused());
        NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerController>().CursorLockUpdate();
    }

    //returns the seed for this game
    public int GetSeed() {
        return seed.Value;
    }

    //handles Quitting the game
    public void Quit() {
        if (IsHost) {
            DisconnectClientRpc(); //disconnects all clients
        }
        else {
            //unless this is a client in which case
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }
    }

    //disconnects a player or client and loads the main menue scene
    [ClientRpc]
    public void DisconnectClientRpc(ClientRpcParams clientRpcParams = default) {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}
