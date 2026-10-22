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
public class CameraRecorder : MonoBehaviour
{
    [Tooltip("The Camera that films. Leave empty to use the Camera on this object.")]
    public Camera captureCamera;

    public int width = 640;
    public int height = 360;
    public int framesPerSecond = 15;

    [Range(10, 100)]
    public int jpgQuality = 75;

    [Tooltip("Optional: a Quad on the camera's back screen that shows the live view")]
    public Renderer viewfinder;

    [Tooltip("Keep the camera rendering when not recording, so the viewfinder is live. Untick to save performance.")]
    public bool liveViewfinder = true;

    [Tooltip("Tick if saved videos come out upside down")]
    public bool flipVertically = false;

    public bool IsRecording { get; private set; }
    public string LastSavedPath { get; private set; }
    public float LastDuration { get; private set; }

    private RenderTexture renderTexture;
    private BlockingCollection<byte[]> frameQueue;
    private float nextFrameTime;
    private float startTime;

    private void Awake()
    {
        if (captureCamera == null) captureCamera = GetComponent<Camera>();

        renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        renderTexture.Create();

        // Render to the texture only, never to the headset
        captureCamera.targetTexture = renderTexture;
        captureCamera.stereoTargetEye = StereoTargetEyeMask.None;
        var cameraData = captureCamera.GetUniversalAdditionalCameraData();
        if (cameraData != null) cameraData.allowXRRendering = false;
        captureCamera.enabled = liveViewfinder;

        if (viewfinder != null) viewfinder.material.mainTexture = renderTexture;
    }

    public void StartRecording()
    {
        if (IsRecording) return;

        string folder = Path.Combine(Application.persistentDataPath, "Recordings");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "clip_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".avi");

        var writer = new AviWriter(path, width, height);
        var queue = new BlockingCollection<byte[]>(30);
        frameQueue = queue;
        LastSavedPath = path;

        // Frames are turned into JPGs and written to the file on a background thread, so the headset doesn't stutter
        int w = width, h = height, quality = jpgQuality;
        var worker = new Thread(() =>
        {
            foreach (byte[] raw in queue.GetConsumingEnumerable())
            {
                byte[] jpg = ImageConversion.EncodeArrayToJPG(raw,
                    UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, (uint)w, (uint)h, 0, quality);
                writer.AddFrame(jpg);
            }
            writer.Finish(writer.Duration);
        });
        worker.IsBackground = true;
        worker.Start();
        queueWriter = writer;

        captureCamera.enabled = true;
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
        queueWriter.Duration = LastDuration;
        frameQueue.CompleteAdding();
        frameQueue = null;

        captureCamera.enabled = liveViewfinder;
        Debug.Log("Saved " + LastDuration.ToString("0.0") + " s video to " + LastSavedPath);
        return LastSavedPath;
    }

    private AviWriter queueWriter;

    private void Update()
    {
        if (!IsRecording) return;

        if (Time.time >= nextFrameTime)
        {
            nextFrameTime += 1f / framesPerSecond;
            if (nextFrameTime < Time.time) nextFrameTime = Time.time + 1f / framesPerSecond;

            var queue = frameQueue;
            AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGBA32, request => OnFrameRead(request, queue));
        }
    }

    private void OnFrameRead(AsyncGPUReadbackRequest request, BlockingCollection<byte[]> queue)
    {
        if (request.hasError || queue == null || queue.IsAddingCompleted) return;

        byte[] raw = request.GetData<byte>().ToArray();
        if (flipVertically) FlipRows(raw, width * 4, height);

        // If the background thread falls behind, drop the frame rather than freeze the headset
        queue.TryAdd(raw);
    }

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

    private void OnDestroy()
    {
        if (IsRecording) StopRecording();
        if (renderTexture != null) renderTexture.Release();
    }

    // Writes JPG frames into a Motion JPEG .avi file
    private class AviWriter
    {
        public volatile float Duration;

        private readonly FileStream file;
        private readonly BinaryWriter output;
        private readonly List<int> offsets = new List<int>();
        private readonly List<int> sizes = new List<int>();
        private long riffSizePos, microSecPerFramePos, totalFramesPos, bufferSizePos1;
        private long ratePos, lengthPos, bufferSizePos2, moviSizePos, moviStartPos;
        private int largestFrame;

        public AviWriter(string path, int width, int height)
        {
            file = new FileStream(path, FileMode.Create, FileAccess.Write);
            output = new BinaryWriter(file);

            FourCC("RIFF"); riffSizePos = file.Position; output.Write(0); FourCC("AVI ");

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
            output.Write(0); output.Write(0); output.Write(0); output.Write(0);

            FourCC("LIST"); long strlSizePos = file.Position; output.Write(0); FourCC("strl");

            // Stream header
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
            FourCC("strf"); output.Write(40);
            output.Write(40);
            output.Write(width);
            output.Write(height);
            output.Write((short)1);   // planes
            output.Write((short)24);  // bits per pixel
            FourCC("MJPG");
            output.Write(width * height * 3);
            output.Write(0); output.Write(0); output.Write(0); output.Write(0);

            Patch(strlSizePos, (int)(file.Position - strlSizePos - 4));
            Patch(hdrlSizePos, (int)(file.Position - hdrlSizePos - 4));

            FourCC("LIST"); moviSizePos = file.Position; output.Write(0);
            moviStartPos = file.Position; FourCC("movi");
        }

        public void AddFrame(byte[] jpg)
        {
            offsets.Add((int)(file.Position - moviStartPos));
            sizes.Add(jpg.Length);
            largestFrame = Math.Max(largestFrame, jpg.Length);

            FourCC("00dc");
            output.Write(jpg.Length);
            output.Write(jpg);
            if (jpg.Length % 2 == 1) output.Write((byte)0);
        }

        public void Finish(float durationSeconds)
        {
            long moviEnd = file.Position;

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
            int frames = offsets.Count;
            float fps = frames > 0 && durationSeconds > 0f ? frames / durationSeconds : 15f;

            Patch(riffSizePos, (int)(end - 8));
            Patch(moviSizePos, (int)(moviEnd - moviSizePos - 4));
            Patch(microSecPerFramePos, (int)(1000000f / fps));
            Patch(totalFramesPos, frames);
            Patch(bufferSizePos1, largestFrame);
            Patch(ratePos, Mathf.RoundToInt(fps * 1000f));
            Patch(lengthPos, frames);
            Patch(bufferSizePos2, largestFrame);

            output.Flush();
            file.Dispose();
        }

        private void FourCC(string code)
        {
            output.Write(System.Text.Encoding.ASCII.GetBytes(code));
        }

        private void Patch(long position, int value)
        {
            long current = file.Position;
            file.Position = position;
            output.Write(value);
            file.Position = current;
        }
    }
}
