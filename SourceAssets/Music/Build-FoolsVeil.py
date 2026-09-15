# -*- coding: utf-8 -*-
"""
《愚者的帷幕》——为愚者途径晋升序列一（诡秘侍者）写的原创纯音乐。

不是采样拼贴，也不是现成曲子：这里只有加法合成、噪声、以及一段用 FFT 卷积做的混响，
所有声部都写在下面的乐谱数据里（D 小调 / 68 BPM / 26 小节 / 约 92 秒）。

结构：
    1-4   序幕    低音持续音 + 风 + 稀疏的钟，帷幕拉开前的那种静
    5-12  主题    弦乐垫 + 大提琴低音 + 独奏主题（半音经过音带出神秘感）
    13-20 上升    竖琴/钢片琴琶音、主题抬高八度、定音鼓进入
    21-24 高潮    合唱、铜管、八度齐奏，第 24 小节定音鼓滚奏推向属和弦
    25-26 落幕    只留持续音与一口钟，落在未解决的 A 上——故事没讲完

重建：python SourceAssets/Music/Build-FoolsVeil.py
输出：SourceAssets/Music/build/FoolsVeil.wav（再用 Convert-ToMp3.ps1 转成 Assets/Music/）
"""
import os
import wave
import numpy as np

SR = 44100
BPM = 68.0
BEAT = 60.0 / BPM
BAR = 4.0 * BEAT
BARS = 26
TAIL = 4.0
DUR = BARS * BAR + TAIL
N = int(DUR * SR)

OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'build')

_rng = np.random.default_rng(20260914)


# ---------------------------------------------------------------- 基础工具
def freq(name):
    """把音名换成频率：D4 / Bb3 / C#5。"""
    names = {'C': 0, 'C#': 1, 'Db': 1, 'D': 2, 'D#': 3, 'Eb': 3, 'E': 4, 'F': 5,
             'F#': 6, 'Gb': 6, 'G': 7, 'G#': 8, 'Ab': 8, 'A': 9, 'A#': 10, 'Bb': 10, 'B': 11}
    i = 2 if len(name) > 1 and name[1] in '#b' else 1
    midi = 12 * (int(name[i:]) + 1) + names[name[:i]]
    return 440.0 * 2.0 ** ((midi - 69) / 12.0)


def new_mix():
    return np.zeros((2, N), dtype=np.float64)


def place(mix, sig, start, pan=0.0, gain=1.0):
    """把一段单声道信号按 pan 放进立体声总线。"""
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
    """淡入淡出包络，单位是秒。"""
    e = np.ones(n)
    a = min(int(attack * sr), n)
    r = min(int(release * sr), n - a)
    if a > 0:
        e[:a] = np.linspace(0.0, 1.0, a) ** 1.6
    if r > 0:
        e[n - r:] = np.linspace(1.0, 0.0, r) ** 1.4
    return e


def additive(f0, dur, partials, attack=0.05, release=0.4, vib=0.0, vib_hz=5.4,
             detune_cents=0.0, decay_tau=None):
    """
    加法合成：partials 是 [(倍数, 振幅), ...]，倍数允许不是整数（钟就是靠这个出金属味）。
    相位是按解析积分算的，所以颤音不会跑调。
    """
    n = int(dur * SR)
    t = np.arange(n) / SR
    out = np.zeros(n)

    detunes = [0.0]
    if detune_cents:
        detunes = [-detune_cents, detune_cents]

    for cents in detunes:
        scale = 2.0 ** (cents / 1200.0)
        # 相位积分：频率带正弦颤音时的解析解
        base = t - (vib / (2.0 * np.pi * vib_hz)) * np.sin(2.0 * np.pi * vib_hz * t)
        for k, a in partials:
            out += a * np.sin(2.0 * np.pi * k * f0 * scale * base)

    out /= max(1, len(detunes))
    if decay_tau:
        out *= np.exp(-t / decay_tau)
    out *= envelope(n, attack, release)
    return out


def timpani(f0, dur, amp=1.0):
    """定音鼓：音高先掉一下，再挂一层噪声瞬态。"""
    n = int(dur * SR)
    t = np.arange(n) / SR
    glide = f0 * (1.0 + 0.45 * np.exp(-t / 0.045))
    phase = 2.0 * np.pi * np.cumsum(glide) / SR
    body = np.sin(phase) * np.exp(-t / 0.55)
    click = _rng.standard_normal(n) * np.exp(-t / 0.02) * 0.5
    return (body + click) * amp


