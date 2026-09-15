# -*- coding: utf-8 -*-
"""
《弑序》——黑皇帝途径晋升序列一（弑序亲王）的那一下。

和《愚者的帷幕》正好相反：这一首没有铺垫，第 0 秒就是全奏，
因为触发它的那一瞬间本来就已经是高潮了。30 秒，C 小调，96 BPM，12 小节。

    第 1 小节      全奏重击（低铜管 + 定音鼓 + 镲 + 合唱 + 弦乐震音）——开场即顶点
    2-4 小节       铜管主题第一次陈述，弦乐八分音符驱动
    5-8 小节       主题抬高八度，加入对位；第 8 小节靠导音顶住
    9-10 小节      半音上行推向第二次全奏
    11-12 小节     落回主调，最后一记重击与一口钟，收在余响里

重建：python SourceAssets/Music/Build-UsurperTheme.py
"""
import os
import wave
import numpy as np

SR = 44100
BPM = 96.0
BEAT = 60.0 / BPM
BAR = 4.0 * BEAT
BARS = 12
TAIL = 1.8
DUR = BARS * BAR + TAIL
N = int(DUR * SR)

OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'build')
_rng = np.random.default_rng(20260915)


def freq(name):
    names = {'C': 0, 'C#': 1, 'Db': 1, 'D': 2, 'D#': 3, 'Eb': 3, 'E': 4, 'F': 5,
             'F#': 6, 'Gb': 6, 'G': 7, 'G#': 8, 'Ab': 8, 'A': 9, 'A#': 10, 'Bb': 10, 'B': 11}
    i = 2 if len(name) > 1 and name[1] in '#b' else 1
    midi = 12 * (int(name[i:]) + 1) + names[name[:i]]
    return 440.0 * 2.0 ** ((midi - 69) / 12.0)


def new_mix():
    return np.zeros((2, N), dtype=np.float64)


def place(mix, sig, start, pan=0.0, gain=1.0):
    if sig is None or len(sig) == 0:
        return
    i = int(start * SR)
    if i < 0:
        sig = sig[-i:]
        i = 0
    j = min(i + len(sig), N)
    if j <= i:
        return
    sig = sig[:j - i]
    left = np.sqrt(max(0.0, 1.0 - max(0.0, pan)))
    right = np.sqrt(max(0.0, 1.0 + min(0.0, pan)))
    mix[0, i:j] += sig * gain * left
    mix[1, i:j] += sig * gain * right


def envelope(n, attack, release, sr=SR):
    e = np.ones(n)
    a = min(int(attack * sr), n)
    r = min(int(release * sr), n - a)
    if a > 0:
        e[:a] = np.linspace(0.0, 1.0, a) ** 1.4
    if r > 0:
        e[n - r:] = np.linspace(1.0, 0.0, r) ** 1.3
    return e


def additive(f0, dur, partials, attack=0.03, release=0.3, vib=0.0, vib_hz=5.6,
             detune_cents=0.0, decay_tau=None):
    n = int(dur * SR)
    t = np.arange(n) / SR
    out = np.zeros(n)
    detunes = [0.0] if not detune_cents else [-detune_cents, detune_cents]
    for cents in detunes:
        scale = 2.0 ** (cents / 1200.0)
        base = t - (vib / (2.0 * np.pi * vib_hz)) * np.sin(2.0 * np.pi * vib_hz * t)
        for k, a in partials:
            out += a * np.sin(2.0 * np.pi * k * f0 * scale * base)
    out /= len(detunes)
    if decay_tau:
        out *= np.exp(-t / decay_tau)
    return out * envelope(n, attack, release)


def timpani(f0, dur, amp=1.0):
    n = int(dur * SR)
    t = np.arange(n) / SR
    glide = f0 * (1.0 + 0.5 * np.exp(-t / 0.04))
    phase = 2.0 * np.pi * np.cumsum(glide) / SR
    body = np.sin(phase) * np.exp(-t / 0.42)
    click = _rng.standard_normal(n) * np.exp(-t / 0.015) * 0.65
    return (body + click) * amp


def hit_noise(dur, amp=0.6, tau=0.25):
    n = int(dur * SR)
    t = np.arange(n) / SR
    noise = _rng.standard_normal(n)
    for _ in range(6):
        noise = np.convolve(noise, np.ones(3) / 3.0, mode='same')
    return noise * np.exp(-t / tau) * amp


