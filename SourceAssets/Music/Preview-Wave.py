# -*- coding: utf-8 -*-
"""通用的波形 + 频谱预览：把 SourceAssets/Music/build/<名字>.wav 画成 PNG，用来肉眼检查编曲。
    python SourceAssets/Music/Preview-Wave.py UsurperTheme
"""
import os
import sys
import wave
import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
W, H_WAVE, H_SPEC = 1200, 220, 380


def read_wav(path):
    with wave.open(path, 'rb') as w:
        ch, sr, n = w.getnchannels(), w.getframerate(), w.getnframes()
        raw = np.frombuffer(w.readframes(n), dtype='<i2').astype(np.float64) / 32768.0
    return raw.reshape(-1, ch).T, sr


def colormap(x):
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


def main(name):
    wav = os.path.join(HERE, 'build', name + '.wav')
    out = os.path.join(HERE, 'build', name + '-preview.png')
    data, sr = read_wav(wav)
    mono = data.mean(axis=0)
    dur = len(mono) / sr

    img = Image.new('RGB', (W, H_WAVE + H_SPEC), (10, 8, 20))
    draw = ImageDraw.Draw(img)

    per = max(1, len(mono) // W)
    for x in range(W):
        seg = mono[x * per:(x + 1) * per]
        if seg.size == 0:
            continue
        y0 = int(H_WAVE / 2 - seg.max() * (H_WAVE / 2 - 6))
        y1 = int(H_WAVE / 2 - seg.min() * (H_WAVE / 2 - 6))
        draw.line([(x, y0), (x, y1)], fill=(196, 168, 232))
    draw.line([(0, H_WAVE // 2), (W, H_WAVE // 2)], fill=(60, 44, 88))

    win, hop = 2048, 1024
    frames = 1 + (len(mono) - win) // hop
    step = max(1, frames // W)
    window = np.hanning(win)
    mags = [np.abs(np.fft.rfft(mono[f * hop:f * hop + win] * window)) for f in range(0, frames, step)]
    spec = np.array(mags).T
    spec = 20 * np.log10(spec + 1e-6)
    spec -= spec.max()
    spec = np.clip((spec + 70) / 70, 0, 1) ** 0.8
    spec = spec[:int(spec.shape[0] * 0.55)]
    img.paste(Image.fromarray(colormap(spec), 'RGB').resize((W, H_SPEC), Image.BILINEAR), (0, H_WAVE))

    for frac, label in [(0.0, '0s'), (0.5, f'{dur / 2:.0f}s'), (0.99, f'{dur:.1f}s')]:
        x = int(W * frac) - (0 if frac == 0 else 40)
        draw.text((max(2, x), 6), label, fill=(220, 200, 240))
    img.save(out)
    print('preview ->', out)


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else 'FoolsVeil')
