using UnityEngine;

public class LoadingZone : MonoBehaviour
{
    [SerializeField]
    private int newScene = 2;

    [SerializeField]
    private Vector2 newPos = Vector2.zero;

    [SerializeField]
    private byte face = 0;

    [SerializeField]
    private int fadeType;

    [SerializeField]
    private int special;

    [SerializeField]
    private bool fadeMusic;

    private bool activated;

    private void Start()
    {
        activated = false;
    }

    private void Update()
    {
        if (!activated)
        {
            return;
        }
        else
        {
            FindFirstObjectByType<GameManager>().LoadArea(newScene, fadeIn: true, newPos, face, special);
        }
        activated = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        activated = true;
        FindFirstObjectByType<GameManager>().DisablePlayerMovement();
        return;
    }

    public int GetScene()
    {
        return newScene;
    }
}
