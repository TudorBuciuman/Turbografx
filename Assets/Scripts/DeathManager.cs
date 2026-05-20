using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeathManager : MonoBehaviour
{
    public GameObject text1;
    public GameObject text2;
    public Image heart;
    private int index = 0;
    void Start()
    {
        GameManager.gm.ChangeMusic("Music/All of the lights");
        StartCoroutine(GameOver());
    }

    public IEnumerator GameOver()
    {
        yield return new WaitForSeconds(2);
        text1.SetActive(true);
        yield return new WaitForSeconds(4);
        while (!UTInput.GetButtonDown("Z"))
        {
            yield return null;
        }
            yield return StartCoroutine(Choice());
    }
    public IEnumerator Choice()
    {
        text1.SetActive(false);
        yield return new WaitForSeconds(0.2f);
        text2.SetActive(true);
        heart.rectTransform.anchoredPosition = new Vector3(-65.5f, 16f, 0);
        heart.gameObject.SetActive(true);
        while (!UTInput.GetButtonDown("Z"))
        {
            if (UTInput.GetAxisDown("Vertical") < 0)
            {
                if (index < 2)
                {
                    index++;
                    HeartMove();
                }
            }
            else if(UTInput.GetAxisDown("Vertical") > 0)
            {
                if (index > 0)
                {
                    index--;
                    HeartMove();
                }
            }
            yield return null;
        }
        if (index == 2)
        {
            Application.Quit();
        }
        else if (index == 1)
        {
            //Save
            GameManager.gm.PlaySong("Music/Idioteque");
            GameManager.gm.ChangeScene(0);

        }
        else if (index == 0)
        {
            GameManager.gm.PlaySong("Music/Idioteque");
            GameManager.gm.ChangeScene(1);
        }
        yield return null;
    }
    public void HeartMove()
    {
        switch (index)
        {
            case 0:
                heart.rectTransform.anchoredPosition = new Vector3(-65.5f, 16f, 0);
                break;
            case 1:
                heart.rectTransform.anchoredPosition = new Vector3(-65.5f, 0, 0);
                break;
            case 2:
                heart.rectTransform.anchoredPosition = new Vector3(-65.5f, -16, 0);
                break;
            default:
                break;
        }

    }
}
