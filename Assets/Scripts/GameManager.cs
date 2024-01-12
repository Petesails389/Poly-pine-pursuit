using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing.Scened;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    [SyncVar] private int seed = 0;
    private int readyPlayers;

    [SerializeField] private GameUI gameUI; //reference to the gameUI script
    [SerializeField] private MainMenuUI mainMenuUI; //reference to the Main Menu UI script

    [SerializeField] private TerrainGeneration terrainGeneration; //reference to the terrain generation script
    [SerializeField] private ConnectionManager connectionManager; //referecne to the connection manager

    [SerializeField] private int targetNumberOfPlayers; 

    private PlayerController playerController; //reference to the player controller of the local player

    enum GameState {
        None = 0,
        Loading = 1,
        Waiting = 2,
        Paused = 3,
        Playing = 4,
        Scoring = 5,
        Stopping = 6,
        Restarting = 7
    }

    private GameState currentGameState;

    private void ChangeGameState(GameState state) {
        currentGameState = state;
        
        switch (state) {
            case GameState.None:
                break;
            case GameState.Loading:
                StartLoading();
                break;
            case GameState.Waiting:
                StartWaiting();
                break;
            case GameState.Paused:
                StartPausing();
                break;
            case GameState.Playing:
                StartPlaying();
                break;
            case GameState.Scoring:
                break;
            case GameState.Stopping:
                StartStopping();
                break;
            case GameState.Restarting:
                if (base.IsServer) readyPlayers = 0;
                ChangeGameState(GameState.Loading); //start the game again
                break;

        } 
    }

    private void StartWaiting() {
        gameUI.SetPause(true,false,"Waiting for Players..."); //shows the pause UI
        //unlocks the cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    private void StartPlaying() {
        gameUI.SetPause(false); //hides the pause UI
        //locks the cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void StartPausing() {
        gameUI.SetPause(true); //shows the pause UI
        //unlocks the cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void StartLoading() {
        //swaps the UI to the game UI
        mainMenuUI.gameObject.SetActive(false);
        gameUI.gameObject.SetActive(true);

        if (base.IsServer) {
            Debug.Log("randomising");
            //randomises the seed if this person is hosting
            this.seed = UnityEngine.Random.Range(-100000,100000);
        }

        terrainGeneration.GenerateTerrain(); //generate the terrain
    }

    private void FinishLoading() {
        StartPausing(); //not actually pausing but the display should be the same for now
        ClientReady();
        ChangeGameState(GameState.Waiting);
    }

    [ObserversRpc]
    private void StopWaiting() {
        if (currentGameState == GameState.Waiting) {
            ChangeGameState(GameState.Playing);
        } 
    }

    private void WaitingCheck() {
        if ((readyPlayers == targetNumberOfPlayers && currentGameState == GameState.Waiting) 
        || currentGameState == GameState.Playing || currentGameState == GameState.Paused) {
            StopWaiting();
        }
    }

    private void StartStopping() {
        if (base.IsServer) {
            ServerStopRpc();
        } else {
            terrainGeneration.DestroyPopulation();
            FinishStopping();
        }
    }

    private  void FinishStopping() {
        //swap the UI back
        mainMenuUI.gameObject.SetActive(true);
        gameUI.gameObject.SetActive(false);

        connectionManager.StopNetwork();
        ChangeGameState(GameState.None); //set the GameState back to null as the game is no longer running
    }

    //toggles local pause
    public void ToggleLocalPause() {
        if (currentGameState == GameState.Playing) {
            ChangeGameState(GameState.Paused);
        } else if (currentGameState == GameState.Paused) {
            ChangeGameState(GameState.Playing);
        }
    }

    //gets and sets for the seed value
    public int GetSeed() {
        return seed;
    }

    [ServerRpc (RequireOwnership = false)]
    public void SetSeed(GameManager script, int value) {
        script.seed = value;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        //starts loading the game scene
        ChangeGameState(GameState.Loading);
        //finds the local player
        playerController = base.LocalConnection.FirstObject.gameObject.GetComponent<PlayerController>();
        Debug.Log(playerController);
        //no players are ready
        if (base.IsServer) readyPlayers = 0;
    }

    //called once per frame
    void Update() {
        switch (currentGameState) {
            case GameState.None:
                break;
            case GameState.Loading:
                break;
            case GameState.Waiting:
                break;
            case GameState.Paused:
                playerController.PausedUpdateCall();
                break;
            case GameState.Playing:
                playerController.PausedUpdateCall();
                playerController.UpdateCall();
                break;
            case GameState.Scoring:
                break;
            case GameState.Stopping:
                break;
            case GameState.Restarting:
                break;
        }
    }

    //called at a fixed time interval
    void FixedUpdate() {
        switch (currentGameState) {
            case GameState.None:
                break;
            case GameState.Loading:
                break;
            case GameState.Waiting:
                break;
            case GameState.Paused:
                break;
            case GameState.Playing:
                playerController.MovementUpdateCall();
                break;
            case GameState.Scoring:
                break;
            case GameState.Stopping:
                break;
            case GameState.Restarting:
                break;
        }
    }

    public void OnTerrainGenerationFisnished() {
        if (currentGameState != GameState.Loading) return; //if the game isn't loading don't finish loading it
        FinishLoading();
    }

    //called to stop the game for all client
    [ObserversRpc]
    private void ClientStopRpc() {
        if (currentGameState == GameState.Stopping) return; // if it's already stopping don't stop again
        ChangeGameState(GameState.Stopping);
    }

    //called to stop the game for a single client
    public void ClientStop() {
        if (currentGameState == GameState.Stopping) return; // if it's already stopping don't stop again
        ChangeGameState(GameState.Stopping);
    }

    //called to stop the game for all players 
    [ServerRpc (RequireOwnership = false)]
    private void ServerStopRpc() {
        ClientStopRpc();
        terrainGeneration.DestroyPopulation();
        FinishStopping();
    }

    //called by each client ONCE and once only to announce that they are ready to start. when this is equal to the number of players excpected then the game starts
    [ServerRpc (RequireOwnership = false)]
    private void ClientReady() {
        readyPlayers += 1;
        WaitingCheck();
    }
}
