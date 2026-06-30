using UnityEngine;

public class ArenaFightTrigger : MonoBehaviour
{
    public BossFightManager fightManager;
    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;
        FindFirstObjectByType<BossDialogueTrigger>().TriggerPreFight();
        DialogueSystem.instance.OnDialogueEnd = () =>
        {
            if (fightManager != null)
                fightManager.StartFight();
            Destroy(gameObject); 
        };
    }
}
