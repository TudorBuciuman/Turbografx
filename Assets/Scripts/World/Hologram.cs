using UnityEngine;

public class HoverAnimation : MonoBehaviour
{
    public float amplitude = 0.1f;
    public float speed = 2f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.position =
            startPos +
            Vector3.up *
            Mathf.Sin(Time.time * speed)
            * amplitude;
    }
}