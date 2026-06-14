## 00_TongQuan
| KỊCH BẢN MÔ PHỎNG 3D — GAME YWONDERLAND | Unnamed: 1 |
| --- | --- |
| Hạng mục | Mô tả |
| Tên dự án | YWONDERLAND — Game nông trại & đời sống 3D |
| Thể loại | Farming / Life-Simulation / Social MMO nhẹ |
| Nền tảng | Unity 3D (URP) — Build cho Mobile (iOS/Android) + PC |
| Đối tượng người chơi | Yêu thích nông trại, sưu tầm, mạng xã hội nhẹ |
| Phiên bản tài liệu | 1.0 — Kịch bản chi tiết dựa trên form mẫu Unity3D |
| Tổng số Module | 16 module chính (xem sheet 01\_Modules) |
| Tổng số Map (Scene 3D) | 5 map: Nông trại, Thành phố, Khai thác mỏ, Đảo Hải Phú, Đảo Mộc Nhi |
| Tổng số Camera Preset | 12 preset (xem sheet 03\_Cameras) |
| Tổng số Animation Trigger | 32 trigger (xem sheet 06\_Animations) |
| Tổng số 3D Asset | 60+ items (xem sheet 02\_Assets) |
| Số NPC | 10+ NPC (NPC hướng dẫn, shop, Pet, AI Chat) |
| Cơ chế điều khiển | Joystick trái di chuyển, vùng phải xoay camera (giới hạn Pitch), nút Jump bên phải |
| Hệ thống tiền tệ | POS (kiếm trong game) + UPOS (nạp tiền) |
| Thời gian thực | Trồng trọt/chăn nuôi theo thời gian thực (24h thu hoạch, 10h tưới...) |
| Ngôn ngữ | Tiếng Việt (có thể mở rộng EN trong cài đặt) |
| NaN | NaN |
| MỤC TIÊU KỊCH BẢN | NaN |
| Về gameplay | Mang trải nghiệm nông trại 3D thư giãn, có chiều sâu (canh tác, chăn nuôi, câu cá, khai mỏ, chế tác). Người chơi có thể tự xây dựng nông trại riêng và phát triển nhân vật theo thời gian thực. |
| Về kinh tế trong game | Vòng kinh tế khép kín: trồng/nuôi/khai thác → bán lấy POS → mua hạt giống/vật nuôi/nâng cấp dụng cụ. Hệ thống Heo đất tạo gửi lãi suất, khuyến khích retention dài hạn. |
| Về xã hội | Hệ thống bạn bè, chat real-time, NPC AI chat làm sôi động cộng đồng. Bảng xếp hạng nhiều tiêu chí (level, thời trang, tiền tệ, thú cưng). |
| Về monetization | F2P-friendly: nạp UPOS để mua vật phẩm VIP, mở Đảo Hải Phú/Mộc Nhi, mua thẻ thành viên KNX. Không pay-to-win nặng, chủ yếu là tiện ích & thẩm mỹ. |

## 01_Modules
| DANH SÁCH 16 MODULE CHỨC NĂNG | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 |
| --- | --- | --- | --- | --- |
| STT | Module | Tên chức năng chính | Cơ chế / Yêu cầu | Mức ưu tiên |
| 1 | Đăng ký tài khoản | Email, Tên đăng nhập, Mật khẩu | Email hợp lệ; username > 8 ký tự; password > 8 ký tự (1 hoa, 1 thường, 1 số, 1 ký tự đặc biệt). | P0 - Bắt buộc |
| 2 | Đăng nhập | Màn hình đăng nhập, chọn giới tính, đặt tên nhân vật | Chỉ chọn giới tính & tên nhân vật khi đăng ký mới; KHÔNG đổi được sau khi xác nhận. | P0 - Bắt buộc |
| 3 | Vào game | Cảnh mở màn, thông báo chào mừng, NPC hướng dẫn | Lần đầu đăng nhập: cinematic chào mừng + tutorial. NPC tự đợi nếu người chơi không đi theo. | P0 - Bắt buộc |
| 4 | Màn hình game (HUD) | Ảnh nhân vật, chat, nhiệm vụ, xếp hạng, hộp thư, bạn bè, túi đồ, tiền tệ, cài đặt, event, info nhân vật, điều khiển 3D | Joystick trái di chuyển, vùng phải xoay camera (Clamp Pitch tránh nhìn lên trời), nút Jump bên phải. | P0 - Bắt buộc |
| 5 | Hệ thống Map (5 cảnh) | Nông trại, Thành phố, Khai thác mỏ, Đảo Hải Phú, Đảo Mộc Nhi | Hải Phú yêu cầu Lv40 + Vip/Vé Hải Phú; Mộc Nhi yêu cầu Lv60 + Vip/Vé Mộc Nhi. Tuyến tính mở khóa. | P0 - Bắt buộc |
| 6 | Nông trại | Cuốc đất, xây chuồng/đường/đèn, trồng hạt giống, nuôi vật nuôi, chặt cây, đào đá, shop hạt giống & item | Trồng/tưới/bón theo thời gian thực. Vật nuôi có thể bị bệnh/chết đói. Chuồng tối đa 10 (mở rộng = VIP). | P0 - Bắt buộc |
| 7 | Thành phố | Câu cá, Bán cá, Workshop, YWonderLand, Mini Garden, Hai Lúa, Kỷ Nguyên Xanh, Maid, Pet, Game, Store, Gift Box, Heo Đất | 13 cửa hàng/dịch vụ con. Câu cá free 10 lần/ngày; Heo đất gửi lãi 12/30/180 ngày. | P0 - Bắt buộc |
| 8 | Khai thác mỏ | Item shop (vé đào, quặng), đào quặng | Free 10 lần đào/ngày, hết phải mua vé. Mỗi lần có xác suất nhận quặng hoặc không. | P1 - Quan trọng |
| 9 | Tiền tệ | POS (kiếm trong game) và UPOS (nạp tiền) | POS: trồng trọt/chăn nuôi/bán quặng-cá/lãi suất. UPOS: nạp; mua vật phẩm VIP. | P0 - Bắt buộc |
| 10 | Event | Đổi quà, gói ưu đãi UPOS, điểm danh hàng ngày | Vật phẩm sự kiện nhận từ câu cá / đào quặng. Có chuỗi điểm danh. | P1 - Quan trọng |
| 11 | Level | Hệ thống cấp độ mở khóa chức năng | Mỗi level mở chức năng/map mới. Lv40 = Hải Phú, Lv60 = Mộc Nhi. | P0 - Bắt buộc |
| 12 | Kinh nghiệm (EXP) | Thu thập từ trồng trọt & chăn nuôi | Mỗi level có định mức EXP cao hơn level trước. Animation level-up khi đầy thanh. | P0 - Bắt buộc |
| 13 | Bạn bè | Kết bạn, tìm bạn theo tên, thăm nông trại bạn | Tìm bạn theo TÊN TÀI KHOẢN. Thăm nông trại bạn yêu cầu VIP. | P1 - Quan trọng |
| 14 | Pet (Thú cưng) | Mua tại Pet Shop, luôn chạy theo người chơi | Pet là follower, có animation Idle/Follow/Happy. Có thể có pet VIP với cosmetic riêng. | P1 - Quan trọng |
| 15 | NPC AI Chat | AI trả lời tự động ở khung chat | Tạo sẵn pool câu trả lời theo tag từ khóa để làm sôi động khung chat khi user ít. | P2 - Nice to have |
| 16 | Chính sách trò chơi | Real-time farming/herding, nhiệm vụ hàng ngày | Người chơi không vào game mỗi ngày → nhiệm vụ thất bại, cây/vật nuôi có thể chết. | P0 - Bắt buộc |

