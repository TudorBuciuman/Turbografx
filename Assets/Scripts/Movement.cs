using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 4f;
    private bool canMove = true;
    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator anim;
    private SpriteRenderer sr;

    private Vector3 moveDir;
    private Vector2 faceDir;

    private byte swordIndex=1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = base.transform.GetComponent<SpriteRenderer>();
        anim = base.transform.GetComponent<Animator>();
        anim.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("miniplayer");
    }

    void Update()
    {
        if (canMove)
        {
            float moveX = UTInput.GetAxisRaw("Horizontal");
            float moveY = UTInput.GetAxisRaw("Vertical");
            if (UTInput.GetButtonDown("Z"))
            {
                Attack();
            }
            if (Moved())
            {
                anim.SetFloat("speed", 1);
                anim.SetBool("isMoving", value: true);
                moveDir = new Vector3(UTInput.GetAxisRaw("Horizontal"), UTInput.GetAxisRaw("Vertical"));
                if (moveDir != Vector3.zero)
                {
                    if (new List<Vector2> { Vector2.up, Vector2.left, Vector2.down, Vector2.right }.Contains(moveDir))
                    {
                        faceDir = moveDir;
                    }
                    else if (0f - moveDir.x == faceDir.x || 0f - moveDir.y == faceDir.y)
                    {
                        faceDir = new Vector3(0f, moveDir.y);
                    }
                    ChangeDirection(faceDir);
                }
            }
            else
            {
                anim.SetBool("isMoving", value: false);
            }
            movement = Vector2.Lerp(movement, new Vector2(moveX, moveY), 0.2f);
        }
    }
    public float GetAxisRaw(string s)
    {
        if (s == "H")
        {
            return Input.GetAxisRaw("Horizontal");
        }
        else if (s == "V")
        {
            return Input.GetAxisRaw("Vertical");
        }
        return 0;
    }
    public void ChangeDirection(Vector2 dir)
    {
        anim.SetFloat("dirX", dir[0]);
        anim.SetFloat("dirY", dir[1]);
    }
    private bool Moved()
    {
        if (UTInput.GetAxisRaw("Horizontal") == 0f)
        {
            return UTInput.GetAxisRaw("Vertical") != 0f;
        }
        return true;
    }
    public Vector2 GetDirection()
    {
        return new Vector2(anim.GetFloat("dirX"), anim.GetFloat("dirY"));
    }
    public void MoveToTheOtherRoom(Vector2 v)
    {
        canMove = false;
        StartCoroutine(MoveAFewPixels(v, 0.8f));
    }
    public IEnumerator MoveAFewPixels(Vector2 v, float duration)
    {
        ChangeDirection(v);

        anim.SetFloat("speed", 1);
        anim.SetBool("isMoving", true);
        canMove = false;

        float timer = 0f;
        float speed = 4f;

        while (timer < duration)
        {
            //transform.position += new Vector3(v.x, v.y, 0) * speed * Time.deltaTime;
            movement = Vector2.Lerp(movement, v, 0.2f);
            timer += Time.deltaTime;
            yield return null;
        }
        movement = Vector2.zero;
        anim.SetBool("isMoving", false);
    }
    public void Reenable()
    {
        canMove = true;
    }
    public void Attack()
    {
        canMove = false;
        string dir="up";
        if(faceDir.x==-1)
                dir = "left";
        else if(faceDir.x==1)
                dir = "right";
        else if(faceDir.y==-1)
                dir = "down";
        else
            dir = "up";
        this.GetComponent<AudioSource>().clip = Resources.Load<AudioClip>("Sound_effects/snd_sword"+(swordIndex).ToString());
        this.GetComponent<AudioSource>().Play();
        swordIndex++;
        if (swordIndex > 3)
            swordIndex = 1;
        anim.Play("attack_" + dir);
        StartCoroutine(AttackDelay());
    }
    public IEnumerator AttackDelay()
    {
        movement = Vector2.zero;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime;
            yield return null;
        }
        canMove = true;
        yield return null;
    }
    void FixedUpdate()
    {
        //if (canMove)
        {
            rb.velocity = movement * moveSpeed;
            float pixelsPerUnit = 16f;

            Vector3 pos = transform.position;
            pos.x = Mathf.Round(pos.x * pixelsPerUnit) / pixelsPerUnit;
            pos.y = Mathf.Round(pos.y * pixelsPerUnit) / pixelsPerUnit;
            transform.position = pos;
        }
    }
}