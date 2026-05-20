using UnityEngine;

public class HealItem : MonoBehaviour
{
    public int healAmount = 2;
    private int room;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth hp = other.GetComponent<PlayerHealth>();
            if (hp != null)
            {
                hp.Heal(healAmount);
            }

            Destroy(gameObject);
        }
    }
    public void SetRoom(int NewRoom)
    {
        room = NewRoom;
    }
    public void DestroyOnExit()
    {
        Destroy(gameObject);
    }
}