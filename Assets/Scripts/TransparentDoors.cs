using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransparentDoors : MonoBehaviour
{
    public bool transparent;
    private void Awake()
    {
        if (transparent)
            GetComponent<SpriteRenderer>().enabled = false;
    }
}
