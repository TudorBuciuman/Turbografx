using System.Collections;
using UnityEngine;

public class CRTRevealCutscene : MonoBehaviour
{
    [SerializeField] private Camera cutsceneCamera;
    [SerializeField] private Transform revealEndPoint;
    [SerializeField] private GameObject bedCollider;
    [SerializeField] private AudioSource noise;
    [SerializeField] private AudioSource music;
    [SerializeField] private AudioSource girlfriend;
    [SerializeField] private GameObject playerRoot; 

    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float initialPause = 1.5f;
    [SerializeField] private float revealDuration = 4f;
    [SerializeField] private float playerMoveDuration = 2f;
    [SerializeField] private float endPause = 0.5f;
    [SerializeField] private float startFOV = 20f;
    [SerializeField] private float endFOV = 60f;

    [SerializeField]
    private AnimationCurve revealCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),
        new Keyframe(1f, 1f, 0f, 0f)
    );
    public Light revealLight; 

    private bool hasPlayed;

    private void Start()
    {
        revealLight.range = 90;
        if (playOnStart)
        {
            MouseLook.CanLook = false;
            PlayCutscene();
        }
    }

    public void PlayCutscene()
    {
        if (hasPlayed) return;
        hasPlayed = true;
        StartCoroutine(PlayCutsceneRoutine());
        StartCoroutine(LightTransition());
    }
    private IEnumerator LightTransition()
    {
        float elapsed = 0f;

        while (elapsed < 25)
        {
            elapsed += Time.deltaTime;
            revealLight.range = Mathf.Lerp(90, 300, elapsed/25.0f);
            yield return null;
        }
        yield return new WaitForSeconds(10);

        elapsed = 0f;
        while (elapsed < 25)
        {
            elapsed += Time.deltaTime;
            revealLight.range = Mathf.Lerp(300, 140, elapsed / 25.0f);
            yield return null;
        }
        yield return null;
    }

    private IEnumerator PlayCutsceneRoutine()
    {   
        cutsceneCamera.fieldOfView = startFOV;

        Vector3 startPos = cutsceneCamera.transform.position;
        Quaternion startRot = cutsceneCamera.transform.rotation;

        Vector3 endPos = revealEndPoint.position;
        Quaternion endRot = revealEndPoint.rotation;

        if (initialPause > 0f)
            yield return new WaitForSeconds(initialPause);

        float elapsed = 0f;

        while (elapsed < revealDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / revealDuration);

            float easedT = revealCurve != null ? revealCurve.Evaluate(t) : t;

            cutsceneCamera.transform.position = Vector3.Lerp(startPos, endPos, easedT);
            cutsceneCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, easedT);
            cutsceneCamera.fieldOfView = Mathf.Lerp(startFOV, endFOV, easedT);

            yield return null;
        }
        cutsceneCamera.transform.position = endPos;
        cutsceneCamera.transform.rotation = endRot;

        cutsceneCamera.fieldOfView = endFOV;

        if (endPause > 0f)
            yield return new WaitForSeconds(endPause);

        elapsed = 0f;
        
        Vector3 pos1 = playerRoot.transform.position;
        Vector3 pos2 = playerRoot.transform.position + new Vector3(-27,0,0);

        while (elapsed < playerMoveDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / playerMoveDuration;
            t = Mathf.SmoothStep(0f, 1f, t);   

            playerRoot.transform.position = Vector3.Lerp(pos1, pos2, t);
            yield return null;
        }


        if (endPause / 2 > 0f)
            yield return new WaitForSeconds(endPause);

        bedCollider.SetActive(true);

        OnCutsceneFinished();
    }
    private void OnCutsceneFinished()
    {
        MouseLook.CanLook = true;
        PlayerMove.canMove=true;
        Debug.Log("It's so over...");
    }
}