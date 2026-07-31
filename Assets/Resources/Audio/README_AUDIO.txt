ÂM THANH — Y WONDER GREEN FARM
================================

AudioManager (Assets/_Project/Scripts/Managers/AudioManager.cs) tự tải clip theo TÊN từ thư mục này.
Thả file .wav / .mp3 / .ogg vào đây, ĐẶT ĐÚNG TÊN (không cần đuôi trong code):

  bgm_farm  -> nhạc nền khi ở Nông trại
  bgm_city  -> nhạc nền khi ở Thành phố
  bgm_mine  -> nhạc nền khi ở Mỏ đào khoáng (đảo này đang MỞ trong bản demo)
  chop      -> chặt cây / đào đá
  harvest   -> thu hoạch cây trồng
  coin      -> mua / bán ở shop

Nhạc nền tự đổi bài khi đi đảo (IslandTravelManager gọi AudioManager.PlayMusicForIsland),
có crossfade nhẹ ~1.2s cho đỡ giật cụt. Thiếu file nào thì game BỎ QUA êm (chỉ log 1 dòng
trong Console), không lỗi — cứ thả file vào là có tiếng ngay, không cần sửa code.

Muốn thêm tiếng khác: gọi  AudioManager.Instance.PlaySFX("tên_file")  ở chỗ cần,
rồi thả file "tên_file" vào thư mục này.

Âm lượng: đã có sẵn 2 thanh trượt Nhạc nền / Hiệu ứng trong popup Cài đặt
(SettingsPopupController), lưu PlayerPrefs YW_MusicVol / YW_SfxVol — không cần làm gì thêm.
