using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager gm;
    public static bool mobile = true;
    void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Application.targetFrameRate = 60;
    }
    public void Start()
    {
        if (gm == null)
        {
            gm = this;
            DontDestroyOnLoad(this);
        }
        else
            Destroy(this);
    }

    public void ChangeScene(int scene)
    {
        SceneManager.LoadScene(scene);
    }
}
