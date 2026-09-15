# -*- coding: utf-8 -*-
"""
从泰拉瑞亚本体的 XNB 里取出原版四档治疗药水贴图，改成暗紫配色。

XNB 是 LZX 压缩的，Windows 上没法纯 Python 解，所以借用游戏自带 FNA 的
LzxDecoder：先由 Decode-Xnb.ps1（PowerShell 7）把内容负载解出来，
这里再解析 Texture2DReader 的数据（RGBA 或 DXT）、改色、写到模组里。

重建：pwsh -File SourceAssets/Spirituality/Extract-VanillaPotions.py 需要先有 Decode-Xnb.ps1
    python SourceAssets/Spirituality/Extract-VanillaPotions.py
"""
import os
import struct
import subprocess
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
GAME = r'D:\steam\steamapps\common\Terraria\Content\Images'
BUILD = os.path.join(HERE, 'build')
OUT = os.path.join(ROOT, 'Content', 'Items', 'Potions', 'Spirituality')
DECODER = os.path.join(HERE, 'Decode-Xnb.ps1')

# 原版四档治疗药水
TIERS = [
    ('LesserSpiritualityPotion', 28),
    ('SpiritualityPotion', 188),
    ('GreaterSpiritualityPotion', 499),
    ('SuperSpiritualityPotion', 3544),
]

PURPLE = np.array([104.0, 58.0, 158.0])       # 中间的紫
HIGHLIGHT = np.array([206.0, 178.0, 246.0])   # 高光偏白紫


def decode_xnb(xnb, out_bin):
    if os.path.exists(out_bin):
        os.remove(out_bin)
    subprocess.run(['pwsh', '-NoProfile', '-File', DECODER, '-Path', xnb, '-Out', out_bin], check=True)


def read7(d, i):
    v = s = 0
    while True:
        b = d[i]
        i += 1
        v |= (b & 0x7F) << s
        s += 7
        if not (b & 0x80):
            break
    return v, i


def parse_texture(payload):
    """返回 (surface_format, width, height, data)。"""
    i = payload.find(b'PublicKeyToken=')
    if i < 0:
        raise ValueError('没找到 Texture2DReader 的类型串')
    end = payload.find(b'\x00', i)
    # 类型串之后是版本、共享资源计数，再往后才是贴图元数据；
    # 具体偏移在版本间有 1~2 字节出入，所以这里扫一遍找到合理的一组。
    for start in range(end + 4, end + 14):
        try:
            fmt, w, h, mips, size = struct.unpack_from('<5i', payload, start)
        except struct.error:
            continue
        if fmt not in (0, 1, 4, 5, 6) or mips < 1 or mips > 8:
            continue
        if not (0 < w <= 4096 and 0 < h <= 4096):
            continue
        bpp = 4 if fmt == 0 else 2  # 只见得到 Color 与 DXT1，其余当 2 字节/像素估
        if size <= 0 or start + 20 + size > len(payload):
            continue
        return fmt, w, h, payload[start + 20:start + 20 + size]
    raise ValueError('没能定位贴图元数据')