def reverb(sig, decay=2.0, mix=0.30):
    n = sig.shape[1]
    impulse_len = int(decay * SR)
    size = 1 << int(np.ceil(np.log2(n + impulse_len)))
    out = sig.copy()
    for ch in range(2):
        imp = _rng.standard_normal(impulse_len) * np.exp(-np.arange(impulse_len) / (0.30 * SR))
        wet = np.fft.irfft(np.fft.rfft(sig[ch], size) * np.fft.rfft(imp, size), size)[:n]
        out[ch] += mix * wet / (np.max(np.abs(wet)) + 1e-9) * np.max(np.abs(sig[ch]) + 1e-9)
    return out
# ---------------------------------------------------------------- 乐谱
CHORDS = {
    'Cm':  ['C4', 'Eb4', 'G4', 'C2', 'G2'],
    'Ab':  ['Ab3', 'C4', 'Eb4', 'Ab1', 'Eb2'],
    'Bb':  ['Bb3', 'D4', 'F4', 'Bb1', 'F2'],
    'Fm':  ['F3', 'Ab3', 'C4', 'F1', 'C2'],
    'G':   ['G3', 'B3', 'D4', 'G1', 'D2'],
}
PROGRESSION = ['Cm', 'Cm', 'Ab', 'Bb', 'Cm', 'Ab', 'Fm', 'G', 'Ab', 'Cm', 'Fm', 'Cm']

# 铜管号角主题：(小节, 拍, 时值拍, 音)。第 0 小节第一拍就是那一记全奏。
FANFARE = [
    (0, 0, 4, 'C4'),
    (1, 0, 1, 'Eb4'), (1, 1, 1, 'G4'), (1, 2, 2, 'C5'),
    (2, 0, 2, 'Bb4'), (2, 2, 2, 'Ab4'),
    (3, 0, 1, 'G4'), (3, 1, 1, 'F4'), (3, 2, 2, 'Eb4'),
    (4, 0, 2, 'C5'), (4, 2, 2, 'Eb5'),
    (5, 0, 1, 'C5'), (5, 1, 1, 'Bb4'), (5, 2, 2, 'Ab4'),
    (6, 0, 2, 'Ab4'), (6, 2, 2, 'G4'),
    (7, 0, 2, 'B4'), (7, 2, 2, 'D5'),
    (8, 0, 1, 'Eb5'), (8, 1, 1, 'D5'), (8, 2, 2, 'C5'),
    (9, 0, 4, 'G5'),
    (10, 0, 2, 'Ab5'), (10, 2, 2, 'G5'),
    (11, 0, 4, 'C5'),
]

BRASS = [(1, 1.0), (2, 0.85), (3, 0.7), (4, 0.55), (5, 0.42), (6, 0.3), (7, 0.22), (8, 0.16)]
CHOIR = [(1, 1.0), (2, 0.7), (3, 0.8), (4, 0.5), (5, 0.55), (6, 0.3), (7, 0.2)]
STRINGS = [(1, 1.0), (2, 0.55), (3, 0.34), (4, 0.2), (5, 0.12), (6, 0.07)]
BELL = [(1.0, 1.0), (2.76, 0.5), (5.4, 0.26), (8.93, 0.12)]


