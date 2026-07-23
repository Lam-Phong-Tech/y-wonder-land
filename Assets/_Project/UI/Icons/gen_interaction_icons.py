# -*- coding: utf-8 -*-
"""Vẽ bộ icon trắng (nền trong suốt) cho cụm nút tương tác vòng cung.

Vẽ ở 512px rồi thu về 128px => viền mượt, không cần font/emoji.
Toạ độ nghĩ theo lưới 64x64 cho dễ hình dung.
"""
import math
import os
from PIL import Image, ImageDraw

OUT = os.path.join(
    "D:/LamGameUnity/BaChuKhuRung3D/Assets/Resources/UI/InteractionIcons"
)
SPRITE_SRC = "D:/LamGameUnity/BaChuKhuRung3D/Assets/Sprites/icon/"
S = 512          # canvas vẽ
FINAL = 128      # kích thước xuất
U = S / 64.0     # 1 đơn vị lưới = bao nhiêu pixel

ON = 255
OFF = 0


def new_mask():
    img = Image.new("L", (S, S), 0)
    return img, ImageDraw.Draw(img)


def circ(d, cx, cy, r, fill=ON):
    d.ellipse([(cx - r) * U, (cy - r) * U, (cx + r) * U, (cy + r) * U], fill=fill)


def rrect(d, x0, y0, x1, y1, r, fill=ON):
    d.rounded_rectangle([x0 * U, y0 * U, x1 * U, y1 * U], radius=r * U, fill=fill)


def poly(d, pts, fill=ON):
    d.polygon([(x * U, y * U) for x, y in pts], fill=fill)


def arc(d, x0, y0, x1, y1, a0, a1, w, fill=ON):
    d.arc([x0 * U, y0 * U, x1 * U, y1 * U], a0, a1, fill=fill, width=int(w * U))


def rot_rect(d, cx, cy, w, h, angle_deg, fill=ON):
    """Chữ nhật quay quanh tâm. angle=0 -> trục dài thẳng đứng."""
    a = math.radians(angle_deg)
    down = (math.sin(a), math.cos(a))
    side = (math.cos(a), -math.sin(a))
    hw, hh = w / 2.0, h / 2.0
    pts = []
    for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
        px = cx + side[0] * hw * sx + down[0] * hh * sy
        py = cy + side[1] * hw * sx + down[1] * hh * sy
        pts.append((px, py))
    poly(d, pts, fill)


def save(mask, name):
    mask = mask.resize((FINAL, FINAL), Image.LANCZOS)
    out = Image.new("RGBA", (FINAL, FINAL), (255, 255, 255, 0))
    out.putalpha(mask)
    white = Image.new("RGBA", (FINAL, FINAL), (255, 255, 255, 255))
    white.putalpha(mask)
    white.save(os.path.join(OUT, name + ".png"))


# ─────────────────────────────────────────────────────────────
def icon_eye():
    m, d = new_mask()
    top, bot = [], []
    for i in range(61):
        x = 5 + i * (54 / 60.0)
        k = math.sin(math.pi * (x - 5) / 54.0)
        top.append((x, 32 - 16 * k))
        bot.append((x, 32 + 16 * k))
    poly(d, top + bot[::-1])
    circ(d, 32, 32, 11, OFF)      # vành mống mắt (khoảng trống)
    circ(d, 32, 32, 7, ON)        # con ngươi
    return m


def icon_hand_release():
    """Bàn tay úp, ngón duỗi ngang — theo emoji 🫳 anh gửi.

    Vẽ thành MỘT khối liền rồi khoét khe giữa các ngón, chứ không ghép từng thanh
    rời: ghép rời thì ra hình cái lược, không ra bàn tay.
    """
    m, d = new_mask()
    curved_bar(d, 9, 29, 52, 35, -3, 25, 14)          # mu bàn tay thon dần ra ngón
    for cy in (30.5, 34.5, 38.5):                     # bo tròn đầu ngón
        circ(d, 51, cy, 3.2)
    for cy in (32.4, 36.4, 40.2):                     # khe giữa các ngón
        rot_rect(d, 46, cy, 2.4, 26, 92, OFF)
    curved_bar(d, 21, 41, 31, 53, 2, 9, 6)            # ngón cái chúc xuống
    return m