def dxt1_decode(data, w, h):
    """DXT1 解码：4x4 一块，8 字节。"""
    out = np.zeros((h, w, 4), dtype=np.uint8)
    blocks_x = (w + 3) // 4
    blocks_y = (h + 3) // 4
    for by in range(blocks_y):
        for bx in range(blocks_x):
            off = (by * blocks_x + bx) * 8
            c0, c1, bits = struct.unpack_from('<HHI', data, off)
            def expand(c):
                r = (c >> 11) & 0x1F
                g = (c >> 5) & 0x3F
                b = c & 0x1F
                return np.array([(r << 3) | (r >> 2), (g << 2) | (g >> 4), (b << 3) | (b >> 2), 255], dtype=np.uint8)
            e0, e1 = expand(c0), expand(c1)
            if c0 > c1:
                pal = [e0, e1,
                       ((2 * e0.astype(int) + e1) // 3).astype(np.uint8),
                       ((e0.astype(int) + 2 * e1) // 3).astype(np.uint8)]
            else:
                pal = [e0, e1,
                       ((e0.astype(int) + e1) // 2).astype(np.uint8),
                       np.array([0, 0, 0, 0], dtype=np.uint8)]
            for py in range(4):
                for px in range(4):
                    x, y = bx * 4 + px, by * 4 + py
                    if x >= w or y >= h:
                        continue
                    idx = (bits >> (2 * (py * 4 + px))) & 0x3
                    out[y, x] = pal[idx]
    return out


def load_vanilla_png(item_id):
    xnb = os.path.join(GAME, f'Item_{item_id}.xnb')
    if not os.path.exists(xnb):
        raise FileNotFoundError(xnb)
    bin_path = os.path.join(BUILD, f'Item_{item_id}.bin')
    os.makedirs(BUILD, exist_ok=True)
    if not os.path.exists(bin_path):
        decode_xnb(xnb, bin_path)
    payload = open(bin_path, 'rb').read()
    fmt, w, h, data = parse_texture(payload)
    if fmt == 0:
        arr = np.frombuffer(data, dtype=np.uint8).reshape(h, w, 4).copy()
    elif fmt in (4, 5, 6):
        arr = dxt1_decode(data, w, h)   # 见得到的都是 DXT1，其它格式也先按它解
    else:
        raise ValueError(f'还不支持 surface format {fmt}')
    return Image.fromarray(arr, 'RGBA'), fmt, w, h


# 深紫三档：阴影 → 药液 → 反光
SHADE_P = np.array([46.0, 12.0, 74.0])
LIQUID_P = np.array([116.0, 40.0, 168.0])
SHINE_P = np.array([196.0, 132.0, 232.0])


def rgb_to_hue_sat_val(rgb):
    """rgb 为 0..255，返回 (色相 0..360, 饱和 0..1, 明度 0..1)。"""
    v = rgb.max(axis=-1)
    mn = rgb.min(axis=-1)
    diff = v - mn
    sat = np.where(v > 1e-6, diff / np.maximum(v, 1e-6), 0.0)

    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    hue = np.zeros_like(v)
    safe = diff > 1e-6
    # 标准 HSV 色相公式
    with np.errstate(invalid='ignore', divide='ignore'):
        hue = np.where(safe & (v == r), ((g - b) / np.maximum(diff, 1e-6)) % 6.0, hue)
        hue = np.where(safe & (v == g), ((b - r) / np.maximum(diff, 1e-6)) + 2.0, hue)
        hue = np.where(safe & (v == b), ((r - g) / np.maximum(diff, 1e-6)) + 4.0, hue)
    return hue * 60.0, np.clip(sat, 0, 1), v / 255.0


def recolor(img):
    """
    只把原版治疗药水里的「红色药液」换成深紫，其它一律不动：
    玻璃、瓶塞、描边、高光都保留原本的样子。

    判定红色用的是色相：真正的药液在大约 340°~20°，
    而瓶塞的棕色在 30° 以上，所以不会被误伤。
    """
    arr = np.asarray(img).astype(np.float64)
    rgb, alpha = arr[..., :3], arr[..., 3:]
    hue, sat, val = rgb_to_hue_sat_val(rgb)

    is_red = (sat > 0.28) & (val > 0.20) & ((hue >= 335.0) | (hue <= 22.0))

    # 用红色像素原本的明度决定紫的深浅，药液的层次就还在
    t = np.clip(val, 0.0, 1.0)[..., None]
    lower = SHADE_P + (LIQUID_P - SHADE_P) * np.clip(t / 0.62, 0.0, 1.0)
    upper = LIQUID_P + (SHINE_P - LIQUID_P) * np.clip((t - 0.62) / 0.38, 0.0, 1.0)
    purple = np.where(t < 0.62, lower, upper)

    out = np.where(is_red[..., None], np.clip(purple, 0, 255), rgb)
    return Image.fromarray(np.concatenate([out, alpha], axis=2).astype(np.uint8), 'RGBA')


def main():
    os.makedirs(OUT, exist_ok=True)
    for label, item_id in TIERS:
        img, fmt, w, h = load_vanilla_png(item_id)
        path = os.path.join(OUT, label + '.png')
        recolor(img).save(path)
        print(f'{label}: Item_{item_id} format={fmt} {w}x{h} -> {path}')


if __name__ == '__main__':
    main()
