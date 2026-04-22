using UnityEngine;
using System.Collections;

public class CRTGlitch : MonoBehaviour
{
    public Material crtMaterial;
    private int tme = 0;

    public void Update()
    {
        tme++;
        if (tme == 350)
        {
            tme = -700;
            TriggerGlitch();
        }
    }
    public void TriggerGlitch()
    {
        StartCoroutine(GlitchRoutine());
    }

    IEnumerator GlitchRoutine()
    {
        crtMaterial.SetFloat("_ShakeTrigger", 1f);

        // lasts just a few frames
        yield return new WaitForSeconds(0.08f);

        crtMaterial.SetFloat("_ShakeTrigger", 0f);
    }
}