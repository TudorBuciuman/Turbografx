using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float speed = 5f;
    public int damage = 1;

    private Vector2 direction;
    public void Start()
    {
        this.GetComponent<AudioSource>().clip = Resources.Load<AudioClip>("Sound_effects/snd_enemy_arrow");
        this.GetComponent<AudioSource>().Play();
    }

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth hp = other.GetComponent<PlayerHealth>();

            if (hp != null)
                hp.TakeDamage(damage);

            Destroy(gameObject);
        }

        if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}