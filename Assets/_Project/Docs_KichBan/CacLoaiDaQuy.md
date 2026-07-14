# Dữ liệu các loại đá quý khi đào đá

> Cập nhật: 29/06/2026  
> Nguồn icon: `Assets/Sprites/icon/CacLoaiDaQuy/`  
> Trạng thái: đã wire item data, icon túi đồ và toast khi đào trúng; chưa thêm shop thu mua đá quý.

## Quy tắc đào đá

- Mỗi lượt đào vẫn luôn nhận đá thường `rock/stone` như hiện tại: 100%, số lượng giữ theo gameplay hiện tại là 10 rock.
- Đá quý là phần thưởng roll riêng khi đào đá.
- Mỗi ngày có 10 lượt đào.
- Tỉ lệ dưới đây áp theo nhóm đá quý, đọc từ đá quý hiếm/giá trị cao xuống đá giá trị thấp.

| Ảnh | Loại đá quý | Giá trị | Số viên khi trúng | Tỉ lệ đào trúng | Điều kiện/chi phí |
|---:|---|---:|---:|---:|---|
| 6 | Ruby quý hiếm | 3000 Point/viên | 1 viên | 1% | Nâng cấp cuốc lv3 tốn 1500 Point |
| 5 | Amethyst | 500 Point/viên | 1 viên | 2% | Nâng cấp cuốc lv2 tốn 250 Point/lượt |
| 4 | Fire Quartz | 12 Point/viên | 2 viên | 5% | - |
| 3 | Green Calcite | 6 Point/viên | 3 viên | 12% | - |
| 2 | Orange Calcite | 3 Point/viên | 4 viên | 30% | - |
| 1 | Kyanite | 2 Point/viên | 4 viên | 50% | - |

Tổng tỉ lệ đá quý: 100%.

## Bảng dữ liệu đá quý

| Ảnh | Nhóm giá | Tỉ lệ | Số viên | Tên đá quý | Item ID đề xuất | Icon |
|---:|---:|---:|---:|---|---|---|
| 1 | 2 Point | 50% | 4 | Kyanite | `gem_kyanite_01` | `Assets/Sprites/icon/CacLoaiDaQuy/Kyanite[1].png` |
| 2 | 3 Point | 30% | 4 | Orange Calcite | `gem_orange_calcite_01` | `Assets/Sprites/icon/CacLoaiDaQuy/OrangeCalcite[2].png` |
| 3 | 6 Point | 12% | 3 | Green Calcite | `gem_green_calcite_01` | `Assets/Sprites/icon/CacLoaiDaQuy/GreenCalcite[3].png` |
| 4 | 12 Point | 5% | 2 | Fire Quartz | `gem_fire_quartz_01` | `Assets/Sprites/icon/CacLoaiDaQuy/FireQuartz[4].png` |
| 5 | 500 Point | 2% | 1 | Amethyst | `gem_amethyst_01` | `Assets/Sprites/icon/CacLoaiDaQuy/Amethyst[5].png` |
| 6 | 3000 Point | 1% | 1 | Ruby quý hiếm | `gem_ruby_01` | `Assets/Sprites/icon/CacLoaiDaQuy/Ruby[6].png` |

## Ghi chú implement sau

- Khi implement, giữ nguyên reward đá thường hiện tại: mỗi lượt đào vẫn cộng 10 rock/stone như cũ.
- Sau khi cộng đá thường, roll thêm đá quý theo bảng tỉ lệ 50% / 30% / 12% / 5% / 2% / 1%.
- `sellPrice` của từng đá quý nên bằng đúng giá trị Point/viên trong bảng.
- Đã thêm item asset `Assets/Resources/Items/gem_*.asset`, category `materials`, icon lấy từ `ItemDefinition.iconTexture` để hiện trong túi đồ.
- Đã gắn toast `ScreenToast.ShowInfoWithIcon` khi đào trúng đá quý; shop thu mua đá quý chưa làm theo yêu cầu hiện tại.
- Nếu có UI nâng cấp cuốc, cần làm rõ cách tiêu 250 Point/lượt cho lv2 và 1500 Point cho lv3 trước khi khóa logic.
- Không tạo/đoán thêm asset mới ngoài 6 icon đang có trong `Assets/Sprites/icon/CacLoaiDaQuy/`.
