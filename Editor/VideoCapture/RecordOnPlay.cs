using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace VideoCapture
{
    /// <summary>
    /// Tools > Video > Record On Play: every play session becomes a 1080p60 MP4 take with audio,
    /// plus draft subtitles (.ko.srt, .en.srt) and an event list (.events.tsv) under .temp/video/raw.
    /// Nothing is drawn on screen; the subtitles are separate files.
    /// </summary>
    [InitializeOnLoad]
    public static class RecordOnPlay
    {
        private const string MenuPath = "Tools/Video/Record On Play";
        private const string PrefKey = "VideoCapture.RecordOnPlay";
        public const string OnceKey = "VideoCapture.RecordOnce"; // set for a single session (Record Autopilot Take)
        private const int Width = 1920, Height = 1080, FrameRate = 60;

        private static RecorderController controller;
        private static GameEventWatcher watcher;
        private static string takePath;

        public static string OutputFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".temp", "video", "raw"));

        static RecordOnPlay() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        [MenuItem(MenuPath)]
        private static void Toggle() => EditorPrefs.SetBool(PrefKey, !EditorPrefs.GetBool(PrefKey));

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PrefKey));
            return true;
        }

        [MenuItem("Tools/Video/Open Output Folder")]
        private static void OpenFolder()
        {
            Directory.CreateDirectory(OutputFolder);
            EditorUtility.RevealInFinder(OutputFolder + Path.DirectorySeparatorChar);
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && (EditorPrefs.GetBool(PrefKey) || SessionState.GetBool(OnceKey, false)))
            {
                SessionState.SetBool(OnceKey, false);
                Begin();
            }
            else if (change == PlayModeStateChange.ExitingPlayMode)
                End();
        }

        private static void Begin()
        {
            Directory.CreateDirectory(OutputFolder);
            takePath = Path.Combine(OutputFolder, "take-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = "Gameplay";
            movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
            };
            movie.CaptureAudio = true;
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = Width, OutputHeight = Height };
            movie.OutputFile = takePath;

            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            settings.AddRecorderSettings(movie);
            settings.SetRecordModeToManual();
            settings.FrameRatePlayback = FrameRatePlayback.Constant;
            settings.FrameRate = FrameRate;
            settings.CapFrameRate = true;

            controller = new RecorderController(settings);
            controller.PrepareRecording();
            if (!controller.StartRecording())
            {
                Debug.LogError("[Video] The Recorder did not start; check the Recorder window for details.");
                controller = null;
                return;
            }

            // Started together so subtitle time 0 is the first recorded frame.
            watcher = new GameEventWatcher();
            watcher.Begin();
            Debug.Log($"[Video] Recording {takePath}.mp4");
        }

        private static void End()
        {
            if (controller == null)
                return;
            controller.StopRecording();
            controller = null;

            watcher.End();
            File.WriteAllText(takePath + ".events.tsv", Captions.ToTsv(watcher.Events), Utf8);
            WriteSubtitles(takePath, watcher.Events);
            Debug.Log($"[Video] Saved {takePath}.mp4 with {watcher.Events.Count} logged events.");
            watcher = null;
        }

        private static readonly UTF8Encoding Utf8 = new(false);

        private static void WriteSubtitles(string take, IReadOnlyList<GameEvent> events)
        {
            File.WriteAllText(take + ".ko.srt", Captions.ToSrt(events, korean: true), Utf8);
            File.WriteAllText(take + ".en.srt", Captions.ToSrt(events, korean: false), Utf8);
        }

        /// <summary>Rewrites every take's subtitles from its event list, after the wording in Captions changes.</summary>
        [MenuItem("Tools/Video/Rebuild Subtitles")]
        public static void RebuildSubtitles()
        {
            foreach (var tsv in Directory.GetFiles(OutputFolder, "*.events.tsv"))
                WriteSubtitles(tsv[..^".events.tsv".Length], Captions.FromTsv(File.ReadAllText(tsv, Utf8)));
            Debug.Log("[Video] Rebuilt subtitles in " + OutputFolder);
        }
    }
}
