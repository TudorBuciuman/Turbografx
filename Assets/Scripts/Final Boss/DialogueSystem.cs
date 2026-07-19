using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DialogueLine
{
    [TextArea(2, 6)]
    public string text;
    public string speakerName;     
    public float  charDelay = 0.04f;   
    public bool   instantReveal = false; 
}

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem instance;

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
    public float typingVolume = 0.6f;
    public int sfxEveryNChars  = 2;

    public float arrowBlinkRate  = 0.45f;   
    public float autoCloseDelay  = 0f;

    private DialogueLine[]  _lines;
    private int             _currentLine;
    private bool            _isTyping;
    private bool            _skipRequested;
    private bool            _dialogueActive;
    private Coroutine       _typeCoroutine;
    private Coroutine       _arrowCoroutine;

    public System.Action OnDialogueEnd;   

    void Awake()
    {
        instance = this;

        dialogueBox.SetActive(false);
        continueArrow.SetActive(false);
    }

    void Update()
    {
        if (!_dialogueActive) return;

        bool advance = Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

        if (!advance) return;

        if (_isTyping)
        {
            _skipRequested = true;
        }
        else
        {
            AdvanceLine();
        }
    }

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

    public void StartDialogue(string text, string speaker = "")
    {
        StartDialogue(new DialogueLine[]
        {
            new DialogueLine { text = text, speakerName = speaker }
        });
    }


    void ShowLine(int index)
    {
        DialogueLine line = _lines[index];

        if (nameLabel != null)
        {
            bool hasName = !string.IsNullOrEmpty(line.speakerName);
            nameLabel.gameObject.SetActive(hasName);
            if (hasName) nameLabel.text = line.speakerName;
        }

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

                if (full[i] != ' ' && charCount % sfxEveryNChars == 0)
                    PlayTypingSound();

                float delay = line.charDelay;
                if (full[i] == ',' || full[i] == ';') delay *= 4f;
                else if (full[i] == '.' || full[i] == '!' || full[i] == '?') delay *= 6f;

                yield return new WaitForSeconds(delay);
            }
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

    void PlayTypingSound()
    {
        if (typingSFX == null || audioSource == null) return;
        audioSource.PlayOneShot(typingSFX, typingVolume);
    }
}