## 02_Assets
| YÊU CẦU 3D ASSETS — MAP, NHÂN VẬT, ITEMS | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 |
| --- | --- | --- | --- | --- |
| A. MÔI TRƯỜNG (5 SCENE 3D) | NaN | NaN | NaN | NaN |
| STT | Scene 3D | ID | Mô tả | Yêu cầu mở khóa |
| 1 | Nông trại | farm\_scene | Khu trồng trọt + chăn nuôi + shop. Có ô đất canh tác, chuồng, ao nhỏ, cây xanh, đá. | Mặc định |
| 2 | Thành phố | city\_scene | Phố thương mại với 13 cửa hàng (Câu cá, Workshop, Store, Maid, Pet, Heo đất...). | Hoàn thành tutorial |
| 3 | Khai thác mỏ | mine\_scene | Hang/mỏ với các cục đá có thể đào. Ánh sáng tối, hiệu ứng particle bụi. | Lv 10 |
| 4 | Đảo Hải Phú | haiphu\_island | Đảo nhiệt đới biển, cây cọ, cát trắng, bãi câu cá đặc biệt. | Lv 40 + VIP/Vé Hải Phú |
| 5 | Đảo Mộc Nhi | mocnhi\_island | Đảo rừng huyền bí, sương mù, vật phẩm hiếm. | Lv 60 + VIP/Vé Mộc Nhi |
| NaN | NaN | NaN | NaN | NaN |
| B. NHÂN VẬT 3D | NaN | NaN | NaN | NaN |
| STT | Nhân vật | Model yêu cầu | Bone/Rig & Tính năng | Animation states |
| 1 | Nhân vật người chơi (Nam) | Nam, tóc/áo/quần thay được (cosmetic), có thẻ thành viên hiển thị | Humanoid Rig - cầm cuốc/rìu/cần câu/xô. Blend shape mặt: vui/buồn/mệt. | Idle, Walk, Run, Jump, Hoe, Water, Chop, Mine, Fish, Feed, Pet, Sit, LevelUp |
| 2 | Nhân vật người chơi (Nữ) | Nữ, tóc/váy/áo thay được (cosmetic), có thẻ thành viên hiển thị | Humanoid Rig - cầm cuốc/rìu/cần câu/xô. Blend shape mặt: vui/buồn/mệt. | Idle, Walk, Run, Jump, Hoe, Water, Chop, Mine, Fish, Feed, Pet, Sit, LevelUp |
| 3 | NPC hướng dẫn | Nhân vật thân thiện, hoạt hình, có biểu cảm chỉ tay | Humanoid Rig - waypoint follow. Tự đợi nếu người chơi không theo (Idle Loop). | Idle, Walk, Wave, Point, Talk, ShowQuest |
| 4 | NPC Shop Keeper (8 loại) | Mỗi shop có 1 NPC riêng (Hai Lúa, Maid, Pet Shop...) | Humanoid Rig - đứng yên tại quầy. Trigger UI khi click. | Idle, Greet, Talk, Wave |
| 5 | Maid (Hầu gái) | Nữ NPC cosmetic váy maid, follow người chơi tại nông trại | Humanoid Rig - hỗ trợ tưới nước/thu hoạch tự động (VIP). | Idle, Walk, Water, Harvest, Bow |
| 6 | Pet (Thú cưng) — nhiều loại | Chó, mèo, thỏ, cáo... mỗi loại 1 model | Quadruped Rig - follow người chơi, không tham gia gameplay khác. | Idle, Walk, Run, Sit, Jump, Happy, Eat |
| 7 | Vật nuôi nông trại | Gà, vịt, bò, heo, dê... có biểu hiện bệnh (model variant) | Quadruped/Biped Rig - di chuyển trong chuồng (NavMesh). | Idle, Eat, Sick, Sleep, Produce (đẻ trứng/cho sữa) |
| 8 | Cá (câu cá) | Nhiều loại cá phổ thông + hiếm. Hiệu ứng nhảy khi cắn câu. | Skeletal/Procedural Animation - swim loop, jump bite. | Swim, Bite, Caught |
| NaN | NaN | NaN | NaN | NaN |
| NaN | NaN | NaN | NaN | NaN |
| C. ITEMS / PROPS 3D (Dụng cụ, vật phẩm) | NaN | NaN | NaN | NaN |
| STT | Tên item | ID | Mô tả & sử dụng | Loại |
| 1 | Cuốc | hoe | Cuốc đất canh tác, có thể nâng cấp ở Workshop | Dụng cụ |
| 2 | Rìu | axe | Chặt cây lấy gỗ | Dụng cụ |
| 3 | Cần câu | fishing\_rod | Câu cá tại thành phố/đảo | Dụng cụ |
| 4 | Xô tưới | watering\_can | Tưới nước cho cây trồng (mỗi 10h) | Dụng cụ |
| 5 | Bình phân bón | fertilizer | Bón phân tăng tốc cây trồng | Tiêu hao |
| 6 | Vacxin | vaccine | Tiêm cho vật nuôi để kháng bệnh | Tiêu hao |
| 7 | Thuốc điều trị | medicine | Chữa bệnh cho vật nuôi đã bệnh | Tiêu hao |
| 8 | Mồi câu | fishing\_bait | Tăng tỉ lệ câu cá hiếm | Tiêu hao |
| 9 | Vé đào mỏ | mine\_ticket | Mua thêm lượt đào quặng | Tiêu hao |
| 10 | Vé Hải Phú | haiphu\_ticket | Mở khóa truy cập đảo Hải Phú (nếu không VIP) | Tiêu hao |
| 11 | Vé Mộc Nhi | mocnhi\_ticket | Mở khóa truy cập đảo Mộc Nhi (nếu không VIP) | Tiêu hao |
| 12 | Hạt cà rốt | seed\_carrot | Trồng cà rốt, thu hoạch sau 24h | Hạt giống |
| 13 | Hạt cải | seed\_cabbage | Trồng cải | Hạt giống |
| 14 | Hạt dưa hấu | seed\_watermelon | Trồng dưa hấu (chu kì dài hơn) | Hạt giống |
| 15 | Hạt rau muống | seed\_morning\_glory | Trồng rau muống (chu kì ngắn) | Hạt giống |
| 16 | Hạt bí ngô | seed\_pumpkin | Trồng bí ngô (theo mùa event) | Hạt giống |
| 17 | Hạt bắp | seed\_corn | Trồng bắp | Hạt giống |
| 18 | Dây khoai lang giống | seed\_sweet\_potato | Trồng khoai lang | Hạt giống |
| 19 | Cỏ voi | seed\_grass | Trồng cỏ làm thức ăn cho vật nuôi | Hạt giống |
| 20 | Gỗ | wood | Chặt cây ra gỗ; xây chuồng cần 4 gỗ | Vật liệu |
| 21 | Đá | stone | Đào ra; xây đường cần 1 đá | Vật liệu |
| 22 | Quặng | ore | Đào ở mỏ; xây đèn cần 8 quặng | Vật liệu |
| 23 | Nông sản thu hoạch | harvest\_produce | Cà rốt, cải, dưa, bắp... bán ở Mini Garden | Sản phẩm |
| 24 | Sản phẩm chăn nuôi | animal\_produce | Trứng, sữa, thịt... bán ở Mini Garden | Sản phẩm |
| 25 | Cá đã câu | fish\_caught | Bán ở Bán cá / Shop bán cá | Sản phẩm |
| 26 | Túi đồ (Inventory) | inventory\_ui | Hiển thị theo danh mục: sản phẩm/nguyên liệu/vật liệu/hạt giống/vé/dụng cụ/quà | UI |
| 27 | Bảng nhiệm vụ | quest\_board\_ui | Liệt kê nhiệm vụ chính + phụ + daily | UI |
| 28 | Hộp thư | mailbox\_ui | Chỉ nhận thông báo hệ thống | UI |
| 29 | Bảng xếp hạng | leaderboard\_ui | 5 tiêu chí: EXP/Level/Thời trang/Tiền tệ/Pet | UI |
| 30 | Khung chat | chat\_ui | Chat global + nút bật/tắt | UI |
| 31 | Tóc (Hair pack) | cosmetic\_hair | Bán tại Store, 10+ kiểu | Cosmetic |
| 32 | Áo (Top) | cosmetic\_top | Bán tại Store | Cosmetic |
| 33 | Quần/Váy (Bottom) | cosmetic\_bottom | Bán tại Store | Cosmetic |
| 34 | Phụ kiện | cosmetic\_accessory | Mũ, kính, balo... | Cosmetic |
| 35 | Tiền POS (icon) | currency\_pos | Tiền kiếm trong game | Tiền tệ |
| 36 | Tiền UPOS (icon) | currency\_upos | Tiền nạp | Tiền tệ |
| 37 | Chuồng vật nuôi | animal\_pen | Hàng rào — yêu cầu 4 gỗ; tối đa 10/người (mở thêm = VIP) | Cấu trúc |
| 38 | Đường đi | path\_tile | Lát đường — yêu cầu 1 đá/ô | Cấu trúc |
| 39 | Đèn chiếu sáng | lamp\_post | Đèn — yêu cầu 8 quặng | Cấu trúc |
| 40 | Ô đất canh tác | farm\_tile | Ô đã cuốc, có thể trồng | Cấu trúc |
| 41 | Heo đất | piggy\_bank | Vật phẩm tương tác — gửi lãi 12/30/180 ngày | Đặc biệt |
| 42 | Thẻ thành viên KNX | knx\_membership | Mua ở Kỷ Nguyên Xanh — quyền lợi VIP | Đặc biệt |
| 43 | Gift Box | gift\_box | Gói quà gửi cho bạn bè | Đặc biệt |

## 03_Cameras
| THIẾT LẬP CAMERA — 12 GÓC NHÌN | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 | Unnamed: 5 |
| --- | --- | --- | --- | --- | --- |
| Camera 3rd-person bám sát nhân vật. Vùng phải màn hình xoay camera (giới hạn Pitch để không nhìn lên trời). Camera tự đổi preset theo bối cảnh (UI shop, câu cá, tutorial...). | NaN | NaN | NaN | NaN | NaN |
| STT | Preset Name | Vị trí (x,y,z) | Góc xoay (x,y,z) | FOV | Mô tả sử dụng |
| 1 | player\_follow | (0, 1.8, -3.5) | (15, 0, 0) | 60° | Mặc định 3rd person bám sau lưng player, dùng cho mọi map khi đi lại |
| 2 | farm\_overview | (0, 12, -10) | (55, 0, 0) | 65° | Toàn cảnh nông trại từ trên cao (mở mini-map / quy hoạch) |
| 3 | city\_overview | (0, 8, -12) | (35, 0, 0) | 60° | Vào thành phố lần đầu, panoramic intro |
| 4 | shop\_interior | (0, 1.5, -2.5) | (10, 0, 0) | 50° | Khi vào shop — focus quầy & NPC keeper |
| 5 | fishing\_view | (0.5, 1.6, -2) | (20, 10, 0) | 45° | Câu cá — focus mặt nước, hiển thị float/phao |
| 6 | mining\_view | (0, 1.4, -2) | (25, 0, 0) | 50° | Khai thác mỏ — góc thấp, không gian hẹp |
| 7 | inventory\_ui | — | — | — | Camera dừng + freeze, UI Inventory full-screen |
| 8 | dialog\_npc | (0.3, 1.6, -1.5) | (8, 15, 0) | 45° | Hội thoại NPC, đặc tả mặt NPC |
| 9 | character\_customize | (0, 1.5, -2.0) | (0, 0, 0) | 40° | Màn hình thay cosmetic — orbit nhân vật |
| 10 | planting\_close | (0, 1.0, -1.0) | (50, 0, 0) | 45° | Cận cảnh cuốc đất / trồng / tưới |
| 11 | cutscene\_intro | (2, 3, -5) | (20, -25, 0) | 55° | Cinematic chào mừng lần đầu vào game |
| 12 | level\_up | (0, 1.8, -2.5) | (10, 0, 0) | 50° | Camera zoom + VFX khi nhân vật lên cấp |

## 04_Maps
| CHI TIẾT 5 MAP (SCENE 3D) | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 | Unnamed: 5 |
| --- | --- | --- | --- | --- | --- |
| STT | Map | Scene file | Khu chức năng | Cơ chế chính | Yêu cầu mở khóa |
| 1 | Nông trại | FarmScene.unity | Khu trồng trọt, khu chăn nuôi, shop hạt giống, shop item, khu chặt cây, ao nhỏ, khu xây dựng | Cuốc đất → trồng hạt giống → tưới (10h) → bón phân → thu hoạch (24h). Xây chuồng/đường/đèn. Đào đá, chặt cây lấy gỗ. | Mặc định (sau tutorial) |
| 2 | Thành phố | CityScene.unity | 13 cửa hàng/dịch vụ: Câu cá, Bán cá, Workshop, YWonderLand, Mini Garden, Hai Lúa, KNX, Maid, Pet, Game, Store, Gift Box, Heo Đất | Click NPC keeper → mở UI shop. Câu cá: 10 lượt free/ngày. Heo đất: gửi 12/30/180 ngày. | Hoàn thành tutorial |
| 3 | Khai thác mỏ | MineScene.unity | Hang động với các tảng đá, NPC bán vé đào, NPC mua quặng | Click vào đá → minigame đào → có XS nhận quặng. 10 lượt free/ngày. | Lv 10 |
| 4 | Đảo Hải Phú | HaiphuIsland.unity | Bãi biển, cây cọ, ao câu đặc biệt, shop đảo, hạt giống/vật nuôi exclusive | Câu cá hiếm + vật nuôi đảo + nông sản theo mùa. | Lv 40 + Vip User HOẶC có Vé Hải Phú |
| 5 | Đảo Mộc Nhi | MocnhiIsland.unity | Rừng huyền bí, sương mù, vật phẩm event hiếm | Quest đảo, vật phẩm sự kiện, drop tỉ lệ thấp. | Lv 60 + Vip User HOẶC có Vé Mộc Nhi |