def render():
    mix = new_mix()

    # ── 第 0 秒：全奏重击，开场就是顶点 ──
    place(mix, hit_noise(2.5, amp=0.55, tau=0.35), 0.0)
    for note in ('C2', 'G2', 'C3'):
        place(mix, additive(freq(note), 2.6, BRASS, attack=0.004, release=1.4, detune_cents=8.0),
              0.0, gain=0.30 if note == 'C2' else 0.20)
    place(mix, timpani(freq('C1'), 3.0, 0.75), 0.0, gain=0.9)
    for note in ('C4', 'Eb4', 'G4', 'C5'):
        place(mix, additive(freq(note), 2.4, CHOIR, attack=0.05, release=1.2, detune_cents=14.0),
              0.0, gain=0.13)

    # ── 合唱垫：整首都在托底 ──
    for bar, name in enumerate(PROGRESSION):
        chord = CHORDS[name]
        for note, pan in ((chord[0], -0.3), (chord[1], 0.15), (chord[2], 0.35)):
            place(mix, additive(freq(note), BAR * 1.02, CHOIR, attack=0.22, release=0.5, detune_cents=12.0),
                  bar * BAR, pan=pan, gain=0.115)

    # ── 弦乐：低音 + 八分音符驱动 ──
    for bar, name in enumerate(PROGRESSION):
        chord = CHORDS[name]
        place(mix, additive(freq(chord[3]), BEAT * 2.0, STRINGS, attack=0.05, release=0.4, detune_cents=5.0),
              bar * BAR, gain=0.24)
        place(mix, additive(freq(chord[4]), BEAT * 2.0, STRINGS, attack=0.08, release=0.4, detune_cents=5.0),
              bar * BAR + BEAT * 2.0, gain=0.20)
        if bar >= 1:
            pattern = [chord[0], chord[2], chord[0], chord[1], chord[2], chord[0], chord[1], chord[2]]
            for i, note in enumerate(pattern):
                place(mix, additive(freq(note), BEAT * 0.9, STRINGS, attack=0.02, release=0.18, detune_cents=4.0),
                      bar * BAR + i * (BEAT / 2.0), pan=-0.4 if i % 2 == 0 else 0.4, gain=0.075)

    # ── 铜管号角 ──
    for (bar, beat, dur, note) in FANFARE:
        place(mix, additive(freq(note), dur * BEAT * 1.02, BRASS, attack=0.03, release=0.35, detune_cents=7.0),
              bar * BAR + beat * BEAT, gain=0.27)
    for bar in (9, 11):
        chord = CHORDS[PROGRESSION[bar]]
        for note in (chord[0], chord[1], chord[2]):
            place(mix, additive(freq(note) * 2.0, BEAT * 2.4, BRASS, attack=0.02, release=0.9, detune_cents=6.0),
                  bar * BAR, gain=0.16)

    # ── 定音鼓：每小节一、三拍，末段滚奏 ──
    for bar, name in enumerate(PROGRESSION):
        root = freq(CHORDS[name][3]) * 2.0
        place(mix, timpani(root, 1.0, 0.42), bar * BAR, gain=0.65)
        place(mix, timpani(root, 0.8, 0.28), bar * BAR + BEAT * 2.0, gain=0.45)
    roll_start = 10 * BAR + BEAT * 2
    for i in range(12):
        place(mix, timpani(freq('G1'), 0.4, 0.18 + 0.02 * i), roll_start + i * (BEAT * 2 / 12.0),
              gain=0.5 + 0.04 * i)
    place(mix, timpani(freq('C1'), 2.6, 0.7), 11 * BAR, gain=0.85)

    # ── 镲与钟 ──
    place(mix, hit_noise(3.0, amp=0.40, tau=0.9), 0.0, pan=0.2)
    place(mix, hit_noise(2.6, amp=0.34, tau=0.8), 9 * BAR, pan=-0.2)
    place(mix, additive(freq('C6'), 5.0, BELL, attack=0.004, release=2.4, decay_tau=1.3), 11 * BAR, pan=0.1, gain=0.30)

    return mix


def master(mix):
    # 这首从头到尾都在高位：只在最后收一下，不做渐强
    mix = np.tanh(mix * 1.05) / np.tanh(1.05)
    peak = np.max(np.abs(mix))
    if peak > 0:
        mix *= 0.95 / peak

    fade_in = int(0.03 * SR)
    fade_out = int(2.2 * SR)
    mix[:, :fade_in] *= np.linspace(0.0, 1.0, fade_in)
    mix[:, -fade_out:] *= np.linspace(1.0, 0.0, fade_out) ** 1.1
    return mix


def write_wav(path, mix):
    pcm = (np.clip(mix.T, -1.0, 1.0) * 32767.0).astype('<i2')
    with wave.open(path, 'wb') as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    out = master(reverb(render(), decay=2.0, mix=0.28))
    path = os.path.join(OUT_DIR, 'UsurperTheme.wav')
    write_wav(path, out)
    print(f'written {path}')
    print(f'duration {len(out[0]) / SR:.2f}s  peak {np.max(np.abs(out)):.3f}  '
          f'rms {np.sqrt(np.mean(out ** 2)):.4f}')
    for start in range(0, int(DUR), 5):
        seg = out[:, start * SR:(start + 5) * SR]
        if seg.size:
            print(f'  {start:>3}-{start + 5:>3}s  rms {np.sqrt(np.mean(seg ** 2)):.4f}')


if __name__ == '__main__':
    main()
