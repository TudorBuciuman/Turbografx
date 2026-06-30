using UnityEngine;
using UnityEngine.UI;

public class Fade : MonoBehaviour
{
    private Image img;

    private AudioSource aud;

    private float frames;

    private float maxFrames;

    private bool isFading = false;

    private bool fadingOut;

    private Color oldColor;

    private Color color;

    private void Awake()
    {
        base.gameObject.layer = 8;
        img = base.transform.GetComponent<Image>();
        aud = base.transform.GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (isFading && frames <= maxFrames)
        {
            if (maxFrames == 0f)
            {
                img.color = (fadingOut ? color : oldColor);
                isFading = false;
                return;
            }
            float num = (float)(frames / maxFrames);
            if (!fadingOut)
            {
                num = 1f - num;
            }
            img.color = Color.Lerp(oldColor, color, num);
            frames += 1f;
        }
        else if (frames > maxFrames)
        {
            isFading = false;
        }
    }

    public void FadeOut(int numFrames, Color color)
    {
        if (isFading)
        {
            oldColor = img.color;
        }
        else
        {
            oldColor = new Color(color.r, color.g, color.b, 0f);
        }
        frames = 0f;
        maxFrames = numFrames;
        img.color = oldColor;
        fadingOut = true;
        isFading = true;
        this.color = color;
        if (maxFrames == 0f)
        {
            Update();
        }
    }

    public void FadeOut(int numFrames)
    {
        FadeOut(numFrames, new Color(0f, 0f, 0f, 1f));
    }

    public void FadeIn(int numFrames, Color color)
    {
        oldColor = new Color(color.r, color.g, color.b, 0f);
        frames = 0f;
        maxFrames = numFrames;
        img.color = color;
        fadingOut = false;
        isFading = true;
        this.color = color;
        if (maxFrames == 0f)
        {
            Update();
        }
    }

    public void FadeIn(int numFrames)
    {
        FadeIn(numFrames, new Color(0f, 0f, 0f, 1f));
    }

    public void UTFadeOut()
    {
        //GameObject.Find("GameManager").GetComponent<GameManager>().StopMusic();
        aud.clip = Resources.Load<AudioClip>("music/mus_cymbal");
        aud.Play();
        FadeOut(156, new Color(1f, 1f, 1f, 1f));
    }

    public bool IsPlaying()
    {
        return isFading;
    }
}