def noise_swell(dur, amp=0.5, cutoff_hz=900.0):
    """风 / 铜锣：低速率噪声插值上来，省掉滤波器也够用。"""
    n = int(dur * SR)
    coarse = int(dur * cutoff_hz / 4.0) + 4
    low = _rng.standard_normal(coarse)
    sig = np.interp(np.linspace(0, coarse - 1, n), np.arange(coarse), low)
    t = np.arange(n) / SR
    return sig * np.exp(-t / (dur * 0.5)) * amp


def reverb(sig, decay=2.4, mix=0.32, pre=0.02):
    """用衰减噪声做脉冲响应的 FFT 卷积混响；左右用不同噪声，得到宽度。"""
    n = sig.shape[1]
    impulse_len = int(decay * SR)
    size = 1 << int(np.ceil(np.log2(n + impulse_len)))
    out = sig.copy()
    for ch in range(2):
        imp = _rng.standard_normal(impulse_len) * np.exp(-np.arange(impulse_len) / (0.42 * SR))
        imp[:int(pre * SR)] *= 0.2
        wet = np.fft.irfft(np.fft.rfft(sig[ch], size) * np.fft.rfft(imp, size), size)[:n]
        out[ch] += mix * wet / (np.max(np.abs(wet)) + 1e-9) * np.max(np.abs(sig[ch]) + 1e-9)
    return out


# ---------------------------------------------------------------- 乐谱数据
# 每小节一个和弦：[根音, 三音, 五音, 低音, 五度低音]
CHORDS = {
    'Dm':  ['D4', 'F4', 'A4', 'D2', 'A2'],
    'Bb':  ['Bb3', 'D4', 'F4', 'Bb1', 'F2'],
    'F':   ['F4', 'A4', 'C5', 'F2', 'C3'],
    'C':   ['C4', 'E4', 'G4', 'C2', 'G2'],
    'Gm':  ['G3', 'Bb3', 'D4', 'G1', 'D2'],
    'A':   ['A4', 'C#5', 'E5', 'A1', 'E2'],
}

# 26 小节的走向
PROGRESSION = ['Dm', 'Dm', 'Bb', 'Bb',
               'Dm', 'Dm', 'Bb', 'F', 'Gm', 'A', 'Dm', 'Bb',
               'F', 'C', 'Gm', 'A', 'Bb', 'F', 'Gm', 'A',
               'Dm', 'Bb', 'F', 'A',
               'Dm', 'Dm']

# 主题 A（第 5-12 小节）：(小节序号从 5 起, 拍偏移, 时值拍, 音名)
THEME_A = [
    (0, 0, 1, 'D5'), (0, 1, 1, 'F5'), (0, 2, 2, 'E5'),
    (1, 0, 2, 'D5'), (1, 2, 2, 'A4'),
    (2, 0, 1, 'Bb4'), (2, 1, 1, 'D5'), (2, 2, 2, 'C5'),
    (3, 0, 4, 'A4'),
    (4, 0, 1, 'G4'), (4, 1, 1, 'Bb4'), (4, 2, 2, 'A4'),
    (5, 0, 2, 'C#5'), (5, 2, 2, 'D5'),
    (6, 0, 1, 'F5'), (6, 1, 1, 'E5'), (6, 2, 2, 'D5'),
    (7, 0, 2, 'Bb4'), (7, 2, 2, 'A4'),
]

# 主题 B（第 13-20 小节）：抬高、更急
THEME_B = [
    (0, 0, 1.5, 'D5'), (0, 1.5, 0.5, 'F5'), (0, 2, 2, 'G5'),
    (1, 0, 2, 'A5'), (1, 2, 2, 'G5'),
    (2, 0, 1, 'F5'), (2, 1, 1, 'E5'), (2, 2, 2, 'D5'),
    (3, 0, 3, 'C#5'), (3, 3, 1, 'E5'),
    (4, 0, 1.5, 'F5'), (4, 1.5, 0.5, 'D5'), (4, 2, 2, 'Bb4'),
    (5, 0, 2, 'C5'), (5, 2, 2, 'A4'),
    (6, 0, 1, 'D5'), (6, 1, 1, 'F5'), (6, 2, 2, 'A5'),
    (7, 0, 2, 'G5'), (7, 2, 1, 'F5'), (7, 3, 1, 'E5'),
]

# 高潮（第 21-24 小节）
THEME_C = [
    (0, 0, 2, 'A5'), (0, 2, 2, 'D6'),
    (1, 0, 2, 'C6'), (1, 2, 2, 'Bb5'),
    (2, 0, 2, 'A5'), (2, 2, 2, 'F5'),
    (3, 0, 2, 'E5'), (3, 2, 2, 'C#5'),
]


def section_scale(bar):
    """给各声部一个进入时间表。"""
    return {
        'pad': bar >= 4,
        'bass': bar >= 4,
        'melody': 4 <= bar <= 23,
        'harp': bar >= 12,
        'timpani': bar >= 4,
        'choir': bar >= 20,
         'brass': bar >= 18,
     }
