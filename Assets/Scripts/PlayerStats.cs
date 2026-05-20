using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    private int level = 1; // 0 = no sword
    private int exp = 0;

    private int[] expToLevel = { 0, 5, 15, 30 ,60}; 

    private int baseDamage = 1;

    public PlayerHealth playerHealth;

    [SerializeField]
    public Text HPText;
    public Text LVText;
    public Slider HPSlider;
    public Slider LVSlider;
    public Image Sword,Sword1,Sword2,Sword3;
    public void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        Sword = GameObject.Find("Sword").GetComponent<Image>();
        Sword1 = GameObject.Find("Sword1").GetComponent<Image>();
        Sword2 = GameObject.Find("Sword2").GetComponent<Image>();
        Sword3 = GameObject.Find("Sword3").GetComponent<Image>();
        HPText = GameObject.Find("HPText").GetComponent<Text>();
        LVText = GameObject.Find("LVText").GetComponent<Text>();
        HPSlider = GameObject.Find("HPSlider").GetComponent<Slider>();
        LVSlider = GameObject.Find("LVSlider").GetComponent<Slider>();
        UpdateHealth();
        UpdateLevel();
        StartCoroutine(ShowStats());
        LVText.text = "LV" + level;
        if (level == 1)
        {
            Sword1.gameObject.SetActive(true);
        }
        else if (level == 2)
        {
            Sword1.gameObject.SetActive(true);
            Sword2.gameObject.SetActive(true);
        }
        else if (level == 3)
        {
            Sword1.gameObject.SetActive(true);
            Sword2.gameObject.SetActive(true);
            Sword3.gameObject.SetActive(true);
        }
    }

    public int GetDamage()
    {
        return baseDamage + level;
    }
    public void UpdateLevel()
    {
        if (level < 5)
        {
            LVText.text = "LV " + level;

            if (level <= 0 || level >= expToLevel.Length)
            {
                LVSlider.value = 0f;
                return;
            }

            float minExp = expToLevel[level - 1];
            float maxExp = expToLevel[level];

            float progress = (exp - minExp) / (maxExp - minExp);

            LVSlider.value = Mathf.Clamp01(progress);
        }
        else
        {
            LVText.text = "MAX";
            LVSlider.value = 1;
        }
    }
    public void UpdateHealth()
    {
        float r = 5 + level;
        float f = (float)(playerHealth.GetHealth() / (r));
        HPSlider.value = f;
    }
    public IEnumerator ShowStats()
    {
        float t = 0;
        float tm = 10;
        while (t < tm)
        {
            HPText.color = new Color(1, 1, 1,Mathf.Lerp(0,1,t/tm));
            LVText.color = new Color(1, 1, 1,Mathf.Lerp(0,1,t/tm));
            Sword.color = new Color(1, 1, 1, Mathf.Lerp(0, 1, t / tm));
            if(level>=2)
            Sword1.color = new Color(1, 1, 1, Mathf.Lerp(0, 1, t / tm));
            if(level>=3)
            Sword2.color = new Color(1, 1, 1, Mathf.Lerp(0, 1, t / tm));
            if(level>=4)
            Sword3.color = new Color(1, 1, 1, Mathf.Lerp(0, 1, t / tm));
            Image[] images = HPSlider.GetComponentsInChildren<Image>();
            foreach (var img in images)
                img.color = new Color(img.color.r, img.color.g, img.color.b, Mathf.Lerp(0, 1, t / tm));
            images = LVSlider.GetComponentsInChildren<Image>();
            foreach (var img in images)
                img.color = new Color(img.color.r, img.color.g, img.color.b, Mathf.Lerp(0, 1, t / tm));
            t += Time.deltaTime;
            yield return null;
        }
        Image[] imagess = HPSlider.GetComponentsInChildren<Image>();
        foreach (var img in imagess)
            img.color = new Color(img.color.r, img.color.g, img.color.b, 1);
        imagess = LVSlider.GetComponentsInChildren<Image>();
        foreach (var img in imagess)
            img.color = new Color(img.color.r, img.color.g, img.color.b, 1);
        yield return null; 
    }
    public void AddExp(int amount)
    {
        exp += amount;
        bool higher = false;
        while (level < expToLevel.Length - 1 && exp >= expToLevel[level])
        {
            level++;
            higher = true;
        }
        if (level == 2)
        {
            Sword1.color = Sword.color;
        }
        else if (level == 3)
        {
            Sword1.color = Sword.color;
            Sword2.color = Sword.color;
        }
        else if (level == 4)
        {
            Sword1.color = Sword.color;
            Sword2.color = Sword.color;
            Sword3.color = Sword.color;
        }
        if(level==4 && exp>= expToLevel[level])
        {
            level = 5;
        }
        if (higher)
        {
            StartCoroutine(HigherEXPSound());
        }
        UpdateLevel();
    }

    public IEnumerator HigherEXPSound()
    {
        AudioSource temp = this.AddComponent<AudioSource>();
        temp.GetComponent<AudioSource>().loop = false;
        temp.GetComponent<AudioSource>().clip = Resources.Load<AudioClip>("Sound_effects/snd_level_up");
        temp.GetComponent<AudioSource>().Play();
        yield return new WaitForSeconds(1);
        Destroy(temp);
    }
    public bool CanBreakObjects()
    {
        return level >= 4;
    }
}