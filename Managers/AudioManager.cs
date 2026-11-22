using System;
using Enums;
using UnityEngine;

namespace Managers
{
    public enum Sfx
    {
        Footstep, Jump, Landing,
        SwordSwing, MeleeHit, HeavyStrike,
        AreaAttackCast, AreaAttackBlast,
        Curse, Heal, ManaRestore, Sprint,
        SkeletonCollapse, BossDeath, CharacterDeath,
        BossDoor, UiClick, UiError
    }

    /// <summary>
    /// Plays music and sound effects from a fixed pool of AudioSources, so effects never create objects at runtime.
    /// Web builds can't pan or spatialize, so every source is 2D and distance only lowers the volume.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        private AudioManager() { }

        [Serializable]
        private struct SfxClip
        {
            public Sfx id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
            [Range(0f, 0.2f)] public float pitchJitter;
        }

        [SerializeField] private SfxClip[] sfxClips = Array.Empty<SfxClip>();
        [SerializeField, Min(1)] private int voices = 12;
        [SerializeField] private float fullVolumeDistance = 4f;
        [SerializeField] private float silentDistance = 35f;

        [Header("Music")]
        [SerializeField] private AudioClip explorationMusic;
        [SerializeField] private AudioClip battleMusic;
        [SerializeField] private AudioClip bossMusic;
        [SerializeField] private AudioClip clearStinger;
        [SerializeField] private AudioClip overStinger;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.55f;
        [SerializeField] private float crossfadeTime = 1.5f;
        [SerializeField] private float battleMusicHoldTime = 3f; // keeps battle music through short gaps between fights

        private SfxClip[] byId;
        private AudioSource[] pool;
        private int nextVoice;
        private Transform listener;

        private readonly AudioSource[] music = new AudioSource[2];
        private int activeMusic;
        private float musicFade = 1f; // 0..1 progress of the current crossfade
        private float lastBattleTime = float.NegativeInfinity;
        private bool gameEnded;

        public AudioClip CurrentMusic => music[activeMusic] ? music[activeMusic].clip : null;

        private void Awake()
        {
            byId = new SfxClip[Enum.GetValues(typeof(Sfx)).Length];
            foreach (var entry in sfxClips)
                byId[(int)entry.id] = entry;

            pool = new AudioSource[voices];
            for (var i = 0; i < voices; i++)
                pool[i] = NewSource("Voice " + i, false);
            for (var i = 0; i < music.Length; i++)
                music[i] = NewSource("Music " + i, true);
        }

        private AudioSource NewSource(string sourceName, bool loop)
        {
            var source = new GameObject(sourceName).AddComponent<AudioSource>();
            source.transform.SetParent(transform, false);
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        private void Start()
        {
            GameManager.Instance.GameEnded += OnGameEnded;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            var gameManager = GameManager.ExistingInstance;
            if (gameManager)
                gameManager.GameEnded -= OnGameEnded;
        }

        /// <summary>Plays a sound effect. Pass a world position to fade it with distance from the listener.</summary>
        /// <returns>The voice playing it, for <see cref="Stop"/>; null when nothing was played.</returns>
        public AudioSource Play(Sfx id, Vector3? position = null, float volumeScale = 1f)
        {
            var entry = byId[(int)id];
            if (!entry.clip)
                return null;

            var volume = entry.volume * volumeScale * SfxGain * DistanceGain(position);
            if (volume <= 0.001f)
                return null;

            var source = TakeVoice();
            source.clip = entry.clip;
            source.volume = volume;
            source.pitch = 1f + UnityEngine.Random.Range(-entry.pitchJitter, entry.pitchJitter);
            source.Play();
            return source;
        }

        /// <summary>Stops a sound started by <see cref="Play"/>, unless its voice has since been reused.</summary>
        public void Stop(AudioSource voice, Sfx id)
        {
            if (voice && voice.clip == byId[(int)id].clip)
                voice.Stop();
        }

        // Prefers an idle voice; when all are busy, the next one in turn (the oldest) is cut off.
        private AudioSource TakeVoice()
        {
            for (var i = 0; i < pool.Length; i++)
            {
                var source = pool[(nextVoice + i) % pool.Length];
                if (!source.isPlaying)
                {
                    nextVoice = (nextVoice + i + 1) % pool.Length;
                    return source;
                }
            }

            var oldest = pool[nextVoice];
            nextVoice = (nextVoice + 1) % pool.Length;
            return oldest;
        }

        private float DistanceGain(Vector3? position)
        {
            if (position == null)
                return 1f;
            if (!listener)
            {
                var found = FindAnyObjectByType<AudioListener>();
                listener = found ? found.transform : null;
                if (!listener)
                    return 1f;
            }

            var distance = Vector3.Distance(listener.position, position.Value);
            return 1f - Mathf.Clamp01((distance - fullVolumeDistance) / (silentDistance - fullVolumeDistance));
        }

        private static float SfxGain
        {
            get
            {
                var gameManager = GameManager.ExistingInstance;
                return gameManager ? gameManager.MasterVolume * gameManager.SFXVolume : 1f;
            }
        }

        private static float MusicGain
        {
            get
            {
                var gameManager = GameManager.ExistingInstance;
                return gameManager ? gameManager.MasterVolume * gameManager.BGMVolume : 1f;
            }
        }

        private void Update()
        {
            if (!gameEnded)
            {
                var desired = DesiredMusic();
                if (desired && desired != CurrentMusic)
                    SwitchMusic(desired, true);
            }

            musicFade = Mathf.MoveTowards(musicFade, 1f, Time.deltaTime / crossfadeTime);
            var gain = musicVolume * MusicGain;
            music[activeMusic].volume = gain * musicFade;
            var fadingOut = music[1 - activeMusic];
            fadingOut.volume = gain * (1f - musicFade);
            if (musicFade >= 1f && fadingOut.isPlaying)
                StopAndUnload(fadingOut);
        }

        private AudioClip DesiredMusic()
        {
            var gameManager = GameManager.Instance;
            if (gameManager.IsInBattle)
                lastBattleTime = Time.time;

            if (gameManager.IsBossEngaged)
                return bossMusic;
            if (CurrentMusic == bossMusic && Time.time - lastBattleTime < battleMusicHoldTime)
                return bossMusic;
            if (gameManager.IsInBattle || (CurrentMusic == battleMusic && Time.time - lastBattleTime < battleMusicHoldTime))
                return battleMusic;
            return explorationMusic;
        }

        private void SwitchMusic(AudioClip clip, bool loop)
        {
            activeMusic = 1 - activeMusic;
            var next = music[activeMusic];
            if (next.isPlaying)
                StopAndUnload(next);

            next.clip = clip;
            next.loop = loop;
            next.volume = 0f;
            // Music clips don't preload, so only the tracks in use take memory (decoded audio is large on the web).
            clip.LoadAudioData();
            next.Play();
            musicFade = 0f;
        }

        private static void StopAndUnload(AudioSource source)
        {
            source.Stop();
            var clip = source.clip;
            source.clip = null;
            if (clip && clip.preloadAudioData == false)
                clip.UnloadAudioData();
        }

        private void OnGameEnded(GameState result)
        {
            gameEnded = true;
            var stinger = result == GameState.Cleared ? clearStinger : overStinger;
            if (stinger)
                SwitchMusic(stinger, false);
        }
    }
}
