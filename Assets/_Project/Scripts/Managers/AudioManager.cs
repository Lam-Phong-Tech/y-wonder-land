using System.Collections.Generic;
using UnityEngine;

namespace YWonderLand.Managers
{
    /// <summary>
    /// AudioManager TỐI GIẢN: nhạc nền (loop) + hiệu ứng (SFX). Tải clip từ Resources/Audio/&lt;tên&gt;.
    /// THIẾU clip thì bỏ qua ÊM (chỉ log 1 lần) — game vẫn chạy bình thường. Tự tạo nếu scene chưa có.
    ///
    /// ► CÁCH CÓ TIẾNG: thả file .wav/.mp3 vào "Assets/Resources/Audio/" đúng tên:
    ///   "chop" (chặt/đào) · "harvest" (thu hoạch) · "coin" (mua/bán).
    ///
    /// ► NHẠC NỀN THEO ĐẢO: thả "bgm_farm" (Nông trại), "bgm_city" (Thành phố), "bgm_mine" (Mỏ)
    ///   vào "Assets/Resources/Audio/". IslandTravelManager tự gọi PlayMusicForIsland() mỗi khi
    ///   đổi đảo — chưa có file thì im lặng, không lỗi. Xem PlayMusicForIsland() bên dưới.
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

        // Cho popup Cài đặt đọc để dựng đúng vị trí thanh trượt lúc mở.
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

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

        public void PlayMusic(string clipName, float fadeSeconds = 0f)
        {
            var clip = Load(clipName);
            if (clip == null || musicSource == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return; // đang phát rồi

            _fadeToken++;
            if (fadeSeconds > 0f && musicSource.isPlaying)
            {
                _ = CrossfadeToAsync(clip, fadeSeconds, _fadeToken);
            }
            else
            {
                musicSource.clip = clip;
                musicSource.volume = musicVolume;
                musicSource.Play();
            }
        }

        /// <summary>
        /// Đổi nhạc nền theo đảo hiện tại. Tên clip quy ước "bgm_&lt;islandId&gt;" (vd "bgm_farm", "bgm_city").
        /// Gọi bởi IslandTravelManager mỗi khi tới đảo mới. Thiếu file clip thì im lặng, không lỗi.
        /// </summary>
        public void PlayMusicForIsland(string islandId, float fadeSeconds = 1.2f)
        {
            if (string.IsNullOrEmpty(islandId)) return;
            PlayMusic($"bgm_{islandId}", fadeSeconds);
        }

        private int _fadeToken;

        // Fade nửa đầu (tắt dần bài cũ) rồi đổi clip, fade nửa sau (lên dần bài mới).
        // Dùng _fadeToken để huỷ ngang nếu có lệnh đổi nhạc mới đè lên giữa chừng.
        private async Awaitable CrossfadeToAsync(AudioClip clip, float duration, int token)
        {
            float half = duration * 0.5f;
            float startVol = musicSource.volume;

            float t = 0f;
            while (t < half)
            {
                if (token != _fadeToken) return;
                t += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, half <= 0f ? 1f : t / half);
                await Awaitable.NextFrameAsync();
            }
            if (token != _fadeToken) return;

            musicSource.clip = clip;
            musicSource.Play();

            t = 0f;
            while (t < half)
            {
                if (token != _fadeToken) return;
                t += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0f, musicVolume, half <= 0f ? 1f : t / half);
                await Awaitable.NextFrameAsync();
            }
            if (token != _fadeToken) return;
            musicSource.volume = musicVolume;
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
