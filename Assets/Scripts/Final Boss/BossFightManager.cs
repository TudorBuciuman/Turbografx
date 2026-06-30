using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// BossFightManager — orchestrates the Shadow Mantle Holder encounter.
/// Place in the scene. Assign bossSpawnPoint and bossPrefab in the Inspector.
/// Hook onBossDefeated to your reward/scene-transition logic.
/// </summary>
public class BossFightManager : MonoBehaviour
{
    [Header("References")]
    public GameObject          bossPrefab;
    public Transform           bossSpawnPoint;

    [Header("Arena Doors (optional)")]
    [Tooltip("Door GameObjects to lock when fight starts / unlock on defeat")]
    public GameObject[]        arenaDoors;

    [Header("Events")]
    public UnityEvent          onFightStart;
    public UnityEvent          onBossDefeated;

    [Header("Reward")]
    [Tooltip("Item dropped at spawn point on defeat")]
    public GameObject          rewardPickupPrefab;

    private ShadowMantleHolder bossInstance;
    private bool               fightStarted = false;

    /// <summary>Call this to begin the fight (e.g. from ArenaFightTrigger or dialogue).</summary>
    public void StartFight()
    {
        if (fightStarted) return;
        fightStarted = true;

        LockDoors(true);
        Vector3 spawnPos = bossSpawnPoint != null ? bossSpawnPoint.position : Vector3.zero;
        GameObject boss  = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        bossInstance     = boss.GetComponent<ShadowMantleHolder>();

        onFightStart?.Invoke();
    }

    /// <summary>Called by ShadowMantleHolder when defeated.</summary>
    public void OnBossDefeated()
    {
        LockDoors(false);

        if (rewardPickupPrefab != null)
        {
            Vector3 dropPos = bossSpawnPoint != null ? bossSpawnPoint.position : Vector3.zero;
            Instantiate(rewardPickupPrefab, dropPos, Quaternion.identity);
        }

        onBossDefeated?.Invoke();
    }

    private void LockDoors(bool locked)
    {
        if (arenaDoors == null) return;
        foreach (GameObject door in arenaDoors)
            if (door != null) door.SetActive(locked);
    }
}
