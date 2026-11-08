# V2_Natasha_DECO2300 – TikTok XR Dance Studio

This is my DECO2300 project repository. It holds my final Unity VR prototype (Interactive Prototype 2b) and all of my reports for the course.

**TikTok XR Dance Studio** is a VR dance studio for TikTok users, built for the Meta Quest with hand tracking. It has three tasks:

1. **Record** – grab the camera, snap it onto the tripod, and press Record and Stop to film yourself.
2. **Filters** – browse filters on the panel (Original, Rio de Janeiro, Clarendon, Gingham, Stars, Water), preview them on the whole room, and press Apply.
3. **Learn a Dance** – learn the Macarena (or the YMCA chorus) by hitting bubbles with your hands, in Practice (0.5x speed), Full speed and Mastery modes, with your score saved after each attempt.

There is also a welcome panel, a Go To panel for jumping between stations, and a **?** hint button at each station.

## Where to find things

| What | Where |
| --- | --- |
| IP2b testing plan | [`All Reports/6 Interactive Prototype 2b/IP2B Testing Plan.docx`](<All Reports/6 Interactive Prototype 2b/IP2B Testing Plan.docx>) |
| IP2b test results (Google Form responses) | [`All Reports/7 Design Evaluation Outcome 3/Evidence/DECO2300 IP2 (Responses).xlsx`](<All Reports/7 Design Evaluation Outcome 3/Evidence/DECO2300 IP2 (Responses).xlsx>) |
| IP2b in-testing note sheets (ease ratings, NASA-TLX) | [`All Reports/7 Design Evaluation Outcome 3/Evidence/In-session notes/IP2 In-Testing.pdf`](<All Reports/7 Design Evaluation Outcome 3/Evidence/In-session notes/IP2 In-Testing.pdf>) |
| Session voice recordings (U3, U4) | [`All Reports/7 Design Evaluation Outcome 3/Evidence/U3-4 Session Voice Recordings/`](<All Reports/7 Design Evaluation Outcome 3/Evidence/U3-4 Session Voice Recordings/>) |
| Meta Quest screen recordings (U2–U4) | [`All Reports/7 Design Evaluation Outcome 3/Evidence/Meta Quest Screen Recordings/`](<All Reports/7 Design Evaluation Outcome 3/Evidence/Meta Quest Screen Recordings/>) |
| Design Evaluation Outcome 3 and Reflection | [`All Reports/7 Design Evaluation Outcome 3/DECO2300 Design Evaluation Outcome 3.pdf`](<All Reports/7 Design Evaluation Outcome 3/DECO2300 Design Evaluation Outcome 3.pdf>) |
| Unity project | [`FINALFINALVR/`](FINALFINALVR/) |
| My scripts | [`FINALFINALVR/Assets/Scripts/`](FINALFINALVR/Assets/Scripts/) |
| Editor tools used to build the scene | [`FINALFINALVR/Assets/Editor/`](FINALFINALVR/Assets/Editor/) |

### Meta Quest screen recordings

The testing sessions for U2, U3 and U4 were screen-recorded on the Meta Quest with Meta Quest Developer Hub casting. They are in [`All Reports/7 Design Evaluation Outcome 3/Evidence/Meta Quest Screen Recordings/`](<All Reports/7 Design Evaluation Outcome 3/Evidence/Meta Quest Screen Recordings/>):

- [U2 Testing.mp4](<All Reports/7 Design Evaluation Outcome 3/Evidence/Meta Quest Screen Recordings/U2 Testing.mp4>) (8 min 54 s)
- [U3 Testing Pt.1.mp4](<All Reports/7 Design Evaluation Outcome 3/Evidence/Meta Quest Screen Recordings/U3 Testing Pt.1.mp4>) (5 min 22 s)
- [U3 Testing Pt.2.mp4](<All Reports/7 Design Evaluation Outcome 3/Evidence/Meta Quest Screen Recordings/U3 Testing Pt.2.mp4>) (9 min 24 s)
- [U4 Testing.mp4](<All Reports/7 Design Evaluation Outcome 3/Evidence/Meta Quest Screen Recordings/U4 Testing.mp4>) (5 min 51 s)

