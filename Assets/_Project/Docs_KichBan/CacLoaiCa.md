# Dữ liệu các loài cá mới

> Cập nhật: 29/06/2026  
> Nguồn icon: `Assets/Sprites/icon/CacLoaiCa/`  
> Trạng thái: đã wire vào item data, gameplay câu cá, icon toast/túi đồ và Fish Shop.

## Quy tắc tỉ lệ câu

Tỉ lệ áp theo nhóm giá, đọc từ cá giá trị cao xuống cá giá trị thấp:

| Giá trị | Tỉ lệ câu được |
|---:|---:|
| 25 Point | 2% |
| 15 Point | 4% |
| 10 Point | 7% |
| 6 Point | 17% |
| 4 Point | 25% |
| 2 Point | 45% |

Tổng tỉ lệ: 100%.

## Bảng dữ liệu cá

| Nhóm giá | Tỉ lệ | Tên cá | Item ID đề xuất | Icon |
|---:|---:|---|---|---|
| 2 Point | 45% | Cá cơm | `fish_ca_com_01` | `Assets/Sprites/icon/CacLoaiCa/CaCom.png` |
| 2 Point | 45% | Cá nục | `fish_ca_nuc_01` | `Assets/Sprites/icon/CacLoaiCa/CaNuc.png` |
| 2 Point | 45% | Cá hồng | `fish_ca_hong_01` | `Assets/Sprites/icon/CacLoaiCa/CaHong.png` |
| 4 Point | 25% | Cá sư tử | `fish_ca_su_tu_01` | `Assets/Sprites/icon/CacLoaiCa/CaSuTu.png` |
| 4 Point | 25% | Cá naso | `fish_ca_naso_01` | `Assets/Sprites/icon/CacLoaiCa/CaNaso.png` |
| 4 Point | 25% | Cá nhồng | `fish_ca_nhong_01` | `Assets/Sprites/icon/CacLoaiCa/CaNhong.png` |
| 6 Point | 17% | Cá sọc dưa | `fish_ca_soc_dua_01` | `Assets/Sprites/icon/CacLoaiCa/CaSocDua.png` |
| 6 Point | 17% | Cá khế | `fish_ca_khe_01` | `Assets/Sprites/icon/CacLoaiCa/CaKhe.png` |
| 6 Point | 17% | Cá mú | `fish_ca_mu_01` | `Assets/Sprites/icon/CacLoaiCa/CaMu.png` |
| 10 Point | 7% | Cá mặt quỷ | `fish_ca_mat_quy_01` | `Assets/Sprites/icon/CacLoaiCa/CaMatQuy.png` |
| 10 Point | 7% | Cá heo biển | `fish_ca_heo_bien_01` | `Assets/Sprites/icon/CacLoaiCa/CaHeoBien.png` |
| 15 Point | 4% | Cá hoàng đế | `fish_ca_hoang_de_01` | `Assets/Sprites/icon/CacLoaiCa/CaHoangDe.png` |
| 15 Point | 4% | Cá ngừ hoàng kim | `fish_ca_ngu_hoang_kim_01` | `Assets/Sprites/icon/CacLoaiCa/CaNguHoangKim.png` |
| 25 Point | 2% | Cá rồng đỏ | `fish_ca_rong_do_01` | `Assets/Sprites/icon/CacLoaiCa/CaRongDo.png` |

## Ghi chú implement

- Đã implement vào `FishingOverlayController`: random nhóm giá trước, sau đó chọn ngẫu nhiên một loài trong nhóm đã trúng.
- Đã thêm item asset `Assets/Resources/Items/fish_ca_*.asset`; shop/túi đồ/toast lấy tên/icon từ `ItemDefinition.iconTexture`.
- Fish Shop đã whitelist các ID cá mới. Cá thuộc category `food`, nên ở popup shop sẽ thấy trong filter `Tất cả` khi bán.
- `sellPrice` của từng cá nên bằng đúng giá trị Point trong bảng.
- Khi câu cá, random trước theo nhóm tỉ lệ, sau đó chọn ngẫu nhiên một loài trong nhóm đã trúng.
- Nếu cần giữ tương thích dữ liệu cá cũ, không đổi ID `fish_01`/`fish_02`; chỉ thêm item mới.