# ---------------------------------------------------------------- 音色配方
STRINGS = [(1, 1.0), (2, 0.5), (3, 0.3), (4, 0.18), (5, 0.1), (6, 0.06)]
VIOLIN = [(1, 1.0), (2, 0.42), (3, 0.3), (4, 0.2), (5, 0.14), (6, 0.09), (7, 0.05)]
CHOIR = [(1, 1.0), (2, 0.6), (3, 0.7), (4, 0.45), (5, 0.5), (6, 0.25), (7, 0.15)]
BRASS = [(1, 1.0), (2, 0.7), (3, 0.55), (4, 0.4), (5, 0.25), (6, 0.18), (7, 0.1), (8, 0.07)]
BELL = [(1.0, 1.0), (2.76, 0.5), (5.4, 0.26), (8.93, 0.12)]
HARP = [(1, 1.0), (2, 0.35), (3, 0.2), (4, 0.1), (5, 0.05)]


def render():
    mix = new_mix()

    # ── 风：帷幕内外的呼吸 ──
    place(mix, noise_swell(13.0, amp=0.26, cutoff_hz=700.0), 0.0, pan=-0.45)
    place(mix, noise_swell(15.0, amp=0.20, cutoff_hz=520.0), 7.5, pan=0.45)
    place(mix, noise_swell(12.0, amp=0.22, cutoff_hz=640.0), BARS * BAR - 7.0, pan=0.05)

    # ── 弦乐垫：整首曲子的地板 ──
    for bar, name in enumerate(PROGRESSION):
        if bar < 4:
            continue
        chord = CHORDS[name]
        thick = bar >= 20
        voices = [(chord[0], -0.22), (chord[1], 0.18), (chord[2], 0.0)]
        if thick:
            voices.append((chord[2], -0.5))
        for note, pan in voices:
            gain = 0.19 if thick else 0.125
            place(mix, additive(freq(note), BAR * 1.06, STRINGS, attack=0.85, release=1.15, detune_cents=8.0),
                  bar * BAR, pan=pan, gain=gain)

    # ── 低音：根音两拍、五度两拍 ──
    for bar, name in enumerate(PROGRESSION):
        if bar < 4:
            continue
        root, fifth = CHORDS[name][3], CHORDS[name][4]
        place(mix, additive(freq(root), BEAT * 2.0, STRINGS, attack=0.25, release=0.7, detune_cents=4.0),
              bar * BAR, gain=0.19)
        place(mix, additive(freq(fifth), BEAT * 2.0, STRINGS, attack=0.35, release=0.8, detune_cents=4.0),
              bar * BAR + BEAT * 2.0, gain=0.15)

    # ── 主题 ──
    def play_theme(data, start_bar, gain, octave_shift=0):
        for (b, beat, dur, note) in data:
            f = freq(note) * (2.0 ** octave_shift)
            place(mix, additive(f, dur * BEAT * 1.02, VIOLIN, attack=0.09,
                                release=min(0.5, dur * BEAT * 0.5), vib=0.005, detune_cents=3.0),
                  (start_bar + b) * BAR + beat * BEAT, gain=gain)

    play_theme(THEME_A, 4, 0.30)
    play_theme(THEME_B, 12, 0.32)
    play_theme(THEME_C, 20, 0.34)
    play_theme(THEME_C, 20, 0.16, octave_shift=1)      # 高潮八度齐奏

    # 落幕：主题最开头那两个音轻轻回来
    place(mix, additive(freq('D5'), BEAT * 3.0, VIOLIN, attack=0.35, release=1.6, vib=0.005),
          24 * BAR, gain=0.22)
    place(mix, additive(freq('A4'), BEAT * 3.0, VIOLIN, attack=0.35, release=1.6, vib=0.005),
          24 * BAR + BEAT * 3.0, gain=0.18)

    # ── 竖琴 / 钢片琴：和弦琶音 ──
    for bar in range(12, 24):
        chord = CHORDS[PROGRESSION[bar]]
        pattern = [chord[0], chord[2], chord[0], chord[1], chord[2], chord[0], chord[1], chord[2]]
        for i, note in enumerate(pattern):
            t0 = bar * BAR + i * (BEAT / 2.0)
            pan = -0.35 if i % 2 == 0 else 0.35
            place(mix, additive(freq(note) * 2.0, BEAT * 1.1, HARP, attack=0.005,
                                release=BEAT * 0.5, detune_cents=4.0),
                  t0, pan=pan, gain=0.095 if bar >= 20 else 0.075)

    # ── 合唱：只在最后把天顶撑起来 ──
    for bar in range(20, 24):
        chord = CHORDS[PROGRESSION[bar]]
        for note, pan in ((chord[0], -0.3), (chord[1], 0.1), (chord[2], 0.35)):
            place(mix, additive(freq(note) * 2.0, BAR * 1.05, CHOIR, attack=1.2, release=1.4, detune_cents=12.0),
                  bar * BAR, pan=pan, gain=0.10)

    # ── 铜管：从底下顶到高潮 ──
    brass_line = [(18, 0, 2, 'G3'), (18, 2, 2, 'A3'), (19, 0, 2, 'Bb3'), (19, 2, 2, 'C4'),
                  (20, 0, 4, 'D4'), (21, 0, 4, 'Bb3'), (22, 0, 4, 'C4'), (23, 0, 4, 'C#4')]
    for (bar, beat, dur, note) in brass_line:
        place(mix, additive(freq(note), dur * BEAT * 1.02, BRASS, attack=0.3, release=0.8, detune_cents=6.0),
              bar * BAR + beat * BEAT, gain=0.16)
    for bar in (20, 24):
        chord = CHORDS[PROGRESSION[bar]]
        for note in (chord[0], chord[1], chord[2]):
            place(mix, additive(freq(note), BEAT * 2.2, BRASS, attack=0.06, release=1.2, detune_cents=5.0),
                  bar * BAR, gain=0.13)

    # ── 钟：序幕与落幕 ──
    bells = [(0.0, 'D5', 0.30), (BAR * 1.5, 'A5', 0.24), (BAR * 2.5, 'F5', 0.22),
             (BAR * 3.5, 'D6', 0.20), (24 * BAR, 'D5', 0.26), (25 * BAR + BEAT * 2, 'A5', 0.20)]
    for t0, note, gain in bells:
        place(mix, additive(freq(note), 6.5, BELL, attack=0.004, release=3.0, decay_tau=1.9),
              t0, pan=0.15, gain=gain)

    # ── 定音鼓 ──
    for bar in range(4, 24):
        root = freq(CHORDS[PROGRESSION[bar]][3]) * 2.0
        if bar < 12:
            place(mix, timpani(root, 1.4, 0.28), bar * BAR, gain=0.5)
        else:
            place(mix, timpani(root, 1.6, 0.34), bar * BAR, gain=0.6)
            place(mix, timpani(root, 1.2, 0.22), bar * BAR + BEAT * 2.5, gain=0.45)

    # 第 24 小节：滚奏推向属和弦，然后落地
    roll_start = 23 * BAR + BEAT * 2
    for i in range(16):
        place(mix, timpani(freq('A1'), 0.5, 0.16 + 0.02 * i),
              roll_start + i * (BEAT * 2 / 16.0), gain=0.45 + 0.03 * i)
    place(mix, timpani(freq('D2'), 3.0, 0.5), 24 * BAR, gain=0.8)
    place(mix, noise_swell(3.2, amp=0.5, cutoff_hz=2600.0), 24 * BAR, gain=0.35)

    return mix


