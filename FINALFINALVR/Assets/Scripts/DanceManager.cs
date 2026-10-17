using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class DanceCue
{
    [Tooltip("When the bubble should be hit, in seconds from the start of the song")]
    public float time;

    [Tooltip("Where the bubble appears in metres, relative to the Dance Spot: x = right, y = up from the floor, z = forward toward the mirror")]
    public Vector3 position;
}

// Runs Learn a Dance: plays the song, spawns a bubble for each cue, keeps score,
// and has three stages: Practice (0.5x), Full speed, and Mastery (bubbles fade, then disappear).
public class DanceManager : MonoBehaviour
{
    public enum Stage { Practice, FullSpeed, Mastery }

    [Header("Song and bubbles")]
    public AudioSource music;
    public DanceBubble bubblePrefab;

    [Tooltip("Where the dancer stands, facing the mirror. Bubble positions are relative to this.")]
    public Transform danceSpot;

    public DanceCue[] cues;

    [Header("Timing (seconds of song time)")]
    [Tooltip("How early the approach glow appears before a bubble is due")]
    public float leadTime = 1f;

    [Tooltip("How long after it's due a bubble can still be hit")]
    public float hitWindow = 0.8f;

    [Tooltip("How early before it's due a bubble can already be hit")]
    public float earlyWindow = 0.3f;

    [Tooltip("Shifts every cue later (+) or earlier (-). The Nudge buttons change this while you test in the headset.")]
    public float timingOffset = 0f;

    [Tooltip("The song starts this many seconds before the first bubble, so long intros are skipped")]
    public float secondsBeforeFirstBubble = 3f;

    [Tooltip("Cue heights are written for someone about 1.6 m tall. Tick to scale them to the player's real head height.")]
    public bool scaleToPlayerHeight = true;

    [Header("Macarena cue generator (right-click the component > Fill with Macarena cues)")]
    [Tooltip("Tempo of your Macarena file. The common versions are about 103 BPM.")]
    public float macarenaBpm = 103f;

    [Tooltip("Song time, in seconds, of the first move (right arm out). Find it by listening, then fine-tune with the Nudge buttons.")]
    public float macarenaFirstMove = 10f;

    [Tooltip("How many times through the 16-count routine (12 bubbles each)")]
    public int macarenaRounds = 3;

    [Header("UI (optional)")]
    public TMP_Text scoreText;
    public TMP_Text stageText;
    public TMP_Text historyText;

    // Song time in seconds. Practice plays at half speed, but cue times still line up because this is the song's own clock.
    public float SongTime
    {
        get
        {
            if (music.isPlaying) return music.time;
            return music.clip != null ? music.clip.length : 0f;
        }
    }

    private Stage stage;
    private bool playing;
    private int nextCue;
    private int hits;
    private int misses;
    private int attempt;
    private float heightScale = 1f;
    private readonly List<DanceBubble> activeBubbles = new List<DanceBubble>();
    private readonly List<string> history = new List<string>();

    private void Start()
    {
        UpdateScore();
        if (stageText != null) stageText.SetText("Pick a stage to start");
    }

    // Wire the three stage buttons' On Click () to these
    public void StartPractice() { Begin(Stage.Practice); }
    public void StartFullSpeed() { Begin(Stage.FullSpeed); }
    public void StartMastery() { Begin(Stage.Mastery); }

    // Wire a Stop button to this
    public void StopDance()
    {
        playing = false;
        music.Stop();
        ClearBubbles();
        if (stageText != null) stageText.SetText("Stopped");
    }

    private void Begin(Stage newStage)
    {
        if (cues == null || cues.Length == 0)
        {
            Debug.LogWarning("DanceManager has no cues. Right-click the component and choose 'Fill with test cues', or add your own.");
            return;
        }

        ClearBubbles();
        System.Array.Sort(cues, (a, b) => a.time.CompareTo(b.time));

        stage = newStage;
        nextCue = 0;
        heightScale = 1f;
        if (scaleToPlayerHeight && Camera.main != null)
        {
            float headHeight = Camera.main.transform.position.y - danceSpot.position.y;
            if (headHeight > 0.8f) heightScale = headHeight / 1.6f;
        }
        hits = 0;
        misses = 0;
        playing = true;

        music.pitch = stage == Stage.Practice ? 0.5f : 1f;
        music.time = Mathf.Max(0f, cues[0].time + timingOffset - secondsBeforeFirstBubble);
        music.Play();

        if (stageText != null) stageText.SetText(StageName());
        UpdateScore();
    }

    private void Update()
    {
        if (!playing) return;

        // Spawn each bubble a little before it's due, so its approach glow shows where it will be
        float now = SongTime;
        while (nextCue < cues.Length && now >= cues[nextCue].time + timingOffset - leadTime)
        {
            Spawn(nextCue);
            nextCue++;
        }

        if (hits + misses >= cues.Length) Finish();
    }

    private void Spawn(int index)
    {
        DanceCue cue = cues[index];
        Vector3 local = cue.position;
        local.y *= heightScale;
        Vector3 worldPos = danceSpot.TransformPoint(local);
        DanceBubble bubble = Instantiate(bubblePrefab, worldPos, Quaternion.identity);

        // Mastery: first half of the song shows faint outlines, second half shows nothing
        DanceBubble.Visibility visibility = DanceBubble.Visibility.Full;
        if (stage == Stage.Mastery)
        {
            visibility = index < cues.Length / 2 ? DanceBubble.Visibility.Outline : DanceBubble.Visibility.Hidden;
        }

        bubble.Init(this, cue.time + timingOffset, visibility);
        activeBubbles.Add(bubble);
    }

