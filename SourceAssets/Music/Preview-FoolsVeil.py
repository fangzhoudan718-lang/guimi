# -*- coding: utf-8 -*-
"""把 SourceAssets/Music/build/FoolsVeil.wav 画成波形 + 频谱预览图，用来肉眼检查编曲结构。"""
import os
import wave
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
WAV = os.path.join(HERE, 'build', 'FoolsVeil.wav')
OUT = os.path.join(HERE, 'build', 'FoolsVeil-preview.png')

W, H_WAVE, H_SPEC = 1200, 220, 380


def read_wav(path):
    with wave.open(path, 'rb') as w:
        ch, sr, n = w.getnchannels(), w.getframerate(), w.getnframes()
        raw = np.frombuffer(w.readframes(n), dtype='<i2').astype(np.float64) / 32768.0
    data = raw.reshape(-1, ch).T
    return data, sr


def colormap(x):
    """x 是 0..1 的数组，返回 (n,3) 的 uint8 颜色（暗紫 → 金）。"""
    stops = [(0.00, (10, 8, 20)), (0.35, (52, 30, 84)), (0.62, (140, 92, 168)),
             (0.82, (214, 160, 120)), (1.00, (255, 236, 190))]
    x = np.clip(x, 0, 1)
    out = np.zeros(x.shape + (3,), dtype=np.float64)
    for i in range(len(stops) - 1):
        a, ca = stops[i]
        b, cb = stops[i + 1]
        m = (x >= a) & (x <= b)
        if not np.any(m):
            continue
        t = (x[m] - a) / (b - a)
        for c in range(3):
            out[m, c] = ca[c] + (cb[c] - ca[c]) * t
    return out.astype(np.uint8)


def main():
    data, sr = read_wav(WAV)
    mono = data.mean(axis=0)
    dur = len(mono) / sr
    img = Image.new('RGB', (W, H_WAVE + H_SPEC), (10, 8, 20))
    draw = ImageDraw.Draw(img)

    # ── 波形 ──
    per = max(1, len(mono) // W)
    for x in range(W):
        seg = mono[x * per:(x + 1) * per]
        if seg.size == 0:
            continue
        lo, hi = seg.min(), seg.max()
        y0 = int(H_WAVE / 2 - hi * (H_WAVE / 2 - 6))
        y1 = int(H_WAVE / 2 - lo * (H_WAVE / 2 - 6))
        draw.line([(x, y0), (x, y1)], fill=(196, 168, 232))
    draw.line([(0, H_WAVE // 2), (W, H_WAVE // 2)], fill=(60, 44, 88))

    # ── 频谱 ──
    win, hop = 2048, 1024
    frames = 1 + (len(mono) - win) // hop
    step = max(1, frames // W)
    mags = []
    window = np.hanning(win)
    for f in range(0, frames, step):
        seg = mono[f * hop:f * hop + win] * window
        mags.append(np.abs(np.fft.rfft(seg)))
    spec = np.array(mags).T                              # (bins, 列)
    spec = 20 * np.log10(spec + 1e-6)
    spec -= spec.max()
    spec = np.clip((spec + 70) / 70, 0, 1) ** 0.8
    spec = spec[:int(spec.shape[0] * 0.55)]              # 只看低中频，更易读
    spec_img = Image.fromarray(colormap(spec), 'RGB').resize((W, H_SPEC), Image.BILINEAR)
    img.paste(spec_img, (0, H_WAVE))

    # 段落刻度
    for bar, label in [(0, '序幕'), (4, '主题'), (12, '上升'), (20, '高潮'), (24, '落幕')]:
        t = bar * (60 / 68.0) * 4
        x = int(W * t / dur)
        draw.line([(x, 0), (x, H_WAVE + H_SPEC)], fill=(90, 70, 120))
        draw.text((x + 4, 6), label, fill=(220, 200, 240))
    draw.text((6, H_WAVE - 18), f'{dur:.1f}s', fill=(200, 180, 220))

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    img.save(OUT)
    print('preview ->', OUT)


if __name__ == '__main__':
    main()
