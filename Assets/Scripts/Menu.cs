using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    public static bool mobile = false;
    private void Awake()
    {
#if UNITY_ANDROID
    mobile=true;
#endif
    }
    private void Start()
    {
        GameManager.gm.PlaySong("Music/Idioteque");
        if (mobile)
        {
            Instantiate(Resources.Load<GameObject>("ui/MobileUI"));
            FindFirstObjectByType<MobileUI>().EnableButtons(dPadEnabled: true, z: true, x: true, c: true, instant: false);
            UTInput.Android();
        }
    }
    private void Update()
    {
        if(Pressed())
        FindFirstObjectByType<GameManager>().ChangeScene(1);
    }
    private bool Pressed()
    {
        return UTInput.GetButton("Z");
    }
}
