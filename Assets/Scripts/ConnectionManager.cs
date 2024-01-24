using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;

public class ConnectionManager : MonoBehaviour
{
    enum State {
        None = 0,
        Starting = 1,
        Connecting = 2,
        Running = 3,
        Disconecting = 4,
        Stopping = 5,
        Stopped = 6
    }

    private State currentState = State.Stopped;

    private void ChangeState(State state) {
        currentState = state;

        switch (currentState) {
            case State.None:
                break;
            case State.Starting:
                StartStarting();
                break;
            case State.Connecting:
                StartConnecting();
                break;
            case State.Running:
                break;
            case State.Disconecting:
                StartDisconecting();
                break;
            case State.Stopping:
                StartStopping();
                break;
            case State.Stopped:
                break;
        }
    }

    private void StartStarting() {
        FishNet.InstanceFinder.NetworkManager.ServerManager.StartConnection();
    }

    private void StartConnecting() {
        FishNet.InstanceFinder.NetworkManager.ClientManager.StartConnection();
    }

    private void StartDisconecting() {
        FishNet.InstanceFinder.NetworkManager.ClientManager.StopConnection();
    }

    private void StartStopping() {
        if (!FishNet.InstanceFinder.NetworkManager.IsServerStarted) {
            //if not a server it must be stopped already so set current state to stopped
            ChangeState(State.Stopped);
        }
        else if (currentState == State.Stopping
        && FishNet.InstanceFinder.NetworkManager.ServerManager.Clients.Count == 0)
        {
            //if the server is stopping and none is connected shutdown the server
            FinishStopping();
        }
        //if clients are still connected wait for the callbacks signalling that all clients have discconected
    }

    private void FinishStopping() {
        FishNet.InstanceFinder.NetworkManager.ServerManager.StopConnection(true);
        ChangeState(State.Stopped);
    }

    void Start () {
        FishNet.InstanceFinder.NetworkManager.ServerManager.OnServerConnectionState += OnServerConnectionChange;
        FishNet.InstanceFinder.NetworkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        FishNet.InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState += OnClientConnectionChange;
    }

    private void OnServerConnectionChange(ServerConnectionStateArgs serverState){
        if (serverState.ConnectionState == LocalConnectionState.Started) {
            ChangeState(State.Connecting); //connect the client after the server has loaded
        } else if (serverState.ConnectionState == LocalConnectionState.Stopped) {
            ChangeState(State.Stopped); //after server stopped set state to stopped
        }
    }

    private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs remoteState) {
        //called when someone connects or disconnects
        if (!FishNet.InstanceFinder.NetworkManager.IsServerStarted) return; //return if they are not the server
        if (currentState == State.Stopping 
        && remoteState.ConnectionState == RemoteConnectionState.Stopped 
        && FishNet.InstanceFinder.NetworkManager.ServerManager.Clients.Count == 0) {
            //if the server is stopping and the last person just discconected
            FinishStopping();
        }
    }

    private void OnClientConnectionChange(ClientConnectionStateArgs clientState){
        if (clientState.ConnectionState == LocalConnectionState.Started) {
            ChangeState(State.Running); //after client connected the system is running
            if (FishNet.InstanceFinder.NetworkManager.IsServerStarted) {
            }
        } else if (clientState.ConnectionState == LocalConnectionState.Stopped) {
            ChangeState(State.Stopping); //after the client disconnects stop the server
        }
    }

    public void StartNetwork(bool host) {
        if (currentState != State.Stopped) return; //if the network isn't already stopped then stop don't start it
        if (host) {
            //if hosting start the server
            ChangeState(State.Starting);
        } else {
            //else go straight to connecting the client
            ChangeState(State.Connecting);
        }
    }

    public void StopNetwork() {
        if (currentState != State.Running) return; //if the network isn't running then don't stop it
        ChangeState(State.Disconecting);
    }
}
