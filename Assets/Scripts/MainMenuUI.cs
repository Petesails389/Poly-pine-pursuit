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
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        clientButton.onClick.AddListener(()=> {
            SceneManager.LoadScene("MainGame", LoadSceneMode.Additive);
            host = false;
        });
        hostButton.onClick.AddListener(()=> {
            SceneManager.LoadScene("MainGame", LoadSceneMode.Additive);
            host = true;
        });
    }

    // called second
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.SetActiveScene(scene);
        
        if (host){
            NetworkManager.Singleton.StartHost();
        } else {
            NetworkManager.Singleton.StartClient();
        }
        SceneManager.UnloadSceneAsync("MainMenu");
    }

}