## 05_KichBanChiTiet
| KỊCH BẢN CHI TIẾT TỪNG MÀN/BƯỚC — FORM MẪU UNITY3D | Unnamed: 1 |
| --- | --- |
| NaN | NaN |
| PHẦN A: ONBOARDING | NaN |
| Bước 1: Đăng ký tài khoản | NaN |
| Camera | ui\_fullscreen (UI overlay, scene background blur) |
| Thời gian giới hạn | Không giới hạn |
| Animation Trigger | ShowRegisterUI |
| Hướng dẫn | Người chơi mới nhập Email, Tên đăng nhập, Mật khẩu, Xác nhận mật khẩu. |
| Mô tả chi tiết (Kịch bản 3D) | • Hiển thị form đăng ký với 4 input: Email, Username, Password, Confirm Password.\n• Validate real-time:\n  - Email: regex chuẩn email.\n  - Username: > 8 ký tự, chỉ chữ/số/\_ .\n  - Password: > 8 ký tự, có ít nhất 1 chữ HOA, 1 chữ thường, 1 số, 1 ký tự đặc biệt.\n  - Confirm: trùng Password.\n• Nút [Đăng ký] disable cho đến khi tất cả input hợp lệ.\n• Gửi API /register → nhận token → sang Bước 2 (Tạo nhân vật). |
| Dụng cụ cần thiết | UI: RegisterPanel, InputField x4, ButtonRegister, TextError |
| Checkpoints (Điểm kiểm tra) | ✓ Email hợp lệ\n✓ Username > 8 ký tự\n✓ Password đủ yêu cầu\n✓ Confirm trùng Password\n✓ API trả về thành công |
| Sai hỏng thường gặp | • Mật khẩu yếu (thiếu hoa/số/ký tự đặc biệt)\n• Email đã tồn tại\n• Username đã có người dùng\n• Mất kết nối mạng |
| Tương tác (Interaction) | Form input (gõ phím + tap nút Đăng ký) |
| NaN | NaN |
| Bước 2: Tạo nhân vật (Chọn giới tính + Đặt tên) | NaN |
| Camera | character\_customize |
| Thời gian giới hạn | Không giới hạn |
| Animation Trigger | CreateCharacter |
| Hướng dẫn | Chọn giới tính (Nam/Nữ) và đặt tên nhân vật. CẢNH BÁO: không thể đổi sau khi xác nhận. |
| Mô tả chi tiết (Kịch bản 3D) | • Hiển thị 2 model 3D nhân vật mặc định (Nam bên trái, Nữ bên phải).\n• Click chọn → camera orbit quanh nhân vật được chọn (idle animation).\n• InputField đặt tên nhân vật (2–16 ký tự, không trùng).\n• Nút [Xác nhận] mở popup cảnh báo: 'Bạn KHÔNG THỂ thay đổi tên & giới tính sau khi xác nhận.'\n• Sau confirm → lưu vào DB → sang Bước 3. |
| Dụng cụ cần thiết | Player Model (Nam + Nữ), InputName, ButtonConfirm, ConfirmDialog |
| Checkpoints (Điểm kiểm tra) | ✓ Chọn giới tính\n✓ Tên hợp lệ, không trùng\n✓ Xác nhận popup cảnh báo |
| Sai hỏng thường gặp | • Tên trùng với người khác\n• Tên có ký tự đặc biệt cấm\n• Người chơi đóng giữa chừng (lưu draft) |
| Tương tác (Interaction) | Click chọn model + Form input + Confirm |
| NaN | NaN |
| Bước 3: Cinematic chào mừng + NPC hướng dẫn | NaN |
| Camera | cutscene\_intro → player\_follow |
| Thời gian giới hạn | Cinematic 15–20s, Tutorial mở rộng |
| Animation Trigger | PlayIntroCutscene → SpawnGuideNPC |
| Hướng dẫn | Lần đầu đăng nhập: cinematic giới thiệu thế giới YWONDERLAND. Sau đó NPC hướng dẫn xuất hiện. |
| Mô tả chi tiết (Kịch bản 3D) | • Cinematic 15–20s: camera bay quanh nông trại + voiceover/text 'Chào mừng đến với YWONDERLAND...'.\n• Skip button (tap để bỏ qua sau 3s).\n• Kết thúc cinematic → Spawn NPC hướng dẫn cạnh nhân vật.\n• NPC chỉ tay vào nhân vật, nói: 'Đi theo tôi nhé!'\n• NPC di chuyển từng đoạn ngắn, ĐỢI nhân vật đi gần (≤ 3m) mới đi tiếp.\n• Nếu nhân vật đứng yên > 10s → NPC quay lại, nhắc lại. |
| Dụng cụ cần thiết | Guide NPC prefab, Camera Sequence, Voiceover audio, Subtitle UI |
| Checkpoints (Điểm kiểm tra) | ✓ Cinematic phát đầy đủ\n✓ NPC spawn đúng vị trí\n✓ NPC đợi player\n✓ Subtitle hiển thị |
| Sai hỏng thường gặp | • Cinematic lag/skip lỗi\n• NPC đi quá nhanh, player không bắt kịp\n• NPC kẹt vào địa hình (NavMesh lỗi) |
| Tương tác (Interaction) | Skip cutscene + Walk follow NPC |
| NaN | NaN |
| Bước 4: Tutorial cuốc đất + trồng hạt giống đầu tiên | NaN |
| Camera | planting\_close |
| Thời gian giới hạn | 120 giây (gợi ý nếu kẹt) |
| Animation Trigger | TutorialFarming |
| Hướng dẫn | NPC hướng dẫn người chơi cách dùng cuốc, trồng hạt cà rốt (miễn phí), tưới nước, đợi 1 phút (rút gọn lần đầu) rồi thu hoạch. |
| Mô tả chi tiết (Kịch bản 3D) | • NPC: 'Hãy thử cuốc đất nào!' → highlight 1 ô đất + UI hint 'Tap để cuốc'.\n• Player tap → animation 'Hoe' → ô đất chuyển thành Farm Tile (texture nâu).\n• NPC: 'Mở túi đồ → chọn hạt cà rốt → kéo thả lên ô đất'.\n• Hoàn thành trồng → NPC: 'Bây giờ tưới nước nhé' → tap nút Watering Can.\n• LẦN ĐẦU: rút gọn thời gian thu hoạch xuống 60s (instead of 24h) để player thấy chu trình.\n• Thu hoạch → +50 POS + +20 EXP. |
| Dụng cụ cần thiết | Cuốc, Hạt cà rốt (free), Xô tưới, Farm Tile |
| Checkpoints (Điểm kiểm tra) | ✓ Cuốc ô đất\n✓ Mở túi đồ\n✓ Kéo thả hạt giống\n✓ Tưới nước\n✓ Thu hoạch lần đầu |
| Sai hỏng thường gặp | • Player không tìm thấy nút túi đồ → highlight đỏ\n• Player thoát giữa chừng → resume tutorial khi quay lại |
| Tương tác (Interaction) | Tap + Drag & Drop + Sequence |
| NaN | NaN |
| PHẦN B: GAMEPLAY CHÍNH — NÔNG TRẠI | NaN |
| Bước 5: Trồng trọt (8 loại hạt giống) | NaN |
| Camera | player\_follow + planting\_close (khi tương tác) |
| Thời gian giới hạn | Real-time (24h thu hoạch, 10h/lần tưới) |
| Animation Trigger | Hoe / Plant / Water / Fertilize / Harvest |
| Hướng dẫn | Người chơi tự cuốc đất, trồng hạt giống, tưới, bón phân và thu hoạch theo chu kì real-time. |
| Mô tả chi tiết (Kịch bản 3D) | • 8 loại hạt giống: cà rốt, cải, dưa hấu, rau muống, bí ngô, bắp, dây khoai lang, cỏ voi.\n• Mỗi loại có:\n  - Giá hạt giống (POS).\n  - Thời gian trưởng thành (mặc định 24h, biến thiên theo loại).\n  - Tần suất tưới: 10h/lần (cần tưới đúng giờ, nếu không cây héo, EXP giảm).\n  - Tăng tốc bằng phân bón (mua từ Hai Lúa).\n  - EXP & POS thu hoạch khác nhau.\n |
| Dụng cụ cần thiết | Cuốc, Hạt giống (8 loại), Xô tưới, Phân bón, Farm Tile |
| Checkpoints (Điểm kiểm tra) | ✓ Ô đất đã cuốc\n✓ Đã gieo hạt\n✓ Tưới đúng giờ\n✓ Thu hoạch khi cây trưởng thành |
| Sai hỏng thường gặp | • Quên tưới → cây héo, mất EXP\n• Trồng sai vùng (ngoài Farm Tile)\n• Hết hạt giống — phải ra Shop mua |
| Tương tác (Interaction) | Tap to hoe + Drag seed + Tap to water/fertilize |
| NaN | NaN |
| Bước 6: Chăn nuôi vật nuôi (xây chuồng + nuôi) | NaN |
| Camera | player\_follow |
| Thời gian giới hạn | Real-time (chu kì khác nhau theo loài) |
| Animation Trigger | BuildPen / Feed / Vaccinate / Treat / CollectProduce |
| Hướng dẫn | Xây chuồng (4 gỗ/chuồng, tối đa 10), mua vật nuôi ở YWonderLand, cho ăn đúng giờ, tiêm vacxin phòng bệnh. |
| Mô tả chi tiết (Kịch bản 3D) | • Xây chuồng: cần 4 gỗ. Tối đa 10 chuồng/người (mở thêm = nạp VIP).\n• Mua vật nuôi tại 'YWonderLand' ở thành phố.\n• Mỗi vật nuôi có:\n  - Chu kì sản phẩm (đẻ trứng / cho sữa).\n  - Số vụ thu hoạch trước khi hết tuổi.\n  - Sản lượng (cao/thấp).\n  - Có thể BỆNH: nếu không tiêm Vacxin trước → có thể chết → mất tiền đầu tư.\n  - Phải cho ăn đúng cữ, nếu không có thể chết đói.\n• Thu sản phẩm: tap vào vật nuôi → animation 'Produce' → nhận trứng/sữa vào inventory. |
| Dụng cụ cần thiết | Gỗ (4/chuồng), Cỏ voi/thức ăn, Vacxin, Thuốc điều trị, Vật nuôi |
| Checkpoints (Điểm kiểm tra) | ✓ Đủ gỗ xây chuồng\n✓ Mua vật nuôi\n✓ Cho ăn đúng giờ\n✓ Tiêm Vacxin\n✓ Thu sản phẩm |
| Sai hỏng thường gặp | • Quên cho ăn → vật nuôi chết đói\n• Không tiêm Vacxin → bệnh → có thể chết\n• Hết slot chuồng (max 10) |
| Tương tác (Interaction) | Build placement + Tap to feed/treat/collect |
| NaN | NaN |
| Bước 7: Khai thác tài nguyên (Chặt cây, Đào đá) | NaN |
| Camera | player\_follow |
| Thời gian giới hạn | Mỗi tài nguyên có thời gian respawn (1–6h) |
| Animation Trigger | ChopTree / MineStone |
| Hướng dẫn | Dùng rìu chặt cây lấy gỗ (dùng xây chuồng). Dùng cuốc đào đá (dùng xây đường + chuyển sang map mỏ). |
| Mô tả chi tiết (Kịch bản 3D) | • Cây xanh trên map nông trại: tap → animation 'Chop' → mất 2–4s → nhận 1–3 gỗ.\n• Đá trên map: tap → animation 'Mine' → nhận 1–2 đá.\n• Resource respawn sau 1–6h tùy loại.\n• Rìu/Cuốc có thể nâng cấp tại Workshop để tăng sản lượng & giảm thời gian. |
| Dụng cụ cần thiết | Rìu, Cuốc, Cây xanh (props), Đá (props) |
| Checkpoints (Điểm kiểm tra) | ✓ Trang bị đúng dụng cụ\n✓ Animation phát đúng\n✓ Nhận tài nguyên vào inventory\n✓ Object biến mất, hẹn respawn |
| Sai hỏng thường gặp | • Hết durability dụng cụ (nếu có)\n• Chặt nhầm cây cảnh không cho gỗ\n• Lag → object không respawn |
| Tương tác (Interaction) | Tap & hold trên target |
| NaN | NaN |
| Bước 8: Xây dựng nông trại (Đường, Đèn, Chuồng) | NaN |
| Camera | farm\_overview (khi vào mode xây dựng) |
| Thời gian giới hạn | Không giới hạn |
| Animation Trigger | EnterBuildMode / PlaceObject |
| Hướng dẫn | Vào chế độ xây dựng, chọn loại công trình, kéo thả vào vị trí mong muốn trên nông trại. |
| Mô tả chi tiết (Kịch bản 3D) | • Tap nút [Xây dựng] góc dưới phải.\n• Camera chuyển sang farm\_overview (top-down).\n• Chọn loại: Đường (1 đá), Đèn (8 quặng), Chuồng (4 gỗ).\n• Preview ghost object di chuyển theo ngón tay, đỏ = không hợp lệ, xanh = OK.\n• Confirm → trừ tài nguyên + spawn object. |
| Dụng cụ cần thiết | Đá, Gỗ, Quặng (đã thu thập trước) |
| Checkpoints (Điểm kiểm tra) | ✓ Đủ tài nguyên\n✓ Vị trí hợp lệ\n✓ Trừ tài nguyên\n✓ Object xuất hiện đúng vị trí |
| Sai hỏng thường gặp | • Thiếu tài nguyên\n• Đặt chồng lên ô đã trồng\n• Vượt quá 10 chuồng (không VIP) |
| Tương tác (Interaction) | Tap to select + Drag preview + Tap to confirm |
| NaN | NaN |
| PHẦN B: GAMEPLAY CHÍNH — THÀNH PHỐ | NaN |
| Bước 9: Câu cá | NaN |
| Camera | fishing\_view |
| Thời gian giới hạn | Mỗi lượt 10–30s; 10 lượt free/ngày |
| Animation Trigger | CastRod / FishBite / ReelIn |
| Hướng dẫn | Ra ao câu cá, dùng cần câu. Free 10 lần/ngày; hết phải mua mồi. Có xác suất rơi item event. |
| Mô tả chi tiết (Kịch bản 3D) | • Đến NPC Câu cá → chọn 'Bắt đầu câu'.\n• Animation 'CastRod' → ném phao xuống nước → đợi 3–10s random.\n• Khi phao rung → UI prompt 'Tap nhanh!' (QTE 1.5s).\n• Thành công → animation 'ReelIn' → cá nhảy lên → vào inventory.\n• Có XS 5% rơi ra Gift Box event.\n• Sau 10 lượt free → popup 'Mua mồi câu' (X POS / Y UPOS). |
| Dụng cụ cần thiết | Cần câu, Mồi câu, Ao nước (props) |
| Checkpoints (Điểm kiểm tra) | ✓ Ném phao\n✓ Đợi cắn câu\n✓ QTE đúng nhịp\n✓ Cá vào inventory |
| Sai hỏng thường gặp | • QTE chậm → cá thoát\n• Hết lượt free, không có mồi\n• Mạng yếu → mất lượt |
| Tương tác (Interaction) | Tap + QTE (Quick Time Event) |
| NaN | NaN |
| Bước 10: Mua sắm (12 shop trong thành phố) | NaN |
| Camera | shop\_interior |
| Thời gian giới hạn | Không giới hạn |
| Animation Trigger | OpenShopUI |
| Hướng dẫn | Click NPC shop keeper → mở UI shop tương ứng. Mua/bán bằng POS hoặc UPOS. |
| Mô tả chi tiết (Kịch bản 3D) | • 12 shop: Bán cá, Workshop, YWonderLand, Mini Garden, Hai Lúa, KNX, Maid, Pet, Game, Store, Gift Box, Heo Đất.\n• Mỗi shop có UI riêng:\n  - Mini Garden: BÁN nông sản, sản phẩm chăn nuôi.\n  - Bán cá: BÁN cá.\n  - Hai Lúa: MUA phân bón, vacxin, thuốc.\n  - Workshop: NÂNG CẤP cuốc/rìu (cần vật liệu).\n  - YWonderLand: MUA vật nuôi, cây trồng mới.\n  - Store: MUA cosmetic (tóc, áo, quần).\n  - Pet: MUA pet (POS / UPOS).\n  - KNX: MUA thẻ thành viên VIP.\n  - Gift Box: TẶNG cho bạn bè trong list.\n  - Game: minigame trúng thưởng (free + paid).\n  - Heo đất: GỬI lãi suất 12/30/180 ngày. |
| Dụng cụ cần thiết | POS, UPOS, NPC Shop x12, ShopUI Panel |
| Checkpoints (Điểm kiểm tra) | ✓ Mở UI shop\n✓ Đủ tiền\n✓ Confirm giao dịch\n✓ Vật phẩm vào inventory hoặc số dư cập nhật |
| Sai hỏng thường gặp | • Hết tiền (POS/UPOS)\n• Inventory đầy\n• Server lag → giao dịch fail |
| Tương tác (Interaction) | Tap NPC + Tap item + Confirm |
| NaN | NaN |
| Bước 11: Gửi Heo Đất (Lãi suất) | NaN |
| Camera | shop\_interior (Heo Đất) |
| Thời gian giới hạn | 12 / 30 / 180 ngày (real-time) |
| Animation Trigger | OpenPiggyBankUI / DepositMoney |
| Hướng dẫn | Gửi POS vào Heo Đất với 3 gói thời hạn để nhận lãi. Mỗi lần chỉ chọn được 1 gói. |
| Mô tả chi tiết (Kịch bản 3D) | • UI Heo Đất hiển thị 3 gói:\n  - Gói 12 ngày: lãi suất X%\n  - Gói 30 ngày: lãi suất Y% (cao hơn)\n  - Gói 180 ngày: lãi suất Z% (cao nhất)\n• Nhập số tiền + chọn gói + Confirm.\n• Đếm ngược real-time, không rút sớm được.\n• Khi đáo hạn → gốc + lãi tự động vào HỘP THƯ.\n• Tab 'Lịch sử gửi' hiển thị các giao dịch trước. |
| Dụng cụ cần thiết | POS, PiggyBankUI, MailboxUI |
| Checkpoints (Điểm kiểm tra) | ✓ Chọn gói\n✓ Nhập số tiền\n✓ Trừ POS\n✓ Đếm ngược chạy đúng\n✓ Lãi về hộp thư khi đáo hạn |
| Sai hỏng thường gặp | • Đã có gói đang gửi → không gửi gói khác\n• Số tiền vượt số dư\n• Đáo hạn nhưng lãi không về (bug server) |
| Tương tác (Interaction) | Form input + Confirm |
| NaN | NaN |
| Bước 12: Workshop nâng cấp dụng cụ | NaN |
| Camera | shop\_interior (Workshop) |
| Thời gian giới hạn | Không giới hạn |
| Animation Trigger | OpenWorkshopUI / Upgrade |
| Hướng dẫn | Đem cuốc/rìu/cần câu lên Workshop để nâng cấp. Yêu cầu đủ vật liệu (gỗ, đá, quặng) + POS. |
| Mô tả chi tiết (Kịch bản 3D) | • UI Workshop liệt kê dụng cụ hiện có + cấp hiện tại.\n• Mỗi cấp tăng:\n  - Sản lượng (tài nguyên thu được/lần).\n  - Tốc độ (rút ngắn animation).\n• Hiển thị yêu cầu: 'Cần 10 gỗ + 5 quặng + 500 POS'.\n• Confirm → trừ vật liệu + tiền → cuốc/rìu lên level. |
| Dụng cụ cần thiết | Gỗ, Đá, Quặng, POS, Cuốc/Rìu/Cần câu |
| Checkpoints (Điểm kiểm tra) | ✓ Đủ vật liệu\n✓ Đủ POS\n✓ Cấp dụng cụ tăng\n✓ Trừ vật liệu |
| Sai hỏng thường gặp | • Thiếu vật liệu\n• Bấm nâng cấp 2 lần → bị trừ 2 lần (cần khóa nút) |
| Tương tác (Interaction) | Tap upgrade + Confirm |
| NaN | NaN |
| PHẦN B: GAMEPLAY CHÍNH — KHAI THÁC MỎ | NaN |
| Bước 13: Đào quặng tại Mỏ | NaN |
| Camera | mining\_view |
| Thời gian giới hạn | 10 lượt free/ngày |
| Animation Trigger | EnterMine / TapRock / GetOre |
| Hướng dẫn | Vào map Khai thác mỏ, tìm cục đá, tap để đào. Mỗi lần có xác suất nhận quặng hoặc không. |
| Mô tả chi tiết (Kịch bản 3D) | • Vào Mine Scene → camera mining\_view.\n• Các cục đá rải rác, có hiệu ứng glow nếu chứa quặng (heuristic).\n• Tap → animation 'Mine' 2s → XS:\n  - 60% nhận 1 quặng thường.\n  - 10% nhận 1 quặng hiếm (event).\n  - 30% không nhận gì.\n• 10 lượt free/ngày. Hết → mua vé tại Item Shop trong mỏ. |
| Dụng cụ cần thiết | Cuốc, Vé đào (nếu hết free), Quặng |
| Checkpoints (Điểm kiểm tra) | ✓ Vào map Mỏ\n✓ Đào → animation đúng\n✓ Cập nhật lượt còn lại\n✓ Quặng vào inventory (nếu may mắn) |
| Sai hỏng thường gặp | • Tap spam → đếm sai lượt\n• Hết vé → không đào được |
| Tương tác (Interaction) | Tap to mine |
| NaN | NaN |
| PHẦN C: HỆ THỐNG MỞ RỘNG | NaN |
| Bước 14: Hệ thống Pet (Thú cưng follow) | NaN |
| Camera | player\_follow |
| Thời gian giới hạn | Persistent (luôn follow) |
| Animation Trigger | SpawnPet / PetFollow |
| Hướng dẫn | Mua Pet ở Pet Shop. Pet luôn chạy theo nhân vật, có thể tap để có animation Happy. |
| Mô tả chi tiết (Kịch bản 3D) | • Mua pet → pet xuất hiện cạnh nhân vật.\n• Pet di chuyển theo NavMesh, luôn cách player 1–2m.\n• Khi player đứng yên → pet ngồi (animation Sit).\n• Tap pet → animation 'Happy' + sound effect.\n• Có thể tháo pet hoặc đổi pet khác trong inventory.\n• Pet KHÔNG tham gia gameplay khác (chỉ trang trí). |
| Dụng cụ cần thiết | Pet model, PetController script |
| Checkpoints (Điểm kiểm tra) | ✓ Pet spawn đúng vị trí\n✓ Pet follow mượt\n✓ Pet không kẹt vào tường |
| Sai hỏng thường gặp | • Pet kẹt địa hình → respawn cạnh player\n• Đổi pet bị duplicate |
| Tương tác (Interaction) | Tap pet để pet vui |
| NaN | NaN |
| Bước 15: Hệ thống Bạn bè (Kết bạn, thăm farm) | NaN |
| Camera | ui\_fullscreen / farm\_overview (khi thăm) |
| Thời gian giới hạn | Không giới hạn |
| Animation Trigger | AddFriend / VisitFriendFarm |
| Hướng dẫn | Tìm bạn theo tên tài khoản, gửi lời mời. Thăm nông trại bạn yêu cầu VIP. |
| Mô tả chi tiết (Kịch bản 3D) | • UI Bạn bè có 3 tab: Friends / Online / Tìm.\n• Tab Tìm: input username → search → gửi lời mời.\n• Lời mời vào Hộp thư của bạn → bạn accept/decline.\n• Tab Friends hiển thị: avatar, level, online status, nút 'Xem profile' & 'Thăm farm'.\n• Thăm farm: chỉ VIP. Camera farm\_overview, không thể tương tác (chỉ xem). |
| Dụng cụ cần thiết | FriendUI, ProfileUI, Network sync |
| Checkpoints (Điểm kiểm tra) | ✓ Search ra bạn\n✓ Gửi lời mời\n✓ Bạn accept → vào list\n✓ Thăm farm (VIP only) |
| Sai hỏng thường gặp | • Tìm sai tên\n• Đã là bạn rồi vẫn gửi mời\n• Không VIP → popup nâng cấp |
| Tương tác (Interaction) | Search + Tap to add + Tap to visit |
| NaN | NaN |
| Bước 16: Chat (Global + AI NPC) | NaN |
| Camera | Giữ camera hiện tại (UI overlay) |
| Thời gian giới hạn | Real-time |
| Animation Trigger | OpenChatUI / SendMessage |
| Hướng dẫn | Khung chat ở dưới màn hình. Có thể chat global. AI NPC tự trả lời để làm sôi động. |
| Mô tả chi tiết (Kịch bản 3D) | • Khung chat scroll history, nút mở rộng/thu gọn.\n• Input message + nút Send.\n• AI NPC: detect keyword trong tin nhắn → trả lời từ pool đã định sẵn (10–30s sau).\n• Lọc từ ngữ nhạy cảm (profanity filter).\n• Có thể ẩn chat trong Cài đặt. |
| Dụng cụ cần thiết | ChatUI, Network, AI Response Pool |
| Checkpoints (Điểm kiểm tra) | ✓ Mở/thu gọn chat\n✓ Gửi tin nhắn lên server\n✓ Nhận tin nhắn realtime\n✓ AI NPC trả lời sau delay |
| Sai hỏng thường gặp | • Spam → giới hạn 5 tin/30s\n• Profanity bypass\n• AI NPC trả lời lặp |
| Tương tác (Interaction) | Input + Send |
| NaN | NaN |
| Bước 17: Nhiệm vụ & Bảng xếp hạng | NaN |
| Camera | ui\_fullscreen |
| Thời gian giới hạn | Daily/Weekly tasks |
| Animation Trigger | OpenQuestBoard / CompleteQuest |
| Hướng dẫn | Mở Bảng nhiệm vụ để xem nhiệm vụ hàng ngày, hàng tuần. Xem Bảng xếp hạng 5 tiêu chí. |
| Mô tả chi tiết (Kịch bản 3D) | • Bảng nhiệm vụ: 3 tab — Daily / Weekly / Main.\n• Mỗi nhiệm vụ có:\n  - Mô tả (vd: 'Thu hoạch 10 cà rốt').\n  - Progress bar.\n  - Reward (POS, EXP, Item).\n  - Nút [Nhận thưởng] khi hoàn thành.\n• Daily reset 00:00, Weekly reset thứ Hai.\n• Bảng xếp hạng 5 tab: EXP / Level / Thời trang / Tiền tệ / Pet. |
| Dụng cụ cần thiết | QuestBoardUI, LeaderboardUI |
| Checkpoints (Điểm kiểm tra) | ✓ Xem quest\n✓ Progress update real-time\n✓ Nhận reward\n✓ Reset đúng giờ |
| Sai hỏng thường gặp | • Reward không vào inventory\n• Reset sai múi giờ |
| Tương tác (Interaction) | Tap tab + Tap claim |
| NaN | NaN |
| Bước 18: Event & Điểm danh hàng ngày | NaN |
| Camera | ui\_fullscreen |
| Thời gian giới hạn | Daily login |
| Animation Trigger | ShowDailyLogin / OpenEventUI |
| Hướng dẫn | Mỗi ngày đăng nhập nhận thưởng. Có thể đổi vật phẩm sự kiện ở UI Event. |
| Mô tả chi tiết (Kịch bản 3D) | • Lần login đầu trong ngày: popup 'Phần thưởng hôm nay' → tap nhận.\n• Chuỗi 7 ngày, ngày thứ 7 reward x3.\n• Tab Event:\n  - Đổi quà: dùng vật phẩm event (từ câu cá / đào quặng) đổi vật phẩm hiếm.\n  - Gói ưu đãi UPOS: giá rẻ, giới hạn thời gian. |
| Dụng cụ cần thiết | EventUI, DailyRewardUI |
| Checkpoints (Điểm kiểm tra) | ✓ Popup đúng 1 lần/ngày\n✓ Reward vào inventory\n✓ Chuỗi 7 ngày đếm đúng |
| Sai hỏng thường gặp | • Reset chuỗi sai\n• Reward bị duplicate khi reconnect |
| Tương tác (Interaction) | Tap claim + Tap exchange |
| NaN | NaN |
| Bước 19: Mở khóa Đảo Hải Phú / Mộc Nhi (Endgame) | NaN |
| Camera | cutscene\_intro (lần đầu) → player\_follow |
| Thời gian giới hạn | Endgame content |
| Animation Trigger | UnlockIsland / TravelToIsland |
| Hướng dẫn | Khi đủ điều kiện (Lv40 + VIP/Vé), người chơi có thể di chuyển tới Đảo Hải Phú. Tương tự Mộc Nhi cần Lv60. |
| Mô tả chi tiết (Kịch bản 3D) | • UI Bản đồ chính: 5 cảnh.\n• Hải Phú khóa nếu chưa đạt: Lv40 + Vip User HOẶC có Vé Hải Phú.\n• Mộc Nhi khóa nếu chưa đạt: Lv60 + Vip User HOẶC có Vé Mộc Nhi.\n• Đáp ứng đủ → tap → cinematic chuyển cảnh → load scene mới.\n• Trên đảo: hạt giống, vật nuôi, cá đặc biệt + nông sản theo sự kiện. |
| Dụng cụ cần thiết | Vé Hải Phú/Mộc Nhi, MapUI, Level data |
| Checkpoints (Điểm kiểm tra) | ✓ Đủ điều kiện\n✓ Cinematic phát\n✓ Load scene đúng\n✓ Player ở vị trí đảo đúng |
| Sai hỏng thường gặp | • Cố vào khi chưa đủ Lv → popup\n• Lag load scene\n• Mất vé sau khi vào (cần xác nhận trừ) |
| Tương tác (Interaction) | Tap on map + Confirm |
| NaN | NaN |
| PHẦN D: HỆ THỐNG NỀN TẢNG | NaN |
| Bước 20: Hệ thống Level & EXP | NaN |
| Camera | level\_up (khi lên cấp) |
| Thời gian giới hạn | Persistent |
| Animation Trigger | AddEXP / LevelUp |
| Hướng dẫn | Người chơi nhận EXP từ trồng trọt và chăn nuôi. Mỗi level mở chức năng mới. Lên cấp có animation đặc biệt. |
| Mô tả chi tiết (Kịch bản 3D) | • EXP nhận khi:\n  - Thu hoạch cây trồng (mỗi loại có EXP riêng).\n  - Thu sản phẩm chăn nuôi.\n  - (Có thể) hoàn thành nhiệm vụ.\n• Bảng EXP tăng dần: Lv 1→2 = 100 EXP, Lv 2→3 = 250, ... (curve hàm mũ nhẹ).\n• Khi đủ EXP → animation 'LevelUp' (VFX vàng + sound) → +1 Lv → unlock kiểm tra.\n• Hiển thị thông báo 'Mở khóa: X' nếu có. |
| Dụng cụ cần thiết | EXP bar UI, LevelUp VFX, Sound |
| Checkpoints (Điểm kiểm tra) | ✓ EXP tăng đúng\n✓ Animation LevelUp phát\n✓ Unlock đúng nội dung |
| Sai hỏng thường gặp | • EXP không sync server\n• LevelUp 2 lần liên tiếp animation chồng chéo |
| Tương tác (Interaction) | Tự động |
| NaN | NaN |
| Bước 21: Tiền tệ POS & UPOS | NaN |
| Camera | Giữ camera hiện tại |
| Thời gian giới hạn | Persistent |
| Animation Trigger | AddCurrency / SpendCurrency |
| Hướng dẫn | POS: kiếm trong game. UPOS: nạp tiền. Cả hai đều dùng để mua, UPOS dùng được cho item VIP. |
| Mô tả chi tiết (Kịch bản 3D) | • POS:\n  - Nhận từ trồng trọt, chăn nuôi, bán cá, bán quặng, gửi lãi.\n  - Dùng mua đa số vật phẩm thường.\n• UPOS:\n  - Nạp tiền qua IAP (Apple/Google).\n  - Mua được tất cả vật phẩm + thêm vật phẩm VIP.\n• Hiển thị 2 chỉ số trên HUD góc trên phải.\n• Nút [+] cạnh UPOS → mở store nạp tiền. |
| Dụng cụ cần thiết | Currency UI, IAP module |
| Checkpoints (Điểm kiểm tra) | ✓ Số dư cập nhật\n✓ Trừ đúng khi mua\n✓ Nạp UPOS thành công → cộng đúng |
| Sai hỏng thường gặp | • Nạp tiền nhưng UPOS không cộng (gọi support)\n• Trừ sai khi mua |
| Tương tác (Interaction) | Tap [+] nạp tiền + IAP flow |
| NaN | NaN |
| Bước 22: Túi đồ (Inventory) | NaN |
| Camera | ui\_fullscreen |
| Thời gian giới hạn | Không giới hạn |
| Animation Trigger | OpenInventoryUI |
| Hướng dẫn | Mở túi đồ để xem vật phẩm theo 7 danh mục. |
| Mô tả chi tiết (Kịch bản 3D) | • 7 tab: Sản phẩm / Nguyên liệu / Vật liệu / Hạt giống / Vé / Dụng cụ / Gói quà - Phiếu quà tặng.\n• Mỗi ô item: icon + số lượng + tooltip mô tả khi hold.\n• Có nút 'Sử dụng' (cho item dùng được).\n• Có nút 'Tặng bạn' (mở list bạn bè).\n• Auto-stack item cùng loại. |
| Dụng cụ cần thiết | InventoryUI |
| Checkpoints (Điểm kiểm tra) | ✓ Đúng số tab\n✓ Item đúng tab\n✓ Stack đúng\n✓ Tooltip hiển thị |
| Sai hỏng thường gặp | • Item không hiển thị (sync sai)\n• Stack quá max |
| Tương tác (Interaction) | Tap tab + Tap item + Tap action |
| NaN | NaN |
| Bước 23: Cài đặt (Sound/Graphics/Language) | NaN |
| Camera | ui\_fullscreen |
| Thời gian giới hạn | Không giới hạn |
| Animation Trigger | OpenSettingsUI |
| Hướng dẫn | Mở Settings để điều chỉnh nhạc nền, âm thanh, đồ hoạ, ngôn ngữ. |
| Mô tả chi tiết (Kịch bản 3D) | • Tab Audio: Music volume slider + SFX volume slider.\n• Tab Graphics: 3 mức (Low/Medium/High) — tự động detect device khả năng.\n• Tab Language: VI (mặc định) / EN (tùy chọn).\n• Lưu PlayerPrefs. |
| Dụng cụ cần thiết | SettingsUI, PlayerPrefs |
| Checkpoints (Điểm kiểm tra) | ✓ Slider hoạt động\n✓ Đổi graphics áp dụng ngay\n✓ Đổi ngôn ngữ reload UI text\n✓ Save khi đóng |
| Sai hỏng thường gặp | • Ngôn ngữ EN thiếu key dịch\n• Graphics Low gây artifact |
| Tương tác (Interaction) | Slider + Toggle + Dropdown |
| NaN | NaN |
| Bước 24: Cosmetic & Cá nhân hóa nhân vật | NaN |
| Camera | character\_customize |
| Thời gian giới hạn | Không giới hạn |
| Animation Trigger | OpenCosmeticUI / ApplyCosmetic |
| Hướng dẫn | Vào tab Cosmetic trong thông tin nhân vật, thay tóc/áo/quần đã mua ở Store. |
| Mô tả chi tiết (Kịch bản 3D) | • Camera orbit nhân vật.\n• 4 tab: Tóc / Áo / Quần - Váy / Phụ kiện.\n• Item đã mua: highlight; chưa mua: grayed + giá.\n• Tap để preview, tap 'Apply' để xác nhận.\n• Equipped item update thông số 'Thời trang' → ảnh hưởng bảng xếp hạng. |
| Dụng cụ cần thiết | CosmeticUI, Character Model, Inventory cosmetic |
| Checkpoints (Điểm kiểm tra) | ✓ Preview đúng\n✓ Apply đúng\n✓ Lưu vào DB\n✓ Update ranking |
| Sai hỏng thường gặp | • Item lỗi mesh\n• Apply nhưng không lưu khi reconnect |
| Tương tác (Interaction) | Tap to preview + Tap Apply |

