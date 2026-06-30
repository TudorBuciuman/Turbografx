using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private int health=5;
    public AudioSource audioSource;
    public float coolDown = 0.2f;
    private float time = 0.2f;
    public bool immune=false;
    public static int MaxHealth = 5;

    public void Update()
    {
        if (time > 0)
        {
            time -= Time.deltaTime;
            if (time <=0)
                immune = false;
        }
    }
    public void Heal(int plus)
    {
        if ((health += plus) <= FindFirstObjectByType<PlayerStats>().GetMaxHealth())
            health += plus;
        else
            health = FindFirstObjectByType<PlayerStats>().GetMaxHealth();
        audioSource.clip = Resources.Load<AudioClip>("Sound_effects/snd_heal");
        audioSource.Play();
        FindFirstObjectByType<PlayerStats>().UpdateHealth();
    }
    public void TakeDamage(int minus)
    {
        if (!immune)
        {
            health -= minus;
            time = coolDown;
            immune = true;
            audioSource.clip = Resources.Load<AudioClip>("Sound_effects/snd_damage");
            audioSource.Play();
            FindFirstObjectByType<PlayerStats>().UpdateHealth();
            if (health <= 0)
            {
                Destroy(FindFirstObjectByType<PlayerMovement>().gameObject);
                GameManager.gm.ChangeScene(2);
            }
        }
    }
    public int GetHealth()
    {
        return health;
    }

}
