using System;
using UnityEngine;
using UnityEngine.UI;

public class TouchPad : MonoBehaviour
{
    private Image reticle;

    private bool followPointer;

    private int sensitivity = 10;

    private Color curColor = Color.red;

    private Color newColor = Color.red;

    private int trackedFingerId = -1;

    private void Awake()
    {
        reticle = base.transform.parent.Find("Reticle").GetComponent<Image>();
        sensitivity = PlayerPrefs.GetInt("DPADSensitivity", 10);
    }

    private void Update()
    {
        //curColor = Color.Lerp(curColor, newColor, 0.1f);

        if (trackedFingerId == -1)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began && t.position.x < (float)(Screen.width / 2))
                {
                    trackedFingerId = t.fingerId;
                    followPointer = true;
                    break;
                }
            }
        }

        if (trackedFingerId != -1)
        {
            bool found = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.fingerId == trackedFingerId)
                {
                    found = true;
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        trackedFingerId = -1;
                        followPointer = false;
                    }
                    break;
                }
            }

            if (!found)
            {
                trackedFingerId = -1;
                followPointer = false;
            }
        }

        if (followPointer && trackedFingerId != -1)
        {
            int num = -Mathf.RoundToInt((float)Screen.width / Screen.height * 240f);

            Vector3 vector = Vector3.zero;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.fingerId == trackedFingerId)
                {
                    vector = t.position / Screen.height * 480f;
                    break;
                }
            }

            reticle.transform.localPosition = vector + new Vector3(num, -240f);

            float num2 = (float)sensitivity / 480f * Screen.height;
            float num3 = Vector3.Distance(reticle.transform.position, transform.position)
                         / Screen.height * 480f;

            transform.GetChild(0).LookAt(reticle.transform);

            Vector3 eulerAngles = transform.GetChild(0).localRotation.eulerAngles;

            if (eulerAngles.y > 180f) eulerAngles.y -= 360f;
            if (eulerAngles.x > 180f) eulerAngles.x -= 360f;

            float num4 = (eulerAngles.y > 0f)
                ? -eulerAngles.x
                : 180f + eulerAngles.x;

            if (num4 < 0f) num4 += 360f;

            Vector3 vector2 = new Vector3(
                Mathf.Abs(Mathf.Cos(num4 * Mathf.Deg2Rad) * num2),
                Mathf.Abs(Mathf.Sin(num4 * Mathf.Deg2Rad) * num2)
            );

            float num5 = Mathf.Max(vector2.x, vector2.y);

            if (num3 > num5)
            {
                if (num4 <= 60f || num4 >= 300f)
                    UTInput.SetValue("Horizontal", true, true, false, false);
                else if (num4 >= 120f && num4 <= 240f)
                    UTInput.SetValue("Horizontal", true, false, false, false);
                else
                    UTInput.SetValue("Horizontal", false, true, false, false);

                if (num4 >= 30f && num4 <= 150f)
                    UTInput.SetValue("Vertical", true, true, false, false);
                else if (num4 >= 210f && num4 <= 330f)
                    UTInput.SetValue("Vertical", true, false, false, false);
                else
                    UTInput.SetValue("Vertical", false, true, false, false);
            }
            else
            {
                UTInput.SetValue("Horizontal", false, false, false, false);
                UTInput.SetValue("Vertical", false, false, false, false);
            }
        }
        else
        {
            UTInput.SetValue("Horizontal", false, false, false, false);
            UTInput.SetValue("Vertical", false, false, false, false);
        }
    }

    public void SetSoulColor(Color newColor)
    {
        this.newColor = newColor;
    }

    public void ChangeSensitivity(int dif)
    {
        sensitivity = Mathf.Clamp(sensitivity + dif, 5, 30);
        PlayerPrefs.SetInt("DPADSensitivity", sensitivity);
    }

    public void OnPointerEnter()
    {
        if (Input.touchCount == 0)
            followPointer = true;
    }

    public void OnPointerExit()
    {
        if (Input.touchCount == 0)
            followPointer = false;
    }
}