## 06_Animations
| BẢNG TỔNG HỢP ANIMATION (32 TRIGGER) | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 |
| --- | --- | --- | --- |
| STT | Trigger Name | Nhân vật | Mô tả |
| 1 | Idle | Player | Đứng yên, thở nhẹ |
| 2 | Walk | Player | Đi bộ |
| 3 | Run | Player | Chạy nhanh (giữ joystick mạnh) |
| 4 | Jump | Player | Nhảy lên |
| 5 | Hoe | Player | Cuốc đất canh tác |
| 6 | Plant | Player | Trồng hạt giống xuống ô đất |
| 7 | Water | Player | Tưới nước |
| 8 | Fertilize | Player | Bón phân |
| 9 | Harvest | Player | Thu hoạch cây trồng |
| 10 | Chop | Player | Chặt cây bằng rìu |
| 11 | Mine | Player | Đào đá / đào quặng |
| 12 | CastRod | Player | Ném phao câu cá |
| 13 | ReelIn | Player | Kéo cần thu cá |
| 14 | Feed | Player | Cho vật nuôi ăn |
| 15 | Vaccinate | Player | Tiêm vacxin cho vật nuôi |
| 16 | PetAnimal | Player | Vuốt ve thú cưng |
| 17 | BuildPlace | Player | Đặt công trình (chuồng/đường/đèn) |
| 18 | LevelUp | Player | VFX vàng + tay giơ lên khi lên cấp |
| 19 | Sit | Player | Ngồi xuống nghỉ |
| 20 | Wave | Player | Vẫy tay (emote xã giao) |
| 21 | NPC\_Idle | NPC | NPC đứng yên tại quầy/vị trí |
| 22 | NPC\_Wave | NPC | Vẫy tay chào khi player đến gần |
| 23 | NPC\_Point | NPC | Chỉ tay (tutorial) |
| 24 | NPC\_Walk | NPC Guide | Đi đến điểm tiếp theo trong tutorial |
| 25 | Pet\_Idle | Pet | Pet đứng/ngồi yên |
| 26 | Pet\_Follow | Pet | Pet đi theo player |
| 27 | Pet\_Happy | Pet | Pet nhảy nhót khi được tap |
| 28 | Animal\_Eat | Vật nuôi | Vật nuôi ăn cỏ/thức ăn |
| 29 | Animal\_Sick | Vật nuôi | Vật nuôi bệnh — loop ngã đầu/yếu ớt |
| 30 | Animal\_Produce | Vật nuôi | Vật nuôi cho sản phẩm (đẻ trứng/sữa) |
| 31 | Fish\_Swim | Cá | Cá bơi quanh ao |
| 32 | Fish\_Caught | Cá | Cá nhảy lên khi bị câu |

