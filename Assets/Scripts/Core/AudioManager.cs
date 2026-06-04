using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// 全局音频管理器（DontDestroyOnLoad 单例，按需懒创建）。
    /// 总音量沿用 AudioListener.volume（已由设置滑条控制），此处不重复做主控。
    ///
    /// 用法：
    ///   AudioManager.Get().PlaySfx("hit");                 // 随机取 Resources/Audio/SFX/hit/* 之一
    ///   AudioManager.Get().PlayMusic("dungeon_ambient_1"); // 循环 Resources/Audio/Music/<name>
    ///
    /// SFX 子目录约定：hit（命中敌人）/ hurt（玩家受伤）/ swing（挥砍）/ coin（拾金）/ ui。
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Range(0f, 1f)] public float musicVolume = 0.5f;
        [Range(0f, 1f)] public float sfxVolume   = 1f;

        private AudioSource   _music;
        private AudioSource[] _sfxPool;   // round-robin，允许多个 SFX 重叠
        private int           _sfxNext;
        private string        _currentMusic;

        private readonly Dictionary<string, AudioClip[]> _sfxCache   = new Dictionary<string, AudioClip[]>();
        private readonly Dictionary<string, AudioClip>   _musicCache = new Dictionary<string, AudioClip>();

        /// 取得（或按需创建）单例
        public static AudioManager Get()
        {
            if (Instance == null)
            {
                var go = new GameObject("AudioManager");
                Instance = go.AddComponent<AudioManager>();
            }
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _music = gameObject.AddComponent<AudioSource>();
            _music.loop        = true;
            _music.playOnAwake = false;

            _sfxPool = new AudioSource[6];
            for (int i = 0; i < _sfxPool.Length; i++)
            {
                _sfxPool[i] = gameObject.AddComponent<AudioSource>();
                _sfxPool[i].playOnAwake = false;
            }
        }

        /// 随机播放某事件类别下的一个音效（带轻微音高扰动，避免连发单调）。
        public void PlaySfx(string category, float volume = 1f, float pitchVar = 0.08f)
        {
            var clips = LoadSfx(category);
            if (clips == null || clips.Length == 0) return;
            var clip = clips[Random.Range(0, clips.Length)];
            var src  = _sfxPool[_sfxNext];
            _sfxNext = (_sfxNext + 1) % _sfxPool.Length;
            src.pitch = 1f + Random.Range(-pitchVar, pitchVar);
            src.PlayOneShot(clip, sfxVolume * volume);
        }

        /// 循环播放一首 BGM；同名且正在播放则忽略（楼层内反复调用不会重启）。
        public void PlayMusic(string name)
        {
            if (_currentMusic == name && _music.isPlaying) return;
            var clip = LoadMusic(name);
            if (clip == null) return;
            _currentMusic = name;
            _music.clip   = clip;
            _music.volume = musicVolume;
            _music.Play();
        }

        public void StopMusic()
        {
            _music.Stop();
            _currentMusic = null;
        }

        private AudioClip[] LoadSfx(string category)
        {
            if (_sfxCache.TryGetValue(category, out var c)) return c;
            c = Resources.LoadAll<AudioClip>($"Audio/SFX/{category}");
            _sfxCache[category] = c;
            return c;
        }

        private AudioClip LoadMusic(string name)
        {
            if (_musicCache.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>($"Audio/Music/{name}");
            _musicCache[name] = c;
            return c;
        }
    }
}
