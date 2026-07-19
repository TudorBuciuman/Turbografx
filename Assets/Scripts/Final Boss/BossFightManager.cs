using UnityEngine;
using UnityEngine.Events;

public class BossFightManager : MonoBehaviour
{
    public GameObject bossPrefab;
    public Transform bossSpawnPoint;

    public GameObject[] arenaDoors;

    public UnityEvent onFightStart;
    public UnityEvent onBossDefeated;

    public GameObject rewardPickupPrefab;
    public AudioSource audioSource;

    private ShadowMantleHolder bossInstance;
    private bool fightStarted = false;

    public void StartFight()
    {
        if (fightStarted) return;
        fightStarted = true;

        audioSource.Play();
        LockDoors(true);
        Vector3 spawnPos = bossSpawnPoint != null ? bossSpawnPoint.position : Vector3.zero;
        GameObject boss  = Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        bossInstance     = boss.GetComponent<ShadowMantleHolder>();

        onFightStart?.Invoke();
    }

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
