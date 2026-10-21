using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Video;
using TMPro;

[System.Serializable]
public class DanceCue
{
    [Tooltip("When the bubble should be hit, in seconds from the start of the song")]
    public float time;

    [Tooltip("Where the bubble appears in metres, relative to the Dance Spot: x = right, y = up from the floor, z = forward toward the mirror")]
    public Vector3 position;
}

// One song you can learn: its audio, an optional example video, and its bubbles
[System.Serializable]
public class DanceSong
{
    public string name = "Macarena";
    public AudioClip clip;

    [Tooltip("Optional: a video of someone doing this dance, played on the Example Video screen in time with the song")]
    public VideoClip exampleVideo;

    [Tooltip("Video time minus song time. If the dance in the video starts 2 s later than in your audio file, type 2.")]
    public float videoOffset = 0f;

    [Tooltip("Tempo in beats per minute, used by the Fill cues menu items")]
    public float bpm = 103f;

    [Tooltip("Song time, in seconds, of the first move. Find it by listening, then fine-tune with the Nudge buttons.")]
    public float firstMove = 10f;

    [Tooltip("How many times through the routine the Fill cues menu item writes")]
    public int rounds = 3;

    [Tooltip("Optional: the exact time (in seconds) each round starts. Leave empty to space rounds evenly every 16 beats from First Move.")]
    public float[] roundStarts;

    public DanceCue[] cues;
}

// Runs Learn a Dance: plays the song, spawns a bubble for each cue, keeps score,
// and has three stages: Practice (0.5x), Full speed, and Mastery (bubbles fade, then disappear).
public class DanceManager : MonoBehaviour
{
    public enum Stage { Practice, FullSpeed, Mastery }

    [Header("Songs and bubbles")]
    public AudioSource music;
    public DanceBubble bubblePrefab;

    [Tooltip("Where the dancer stands, facing the mirror. Bubble positions are relative to this.")]
    public Transform danceSpot;

    [Tooltip("Element 0 = Macarena, Element 1 = YMCA chorus. Right-click the component to fill their cues.")]
    public DanceSong[] songs;

    [Header("Example video (optional)")]
    [Tooltip("The Video Player on the Example Video screen beside the mirror")]
    public VideoPlayer exampleVideo;

    [Tooltip("Shown only while a dance is playing, e.g. the Example Video screen")]
    public GameObject exampleScreen;

    [Header("Practice speed (optional)")]
    [Tooltip("An Audio Mixer group with a Pitch Shifter set to 200%. Practice plays through it, so the song keeps its normal pitch at 0.5x.")]
    public AudioMixerGroup practiceGroup;

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

    [Header("UI (optional)")]
    [Tooltip("Shows which song is selected")]
    public TMP_Text songText;
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
    private int songIndex;
    private DanceCue[] cues;
    private int nextCue;
    private int hits;
    private int misses;
    private int attempt;
    private float heightScale = 1f;
    private float nextVideoCheck;
    private readonly List<DanceBubble> activeBubbles = new List<DanceBubble>();
    private readonly List<string> history = new List<string>();

    private void Start()
    {
        if (exampleScreen != null) exampleScreen.SetActive(false);
        SelectSong(0);
        if (stageText != null) stageText.SetText("Pick a stage to start");
    }

    // Wire the song buttons' On Click () to this, with 0 for Macarena and 1 for YMCA
    public void SelectSong(int index)
    {
        if (songs == null || index < 0 || index >= songs.Length) return;
        if (playing) StopDance();
        songIndex = index;
        DanceSong song = songs[index];
        cues = song.cues;
        if (song.clip != null) music.clip = song.clip;
        if (exampleVideo != null) exampleVideo.clip = song.exampleVideo;
        if (songText != null) songText.SetText(song.name);
        UpdateScore();
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
        StopVideo();
        ClearBubbles();
        if (stageText != null) stageText.SetText("Stopped");
    }

