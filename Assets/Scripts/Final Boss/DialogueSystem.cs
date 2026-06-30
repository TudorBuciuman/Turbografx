using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BITROOT Dialogue System — Deltarune ch3 minigame style.
/// Attach to a DialogueBox GameObject in your Canvas.
///
/// Setup:
///   - DialogueBox (this script lives here, with a Panel/Image background)
///       - NameLabel         (TMP_Text)  — optional speaker name tag
///       - DialogueText      (TMP_Text)  — main typewriter text
///       - ContinueArrow     (GameObject) — blinking ▼ arrow at box bottom-right
///
/// Assign an AudioClip (8-bit blip) to typingSFX.
/// Call DialogueSystem.instance.StartDialogue(DialogueLine[]) from anywhere.
/// </summary>

[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 6)]
    public string text;
    public string speakerName;          // leave empty to hide name tag
    public float  charDelay = 0.04f;    // seconds per character (default Deltarune feel)
    public bool   instantReveal = false; // for short impact lines
}

public class DialogueSystem : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static DialogueSystem instance;

    // ── Inspector refs ─────────────────────────────────────────────────────
    [Header("UI References")]
    public GameObject    dialogueBox;
    public Text      dialogueText;
    public Text      nameLabel;
    public GameObject    continueArrow;
    public Image    panel;

    [Header("Typing Sound")]
    public AudioSource   audioSource;
    public AudioClip     typingSFX;
    [Range(0f, 1f)]
    public float         typingVolume    = 0.6f;
    [Tooltip("Play sfx every N characters (1 = every char, 2 = every other, etc.)")]
    public int           sfxEveryNChars  = 2;

    [Header("Arrow Blink")]
    public float         arrowBlinkRate  = 0.45f;   // seconds per blink half-cycle

    [Header("Timing")]
    [Tooltip("How long to wait after last line before auto-closing (0 = never auto-close)")]
    public float         autoCloseDelay  = 0f;

    // ── Private state ──────────────────────────────────────────────────────
    private DialogueLine[]  _lines;
    private int             _currentLine;
    private bool            _isTyping;
    private bool            _skipRequested;
    private bool            _dialogueActive;
    private Coroutine       _typeCoroutine;
    private Coroutine       _arrowCoroutine;

    // ── Events ─────────────────────────────────────────────────────────────
    public System.Action    OnDialogueEnd;   // fired when all lines are done

    // ───────────────────────────────────────────────────────────────────────
    void Awake()
    {
        //if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        dialogueBox.SetActive(false);
        continueArrow.SetActive(false);
    }

    void Update()
    {
        if (!_dialogueActive) return;

        bool advance = Input.GetKeyDown(KeyCode.Z)
                    || Input.GetKeyDown(KeyCode.Return)
                    || Input.GetKeyDown(KeyCode.KeypadEnter);

        if (!advance) return;

        if (_isTyping)
        {
            // First press while typing → instantly reveal full line
            _skipRequested = true;
        }
        else
        {
            // Line already fully shown → advance to next
            AdvanceLine();
        }
    }

    // ───────────────────────────────────────────────────────────────────────
    // Public API
    // ───────────────────────────────────────────────────────────────────────

    public void StartDialogue(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0) return;

        _lines         = lines;
        _currentLine   = 0;
        _dialogueActive = true;

        panel.color = Color.black;
        FindFirstObjectByType<GameManager>().DisablePlayerMovement();
        FindFirstObjectByType<PlayerMovement>().MovementToZero();
        dialogueBox.SetActive(true);
        ShowLine(_currentLine);
    }

    // Convenience overload: single line
    public void StartDialogue(string text, string speaker = "")
    {
        StartDialogue(new DialogueLine[]
        {
            new DialogueLine { text = text, speakerName = speaker }
        });
    }

    // ───────────────────────────────────────────────────────────────────────
    // Internal
    // ───────────────────────────────────────────────────────────────────────

    void ShowLine(int index)
    {
        DialogueLine line = _lines[index];

        // Name tag
        if (nameLabel != null)
        {
            bool hasName = !string.IsNullOrEmpty(line.speakerName);
            nameLabel.gameObject.SetActive(hasName);
            if (hasName) nameLabel.text = line.speakerName;
        }

        // Hide arrow while typing
        SetArrow(false);

        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        _typeCoroutine = StartCoroutine(TypeLine(line));
    }

    IEnumerator TypeLine(DialogueLine line)
    {
        _isTyping      = true;
        _skipRequested = false;
        dialogueText.text = "";

        string full = line.text;

        if (line.instantReveal)
        {
            dialogueText.text = full;
        }
        else
        {
            int charCount = 0;

            for (int i = 0; i < full.Length; i++)
            {
                if (_skipRequested) break;

                // Handle rich text tags: skip ahead without delay
                if (full[i] == '<')
                {
                    int close = full.IndexOf('>', i);
                    if (close != -1)
                    {
                        dialogueText.text = full.Substring(0, close + 1);
                        i = close;
                        continue;
                    }
                }

                dialogueText.text = full.Substring(0, i + 1);
                charCount++;

                // Sound every N visible characters (skip spaces & punctuation pauses)
                if (full[i] != ' ' && charCount % sfxEveryNChars == 0)
                    PlayTypingSound();

                // Punctuation micro-pause (feels more natural / Deltarune-like)
                float delay = line.charDelay;
                if (full[i] == ',' || full[i] == ';') delay *= 4f;
                else if (full[i] == '.' || full[i] == '!' || full[i] == '?') delay *= 6f;

                yield return new WaitForSeconds(delay);
            }

            // Instant-fill if skipped mid-type
            dialogueText.text = full;
        }

        _isTyping = false;
        OnLineFullyShown();
    }

    void OnLineFullyShown()
    {
        bool isLast = (_currentLine >= _lines.Length - 1);

        if (isLast && autoCloseDelay > 0f)
        {
            StartCoroutine(AutoCloseAfterDelay(autoCloseDelay));
        }
        else
        {
            // Show blinking arrow only if there's more or player must dismiss
            SetArrow(true);
        }
    }

    void AdvanceLine()
    {
        _currentLine++;

        if (_currentLine >= _lines.Length)
        {
            EndDialogue();
        }
        else
        {
            ShowLine(_currentLine);
        }
    }

    void EndDialogue()
    {
        _dialogueActive = false;
        SetArrow(false);
        dialogueBox.SetActive(false);
        FindFirstObjectByType<GameManager>().EnablePlayerMovement();
        OnDialogueEnd?.Invoke();
    }

    IEnumerator AutoCloseAfterDelay(float delay)
    {
        SetArrow(false);
        yield return new WaitForSeconds(delay);
        EndDialogue();
    }

    // ── Arrow blink ────────────────────────────────────────────────────────

    void SetArrow(bool visible)
    {
        if (_arrowCoroutine != null) StopCoroutine(_arrowCoroutine);
        continueArrow.SetActive(false);

        if (visible)
            _arrowCoroutine = StartCoroutine(BlinkArrow());
    }

    IEnumerator BlinkArrow()
    {
        while (true)
        {
            continueArrow.SetActive(true);
            yield return new WaitForSeconds(arrowBlinkRate);
            continueArrow.SetActive(false);
            yield return new WaitForSeconds(arrowBlinkRate);
        }
    }

    // ── Audio ──────────────────────────────────────────────────────────────

    void PlayTypingSound()
    {
        if (typingSFX == null || audioSource == null) return;
        audioSource.PlayOneShot(typingSFX, typingVolume);
    }
}
