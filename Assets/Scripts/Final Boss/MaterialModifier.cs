using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MaterialModifier : MonoBehaviour
{
    public static Material Turbografx;
    public static Material Turbographics;
    [SerializeField]
    private bool isBleeding = false;
    public const float TransitionTime = 3;
    private bool tgfxSet = true;
    private bool canChange = false;

    public void Start()
    {
        SetMaterial();
        if (SceneManager.GetActiveScene().name == "Bunker1")
            canChange = true;
        else
            canChange = false;
    }
    public void Update()
    {
        if (!canChange) return;

        if (UTInput.GetButtonDown("C"))
        {
            ChangeShader();
        }
    }
    public void SetMaterial()
    {
        Turbografx = Resources.Load<Material>("Material/Turbografx");
        Turbographics = Resources.Load<Material>("Material/Turbographics");
        SetTurbografx();
        if(isBleeding)
        this.GetComponent<MaterialModifier>().SetBleeding();
    }
    /* Normal values
     * scanline intensity = 0.106
     * screen curvature = 0.044
     * vignette = 1.29
     * color bleed = 0.00339
     * noise = 0
     * flicker = 0.0887
     */
    public static void SetTurbografx()
    {
        Turbografx.SetFloat("_ScanlineIntensity", 0.106f);
        Turbografx.SetFloat("_ScreenCurvature", 0.044f);
        Turbografx.SetFloat("_Vignette", 1.29f);
        Turbografx.SetFloat("_ColorBleed", 0.00339f);
        Turbografx.SetFloat("_Noise", 0f);
        Turbografx.SetFloat("_Flicker", 0.0887f);
    }

    public void SetBleeding()
    {
        StartCoroutine(SetBleedingCoroutine());
    }
    public IEnumerator SetBleedingCoroutine()
    {
        float time = 0f;
        while (time < TransitionTime)
        {
            time += Time.deltaTime;
            Turbografx.SetFloat("_ColorBleed", Mathf.Lerp(0.00339f, 0.005f, time/TransitionTime));
            Turbografx.SetFloat("_Vignette", Mathf.Lerp(1.29f, 2.0f, time/TransitionTime));
            yield return null;
        }
    }
    private void OnApplicationQuit()
    {
        if (Turbografx != null)
            SetTurbografx();
    }
    public void ChangeShader()
    {
        if(!tgfxSet)
            FindFirstObjectByType<RawImage>().material = Turbografx;
        else
            FindFirstObjectByType<RawImage>().material = Turbographics;
        tgfxSet = !tgfxSet;
    }
}
