using System.Collections;
using UnityEngine;

// One bubble in Learn a Dance, spawned by DanceManager.
// Hit it by touching it with a hand (anything tagged "Hand" with a trigger collider).
// Feedback: glows and grows while approaching, pops green with a sound on a hit, turns red and shrinks on a miss.
// WHY: in Design Evaluation 2, testers couldn't tell if they'd hit a bubble and asked for sound,
// and said the bubbles were small and hard to see coming. So each bubble grows in before it's due,
// and a hit and a miss look and sound clearly different.
public class DanceBubble : MonoBehaviour
{
    // The three ways a bubble can be shown. Mastery stage uses Outline (faint) and Hidden.
    // An enum is a list of named options, which is clearer than using numbers 0, 1, 2.
    public enum Visibility { Full, Outline, Hidden }

    [Tooltip("The bubble's mesh. Its material must be transparent (URP Unlit or Lit, Surface Type: Transparent).")]
    public Renderer body;

    [Tooltip("Full size in metres")]
    public float size = 0.25f;

    // Colours for each moment. The last number is alpha (see-through-ness): 0 = invisible, 1 = solid.
    public Color approachColor = new Color(1f, 1f, 1f, 0.3f);    // faint white while it's coming
    public Color readyColor = new Color(0.3f, 0.8f, 1f, 0.85f);  // blue: hit it now
    public Color hitColor = new Color(0.3f, 1f, 0.4f, 1f);       // green: success
    public Color missColor = new Color(1f, 0.3f, 0.3f, 0.6f);    // red: missed

    [Tooltip("How see-through bubbles are in Mastery's outline stage (0 = invisible, 1 = normal)")]
    [Range(0f, 1f)]
    public float outlineAlpha = 0.2f;

    public AudioClip hitSound;
    public AudioClip missSound;

    // The miss sound is quieter so a miss doesn't feel like a punishment
    [Range(0f, 1f)]
    public float missVolume = 0.4f;

    [Tooltip("Optional particle effect spawned on a hit")]
    public GameObject popEffect;

    private DanceManager manager;   // the DanceManager that made this bubble (for song time and scoring)
    private float hitTime;          // the song time when this bubble should be hit
    private Visibility visibility;
    private bool finished;          // true once hit or missed, so it can't be scored twice
    private int handsInside;        // how many hands are touching it right now (could be both)
    private Material material;

    // Called by DanceManager straight after it creates the bubble (like a setup function)
    public void Init(DanceManager owner, float time, Visibility vis)
    {
        manager = owner;
        hitTime = time;
        visibility = vis;

        if (body == null) body = GetComponentInChildren<Renderer>();
        // .material gives this bubble its OWN copy, so changing its colour doesn't change every bubble
        material = body.material;
        body.enabled = visibility != Visibility.Hidden;   // Hidden bubbles still work, you just can't see them
        transform.localScale = Vector3.one * size * 0.3f; // start small (30%) and grow
    }

    private void Update()
    {
        if (finished || manager == null) return;

        // Use the SONG's time, not real time, so bubbles stay in time even at 0.5x speed in Practice
        float now = manager.SongTime;
        if (now < hitTime)
        {
            // Approach glow: small and faint, growing and pulsing until it's due
            // progress goes 0 -> 1 over the lead time (1 = it's due now)
            float progress = Mathf.Clamp01(1f - (hitTime - now) / manager.leadTime);
            // A gentle sine "heartbeat" so it catches your eye: size wobbles by 8%
            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 12f);
            transform.localScale = Vector3.one * size * Mathf.Lerp(0.3f, 1f, progress) * pulse;
            SetColor(Dim(approachColor));
        }
        else
        {
            // It's due: full size and blue
            transform.localScale = Vector3.one * size;
            SetColor(Dim(readyColor));
            // Too late: the hit window has passed, so it's a miss
            if (now > hitTime + manager.hitWindow)
            {
                StartCoroutine(Finish(false));
                return;
            }
        }

        // A hand is touching it, and it's not too early (a small early window makes it feel fair)
        if (handsInside > 0 && now >= hitTime - manager.earlyWindow)
        {
            StartCoroutine(Finish(true));
        }
    }

    // Unity calls these when another collider enters/leaves this bubble's trigger collider.
    // Counting hands (instead of hitting straight away) lets a hand that arrives a bit early
    // still count once the bubble is ready.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand")) handsInside++;
    }

    private void OnTriggerExit(Collider other)
    {
        // Mathf.Max(0, ...) stops the count going below zero
        if (other.CompareTag("Hand")) handsInside = Mathf.Max(0, handsInside - 1);
    }

    // A coroutine (IEnumerator) can pause with "yield return" and carry on next frame.
    // That's how the pop animation plays over several frames without freezing the game.
    private IEnumerator Finish(bool hit)
    {
        finished = true;
        // Turn off the collider so it can't be hit again while it animates away
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (hit)
        {
            manager.BubbleHit(this);   // tell the manager to add to the score
            body.enabled = true; // always show the pop, even in Mastery
            // PlayClipAtPoint plays from the bubble's position, so the sound comes from where you hit it
            if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, transform.position);
            if (popEffect != null) Instantiate(popEffect, transform.position, Quaternion.identity);
        }
        else
        {
            manager.BubbleMissed(this);
            if (missSound != null) AudioSource.PlayClipAtPoint(missSound, transform.position, missVolume);
        }

        // Hit: grow and fade out green. Miss: turn red and shrink away.
        Color color = hit ? hitColor : missColor;
        float duration = hit ? 0.2f : 0.35f;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = hit ? startScale * 1.6f : Vector3.zero;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;   // 0 -> 1 through the animation
            transform.localScale = Vector3.Lerp(startScale, endScale, k);
            Color c = color;
            if (hit) c.a = color.a * (1f - k);   // fade out
            SetColor(c);
            yield return null;   // wait one frame, then continue the loop
        }
        Destroy(gameObject);   // remove the bubble once the animation is done
    }

    // In Mastery's outline stage, make the colour much more see-through
    private Color Dim(Color c)
    {
        if (visibility == Visibility.Outline) c.a *= outlineAlpha;
        return c;
    }

    // URP materials call their colour _BaseColor, older ones just "color"
    private void SetColor(Color c)
    {
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", c);
        else material.color = c;
    }
}