def master(mix):
    """整体动态 + 轻微软削波 + 归一化 + 首尾淡入淡出。"""
    # 渐强曲线：序幕压着，高潮放开，落幕收回来
    t = np.linspace(0.0, 1.0, mix.shape[1])
    curve = np.interp(t, [0.0, 0.16, 0.45, 0.70, 0.84, 1.0], [0.52, 0.70, 0.88, 1.00, 0.68, 0.46])
    mix *= curve

    mix = np.tanh(mix * 0.95) / np.tanh(0.95)
    peak = np.max(np.abs(mix))
    if peak > 0:
        mix *= 0.90 / peak

    fade_in = int(1.2 * SR)
    fade_out = int(4.5 * SR)
    mix[:, :fade_in] *= np.linspace(0.0, 1.0, fade_in) ** 1.4
    mix[:, -fade_out:] *= np.linspace(1.0, 0.0, fade_out) ** 1.2
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
    dry = render()
    wet = reverb(dry, decay=2.6, mix=0.34)
    out = master(wet)
    path = os.path.join(OUT_DIR, 'FoolsVeil.wav')
    write_wav(path, out)
    print(f'written {path}')
    print(f'duration {len(out[0]) / SR:.2f}s  peak {np.max(np.abs(out)):.3f}  '
          f'rms {np.sqrt(np.mean(out ** 2)):.4f}')
    for start in range(0, int(DUR), 10):
        seg = out[:, start * SR:(start + 10) * SR]
        if seg.size:
            print(f'  {start:>3}-{start + 10:>3}s  rms {np.sqrt(np.mean(seg ** 2)):.4f}')


if __name__ == '__main__':
    main()
