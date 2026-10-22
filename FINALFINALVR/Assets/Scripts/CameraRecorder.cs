using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Records what the in-game camera sees to a video file on the headset (no plugin needed).
// Put it on a Unity Camera inside the Digital Camera model, pointing out of the lens.
// Videos are saved as .avi (Motion JPEG, no sound) in the app's Recordings folder:
// Android/data/<your package name>/files/Recordings on the Quest. Play them with VLC.
//
// HOW IT WORKS, in short:
//  1. A second Unity Camera sits in the camera model's lens and draws into a texture (an image in memory),
//     not into the headset.
//  2. 15 times a second, that image is copied from the graphics card back to normal memory.
//  3. A background thread squashes each image into a JPG and adds it to the .avi file.
//  4. On Stop, the file's header is filled in (frame count, frame rate) so video players can open it.
// WHY: an app on the Quest can't press the headset's own Record button, so to make "Record" really
// record I had to save the video myself. Motion JPEG .avi was chosen because it is simple enough to write
// by hand (each frame is just a JPG) and Unity can already make JPGs, so no extra plugin is needed.
public class CameraRecorder : MonoBehaviour
{
    [Tooltip("The Camera that films. Leave empty to use the Camera on this object.")]
    public Camera captureCamera;

    // Video size and frame rate. Kept small on purpose: the Quest is a phone-sized computer and it
    // also has to draw the VR scene twice (once per eye) at the same time.
    public int width = 640;
    public int height = 360;
    public int framesPerSecond = 15;

    // JPG quality: higher = sharper but bigger files and more work per frame
    [Range(10, 100)]
    public int jpgQuality = 75;

    [Tooltip("Optional: a Quad on the camera's back screen that shows the live view")]
    public Renderer viewfinder;

    [Tooltip("Keep the camera rendering when not recording, so the viewfinder is live. Untick to save performance.")]
    public bool liveViewfinder = true;

    [Tooltip("Tick if saved videos come out upside down")]
    public bool flipVertically = false;

    // "{ get; private set; }" means other scripts (like RecordToggle) can READ these, but only this script can change them
    public bool IsRecording { get; private set; }
    public string LastSavedPath { get; private set; }
    public float LastDuration { get; private set; }

    // The image in memory that the lens camera draws into
    private RenderTexture renderTexture;
    // A queue (waiting line) of frames between the main thread and the background thread.
    // BlockingCollection is a thread-safe queue, so both threads can use it at once without clashing.
    private BlockingCollection<byte[]> frameQueue;
    private float nextFrameTime;   // when the next frame should be grabbed
    private float startTime;       // when recording started, to work out the length

    private void Awake()
    {
        if (captureCamera == null) captureCamera = GetComponent<Camera>();

        // Make the texture the camera draws into (24 = depth buffer bits, so things in front hide things behind)
        renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        renderTexture.Create();

        // Render to the texture only, never to the headset
        // (otherwise the lens camera would take over the player's view)
        captureCamera.targetTexture = renderTexture;
        captureCamera.stereoTargetEye = StereoTargetEyeMask.None;
        var cameraData = captureCamera.GetUniversalAdditionalCameraData();
        if (cameraData != null) cameraData.allowXRRendering = false;
        // If the live viewfinder is off, the lens camera only switches on while recording, to save performance
        captureCamera.enabled = liveViewfinder;

        // Show the same texture on the little screen on the back of the camera model
        if (viewfinder != null) viewfinder.material.mainTexture = renderTexture;
    }

