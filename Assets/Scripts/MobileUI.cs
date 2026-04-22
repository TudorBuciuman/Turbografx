using System;
using UnityEngine;
using UnityEngine.UI;

public class MobileUI : MonoBehaviour
{
    private GameObject[] dpad;

    private GameObject[] buttons;

    private string[] buttonLetters;

    private Vector2[] dOrigPos;

    private Vector2[] dNewPos;

    private bool dPadEnabled;

    private Vector2[] bOrigPos;

    private Vector2[] bNewPos;

    private bool[] buttonEnabled;

    [SerializeField]
    private GameObject selfPrefab;

    private int frames;

    private bool isPlaying;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        frames = 0;
        isPlaying = false;
        dpad = new GameObject[2];
        dOrigPos = new Vector2[2];
        dNewPos = new Vector2[2];
        dPadEnabled = false;
        buttons = new GameObject[3];
        buttonLetters = new string[3];
        bOrigPos = new Vector2[3];
        bNewPos = new Vector2[3];
        buttonEnabled = new bool[3];
        int num = 0;
        int num2 = 0;
        for (int i = 0; i < base.transform.childCount; i++)
        {
            GameObject gameObject = base.transform.GetChild(i).gameObject;
            if (gameObject.name.StartsWith("DPAD") || gameObject.name.StartsWith("PAD"))
            {
                dpad[num] = gameObject;
                if (gameObject.name == "DPAD")
                {
                    gameObject.GetComponent<Image>().enabled = false;
                }
                num++;
            }
            else if (gameObject.name.EndsWith("BUTTON"))
            {
                buttons[num2] = gameObject;
                buttonLetters[num2] = buttons[num2].name[0].ToString();
                gameObject.GetComponent<Image>().enabled = false;
                num2++;
            }
        }
    }

    private void Start()
    {
        for (int i = 0; i < 2; i++)
        {
            dOrigPos[i] = dpad[i].transform.localPosition;
            dNewPos[i] = dOrigPos[i] - new Vector2(320f, 0f);
            dpad[i].transform.localPosition = dNewPos[i];
        }
        for (int j = 0; j < 3; j++)
        {
            bOrigPos[j] = buttons[j].transform.localPosition;
            bNewPos[j] = bOrigPos[j] + new Vector2(320f, 0f);
            buttons[j].transform.localPosition = bNewPos[j];
        }
    }

    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }
        float num = 10f;
        frames++;
        float num2 = (float)frames / num;
        num2 = Mathf.Sin(num2 * (float)Math.PI * 0.5f);
        for (int i = 0; i < dpad.Length; i++)
        {
            if (dpad[i].name == "DPAD")
            {
                dpad[i].GetComponent<Image>().enabled = true;
            }
            if (dPadEnabled)
            {
                dpad[i].transform.localPosition = Vector2.Lerp(dpad[i].transform.localPosition, dOrigPos[i], num2);
                continue;
            }
            dpad[i].transform.localPosition = Vector2.Lerp(dpad[i].transform.localPosition, dNewPos[i], num2);
            if ((float)frames == num && dpad[i].name == "DPAD")
            {
                dpad[i].GetComponent<Image>().enabled = false;
            }
        }
        for (int j = 0; j < buttons.Length; j++)
        {
            buttons[j].GetComponent<Image>().enabled = true;
            if (buttonEnabled[j])
            {
                buttons[j].transform.localPosition = Vector2.Lerp(buttons[j].transform.localPosition, bOrigPos[j], num2);
                continue;
            }
            buttons[j].transform.localPosition = Vector2.Lerp(buttons[j].transform.localPosition, bNewPos[j], num2);
            if ((float)frames == num)
            {
                buttons[j].GetComponent<Image>().enabled = false;
            }
        }
        if ((float)frames == num)
        {
            frames = 0;
            isPlaying = false;
        }
    }

    private void LateUpdate()
    {
    }

    public void EnableButtons(bool dPadEnabled, bool z, bool x, bool c, bool instant)
    {
        frames = -1;
        if (instant)
        {
            frames = 9;
        }
        isPlaying = true;
        this.dPadEnabled = dPadEnabled;
        buttonEnabled = new bool[3] { z, x, c };
    }

    public void ApplyButtonColors()
    {
        if (PlayerPrefs.HasKey("ButtonStyle"))
        {
            Image[] componentsInChildren;
            if (PlayerPrefs.GetInt("ButtonStyle", 0) == 1)
            {
                componentsInChildren = GetComponentsInChildren<Image>();
                foreach (Image image in componentsInChildren)
                {
                    if (image.gameObject.name.StartsWith("DPAD"))
                    {
                        image.color = new Color(0.392f, 0.91f, 0.91f, 0.39f);
                    }
                    else if (image.gameObject.name.StartsWith("Z"))
                    {
                        image.color = new Color(0f, 1f, 0f, 0.39f);
                    }
                    else if (image.gameObject.name.StartsWith("X"))
                    {
                        image.color = new Color(1f, 0.28f, 0.28f, 0.39f);
                    }
                    else if (image.gameObject.name.StartsWith("C"))
                    {
                        image.color = new Color(1f, 1f, 0f, 0.39f);
                    }
                }
                return;
            }
            if (PlayerPrefs.GetInt("ButtonStyle", 0) == 2)
            {
                componentsInChildren = GetComponentsInChildren<Image>();
                foreach (Image image2 in componentsInChildren)
                {
                    if (image2.gameObject.name.StartsWith("DPAD"))
                    {
                        image2.color = new Color(1f, 0.65f, 0f, 0.39f);
                    }
                    else if (image2.gameObject.name.StartsWith("Z"))
                    {
                        image2.color = new Color(0.84f, 0.21f, 0.851f, 0.39f);
                    }
                    else if (image2.gameObject.name.StartsWith("X"))
                    {
                        image2.color = new Color(1f, 0f, 0f, 0.39f);
                    }
                    else if (image2.gameObject.name.StartsWith("C"))
                    {
                        image2.color = new Color(0f, 0.635f, 0.91f, 0.39f);
                    }
                }
                return;
            }
            if (PlayerPrefs.GetInt("ButtonStyle", 0) == 3)
            {
                componentsInChildren = GetComponentsInChildren<Image>();
                foreach (Image image3 in componentsInChildren)
                {
                    if (image3.gameObject.name.StartsWith("DPAD"))
                    {
                        image3.color = new Color(1f, 0f, 0f, 0.39f);
                    }
                    else if (image3.gameObject.name.StartsWith("Z"))
                    {
                        image3.color = new Color(0f, 1f, 1f, 0.39f);
                    }
                    else if (image3.gameObject.name.StartsWith("X"))
                    {
                        image3.color = new Color(1f, 0f, 1f, 0.39f);
                    }
                    else if (image3.gameObject.name.StartsWith("C"))
                    {
                        image3.color = new Color(1f, 1f, 0f, 0.39f);
                    }
                }
                return;
            }
            if (PlayerPrefs.GetInt("ButtonStyle", 0) == 4)
            {
                componentsInChildren = GetComponentsInChildren<Image>();
                foreach (Image image4 in componentsInChildren)
                {
                    if (image4.gameObject.name != "Widescreen" && image4.gameObject.name != "BlackScreen")
                    {
                        image4.color = new Color(1f, 1f, 1f, 0f);
                    }
                }
                return;
            }
            PlayerPrefs.SetInt("ButtonStyle", 0);
            componentsInChildren = GetComponentsInChildren<Image>();
            foreach (Image image5 in componentsInChildren)
            {
                if (image5.gameObject.name != "PAD_CTRL" && image5.gameObject.name != "Reticle" && image5.gameObject.name != "Widescreen" && image5.gameObject.name != "BlackScreen")
                {
                    image5.color = new Color(1f, 1f, 1f, 0.39f);
                }
            }
        }
        else
        {
            PlayerPrefs.SetInt("ButtonStyle", 0);
        }
    }

    public void ApplyButtonGFX()
    {
        if (PlayerPrefs.HasKey("DPADStyle"))
        {
            string[] array = new string[21]
            {
                "base", "base_neg", "xbone", "xbone_neg", "nin82", "nin82_neg", "nin91", "nin91_neg", "nin06", "nin06_neg",
                "x360", "x360_neg", "flowey", "bone_p", "bone_s", "spear", "legs", "nx64", "nx64_neg", "gen",
                "gen_neg"
            };
            int num = PlayerPrefs.GetInt("DPADStyle", 0);
            if (num >= array.Length)
            {
                PlayerPrefs.SetInt("DPADStyle", 0);
                num = 0;
            }
            UnityEngine.Object.FindObjectOfType<MobileUI>().transform.Find("DPAD").GetComponent<Image>().sprite = Resources.Load<Sprite>("ui/spr_dpad_" + array[num]);
        }
        else
        {
            PlayerPrefs.SetInt("DPADStyle", 0);
        }
        if (PlayerPrefs.HasKey("ButtonGFX"))
        {
            string[] array2 = new string[11]
            {
                "", "_neg", "blank", "blank_neg", "soul", "soul_neg", "box", "box_neg", "box_mtt", "_ctrltest",
                "_ctrltest_neg"
            };
            int num2 = PlayerPrefs.GetInt("ButtonGFX", 0);
            if (num2 >= array2.Length)
            {
                PlayerPrefs.SetInt("ButtonGFX", 0);
                num2 = 0;
            }
            Image[] componentsInChildren;
            if (array2[num2] == "" || array2[num2].StartsWith("_"))
            {
                componentsInChildren = UnityEngine.Object.FindObjectOfType<MobileUI>().GetComponentsInChildren<Image>();
                foreach (Image image in componentsInChildren)
                {
                    if (image.gameObject.name.EndsWith("_BUTTON"))
                    {
                        image.sprite = Resources.Load<Sprite>("ui/spr_button_" + image.gameObject.name[0].ToString().ToLower() + array2[num2]);
                    }
                }
                return;
            }
            componentsInChildren = UnityEngine.Object.FindObjectOfType<MobileUI>().GetComponentsInChildren<Image>();
            foreach (Image image2 in componentsInChildren)
            {
                if (image2.gameObject.name.EndsWith("_BUTTON"))
                {
                    image2.sprite = Resources.Load<Sprite>("ui/spr_button_" + array2[num2]);
                }
            }
        }
        else
        {
            PlayerPrefs.SetInt("ButtonGFX", 0);
        }
    }

    public string GetCurrentColorName()
    {
        int @int = PlayerPrefs.GetInt("ButtonStyle", 0);
        if (@int > 4)
        {
            return "N/A";
        }
        return (new string[5] { "PLAIN", "COLORS", "SOUL", "NEON", "HIDDEN" })[@int];
    }

    public string GetCurrentPadSkin()
    {
        int @int = PlayerPrefs.GetInt("DPADStyle", 0);
        if (@int > 20)
        {
            return "N/A";
        }
        return (new string[21]
        {
            "PS", "-PS", "PLUS", "-PLUS", "NIN'82", "-NIN'82", "NIN'91", "-NIN'91", "NIN'06", "-NIN'06",
            "360", "-360", "SOULTUT", "PBONE", "SBONE", "SPEAR", "LEG", "NX64", "-NX64", "GEN",
            "-GEN"
        })[@int];
    }

    public string GetCurrentButtonSkin()
    {
        int @int = PlayerPrefs.GetInt("ButtonGFX", 0);
        if (@int > 10)
        {
            return "N/A";
        }
        return (new string[11]
        {
            "LETTER", "-LETTER", "BLANK", "-BLANK", "SOUL", "-SOUL", "BOX", "-BOX", "SEXBOX", "TEST",
            "-TEST"
        })[@int];
    }

    public void CycleButtonColors()
    {
        PlayerPrefs.SetInt("ButtonStyle", PlayerPrefs.GetInt("ButtonStyle", 0) + 1);
        ApplyButtonColors();
    }

    public void CycleButtonSkin()
    {
        PlayerPrefs.SetInt("ButtonGFX", PlayerPrefs.GetInt("ButtonGFX", 0) + 1);
        ApplyButtonGFX();
    }

    public void CyclePadSkin()
    {
        PlayerPrefs.SetInt("DPADStyle", PlayerPrefs.GetInt("DPADStyle", 0) + 1);
        ApplyButtonGFX();
    }

    public bool IsDPadEnabled()
    {
        return dPadEnabled;
    }

    public bool[] IsButtonsEnabled()
    {
        return buttonEnabled;
    }
}
