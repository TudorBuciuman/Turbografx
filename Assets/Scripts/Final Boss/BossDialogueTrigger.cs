using UnityEngine;

/// <summary>
/// Drop this on any GameObject to trigger the TARGET boss dialogue.
/// Calls DialogueSystem.instance — make sure that's in the scene.
///
/// For BITROOT: call TriggerPreFight() when the player enters the final room,
/// and TriggerPostFight() from your combat system when the boss HP hits 0.
/// </summary>
public class BossDialogueTrigger : MonoBehaviour
{
    // ── Pre-fight ──────────────────────────────────────────────────────────
    public void TriggerPreFight()
    {
        DialogueLine[] lines = new DialogueLine[]
        {
            new DialogueLine
            {
                text      = "You got stronger.",
                charDelay = 0.055f
            },
            new DialogueLine
            {
                text      = "Every floor.\nA little more.\nThen a lot more.",
                charDelay = 0.045f
            },
            new DialogueLine
            {
                text      = "Then you stopped\nnoticing.\nWonder why?",
                charDelay = 0.045f
            },
            new DialogueLine
            {
                text      = "That's when I knew you'd make it here.",
                charDelay = 0.045f
            },
            new DialogueLine
            {
                text      = "You think this is\nthe part\nwhere you win.",
                charDelay = 0.05f
            },
            new DialogueLine
            {
                text      = "I can tell.\nYou're standing like it.",
                charDelay = 0.05f
            },
            new DialogueLine
            {
                text      = "You've been standing like it\nsince floor two.",
                charDelay = 0.05f
            },
            new DialogueLine
            {
                text      = "I've seen the ones who hesitate.\nThey don't make it down this far.",
                charDelay = 0.045f
            },
            new DialogueLine
            {
                text      = "You never hesitated.",
                charDelay = 0.06f
            },
            new DialogueLine
            {
                text      = "Not once.",
                charDelay  = 0.07f,
                instantReveal = false
            },
            new DialogueLine
            {
                text      = "Do you know what\nthat means?",
                charDelay = 0.05f
            },
            new DialogueLine
            {
                text      = "It means you're\nready.",
                charDelay = 0.055f
            },
            new DialogueLine
            {
                text      = "It means I've been\nwaiting\nfor exactly you.",
                charDelay = 0.05f
            },
        };

        DialogueSystem.instance.OnDialogueEnd = OnPreFightDone;
        DialogueSystem.instance.StartDialogue(lines);
    }

    void OnPreFightDone()
    {
        // Hook into your combat system here
        // e.g.: BossController.instance.StartFight();
        Debug.Log("[BITROOT] Pre-fight dialogue done. Start boss fight.");
    }
    // ── Post-fight ─────────────────────────────────────────────────────────
    public void TriggerPostFight()
    {
        DialogueLine[] lines = new DialogueLine[]
        {
            new DialogueLine
            {
                text      = "There it is.",
                charDelay = 0.065f
            },
            new DialogueLine
            {
                text      = "You felt it, didn't you.",
                charDelay = 0.05f
            },
            new DialogueLine
            {
                text      = "The whole way down.\nFloor after floor.\nSomething building.",
                charDelay = 0.045f
            },
            new DialogueLine
            {
                text      = "You called it determination.\nYou called it survival.",
                charDelay = 0.045f
            },
            new DialogueLine
            {
                text      = "You called it whatever\nmade it easier\nto keep going.",
                charDelay = 0.045f
            },
            new DialogueLine
            {
                text      = "But I felt it too.\nFrom up here.\nEvery floor.",
                charDelay = 0.05f
            },
            new DialogueLine
            {
                text      = "It wasn't determination.",
                charDelay = 0.055f
            },
            new DialogueLine
            {
                // The line. Slow it down.
                text      = "It was hunger.",
                charDelay = 0.09f
            },
            new DialogueLine
            {
                text      = "And you fed it\nall the way\nto the bottom.",
                charDelay = 0.05f
            },
            new DialogueLine
            {
                text      = "So.",
                charDelay = 0.08f
            },
            new DialogueLine
            {
                text      = "What are you going to do\nwhen you get back up there",
                charDelay = 0.05f
            },
            new DialogueLine
            {
                text      = "and you're still hungry?",
                charDelay  = 0.06f
            },
        };

        DialogueSystem.instance.OnDialogueEnd = OnPostFightDone;
        DialogueSystem.instance.StartDialogue(lines);
    }

    void OnPostFightDone()
    {
        // Trigger red eyes on player sprite, then begin unzoom sequence
        // e.g.: PlayerController.instance.TriggerRedEyes();
        //       EndingCutsceneDirector.instance.BeginUnzoom();
        Debug.Log("[BITROOT] Post-fight done. Trigger red eyes → unzoom.");
    }
}