    // Wire two small buttons to these. If bubbles arrive after the beat, press Earlier; if before, Later.
    public void NudgeEarlier() { Nudge(-0.05f); }
    public void NudgeLater() { Nudge(0.05f); }

    private void Nudge(float amount)
    {
        timingOffset += amount;
        Debug.Log("Dance timing offset: " + timingOffset.ToString("0.00") + " s (copy this into Timing Offset after testing)");
        if (stageText != null) stageText.SetText("Timing " + timingOffset.ToString("+0.00;-0.00") + " s");
    }

    // Called by DanceBubble
    public void BubbleHit(DanceBubble bubble)
    {
        hits++;
        activeBubbles.Remove(bubble);
        UpdateScore();
    }

    // Called by DanceBubble
    public void BubbleMissed(DanceBubble bubble)
    {
        misses++;
        activeBubbles.Remove(bubble);
        UpdateScore();
    }

    private void Finish()
    {
        playing = false;
        music.Stop();
        attempt++;

        int percent = Mathf.RoundToInt(100f * hits / cues.Length);
        string line = "Attempt " + attempt + " (" + StageName() + "): " + hits + "/" + cues.Length + " (" + percent + "%)";
        history.Add(line);
        Debug.Log(line); // also visible in Android Logcat, handy for your log sheet

        if (stageText != null) stageText.SetText("Done! " + percent + "%");
        if (historyText != null)
        {
            int first = Mathf.Max(0, history.Count - 5);
            historyText.SetText(string.Join("\n", history.GetRange(first, history.Count - first)));
        }
    }

    private void ClearBubbles()
    {
        foreach (var bubble in activeBubbles)
        {
            if (bubble != null) Destroy(bubble.gameObject);
        }
        activeBubbles.Clear();
    }

    private void UpdateScore()
    {
        if (scoreText == null) return;
        int total = cues != null ? cues.Length : 0;
        scoreText.SetText("Hit " + hits + "   Missed " + misses + "   of " + total);
    }

    private string StageName()
    {
        switch (stage)
        {
            case Stage.Practice: return "Practice (0.5x)";
            case Stage.FullSpeed: return "Full speed";
            default: return "Mastery";
        }
    }

    // Right-click the component in the Inspector to fill in 28 placeholder cues, 2 seconds apart,
    // alternating high/low and left/right. Replace them with your real timings and positions.
    [ContextMenu("Fill with test cues")]
    private void FillTestCues()
    {
        Vector3[] spots =
        {
            new Vector3(-0.4f, 1.5f, 0.45f),
            new Vector3(0.4f, 1.5f, 0.45f),
            new Vector3(-0.5f, 1.0f, 0.4f),
            new Vector3(0.5f, 1.0f, 0.4f),
        };
        cues = new DanceCue[28];
        for (int i = 0; i < cues.Length; i++)
        {
            cues[i] = new DanceCue { time = 2f + i * 2f, position = spots[i % spots.Length] };
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // Right-click the component > Fill with Macarena cues. One round is 16 beats: 12 hand moves
    // (one per beat), then the hip sway and jump, which have no bubbles. Positions are where the moving
    // hand ends up, relative to the Dance Spot (x = right, y = up, z = toward the mirror), for a 1.6 m person.
    [ContextMenu("Fill with Macarena cues")]
    private void FillMacarenaCues()
    {
        Vector3[] moves =
        {
            new Vector3( 0.25f, 1.30f, 0.50f), // 1  right arm out, palm down
            new Vector3(-0.25f, 1.30f, 0.50f), // 2  left arm out, palm down
            new Vector3( 0.25f, 1.20f, 0.50f), // 3  right palm up
            new Vector3(-0.25f, 1.20f, 0.50f), // 4  left palm up
            new Vector3(-0.15f, 1.40f, 0.20f), // 5  right hand to left shoulder
            new Vector3( 0.15f, 1.40f, 0.20f), // 6  left hand to right shoulder
            new Vector3( 0.15f, 1.75f, 0.10f), // 7  right hand to back of head
            new Vector3(-0.15f, 1.75f, 0.10f), // 8  left hand to back of head
            new Vector3(-0.15f, 0.95f, 0.20f), // 9  right hand to left hip
            new Vector3( 0.15f, 0.95f, 0.20f), // 10 left hand to right hip
            new Vector3( 0.28f, 0.95f, 0.00f), // 11 right hand to right back pocket
            new Vector3(-0.28f, 0.95f, 0.00f), // 12 left hand to left back pocket
        };

        float beat = 60f / macarenaBpm;
        int rounds = Mathf.Max(1, macarenaRounds);
        cues = new DanceCue[rounds * moves.Length];
        for (int r = 0; r < rounds; r++)
        {
            for (int m = 0; m < moves.Length; m++)
            {
                float time = macarenaFirstMove + (r * 16 + m) * beat;
                cues[r * moves.Length + m] = new DanceCue { time = time, position = moves[m] };
            }
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
