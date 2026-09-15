# -*- coding: utf-8 -*-
"""
生成灵性恢复药水（四档）与「灵质」材料的像素贴图。

原版治疗药水的原始像素在离线环境里取不到，所以这里按原版那套
「小瓶 → 大瓶」的轮廓与比例手绘，统一换成暗紫配色：
    弱效 21x26 / 普通 25x30 / 强效 29x34 / 超级 33x38
以及一枚 20x20 的灵质结晶。

重建：python SourceAssets/Spirituality/Build-SpiritSprites.py
"""
import os
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
POTION_DIR = os.path.join(ROOT, 'Content', 'Items', 'Potions', 'Spirituality')
MAT_DIR = os.path.join(ROOT, 'Content', 'Items', 'Materials')

# 暗紫配色
OUTLINE = (26, 18, 40, 255)
GLASS = (78, 62, 104, 255)
GLASS_DARK = (52, 40, 74, 255)
LIQUID = (96, 46, 146, 255)
LIQUID_DARK = (62, 28, 100, 255)
LIQUID_LIGHT = (138, 78, 190, 255)
HIGHLIGHT = (188, 156, 226, 160)
CORK = (126, 96, 62, 255)
CORK_DARK = (92, 68, 44, 255)
SHINE = (226, 208, 255, 90)


def flask_mask(w, h):
    """上窄下宽的圆肩瓶：返回 bool 掩码。"""
    mask = np.zeros((w, h), dtype=bool)
    neck_h = max(3, int(round(h * 0.20)))
    neck_half = max(1.0, w * 0.16)
    cx = (w - 1) / 2.0
    body_h = max(1, h - neck_h)
    for y in range(h):
        if y < neck_h:
            half = neck_half
        else:
            t = (y - neck_h) / body_h
            half = (w / 2.0 - 1.0) * min(1.0, 0.42 + 0.85 * t)
        for x in range(int(round(cx - half)), int(round(cx + half)) + 1):
            if 0 <= x < w:
                mask[x, y] = True
    return mask


def build_potion(w, h, liquid_fill, label):
    mask = flask_mask(w, h)
    img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    px = img.load()
    neck_h = max(3, int(round(h * 0.20)))
    liquid_top = int(round(h * (1.0 - liquid_fill)))
    cx = (w - 1) / 2.0

    for y in range(h):
        for x in range(w):
            if not mask[x, y]:
                continue
            if y < neck_h - 1:
                px[x, y] = CORK_DARK if x > cx else CORK           # 软木塞
            elif y < liquid_top:
                px[x, y] = GLASS_DARK if abs(x - cx) > w * 0.22 else GLASS
            else:
                depth = (y - liquid_top) / max(1, h - liquid_top)
                if abs(x - cx) > w * 0.30:
                    px[x, y] = LIQUID_DARK
                elif depth < 0.25:
                    px[x, y] = LIQUID_LIGHT                            # 液面反光
                else:
                    px[x, y] = LIQUID
                if y - liquid_top <= 1:
                    px[x, y] = LIQUID_LIGHT                        # 液面亮线

    # 高光：瓶身左侧一条
    for y in range(neck_h + 1, h - 2):
        x = int(round(cx - (w * 0.26)))
        if 0 <= x < w and mask[x, y]:
            base = px[x, y]
            px[x, y] = (min(255, base[0] + 60), min(255, base[1] + 60), min(255, base[2] + 60), 255)

    # 描边：掩码边缘一圈
    for y in range(h):
        for x in range(w):
            if not mask[x, y]:
                continue
            edge = False
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if nx < 0 or ny < 0 or nx >= w or ny >= h or not mask[nx, ny]:
                    edge = True
                    break
            if edge:
                px[x, y] = OUTLINE

    path = os.path.join(POTION_DIR, label + '.png')
    img.save(path)
    return path


def build_essence(size=20):
    """灵质：一枚悬着的暗紫结晶，中心有点亮。"""
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx = cy = (size - 1) / 2.0
    # 光晕
    for r in range(int(size * 0.48), 1, -1):
        a = int(26 * (1.0 - r / (size * 0.5)))
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(120, 70, 180, a))
    # 结晶本体：上下尖、左右宽的菱形
    d.polygon([(cx, cy - size * 0.40), (cx + size * 0.26, cy), (cx, cy + size * 0.40), (cx - size * 0.26, cy)],
              fill=(96, 52, 148, 255), outline=(38, 24, 56, 255))
    # 内部亮面与高光
    d.polygon([(cx, cy - size * 0.30), (cx + size * 0.13, cy - size * 0.02), (cx, cy + size * 0.24),
               (cx - size * 0.13, cy - size * 0.02)], fill=(140, 88, 200, 255))
    d.line([(cx - size * 0.05, cy - size * 0.22), (cx - size * 0.05, cy + size * 0.12)], fill=(226, 206, 255, 220))
    path = os.path.join(MAT_DIR, 'SpiritEssence.png')
    img.save(path)
    return path


def main():
    os.makedirs(POTION_DIR, exist_ok=True)
    os.makedirs(MAT_DIR, exist_ok=True)
    tiers = [('LesserSpiritualityPotion', 21, 26, 0.42),
             ('SpiritualityPotion', 25, 30, 0.50),
             ('GreaterSpiritualityPotion', 29, 34, 0.58),
             ('SuperSpiritualityPotion', 33, 38, 0.66)]
    for label, w, h, fill in tiers:
        print('potion ->', build_potion(w, h, fill, label))
    print('material ->', build_essence())


if __name__ == '__main__':
    main()
