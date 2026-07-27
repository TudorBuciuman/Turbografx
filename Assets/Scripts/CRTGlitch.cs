using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class CRTGlitch : MonoBehaviour
{
    public Material crtMaterial;
    private int tme = 0;
    private bool botForInfoeducatie = false;

    private void Start()
    {
        crtMaterial.SetFloat("_ShakeTrigger", 0.01f);
    }
    public void Update()
    {
        tme++;
        if (tme == 350)
        {
            tme = -600;
            TriggerGlitch();
        }
        if(UTInput.GetButtonDown("C"))
        {
            FindFirstObjectByType<GameManager>().StoppingMusic(3);
            SceneManager.LoadScene(4);
        }
    }
    public void TriggerGlitch()
    {
        StartCoroutine(GlitchRoutine());
    }

    IEnumerator GlitchRoutine()
    {
        //crtMaterial.SetFloat("_ShakeTrigger", 1f);
        crtMaterial.SetFloat("_ShakeTrigger", 1.1f);

        yield return new WaitForSeconds(0.1f);

        crtMaterial.SetFloat("_ShakeTrigger", 0.01f);

        if(botForInfoeducatie)
        {
            yield return new WaitForSeconds(4f);
            SceneManager.LoadScene(1);
        }
    }
}