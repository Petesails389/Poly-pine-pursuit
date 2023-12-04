using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button clientButton;
    [SerializeField] private Button hostButton;

    private bool host;

    // Start is called before the first frame update
    void Start()
    {
        
        clientButton.onClick.AddListener(()=> {
            NetworkManager.Singleton.StartClient();
        });
        hostButton.onClick.AddListener(()=> {
            NetworkManager.Singleton.StartHost();
        });
    }

    /*/ called second
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.SetActiveScene(scene);
        
        if (host){
            NetworkManager.Singleton.StartHost();
        } else {
            NetworkManager.Singleton.StartClient();
        }

        SceneManager.sceneLoaded -= OnSceneLoaded; //unsubsribe to scene events to prevent this being reference after scene unloaded
        SceneManager.UnloadSceneAsync("MainMenu"); //unload the scene
    }*/

}
