using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    private void Start()
    {
        Instantiate(Resources.Load<GameObject>("ui/MobileUI"));
        FindFirstObjectByType<MobileUI>().EnableButtons(dPadEnabled: true, z: true, x: true, c: true, instant: false);
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
