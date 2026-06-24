using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Managers
{
    /// <summary>
    /// AudioManager TỐI GIẢN: nhạc nền (loop) + hiệu ứng (SFX). Tải clip từ Resources/Audio/&lt;tên&gt;.
    /// THIẾU clip thì bỏ qua ÊM (chỉ log 1 lần) — game vẫn chạy bình thường. Tự tạo nếu scene chưa có.
    ///
    /// ► CÁCH CÓ TIẾNG: thả file .wav/.mp3 vào "Assets/Resources/Audio/" đúng tên:
    ///   "bgm" (nhạc nền) · "chop" (chặt/đào) · "harvest" (thu hoạch) · "coin" (mua/bán).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null && Application.isPlaying)
                {
                    var go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private AudioSource musicSource;
        private AudioSource sfxSource;
        private float musicVolume = 0.5f;
        private float sfxVolume = 0.8f;

        private readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
        private readonly HashSet<string> _missingLogged = new HashSet<string>();

        void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;

            musicVolume = PlayerPrefs.GetFloat("YW_MusicVol", 0.5f);
            sfxVolume = PlayerPrefs.GetFloat("YW_SfxVol", 0.8f);

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        // Tải + cache clip (cache cả null để khỏi load lại file thiếu).
        private AudioClip Load(string clipName)
        {
            if (string.IsNullOrEmpty(clipName)) return null;
            if (_cache.TryGetValue(clipName, out var cached)) return cached;

            var clip = Resources.Load<AudioClip>($"Audio/{clipName}");
            _cache[clipName] = clip;
            if (clip == null && _missingLogged.Add(clipName))
                Debug.Log($"[Audio] Chưa có 'Resources/Audio/{clipName}' — bỏ qua (thả file vào để có tiếng).");
            return clip;
        }

        public void PlayMusic(string clipName)
        {
            var clip = Load(clipName);
            if (clip == null || musicSource == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return; // đang phát rồi
            musicSource.clip = clip;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        public void PlaySFX(string clipName, float volumeScale = 1f)
        {
            var clip = Load(clipName);
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume * Mathf.Clamp01(volumeScale));
        }

        public void SetMusicVolume(float v)
        {
            musicVolume = Mathf.Clamp01(v);
            if (musicSource != null) musicSource.volume = musicVolume;
            PlayerPrefs.SetFloat("YW_MusicVol", musicVolume);
        }

        public void SetSFXVolume(float v)
        {
            sfxVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("YW_SfxVol", sfxVolume);
        }
    }
}