    private void Begin(Stage newStage)
    {
        if (cues == null || cues.Length == 0)
        {
            Debug.LogWarning("This song has no cues. Right-click the Dance Manager component and choose a Fill cues item.");
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
        // Practice goes through the Pitch Shifter group, which undoes the deeper sound of half speed (0.5 x 2 = normal pitch)
        if (practiceGroup != null) music.outputAudioMixerGroup = stage == Stage.Practice ? practiceGroup : null;
        music.time = Mathf.Max(0f, cues[0].time + timingOffset - secondsBeforeFirstBubble);
        music.Play();
        StartVideo();

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
        else KeepVideoInTime();
    }

    private void StartVideo()
    {
        if (exampleVideo == null || exampleVideo.clip == null) return;
        if (exampleScreen != null) exampleScreen.SetActive(true);
        exampleVideo.audioOutputMode = VideoAudioOutputMode.None; // the song comes from Music
        exampleVideo.playbackSpeed = music.pitch;
        exampleVideo.time = VideoTimeNow();
        exampleVideo.Play();
        nextVideoCheck = Time.time + 1f;
    }

    private void StopVideo()
    {
        if (exampleVideo != null) exampleVideo.Stop();
        if (exampleScreen != null) exampleScreen.SetActive(false);
    }

    // The video and the song drift apart slowly, so jump the video back in line if it's off by more than 0.2 s
    private void KeepVideoInTime()
    {
        // Jumping takes the video a moment, so check at most once a second; checking every frame kept it black
        if (exampleVideo == null || !exampleVideo.isPrepared || !exampleVideo.isPlaying || Time.time < nextVideoCheck) return;
        nextVideoCheck = Time.time + 1f;
        if (Mathf.Abs((float)exampleVideo.time - VideoTimeNow()) > 0.3f) exampleVideo.time = VideoTimeNow();
    }

    private float VideoTimeNow()
    {
        return Mathf.Max(0f, music.time + songs[songIndex].videoOffset);
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
        StopVideo();
        attempt++;

        int percent = Mathf.RoundToInt(100f * hits / cues.Length);
        string line = "Attempt " + attempt + " (" + songs[songIndex].name + ", " + StageName() + "): " + hits + "/" + cues.Length + " (" + percent + "%)";
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

    // Right-click the component > Fill Macarena cues (song 0). One round is 16 beats: 12 hand moves
    // (one per beat), then the hip sway and jump, which have no bubbles. Positions are where the moving
    // hand ends up, relative to the Dance Spot (x = right, y = up, z = toward the mirror), for a 1.6 m person.
    [ContextMenu("Fill Macarena cues (song 0)")]
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

        DanceSong song = EnsureSong(0, "Macarena", 103f, 3);
        float beat = 60f / song.bpm;
        int rounds = Mathf.Max(1, song.rounds);
        song.cues = new DanceCue[rounds * moves.Length];
        for (int r = 0; r < rounds; r++)
        {
            for (int m = 0; m < moves.Length; m++)
            {
                float time = song.firstMove + (r * 16 + m) * beat;
                song.cues[r * moves.Length + m] = new DanceCue { time = time, position = moves[m] };
            }
        }
        MarkDirty();
    }

    // Right-click the component > Fill YMCA chorus cues (song 1). Each "Y-M-C-A" is four letters, one beat apart,
    // and comes round every 16 beats. Two-handed letters get a bubble for each hand at the same moment.
    [ContextMenu("Fill YMCA chorus cues (song 1)")]
    private void FillYmcaCues()
    {
        Vector3[][] letters =
        {
            new[] { new Vector3( 0.35f, 1.95f, 0.25f), new Vector3(-0.35f, 1.95f, 0.25f) }, // Y  both arms up and out
            new[] { new Vector3( 0.12f, 1.78f, 0.10f), new Vector3(-0.12f, 1.78f, 0.10f) }, // M  fingertips on top of head
            new[] { new Vector3(-0.05f, 1.90f, 0.30f), new Vector3(-0.40f, 1.30f, 0.30f) }, // C  both arms curve to the left
            new[] { new Vector3( 0.00f, 2.00f, 0.20f) },                                    // A  hands meet above head
        };

        DanceSong song = EnsureSong(1, "YMCA", 127f, 4);
        // Where "Y-M-C-A" is sung in the clip: 0:14, 0:17, 0:29, 0:32
        if (song.roundStarts == null || song.roundStarts.Length == 0) song.roundStarts = new[] { 14f, 17f, 29f, 32f };
        float beat = 60f / song.bpm;
        var list = new List<DanceCue>();
        int rounds = song.roundStarts.Length;
        for (int r = 0; r < rounds; r++)
        {
            for (int l = 0; l < letters.Length; l++)
            {
                float time = song.roundStarts[r] + l * beat;
                foreach (var pos in letters[l]) list.Add(new DanceCue { time = time, position = pos });
            }
        }
        song.cues = list.ToArray();
        MarkDirty();
    }

    private DanceSong EnsureSong(int index, string name, float bpm, int rounds)
    {
        if (songs == null || songs.Length <= index) System.Array.Resize(ref songs, index + 1);
        if (songs[index] == null) songs[index] = new DanceSong { name = name, bpm = bpm, rounds = rounds };
        if (string.IsNullOrEmpty(songs[index].name)) songs[index].name = name;
        return songs[index];
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
