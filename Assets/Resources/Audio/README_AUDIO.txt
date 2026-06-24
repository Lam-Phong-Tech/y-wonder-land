ÂM THANH — Y WONDER GREEN FARM
================================

AudioManager (Assets/_Project/Scripts/Managers/AudioManager.cs) tự tải clip theo TÊN từ thư mục này.
Thả file .wav / .mp3 / .ogg vào đây, ĐẶT ĐÚNG TÊN (không cần đuôi trong code):

  bgm       -> nhạc nền (tự loop khi vào game)
  chop      -> chặt cây / đào đá
  harvest   -> thu hoạch cây trồng
  coin      -> mua / bán ở shop

Thiếu file nào thì game BỎ QUA êm (chỉ log 1 dòng trong Console), không lỗi.

Muốn thêm tiếng khác: gọi  AudioManager.Instance.PlaySFX("tên_file")  ở chỗ cần,
rồi thả file "tên_file" vào thư mục này.

Âm lượng: AudioManager.SetMusicVolume(0..1) / SetSFXVolume(0..1) (lưu PlayerPrefs YW_MusicVol/YW_SfxVol).
(Có thể nối với slider trong SettingsPopup sau — hiện 2 slider đó đang là TODO.)
