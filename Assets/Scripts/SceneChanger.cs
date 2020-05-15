using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    // Start is called before the first frame update
    /*void Start()
    {
        
    }*/

    public void ARScene()
    {
        SceneManager.LoadScene("ARView");
    }

    public void HomeScene()
    {
        SceneManager.LoadScene("HomeView");
    }

    public void MapScene()
    {
        SceneManager.LoadScene("MapView");
    }
}