The original recordings were about 7 GB, which is over GitHub's 100 MB file limit, so these are compressed copies (720p, 30 fps, no audio, as in the originals). The full-quality originals are kept on my laptop and are available on request.

## All reports

Reports from the whole course are in [`All Reports/`](<All Reports/>), numbered in the order they were submitted:

0. [AI acknowledgement and asset references for the whole VR project](<All Reports/0 VR Project AI Acknowledgement and References/>)
1. [Design Concept Report](<All Reports/1 Design Concept Report/>)
2. [Interactive Prototype 1](<All Reports/2 Interactive Prototype 1/>)
3. [Design Evaluation Outcome 1](<All Reports/3 Design Evaluation Outcome 1/>)
4. [Interactive Prototype 2a – testing plan](<All Reports/4 Interactive Prototype 2a/>)
5. [Design Evaluation Outcome 2](<All Reports/5 Design Evaluation Outcome 2/>)
6. [Interactive Prototype 2b – testing plan](<All Reports/6 Interactive Prototype 2b/>)
7. [Design Evaluation Outcome 3 and Reflection, with testing evidence](<All Reports/7 Design Evaluation Outcome 3/>)

AI acknowledgement coversheets are in folder 0 (whole project), the IP2b folder and the Design Evaluation Outcome 3 folder. See [How AI was used](#how-ai-was-used) below.

## Opening and running the prototype

1. Open the `FINALFINALVR` folder in **Unity 6000.3.20f1** with Android Build Support installed.
2. Open the scene `Assets/Scenes/SampleScene.unity`.
3. To run it on a Meta Quest: switch the platform to Android in **File > Build Profiles**, connect the headset, then **Build and Run**. Hand tracking needs to be turned on in the headset's settings.

## A note on the repository history (and the names)

This project has ended up spread across three repositories, so here is what happened:

1. **[Natasha_DECO2300_2026](https://github.com/natashalim-uq/Natasha_DECO2300_2026/tree/main)** was my original repository. About a week after Interactive Prototype 1 (IP1) my files stopped opening, and even with my tutor's help we couldn't get them working again.
2. **Natasha_DECO2300_2026_redo** was created with my tutor's help as a second attempt. Unfortunately, a few days later its files stopped opening as well. I only ever kept this one locally on my laptop and never pushed it to GitHub, because its `.gitignore` wasn't working and it would have uploaded far too many files. Its contents are now on the [`old-repo-archive`](../../tree/old-repo-archive) branch of this repository instead.
3. **V2_Natasha_DECO2300** (this repository) is where I restarted the project from scratch, and it holds my current, working project.

I'm sorry about the confusing names of the repositories and folders (including `FINALFINALVR`). I've kept them as they are because renaming them now could break file paths and references inside the Unity project.

### Where the older work is

The earlier work (week activities, the original Design Concept Report files with storyboards and paper prototype photos, Design Evaluation Outcome 1, and Interactive Prototype 1 with its full commit history) is on a separate branch:

**[`old-repo-archive`](../../tree/old-repo-archive)**

It is kept apart from `main` so it doesn't mix with or affect the current Unity project.

## How AI was used

I used Claude throughout the build of this project. The full statement is in the [AI acknowledgement coversheet for the whole VR project](<All Reports/0 VR Project AI Acknowledgement and References/AI_Acknowledgment_Coversheet_Entire VR Project.docx>). In short:

- **IP1:** I built it myself. Claude only helped write the C# for the interactions.
- **The two broken repositories:** when my files stopped opening, I used Claude for several days to try to debug the problem, and to write a new `.gitignore` for the second repository with my tutor's help. Neither fix worked, which is why this third repository exists (see the history above).
- **Rebuilding in this repository:** a lot of time had been lost by then, so I asked Claude to look through my IP1 files and write step-by-step instructions for rebuilding the project in Unity. I followed those instructions to rebuild it.
- **Finishing the tasks and making improvements from user feedback:** I described what each task still needed and what testers wanted changed, and Claude wrote step-by-step instructions and helped with the scripting.

Every single report also has its own AI acknowledgement coversheets in their folders.
