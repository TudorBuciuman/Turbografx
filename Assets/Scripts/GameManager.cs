using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
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
        Debug.Log(newClip!=null);
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
}
