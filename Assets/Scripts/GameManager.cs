using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

[RequireComponent(typeof(AudioSource))]
public class GameManager : MonoBehaviour
{
    public static GameManager gm;
    public static bool mobile = false;
    void Awake()
    {
#if !UNITY_ANDROID
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
#endif

        Application.targetFrameRate = 60;
    }
    public void Start()
    {
        if (gm == null)
        {
            gm = this;
            DontDestroyOnLoad(this);
            GameObject gameObject = new GameObject("FadeCanvas", typeof(Canvas));
            gameObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.GetComponent<Canvas>().sortingOrder = 2000;
            gameObject.transform.position = Vector3.zero;
            gameObject.transform.localScale = new Vector3(1f / 48f, 1f / 48f, 1f);
            Instantiate(Resources.Load<GameObject>("UI/FadeObj"), gameObject.transform).name = "FadeObj";
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(this.gameObject);
    }

    public void ChangeScene(int scene)
    {
        SceneManager.LoadScene(scene);
    }
    public void PlaySong(string song)
    {
        AudioSource audio = gm.GetComponent<AudioSource>();
        AudioClip newClip = Resources.Load<AudioClip>(song);
        audio.clip = newClip;
        audio.Play();
    }
    public void ChangeMusic(string song)
    {
        StartCoroutine(ChangeMusicCoroutine(song, 2));
    }
    private IEnumerator ChangeMusicCoroutine(string song, float duration)
    {
        AudioSource audio = GetComponent<AudioSource>();
        float startVolume = audio.volume;

        float time = 0f;
        while (time < duration)
        {
            audio.volume = Mathf.Lerp(startVolume, 0f, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        audio.volume = 0f;

        AudioClip newClip = Resources.Load<AudioClip>(song);
        audio.clip = newClip;
        audio.Play();
        time = 0f;
        while (time < duration)
        {
            audio.volume = Mathf.Lerp(0f, startVolume, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        audio.volume = startVolume;
    }

    public void DisablePlayerMovement()
    {
        if (FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None) != null)
        {
            FindFirstObjectByType<PlayerMovement>().canMove=false;
        }
    }
    public void EnablePlayerMovement()
    {
        if (FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None) != null)
        {
            FindFirstObjectByType<PlayerMovement>().canMove = true;
        }
    }
    private Vector2 spawnPos;
    private Vector2 spawnDir;
    private int currentsc;
    private int zone;
    private bool newSceneFadeIn = false;
    public void LoadArea(int sceneName, bool fadeIn, Vector2 pos, byte dir)
    {
        currentsc = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        GameObject.Find("Main Camera").name = "Camera";
        spawnPos = pos;
        zone = sceneName;
        if (dir == 0)
        {
            //up
            spawnDir = new Vector2(0, 1);
        }
        else if (dir == 1)
        {
            //right
            spawnDir = new Vector2(1, 0);
        }
        else if (dir == 2)
        {
            //left
            spawnDir = new Vector2(0, -1);
        }
        else
        {
            //down
            spawnDir = new Vector2(-1, 0);
        }
        newSceneFadeIn = fadeIn;
        SceneManager.sceneLoaded += OnAreaLoaded;
    }
    private void OnAreaLoaded(Scene ascene, LoadSceneMode aMode)
    {
        SceneManager.sceneLoaded -= OnAreaLoaded;
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(zone));
        SceneManager.UnloadSceneAsync(currentsc);
        GameObject gameObject = GameObject.Find("FadeObj");
        if (newSceneFadeIn)
        {
            gameObject.GetComponent<Fade>().FadeIn(30);
        }
        if ((bool)GameObject.Find("Player"))
        {
            if ((bool)GameObject.Find("Player").GetComponent<PlayerMovement>())
            {
                GameObject.Find("Player").GetComponent<PlayerMovement>().HandleSpawn(spawnPos, spawnDir);
            }
        }
        //savePointSpawn = false;
        //PlayMusic(nextOWSong);
        foreach (CanvasManager a in FindObjectsByType<CanvasManager>(FindObjectsSortMode.None))
        {
            a.ChangeCamera();
            a.GetComponent<Canvas>().worldCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        }
        EnablePlayerMovement();
    }

}