    public void StartRecording()
    {
        if (IsRecording) return;   // already recording, so ignore a second press

        // persistentDataPath is the app's own folder on the headset, which it's always allowed to write to
        string folder = Path.Combine(Application.persistentDataPath, "Recordings");
        Directory.CreateDirectory(folder);   // does nothing if the folder is already there
        // Name each file with the date and time so new recordings never overwrite old ones
        string path = Path.Combine(folder, "clip_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".avi");

        var writer = new AviWriter(path, width, height);
        // The queue holds at most 30 frames (2 seconds), so memory can't fill up if the writer falls behind
        var queue = new BlockingCollection<byte[]>(30);
        frameQueue = queue;
        LastSavedPath = path;

        // Frames are turned into JPGs and written to the file on a background thread, so the headset doesn't stutter
        // (JPG encoding is slow. Doing it on the main thread would drop the VR frame rate, which can cause motion sickness.)
        // The values are copied into local variables so the thread uses fixed values even if the Inspector changes.
        int w = width, h = height, quality = jpgQuality;
        var worker = new Thread(() =>
        {
            // GetConsumingEnumerable waits for the next frame, and ends the loop once recording has stopped
            foreach (byte[] raw in queue.GetConsumingEnumerable())
            {
                byte[] jpg = ImageConversion.EncodeArrayToJPG(raw,
                    UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, (uint)w, (uint)h, 0, quality);
                writer.AddFrame(jpg);
            }
            // All frames are written, so finish the file (fill in the header and index)
            writer.Finish(writer.Duration);
        });
        worker.IsBackground = true;   // a background thread won't keep the app alive if it is closed
        worker.Start();
        queueWriter = writer;

        captureCamera.enabled = true;   // make sure the lens camera is drawing
        startTime = Time.time;
        nextFrameTime = Time.time;
        IsRecording = true;
        Debug.Log("Recording to " + path);
    }

    // Returns the saved file's path
    public string StopRecording()
    {
        if (!IsRecording) return null;
        IsRecording = false;

        // Let frames already on their way arrive, then tell the background thread to finish the file
        AsyncGPUReadback.WaitAllRequests();
        LastDuration = Time.time - startTime;
        // The real length is passed to the writer so the video plays back at the right speed
        queueWriter.Duration = LastDuration;
        frameQueue.CompleteAdding();   // "no more frames are coming", which ends the background loop
        frameQueue = null;

        captureCamera.enabled = liveViewfinder;
        Debug.Log("Saved " + LastDuration.ToString("0.0") + " s video to " + LastSavedPath);
        return LastSavedPath;
    }

    // The writer for the current recording (kept so StopRecording can give it the final length)
    private AviWriter queueWriter;

    private void Update()
    {
        if (!IsRecording) return;

        // Grab a frame only every 1/15th of a second, not every VR frame (the headset runs at 72+ fps)
        if (Time.time >= nextFrameTime)
        {
            nextFrameTime += 1f / framesPerSecond;
            // If the app hitched and we're far behind, don't try to catch up with a burst of frames
            if (nextFrameTime < Time.time) nextFrameTime = Time.time + 1f / framesPerSecond;

            var queue = frameQueue;
            // Ask the graphics card for a copy of the image WITHOUT waiting for it (Async).
            // Waiting would freeze the game for a moment every frame. When the copy is ready,
            // OnFrameRead is called with it.
            AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGBA32, request => OnFrameRead(request, queue));
        }
    }

    // Called when the graphics card has finished copying a frame back
    private void OnFrameRead(AsyncGPUReadbackRequest request, BlockingCollection<byte[]> queue)
    {
        // Ignore the frame if something went wrong or recording has already stopped
        if (request.hasError || queue == null || queue.IsAddingCompleted) return;

        byte[] raw = request.GetData<byte>().ToArray();   // the pixels as plain bytes (4 per pixel: R, G, B, A)
        if (flipVertically) FlipRows(raw, width * 4, height);

        // If the background thread falls behind, drop the frame rather than freeze the headset
        // (TryAdd gives up straight away if the queue is full; Add would wait)
        queue.TryAdd(raw);
    }

    // Swaps the top row of pixels with the bottom row, then the 2nd with the 2nd-last, and so on.
    // Some graphics cards store images bottom-up, which would give an upside-down video.
    private static void FlipRows(byte[] data, int rowBytes, int rows)
    {
        byte[] temp = new byte[rowBytes];
        for (int top = 0, bottom = rows - 1; top < bottom; top++, bottom--)
        {
            Buffer.BlockCopy(data, top * rowBytes, temp, 0, rowBytes);
            Buffer.BlockCopy(data, bottom * rowBytes, data, top * rowBytes, rowBytes);
            Buffer.BlockCopy(temp, 0, data, bottom * rowBytes, rowBytes);
        }
    }

    // OnDestroy runs when the object is removed or the app closes
    private void OnDestroy()
    {
        if (IsRecording) StopRecording();   // so a half-finished video is still saved properly
        if (renderTexture != null) renderTexture.Release();   // free the graphics memory
    }

    // Writes JPG frames into a Motion JPEG .avi file
    // An .avi file is made of labelled "chunks": a 4-letter code (FourCC) like "RIFF" or "LIST",
    // then the chunk's size, then its contents. The layout below follows the AVI file format:
    //   RIFF "AVI "
    //     LIST "hdrl"  (headers: size, frame rate, number of frames...)
    //     LIST "movi"  (the frames themselves, one "00dc" chunk per JPG)
    //     idx1         (an index saying where each frame starts, so players can skip around)
    // Some numbers (sizes, frame count, frame rate) aren't known until recording ends, so a 0 is written
    // as a placeholder, its position is remembered (the ...Pos variables), and Patch() fills it in at the end.
    private class AviWriter
    {
        // "volatile" because this is set on the main thread and read on the background thread
        public volatile float Duration;

        private readonly FileStream file;
        private readonly BinaryWriter output;
        private readonly List<int> offsets = new List<int>();   // where each frame starts, for the index
        private readonly List<int> sizes = new List<int>();     // how big each frame is, for the index
        // Positions of placeholder numbers to fill in at the end
        private long riffSizePos, microSecPerFramePos, totalFramesPos, bufferSizePos1;
        private long ratePos, lengthPos, bufferSizePos2, moviSizePos, moviStartPos;
        private int largestFrame;

