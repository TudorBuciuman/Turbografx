using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

public class TouchButton : MonoBehaviour
{
    private Button button;

    [SerializeField]
    private Sprite downSprite;

    [SerializeField]
    private Sprite normalSprite;

    [SerializeField]
    private string input;

    [SerializeField]
    private bool pos = true;

    [SerializeField]
    private bool isDiag;

    [SerializeField]
    private bool goLeft;

    private void Start()
    {
        button = GetComponent<Button>();
    }

    private void Update()
    {
    }

    public void OnPointerEnter()
    {
        //Object.FindObjectOfType<GameManager>().PlayGlobalSFX("sounds/snd_menumove");
        GetComponent<Image>().color = new Color(1f, 1f, 0f);
    }

    public void OnPointerExit()
    {
        GetComponent<Image>().color = new Color(1f, 1f, 1f);
    }

    public void OnPointerDown()
    {
        UTInput.SetValue(input, value: true, pos, isDiag, goLeft);
        GetComponent<Image>().sprite = downSprite;
    }

    public void OnPointerUp()
    {
        if (input != "quit")
        {
            UTInput.SetValue(input, value: false, pos, isDiag, goLeft);
            GetComponent<Image>().sprite = normalSprite;
        }
    }
}
