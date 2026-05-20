using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private int health=5;
    public AudioSource audioSource;

    public void Heal(int plus)
    {
        health += plus;
        audioSource.clip = Resources.Load<AudioClip>("Sound_effects/snd_heal");
        audioSource.Play();
        FindFirstObjectByType<PlayerStats>().UpdateHealth();
    }
    public void TakeDamage(int minus)
    {
        health -=minus;
        audioSource.clip = Resources.Load<AudioClip>("Sound_effects/snd_damage");
        audioSource.Play();
        FindFirstObjectByType<PlayerStats>().UpdateHealth();

        if (health <= 0)
        {
            GameManager.gm.ChangeScene(2);
        }
    }
    public int GetHealth()
    {
        return health;
    }

}