        public AviWriter(string path, int width, int height)
        {
            file = new FileStream(path, FileMode.Create, FileAccess.Write);
            output = new BinaryWriter(file);

            // The whole file: "RIFF", file size (filled in later), "AVI "
            FourCC("RIFF"); riffSizePos = file.Position; output.Write(0); FourCC("AVI ");

            // Header list
            FourCC("LIST"); long hdrlSizePos = file.Position; output.Write(0); FourCC("hdrl");

            // Main header
            FourCC("avih"); output.Write(56);
            microSecPerFramePos = file.Position; output.Write(0);
            output.Write(0);          // max bytes per second
            output.Write(0);          // padding
            output.Write(0x10);       // has index
            totalFramesPos = file.Position; output.Write(0);
            output.Write(0);          // initial frames
            output.Write(1);          // streams
            bufferSizePos1 = file.Position; output.Write(0);
            output.Write(width);
            output.Write(height);
            output.Write(0); output.Write(0); output.Write(0); output.Write(0);   // reserved, always 0

            FourCC("LIST"); long strlSizePos = file.Position; output.Write(0); FourCC("strl");

            // Stream header
            // "vids" = this stream is video, "MJPG" = each frame is a JPG
            FourCC("strh"); output.Write(56);
            FourCC("vids"); FourCC("MJPG");
            output.Write(0);          // flags
            output.Write((short)0);   // priority
            output.Write((short)0);   // language
            output.Write(0);          // initial frames
            output.Write(1000);       // scale (rate / scale = frames per second)
            ratePos = file.Position; output.Write(0);
            output.Write(0);          // start
            lengthPos = file.Position; output.Write(0);
            bufferSizePos2 = file.Position; output.Write(0);
            output.Write(-1);         // quality
            output.Write(0);          // sample size
            output.Write((short)0); output.Write((short)0);
            output.Write((short)width); output.Write((short)height);

            // Stream format
            // Describes each picture: its size and that it's MJPG
            FourCC("strf"); output.Write(40);
            output.Write(40);
            output.Write(width);
            output.Write(height);
            output.Write((short)1);   // planes
            output.Write((short)24);  // bits per pixel
            FourCC("MJPG");
            output.Write(width * height * 3);
            output.Write(0); output.Write(0); output.Write(0); output.Write(0);

            // The two header lists are complete now, so their sizes are known and can be filled in
            Patch(strlSizePos, (int)(file.Position - strlSizePos - 4));
            Patch(hdrlSizePos, (int)(file.Position - hdrlSizePos - 4));

            // Start the "movi" list, where the frames go
            FourCC("LIST"); moviSizePos = file.Position; output.Write(0);
            moviStartPos = file.Position; FourCC("movi");
        }

        // Adds one JPG as the next frame
        public void AddFrame(byte[] jpg)
        {
            // Remember where this frame is and how big, for the index at the end
            offsets.Add((int)(file.Position - moviStartPos));
            sizes.Add(jpg.Length);
            largestFrame = Math.Max(largestFrame, jpg.Length);

            // "00dc" = stream 0, compressed video frame
            FourCC("00dc");
            output.Write(jpg.Length);
            output.Write(jpg);
            // The AVI format needs every chunk to be an even number of bytes, so pad odd ones with one 0
            if (jpg.Length % 2 == 1) output.Write((byte)0);
        }

        public void Finish(float durationSeconds)
        {
            long moviEnd = file.Position;

            // Write the index: one 16-byte entry per frame
            FourCC("idx1"); output.Write(offsets.Count * 16);
            for (int i = 0; i < offsets.Count; i++)
            {
                FourCC("00dc");
                output.Write(0x10);   // keyframe
                output.Write(offsets[i]);
                output.Write(sizes[i]);
            }
            long end = file.Position;

            // Use the real frame rate, so the video plays back at the right speed
            // (the Quest may not manage exactly 15 fps, so frames / seconds gives the true rate)
            int frames = offsets.Count;
            float fps = frames > 0 && durationSeconds > 0f ? frames / durationSeconds : 15f;

            // Go back and fill in every placeholder now that the numbers are known
            Patch(riffSizePos, (int)(end - 8));
            Patch(moviSizePos, (int)(moviEnd - moviSizePos - 4));
            Patch(microSecPerFramePos, (int)(1000000f / fps));
            Patch(totalFramesPos, frames);
            Patch(bufferSizePos1, largestFrame);
            Patch(ratePos, Mathf.RoundToInt(fps * 1000f));
            Patch(lengthPos, frames);
            Patch(bufferSizePos2, largestFrame);

            output.Flush();
            file.Dispose();   // close the file so it's complete on disk
        }

        // Writes a 4-letter chunk label such as "RIFF"
        private void FourCC(string code)
        {
            output.Write(System.Text.Encoding.ASCII.GetBytes(code));
        }

        // Jumps back to an earlier position in the file, overwrites the placeholder, then jumps back to the end
        private void Patch(long position, int value)
        {
            long current = file.Position;
            file.Position = position;
            output.Write(value);
            file.Position = current;
        }
    }
}
