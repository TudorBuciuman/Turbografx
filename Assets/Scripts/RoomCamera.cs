using UnityEngine;
using System.Collections;

public class RoomCamera : MonoBehaviour
{
    private float roomWidth = 12;
    private float roomHeight = 8f;

    private float moveSpeed = 8f;
    private bool isMoving = false;

    private Vector3 targetPosition;
    //8 is the default speed, slow but good at the start


    void Start()
    {
        // Snap camera to starting room
        SnapToRoom(transform.position);
    }

    public void MoveToRoom(Vector2 direction, float speed)
    {
        if (isMoving) return;

        moveSpeed = speed;
        Vector3 newRoomPos = transform.position + new Vector3(
            direction.x * roomWidth,
            direction.y * roomHeight,
            0
        );

        StartCoroutine(SmoothMove(newRoomPos));
    }

    IEnumerator SmoothMove(Vector3 target)
    {
        isMoving = true;

        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        transform.position = target;
        isMoving = false;
    }

    void SnapToRoom(Vector3 pos)
    {
        float x = Mathf.Round(pos.x / roomWidth) * roomWidth;
        float y = Mathf.Round(pos.y / roomHeight) * roomHeight;

        transform.position = new Vector3(x, y, transform.position.z);
    }

    public bool IsMoving()
    {
        return isMoving;
    }
}