def icon_x():
    m, d = new_mask()
    rot_rect(d, 32, 32, 11, 48, 45)
    rot_rect(d, 32, 32, 11, 48, -45)
    return m


def curved_bar(d, x0, y0, x1, y1, bow, w0, w1, fill=ON):
    """Thanh cong (cán rìu/cán cuốc). bow = độ phình ngang so với đường thẳng."""
    import math as _m
    left, right = [], []
    n = 24
    for i in range(n + 1):
        t = i / n
        bx = x0 + (x1 - x0) * t + bow * _m.sin(_m.pi * t)
        by = y0 + (y1 - y0) * t
        dx, dy = (x1 - x0) + bow * _m.pi * _m.cos(_m.pi * t), (y1 - y0)
        ln = _m.hypot(dx, dy) or 1.0
        nx, ny = -dy / ln, dx / ln
        hw = (w0 + (w1 - w0) * t) / 2.0
        left.append((bx + nx * hw, by + ny * hw))
        right.append((bx - nx * hw, by - ny * hw))
    poly(d, left + right[::-1], fill)


def silhouette(source_path):
    """Đổ BÓNG TRẮNG của art màu có sẵn trong kho ảnh dự án.

    Mấy món này (rìu, cuốc, cuốc chim, cần câu) vẽ tay ra hình không ưng, trong khi
    art của dự án đã đúng y dáng khách gửi. Lấy bóng thì vừa đúng dáng vừa đồng bộ
    với các icon ký hiệu vẽ tay (X, con mắt, chữ i...).
    """
    from PIL import Image as _I
    src = _I.open(SPRITE_SRC + source_path + ".png").convert("RGBA")
    box = src.getchannel("A").getbbox()
    if box:
        src = src.crop(box)

    pad = 0.90  # chừa lề, kẻo nét chạm sát mép ô icon trông như bị xén
    alpha = src.getchannel("A")
    k = min(S * pad / alpha.width, S * pad / alpha.height)
    alpha = alpha.resize((max(1, int(alpha.width * k)), max(1, int(alpha.height * k))), _I.LANCZOS)

    out = _I.new("L", (S, S), 0)
    out.paste(alpha, ((S - alpha.width) // 2, (S - alpha.height) // 2))
    return out


def icon_seed():
    m, d = new_mask()
    # Mầm cây nhú lên khỏi luống đất — rõ nghĩa "gieo hạt" hơn là mấy hạt rơi
    # (hạt rơi trông hệt giọt nước, đụng nghĩa với icon tưới).
    d.pieslice([4 * U, 40 * U, 60 * U, 74 * U], 180, 360, fill=ON)   # luống đất
    rrect(d, 30, 18, 34, 46, 2)                       # thân mầm
    poly(d, [(32, 30), (14, 26), (12, 14), (28, 20)])  # lá trái
    poly(d, [(32, 24), (50, 16), (54, 27), (34, 33)])  # lá phải
    return m


def icon_watering_can():
    m, d = new_mask()
    arc(d, 21, 5, 45, 29, 180, 360, 5)                # quai vắt qua miệng bình
    poly(d, [(20, 32), (7, 19), (2, 26), (16, 41)])   # vòi
    rot_rect(d, 5, 22, 14, 5, 45)                     # đầu vòi loe
    rrect(d, 14, 21, 50, 28, 3)                       # miệng bình
    rrect(d, 18, 27, 46, 56, 7)                       # thân bình
    return m


def icon_basket():
    m, d = new_mask()
    circ(d, 23, 29, 6)                                # nông sản nhô lên
    circ(d, 34, 26, 7)
    circ(d, 44, 30, 6)
    arc(d, 19, 14, 45, 40, 180, 360, 4.5)             # quai giỏ
    poly(d, [(12, 33), (52, 33), (46, 55), (18, 55)])  # giỏ
    return m


def icon_feed_bowl():
    m, d = new_mask()
    for cx, cy in ((23, 27), (32, 22), (41, 27)):
        circ(d, cx, cy, 4.5)                          # thức ăn
    poly(d, [(9, 34), (55, 34), (46, 54), (18, 54)])  # bát
    rrect(d, 14, 52, 50, 56, 2)                       # đế bát
    return m


def icon_cross():
    m, d = new_mask()
    rrect(d, 26, 9, 38, 55, 4)
    rrect(d, 9, 26, 55, 38, 4)
    return m


def icon_syringe():
    m, d = new_mask()
    rot_rect(d, 36, 28, 15, 30, -45)                  # ống thuốc
    rot_rect(d, 47, 17, 5, 18, -45)                   # cần đẩy
    rot_rect(d, 44, 20, 20, 4.5, -45 + 90)            # vành tay cầm
    rot_rect(d, 51, 13, 14, 4.5, -45 + 90)            # núm đẩy
    rot_rect(d, 18, 46, 3.2, 18, -45)                 # kim
    return m


def icon_info():
    m, d = new_mask()
    circ(d, 32, 32, 27)
    circ(d, 32, 18, 4.5, OFF)
    rrect(d, 28.5, 25, 35.5, 48, 3.2, OFF)
    return m


def icon_water_bucket():
    m, d = new_mask()
    arc(d, 15, 9, 49, 41, 180, 360, 4.5)              # quai
    poly(d, [(13, 25), (51, 25), (46, 55), (18, 55)])  # xô
    rrect(d, 16, 33, 48, 36.5, 1.5, OFF)              # mực nước
    circ(d, 56, 12, 3.5)                              # giọt nước
    return m



# ── Đổ bóng từ art có sẵn: dụng cụ, vẽ tay không bằng ──────────────────────
TRACED = {
    "Icon_Axe": "BoSungIcon/Axe",            # Chặt cây
    "Icon_Hoe": "BoSungIcon/Cuoc",           # Cuốc đất
    "Icon_Pickaxe": "BoSungIcon/CuocChim",   # Đào khoáng
    "Icon_FishingRod": "BoSungIcon/CauCa",   # Câu cá
}

# ── Vẽ tay: ký hiệu và đồ vật không có art sẵn ─────────────────────────────
# Icon_Basket / Icon_FeedBowl / Icon_HandRelease là bản TẠM, chờ art anh gửi.
ICONS = {
    "Icon_Eye": icon_eye,
    "Icon_HandRelease": icon_hand_release,
    "Icon_Cancel": icon_x,
    "Icon_Seed": icon_seed,
    "Icon_WateringCan": icon_watering_can,
    "Icon_Basket": icon_basket,
    "Icon_FeedBowl": icon_feed_bowl,
    "Icon_Cross": icon_cross,
    "Icon_Syringe": icon_syringe,
    "Icon_Info": icon_info,
    "Icon_WaterBucket": icon_water_bucket,
}

# Icon_Shop / Icon_Anvil / Icon_PiggyBank / Icon_Hand chép tay một lần từ bộ
# FarmingIconsCollection_REMOVE_BG, script này KHÔNG sinh lại — đừng xoá chúng
# trong Resources/UI/InteractionIcons rồi chạy lại script mà tưởng là đủ.

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    made = []
    for name, fn in ICONS.items():
        save(fn(), name)
        made.append(name)
    for name, src in TRACED.items():
        save(silhouette(src), name)
        made.append(name)
    print("da sinh", len(made), "icon")

    cols = 5
    rows = (len(made) + cols - 1) // cols
    sheet = Image.new("RGBA", (FINAL * cols, FINAL * rows), (90, 90, 90, 255))
    for i, name in enumerate(made):
        im = Image.open(os.path.join(OUT, name + ".png"))
        sheet.paste(im, ((i % cols) * FINAL, (i // cols) * FINAL), im)
    sheet.save(os.path.join(os.path.dirname(os.path.abspath(__file__)), "preview.png"))
    print("sheet ->", os.path.dirname(os.path.abspath(__file__)))