## 07_RuiRo_LoiThuongGap
| RỦI RO / LỖI THƯỜNG GẶP & CÁCH XỬ TRÍ | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 |
| --- | --- | --- | --- | --- |
| STT | Vấn đề | Nguyên nhân | Đề phòng (Dev-side) | Xử trí (Player-side / Support) |
| 1 | Vật nuôi chết đói | Player không vào game cho ăn đúng cữ | Gửi push notification cảnh báo trước 2h | Khôi phục bằng UPOS / Item revive event |
| 2 | Cây trồng héo do không tưới | Không tưới sau 10h | Push notification | Mất một phần EXP, vẫn thu hoạch được sản lượng thấp |
| 3 | Mất kết nối khi giao dịch | Mạng yếu / server lag | Idempotent transactions, retry 3 lần | Khôi phục từ log giao dịch + bồi thường UPOS hỗ trợ |
| 4 | Nạp UPOS không vào | IAP receipt chưa validate / server delay | Validate ngay khi nhận callback từ store | Gửi receipt qua Support → cộng tay |
| 5 | Vật nuôi bị bệnh chết | Quên tiêm vacxin | Cảnh báo trong UI khi vật nuôi gần đến chu kì bệnh | Mua mới — game design ý đồ (consequence) |
| 6 | Pet kẹt địa hình | NavMesh không cover hết / collider | Bake NavMesh kỹ + script teleport pet về cạnh player nếu xa > 15m | Tự động teleport — player không thấy lỗi |
| 7 | Chat spam / từ ngữ xấu | Player spam | Rate limit 5 tin/30s + profanity filter + report system | Mute / ban theo level vi phạm |
| 8 | Bug duplicate item | Race condition | Lock transaction server-side + audit log | Rollback + cảnh báo account |
| 9 | Tutorial bị skip giữa chừng | Player đóng app | Save tutorial progress local + cloud | Resume từ bước cuối khi mở lại |
| 10 | Mất tài khoản do quên mật khẩu | — | Recovery qua email | Reset password via email link |

