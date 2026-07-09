using System.Collections;
using UnityEngine;

public class CRTRevealCutscene : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cutsceneCamera;
    [SerializeField] private Transform revealEndPoint;
    [SerializeField] private GameObject bedCollider;
    [SerializeField] private AudioSource noise;
    [SerializeField] private AudioSource music;
    [SerializeField] private AudioSource girlfriend;

    [Tooltip("Optional: player root / controller object to disable during the cutscene and re-enable afterward.")]
    [SerializeField] private GameObject playerRoot;

    [Header("Cutscene Timing")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("Delay before the camera starts pulling back.")]
    [SerializeField] private float initialPause = 1.5f;

    [Tooltip("How long the zoom-out / reveal lasts.")]
    [SerializeField] private float revealDuration = 4f;
    [SerializeField] private float playerMoveDuration = 2f;

    [Tooltip("Optional pause after the reveal completes, before handing control back.")]
    [SerializeField] private float endPause = 0.5f;

    [Header("FOV")]
    [SerializeField] private float startFOV = 20f;
    [SerializeField] private float endFOV = 60f;

    [Header("Motion")]
    [Tooltip("If true, the camera will also rotate to match the end transform.")]
    [SerializeField] private bool rotateCamera = true;

    [Tooltip("Use an ease curve instead of linear interpolation.")]
    [SerializeField]
    private AnimationCurve revealCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2f),
        new Keyframe(1f, 1f, 0f, 0f)
    );

    [Header("Optional Final Step")]
    [Tooltip("If true, the cutscene script enables the player object at the end.")]
    [SerializeField] private bool enablePlayerAtEnd = true;

    [Tooltip("If true, the cutscene script disables the player object at the beginning.")]
    [SerializeField] private bool disablePlayerAtStart = true;

    [Tooltip("Optional Animator trigger to fire at the end, for the 'getting up' animation/sequence.")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string standUpTrigger = "StandUp";

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
        if (cutsceneCamera == null)
        {
            Debug.LogError("CRTRevealCutscene: No camera assigned.");
            yield break;
        }

        if (revealEndPoint == null)
        {
            Debug.LogError("CRTRevealCutscene: No reveal end point assigned.");
            yield break;
        }

        
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

            if (rotateCamera)
            {
                cutsceneCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, easedT);
            }

            cutsceneCamera.fieldOfView = Mathf.Lerp(startFOV, endFOV, easedT);

            yield return null;
        }
        cutsceneCamera.transform.position = endPos;
        if (rotateCamera)
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
            t = Mathf.SmoothStep(0f, 1f, t);   // ease-out curve

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
        Debug.Log("CRT reveal cutscene finished.");
    }
}