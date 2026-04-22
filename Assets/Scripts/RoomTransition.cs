using UnityEngine;
using System.Collections;

public class RoomTransition : MonoBehaviour
{
    private static bool inUse = false;
    [SerializeField]
    public Vector2 direction; // (1,0) right, (-1,0) left, (0,1) up, (0,-1) down
    [SerializeField]
    public float controlDelay = 0.5f;
    [SerializeField]
    public float speed=8; //8 is the default, a little slow

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || inUse) return;
        if (!other.CompareTag("Player")) return;

        RoomCamera cam = Camera.main.GetComponent<RoomCamera>();
        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if (cam.IsMoving()) return;

        triggered = true;

        StartCoroutine(Transition(cam, player));
    }

    IEnumerator Transition(RoomCamera cam, PlayerMovement player)
    {
        inUse = true;
        // Disable player control
        //player.enabled = false;

        // Move camera
        cam.MoveToRoom(direction,speed);
        player.MoveToTheOtherRoom(direction);
        // Wait for camera to finish
        while (cam.IsMoving())
            yield return null;

        // Small delay (Zelda feel)
        yield return new WaitForSeconds(controlDelay);

        // Re-enable control
        player.enabled = true;

        triggered = false;
        player.Reenable();
        inUse = false;
    }
}