## 09_UI_UX
| GIAO DIỆN NGƯỜI DÙNG (UI / UX) | Unnamed: 1 | Unnamed: 2 |
| --- | --- | --- |
| Màn hình | Vị trí trên screen | Thành phần & hành vi |
| Splash / Loading | Toàn màn hình | Logo YWONDERLAND + thanh loading. Sau khi load → Login screen. |
| Login | Toàn màn hình | InputUsername + InputPassword + ButtonLogin + Link 'Đăng ký' / 'Quên mật khẩu'. |
| Đăng ký | Toàn màn hình | 4 InputField (Email/Username/Password/Confirm) + Validate realtime + ButtonSubmit. |
| Tạo nhân vật | Toàn màn hình | 2 model 3D (Nam/Nữ) + InputName + ConfirmDialog cảnh báo không đổi được. |
| HUD chính — Top Left | Góc trên trái | Avatar nhân vật + Level + EXP bar + Tên nhân vật. |
| HUD chính — Top Right | Góc trên phải | POS icon + số dư | UPOS icon + số dư + nút [+] | Settings icon. |
| HUD chính — Bottom Left | Góc dưới trái | Joystick di chuyển (drag radius). |
| HUD chính — Bottom Right | Góc dưới phải | Nút Jump + Nút Action (context-sensitive: cuốc/tưới/chặt/đào tùy đối tượng đứng gần). |
| Menu bottom bar | Cạnh dưới giữa | Inventory | Quests | Friends | Mailbox | Map | Build. |
| Chat | Cạnh dưới giữa (thu/mở) | Khung scroll + InputMessage + Send. Có thể ẩn trong Settings. |
| Inventory UI | Modal trung tâm | 7 tab (Sản phẩm/Nguyên liệu/Vật liệu/Hạt giống/Vé/Dụng cụ/Quà). Grid item + tooltip. |
| Quest Board | Modal trung tâm | 3 tab (Daily/Weekly/Main). Progress bar + Claim button. |
| Leaderboard | Modal trung tâm | 5 tab (EXP/Level/Thời trang/Tiền tệ/Pet). Top 100. Tab tô đỏ nếu user trong top. |
| Mailbox | Modal trung tâm | List thông báo hệ thống. Tap item → xem chi tiết + nhận đính kèm. |
| Map UI | Modal trung tâm | 5 cảnh — icon khóa nếu chưa đủ điều kiện. Tap → cinematic chuyển scene. |
| Shop UI | Modal trung tâm | List item + filter + giá + ButtonBuy. Có tab Bán nếu shop chấp nhận bán. |
| Friends UI | Modal trung tâm | Tab Friends/Online/Tìm. Search bar + add button. |
| Settings | Modal trung tâm | Tab Audio/Graphics/Language + nút Logout. |
| Daily Login Popup | Modal trung tâm (auto) | Hiển thị lần đầu login trong ngày. 7 ô chuỗi + Claim. |
| Level Up VFX | Toàn màn hình overlay | Tia sáng vàng + text 'Level UP!' + sound + 'Mở khóa: X' nếu có. |
| Confirm Dialog | Modal nhỏ trung tâm | Cảnh báo hành động không thể hoàn tác (tên nhân vật, mua VIP...). |
| Build Mode | Toàn màn hình | Camera farm\_overview + grid + ghost object + ButtonConfirm/Cancel. |
| Result / Reward Popup | Modal nhỏ | Hiển thị reward sau quest/lãi suất/event. |

## 08_KinhTeGame
| CƠ CHẾ KINH TẾ TRONG GAME & MONETIZATION | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 |
| --- | --- | --- | --- |
| STT | Hạng mục | Loại | Mô tả & giá trị tham khảo |
| 1 | Bán cà rốt | Nguồn POS | Bán tại Mini Garden — giá tham khảo 15 POS/củ |
| 2 | Bán dưa hấu | Nguồn POS | Bán tại Mini Garden — 45 POS/quả |
| 3 | Bán cá (tham khảo) | Nguồn POS | Phụ thuộc loại — 10–200 POS |
| 4 | Bán quặng thường | Nguồn POS | Bán tại Mỏ — 20 POS/cục |
| 5 | Bán trứng/sữa | Nguồn POS | Bán tại Mini Garden — 5–30 POS |
| 6 | Lãi Heo Đất 12 ngày | Nguồn POS | +2% gốc |
| 7 | Lãi Heo Đất 30 ngày | Nguồn POS | +6% gốc |
| 8 | Lãi Heo Đất 180 ngày | Nguồn POS | +45% gốc (rate hấp dẫn nhất) |
| 9 | Reward nhiệm vụ daily | Nguồn POS | 50–200 POS/nhiệm vụ |
| 10 | Mua hạt giống | Tiêu POS | 5–50 POS/hạt |
| 11 | Mua vật nuôi | Tiêu POS | 500–5000 POS/con |
| 12 | Xây chuồng (4 gỗ) | Tiêu POS/Vật liệu | Vật liệu là chính, có thể buy thiếu bằng POS |
| 13 | Mồi câu | Tiêu POS | 20 POS/mồi |
| 14 | Vé đào mỏ | Tiêu POS | 100 POS/vé |
| 15 | Phân bón / Vacxin / Thuốc | Tiêu POS | 30–100 POS |
| 16 | Gói UPOS S | Nạp tiền | ~ 20.000đ = 100 UPOS |
| 17 | Gói UPOS M | Nạp tiền | ~ 100.000đ = 600 UPOS (+ bonus) |
| 18 | Gói UPOS L | Nạp tiền | ~ 500.000đ = 3500 UPOS (+ bonus) |
| 19 | Gói UPOS XL | Nạp tiền | ~ 1.000.000đ = 8000 UPOS (+ bonus + skin) |
| 20 | Vé Hải Phú | Tiêu UPOS | 50 UPOS |
| 21 | Vé Mộc Nhi | Tiêu UPOS | 120 UPOS |
| 22 | Thẻ thành viên KNX 30 ngày | Tiêu UPOS | 200 UPOS |
| 23 | Maid VIP | Tiêu UPOS | 300 UPOS (vĩnh viễn) |
| 24 | Pet VIP | Tiêu UPOS | 200–500 UPOS/pet |
| 25 | Mở rộng chuồng vượt 10 | Tiêu UPOS | 50 UPOS/chuồng thêm |
| 26 | Cosmetic VIP | Tiêu UPOS | 30–200 UPOS |
| 27 | Sink: Workshop nâng cấp | Cân bằng | Tiêu vật liệu + POS lớn — giữ economy không lạm phát |
| 28 | Source: Daily login chuỗi 7 | Cân bằng | Tặng POS + item nhẹ để retain |
| 29 | Source: Event Đổi quà | Cân bằng | Vật phẩm event đổi item hiếm — drive engagement |

## 10_Unity_Setup
| HƯỚNG DẪN THIẾT LẬP UNITY PROJECT | Unnamed: 1 |
| --- | --- |
| Bước | Nội dung |
| 1 | Tạo Unity Project: Unity Hub → New Project → 3D (URP) → đặt tên 'YWonderland'. |
| 2 | Cấu trúc thư mục Assets:\n  /Scenes — chứa các scene (Farm/City/Mine/Haiphu/Mocnhi).\n  /Scripts — chứa script C#.\n  /Prefabs — chứa prefab.\n  /Models — chứa 3D model.\n  /Textures — chứa texture.\n  /Animations — chứa animation clip + Animator Controllers.\n  /Audio — chứa BGM + SFX.\n  /UI — chứa Canvas prefabs + sprites.\n  /Resources — chứa data scriptable objects. |
| 3 | Cài đặt package qua Package Manager:\n  - TextMeshPro (Import Essential Resources).\n  - Cinemachine (cho camera 3rd person).\n  - URP (Universal Render Pipeline) — nếu chưa có.\n  - Input System (joystick + touch input).\n  - Addressables (load/unload scene + asset).\n  - Newtonsoft Json (serialize data). |
| 4 | Network/Backend:\n  - Photon PUN 2 hoặc Mirror cho realtime chat & bạn bè.\n  - REST API riêng cho account / inventory / IAP (Node/Go/Python).\n  - Database: PostgreSQL/MongoDB lưu user + farm state.\n  - Firebase Cloud Messaging cho push notification (cây/vật nuôi). |
| 5 | Tạo các GameObject Manager (singleton, DontDestroyOnLoad):\n  - GameManager — điều phối toàn game.\n  - SceneLoader — chuyển scene (Addressables).\n  - NetworkManager — kết nối server.\n  - PlayerData — lưu thông tin nhân vật, inventory, farm.\n  - UIManager — quản lý UI panel.\n  - AudioManager — BGM + SFX.\n  - QuestManager — quản lý nhiệm vụ.\n  - TimeManager — đồng bộ thời gian server (trồng/tưới/heo đất). |
| 6 | Tạo Animator Controller cho:\n  - PlayerController (32 state, transition theo trigger).\n  - NPCController.\n  - PetController.\n  - AnimalController. |
| 7 | Setup NavMesh cho mỗi scene (Bake) — cho NPC, Pet, Animal di chuyển. |
| 8 | Cấu hình Input System: tạo Action Map 'Player' với Move (joystick), Look (vùng phải xoay), Jump, Action. |
| 9 | Build & test trên Android/iOS device thật trước khi release. |
| 10 | CI/CD: GitHub Actions hoặc Unity Cloud Build, auto build mỗi merge vào main. |

## 11_3D_Models
| DANH SÁCH 3D MODEL — CON GIỐNG, CÂY TRỒNG, CÂY LÂU NĂM, THỦY SẢN, SẢN PHẨM | Unnamed: 1 | Unnamed: 2 | Unnamed: 3 | Unnamed: 4 | Unnamed: 5 | Unnamed: 6 | Unnamed: 7 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Tổng hợp các 3D model cần dựng cho game YWONDERLAND. Mỗi model có ID, loại rig, polycount khuyến nghị, animation cần có, và đánh dấu sự kiện (event-only) hoặc có thể tặng bạn bè. | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| STT | Tên Model | ID | Polycount | Rig / Loại | Animation cần có | Mô tả & Cách dùng trong game | Tặng bạn? |
| 🐄 NHÓM 1: CON GIỐNG / VẬT NUÔI (Animals & Livestock) | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| 1 | Bò sữa | animal\_cow | 5K–8K | Quadruped | Idle, Walk, Eat, Sick, Sleep, ProduceMilk | Cho sữa định kì. Cần cho ăn cỏ voi đúng giờ, tiêm vacxin và cho uống thuốc khi bị bệnh | ✓ |
| 2 | Rùa con | animal\_turtle\_baby | 3K–5K | Quadruped | Idle, Walk(slow), Eat, Sleep, Grow | Vật nuôi cảnh, tăng trưởng chậm. Có thể nâng cấp lên rùa lớn. | ✓ |
| 3 | Heo con | animal\_pig\_baby | 4K–6K | Quadruped | Idle, Walk, Run, Eat, Sick, Sleep, ProduceMeat | Vật nuôi lấy thịt. Cho ăn để lớn nhanh. | ✓ |
| 4 | Đà điểu | animal\_ostrich | 6K–9K | Biped | Idle, Walk, Run, Eat, Sick, Sleep, ProduceEgg | Vật nuôi - đẻ trứng đà điểu giá trị cao. Tốc độ di chuyển nhanh. | ✓ |
| 5 | Hươu | animal\_deer | 5K–8K | Quadruped | Idle, Walk, Run, Eat, Sleep, ProduceAntler | Vật nuôi quý hiếm, cung cấp nhung hươu. Cần level cao mới mở. | — |
| 6 | Gà mái | animal\_hen | 3K–5K | Biped | Idle, Walk, Eat, Sick, Sleep, ProduceEgg | Đẻ trứng gà — sản phẩm phổ biến. | ✓ |
| 7 | Dê con | animal\_goat\_baby | 4K–6K | Quadruped | Idle, Walk, Run, Eat, Sick, Sleep, ProduceMilk | Cho sữa dê. Có thể leo trèo nhẹ trên địa hình. | ✓ |
| 8 | Ngỗng con | animal\_gosling | 3K–5K | Biped | Idle, Walk, Swim, Eat, Sleep, ProduceEgg | Vật nuôi nước, có thể bơi trong ao. Đẻ trứng ngỗng. | ✓ |
| 9 | Thỏ con | animal\_rabbit\_baby | 3K–5K | Quadruped | Idle, Hop, Run, Eat, Sleep, ProduceFur | Vật nuôi đáng yêu, sản phẩm lông thỏ. | ✓ |
| 10 | Vịt | animal\_duck | 4K–6K | Biped | Idle, Walk, Swim, Eat, Sick, Sleep, ProduceEgg | Đẻ trứng vịt, bơi trong ao. Model cải tiến. | ✓ |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| 🌱 NHÓM 2: HẠT GIỐNG & CÂY TRỒNG NGẮN NGÀY (Seeds & Short-cycle Crops) | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| 1 | Giống cỏ voi | seed\_napier\_grass | 1K–2K | Static + Growth Stages | Stage 1 (mầm) → Stage 2 (nhỏ) → Stage 3 (lớn) → Stage 4 (chín) | Cỏ voi làm thức ăn cho vật nuôi. Chu kì ngắn, sản lượng cao. | ✓ |
| 2 | Hạt giống bắp ngô | seed\_corn | 1K–2K | Static + Growth Stages | 4 stage tăng trưởng + Harvest VFX | Trồng bắp ngô. Thu hoạch sau 24h. Bán giá khá. | ✓ |
| 3 | Hạt giống bắp cải | seed\_cabbage | 1K–2K | Static + Growth Stages | 4 stage tăng trưởng + Harvest VFX | Trồng bắp cải. Cây thấp, gọn. | ✓ |
| 4 | Hạt giống bí ngô | seed\_pumpkin | 2K–3K | Static + Growth Stages | 4 stage tăng trưởng + Harvest VFX | Trồng bí ngô. Quả to, giá trị cao. Chu kì dài hơn. | ✓ |
| 5 | Hạt giống cà rốt | seed\_carrot | 1K–2K | Static + Growth Stages | 4 stage tăng trưởng + Harvest VFX | Cây cơ bản, dùng cho tutorial. Chu kì ngắn. | ✓ |
| 6 | Hạt giống dưa hấu | seed\_watermelon | 2K–3K | Static + Growth Stages | 4 stage tăng trưởng + Harvest VFX | Trồng dưa hấu. Quả lớn, bán giá cao. Chu kì dài. | ✓ |
| 7 | Dây khoai lang giống | seed\_sweet\_potato | 1K–2K | Static + Growth Stages | 4 stage tăng trưởng + Harvest VFX | Trồng dưới đất, animation đào khi thu hoạch. | ✓ |
| 8 | Hạt giống rau muống | seed\_morning\_glory | 1K–2K | Static + Growth Stages | 3 stage tăng trưởng + Harvest VFX | Cây ngắn ngày nhất, dùng cho daily quest. | ✓ |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| 🌳 NHÓM 3: CÂY LÂU NĂM (Long-cycle Trees) | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| 1 | Cây chuối | tree\_banana | 3K–5K | Static + 5 Growth Stages | Sprout → Sapling → Young → Mature → Fruit-bearing | Cây ăn quả lâu năm. Cho buồng chuối nhiều lần. | — |
| 2 | Cây dừa | tree\_coconut | 4K–6K | Static + 5 Growth Stages | Sprout → Sapling → Young → Mature → Fruit-bearing | Cây cao, biểu tượng nhiệt đới. Thu trái dừa. | — |
| 3 | Cây cau | tree\_areca | 3K–5K | Static + 5 Growth Stages | Sprout → Sapling → Young → Mature → Fruit-bearing | Cây cao thon, trang trí nông trại. Thu trái cau. | — |
| 4 | Giống Chà Là | tree\_date\_palm | 4K–6K | Static + 5 Growth Stages | Sprout → Sapling → Young → Mature → Fruit-bearing | Cây nhiệt đới, cho quả chà là. Sản phẩm: Hộp Chà Là. | ✓ |
| 5 | Giống Sa Chi | tree\_sacha | 3K–5K | Static + 5 Growth Stages | Sprout → Sapling → Young → Mature → Fruit-bearing | Cây dược liệu. Sản phẩm: Hộp Sa Chi. | ✓ |
| 6 | Giống Cây Trà | tree\_tea | 3K–5K | Static + 5 Growth Stages | Sprout → Sapling → Young → Mature → Leaf-bearing | Cây trà, thu hái lá. Sản phẩm: Túi Trà. | ✓ |
| 7 | Giống Sầu Riêng | tree\_durian | 5K–7K | Static + 5 Growth Stages | Sprout → Sapling → Young → Mature → Fruit-bearing | Cây ăn quả cao cấp. Sản phẩm: Hộp Sầu Riêng. | ✓ |
| 8 | Giống Măng Tây | plant\_asparagus | 2K–4K | Static + 4 Growth Stages | Sprout → Young → Mature → Bud-bearing | Cây cho búp măng tây. Chu kì trung bình. | ✓ |
| 9 | Giống hồng sâm | plant\_red\_ginseng | 2K–4K | Static + 5 Growth Stages | Sprout → Sapling → Young → Mature → Root-bearing | Cây dược liệu quý. Đào củ. Sản phẩm: Hộp hồng sâm. | ✓ |
| 10 | Sâm Tiến Vua | plant\_royal\_ginseng | 3K–5K | Static + 5 Growth Stages | Sprout → Sapling → Young → Mature → Premium Root | Sâm quý hiếm, level cao. Sản phẩm cao cấp nhất. | — |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| ✨ NHÓM 4: PHIÊN BẢN V2 / EVENT (Phiên bản nâng cấp & sự kiện) | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| 1 | Giống Sa Chi V2 | tree\_sacha\_v2 | 3K–5K | Static + 5 Growth Stages | 5 stage + Sparkle VFX | Phiên bản event với hiệu ứng VFX, sản lượng x2. Sản phẩm: Hộp Sa Chi V2. | — |
| 2 | Giống Sầu Riêng V2 | tree\_durian\_v2 | 5K–7K | Static + 5 Growth Stages | 5 stage + Glow VFX | Phiên bản event sầu riêng vàng. Sản phẩm: Hộp Sầu Riêng V2. | — |
| 3 | Giống Măng Tây V2 | plant\_asparagus\_v2 | 2K–4K | Static + 4 Growth Stages | 4 stage + Sparkle VFX | Phiên bản event. Sản phẩm: Búp Măng Tây V2. | — |
| 4 | Giống hồng sâm V2 | plant\_red\_ginseng\_v2 | 2K–4K | Static + 5 Growth Stages | 5 stage + Glow VFX | Phiên bản event. Sản phẩm: Hộp hồng sâm V2. | — |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| 🐟 NHÓM 5: THỦY SẢN (Aquaculture) | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| 1 | Dây Hàu Giống | aqua\_oyster\_seedline | 2K–4K | Static (underwater) | Idle (swing nhẹ theo dòng nước) + Growth Stages | Dây nuôi hàu — vật phẩm thủy sản. Trồng dưới nước, thu hoạch sau chu kì. | ✓ |
| 2 | Hàu lớn | aqua\_oyster\_mature | 1K–2K | Static | Open/Close shell animation | Hàu trưởng thành, sản phẩm thu hoạch từ dây hàu giống. | — |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| 📦 NHÓM 6: SẢN PHẨM THU HOẠCH (Harvested Products — Boxes & Pouches) | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| 1 | Hộp Chà Là | product\_date\_box | 1K–2K | Static prop | Idle (rotate slow trong inventory) | Sản phẩm thu từ cây Chà Là. Bán tại Mini Garden. | — |
| 2 | Hộp Sa Chi | product\_sacha\_box | 1K–2K | Static prop | Idle (rotate slow) | Sản phẩm thu từ Giống Sa Chi. | — |
| 3 | Túi Trà | product\_tea\_pouch | 1K–2K | Static prop | Idle (rotate slow) | Sản phẩm thu từ Cây Trà. Có thể bán hoặc đổi event. | — |
| 4 | Hộp Sầu Riêng | product\_durian\_box | 1K–2K | Static prop | Idle (rotate slow) | Sản phẩm thu từ Giống Sầu Riêng. | — |
| 5 | Búp Măng Tây | product\_asparagus\_bud | 1K–2K | Static prop | Idle (rotate slow) | Sản phẩm thu từ Giống Măng Tây. | — |
| 6 | Hộp hồng sâm | product\_red\_ginseng\_box | 1K–2K | Static prop | Idle (rotate slow) + glow | MỚI. Sản phẩm thu từ Giống hồng sâm. | — |
| 7 | Hộp Sa Chi V2 | product\_sacha\_box\_v2 | 1K–2K | Static prop | Idle + sparkle VFX | Sản phẩm event V2 cao cấp. | — |
| 8 | Túi Trà V2 | product\_tea\_pouch\_v2 | 1K–2K | Static prop | Idle + sparkle VFX | Sản phẩm event V2 cao cấp. | — |
| 9 | Hộp Sầu Riêng V2 | product\_durian\_box\_v2 | 1K–2K | Static prop | Idle + sparkle VFX | Sản phẩm event V2 cao cấp. | — |
| 10 | Búp Măng Tây V2 | product\_asparagus\_bud\_v2 | 1K–2K | Static prop | Idle + sparkle VFX | Sản phẩm event V2 cao cấp. | — |
| 11 | Hộp hồng sâm V2 | product\_red\_ginseng\_box\_v2 | 1K–2K | Static prop | Idle + glow VFX | Sản phẩm event V2 cao cấp. | — |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| 🎁 NHÓM 7: VẬT PHẨM CÓ THỂ TẶNG BẠN BÈ (Giftable Items Summary) | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Các vật phẩm có cờ ✓ ở cột 'Tặng bạn?' của nhóm 1–6 đều có thể gửi qua Gift Box (mục Bạn bè → Tặng quà). Dưới đây liệt kê tổng hợp lại để dev dễ làm UI Gift Picker: | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| STT | Tên vật phẩm | NaN | ID model | NaN | Ghi chú | NaN | NaN |
| 1 | Bò sữa | NaN | animal\_cow | NaN | Vật nuôi gốc | NaN | NaN |
| 2 | Rùa con | NaN | animal\_turtle\_baby | NaN | Vật nuôi cảnh | NaN | NaN |
| 3 | Heo con | NaN | animal\_pig\_baby | NaN | Vật nuôi | NaN | NaN |
| 4 | Hàu giống | NaN | aqua\_oyster\_seedline | NaN | Dây giống nuôi hàu | NaN | NaN |
| 5 | Giống Chà Là | NaN | tree\_date\_palm | NaN | Giống cây ăn quả | NaN | NaN |
| 6 | Giống Sa Chi | NaN | tree\_sacha | NaN | Giống cây dược liệu | NaN | NaN |
| 7 | Giống Cây Trà | NaN | tree\_tea | NaN | Giống cây thu lá | NaN | NaN |
| 8 | Giống Sầu Riêng | NaN | tree\_durian | NaN | Giống cây cao cấp | NaN | NaN |
| 9 | Giống Măng Tây | NaN | plant\_asparagus | NaN | Giống cây thu búp | NaN | NaN |
| 10 | Đà điểu V2 | NaN | animal\_ostrich | NaN | Vật nuôi V2 | NaN | NaN |
| 11 | Giống hồng sâm | NaN | plant\_red\_ginseng | NaN | Cây dược liệu quý | NaN | NaN |
| 12 | Cút mái V2 | NaN | animal\_quail\_female | NaN | Vật nuôi nhỏ V2 | NaN | NaN |
| 13 | Gà mái V2 | NaN | animal\_hen | NaN | Vật nuôi V2 | NaN | NaN |
| 14 | Dê con V2 | NaN | animal\_goat\_baby | NaN | Vật nuôi V2 | NaN | NaN |
| 15 | Giống cỏ voi | NaN | seed\_napier\_grass | NaN | Thức ăn cho vật nuôi | NaN | NaN |
| 16 | Hạt giống bắp | NaN | seed\_corn | NaN | Cây ngắn ngày | NaN | NaN |
| 17 | Hạt giống bắp cải | NaN | seed\_cabbage | NaN | Cây ngắn ngày | NaN | NaN |
| 18 | Hạt giống bí ngô | NaN | seed\_pumpkin | NaN | Cây ngắn ngày | NaN | NaN |
| 19 | Hạt giống cà rốt | NaN | seed\_carrot | NaN | Cây ngắn ngày (tutorial) | NaN | NaN |
| 20 | Hạt giống dưa hấu | NaN | seed\_watermelon | NaN | Cây ngắn ngày | NaN | NaN |
| 21 | Dây khoai lang giống | NaN | seed\_sweet\_potato | NaN | Cây ngắn ngày | NaN | NaN |
| 22 | Hạt giống rau muống | NaN | seed\_morning\_glory | NaN | Cây ngắn ngày | NaN | NaN |
| 23 | Ngỗng con V2 | NaN | animal\_gosling | NaN | Vật nuôi MỚI V2 | NaN | NaN |
| 24 | Thỏ con V2 | NaN | animal\_rabbit\_baby | NaN | Vật nuôi MỚI V2 | NaN | NaN |
| 25 | Vịt V3 | NaN | animal\_duck | NaN | Vật nuôi MỚI V3 | NaN | NaN |
| NaN | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| 📊 TỔNG KẾT YÊU CẦU 3D MODEL | NaN | NaN | NaN | NaN | NaN | NaN | NaN |
| Tổng số model con giống/vật nuôi | NaN | NaN | 11 model (3 V1 + 7 V2 + 1 V3) | NaN | NaN | NaN | NaN |
| Tổng số hạt giống/cây ngắn ngày | NaN | NaN | 8 model (đầy đủ growth stages) | NaN | NaN | NaN | NaN |
| Tổng số cây lâu năm | NaN | NaN | 10 model (5 growth stages mỗi cây) | NaN | NaN | NaN | NaN |
| Tổng số model V2/Event | NaN | NaN | 4 model phiên bản event | NaN | NaN | NaN | NaN |
| Tổng số model thủy sản | NaN | NaN | 2 model (dây giống + hàu lớn) | NaN | NaN | NaN | NaN |
| Tổng số sản phẩm (hộp/túi/búp) | NaN | NaN | 11 model (6 V1 + 5 V2) | NaN | NaN | NaN | NaN |
| Tổng số vật phẩm có thể tặng bạn | NaN | NaN | 25 vật phẩm (theo bảng Gift) | NaN | NaN | NaN | NaN |
| Tổng cộng 3D model cần dựng | NaN | NaN | ≈ 46 model riêng biệt + variants | NaN | NaN | NaN | NaN |
| Ngân sách polycount/model | NaN | NaN | 1K–9K tris (tối ưu cho mobile) | NaN | NaN | NaN | NaN |
| Định dạng yêu cầu | NaN | NaN | FBX với rig nếu có animation; OBJ cho prop tĩnh | NaN | NaN | NaN | NaN |
| LOD (Level of Detail) | NaN | NaN | 3 mức: LOD0 (full), LOD1 (50%), LOD2 (25%) — cho farm view rộng | NaN | NaN | NaN | NaN |
| Texture | NaN | NaN | Atlas chung theo nhóm: animals\_atlas, plants\_atlas, products\_atlas | NaN | NaN | NaN | NaN |