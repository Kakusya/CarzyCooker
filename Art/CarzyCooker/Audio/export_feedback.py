"""Original CarzyCooker short cues. Python standard library, editable synthesis.

Run from any directory; writes only the six named product WAV assets.
No recordings, sampled libraries, or third-party music are used.
"""
from array import array
import math
from pathlib import Path
import random
import wave

ROOT = Path(__file__).resolve().parents[3]
OUTPUT = ROOT / 'Unity/Assets/Res/Sound/Product'
RATE = 44100

# name, duration seconds, successive notes (Hz), tone/noise character
CUES = (
    ('Pickup', .11, (880, 1320), 'bell'),
    ('Place', .12, (190, 150), 'wood'),
    ('WorkStart', .18, (330, 440, 550), 'bell'),
    ('WorkFinished', .34, (784, 1047), 'bell'),
    ('Serve', .42, (659, 831, 988), 'bell'),
    ('LevelComplete', .72, (523, 659, 784, 1047), 'bell'),
)


def synthesize(duration, notes, character):
    rng = random.Random(173)
    values = []
    for index in range(round(RATE * duration)):
        time = index / RATE
        value = 0.0
        for note_index, frequency in enumerate(notes):
            onset = note_index * duration * .6 / len(notes)
            local = time - onset
            if local < 0:
                continue
            attack = min(1.0, local / .004)
            decay = math.exp(-local * (42 if character == 'wood' else 15))
            phase = math.tau * frequency * local
            value += attack * decay * (math.sin(phase) + .18 * math.sin(phase * 2.01))
            if character == 'wood':
                value += .2 * rng.uniform(-1, 1) * attack * math.exp(-local * 90)
        value *= min(1.0, (duration - time) / .025)
        values.append(value)
    mean = sum(values) / len(values)
    values = [value - mean for value in values]
    peak = max(abs(value) for value in values)
    return array('h', (round(value / peak * .52 * 32767) for value in values))


if __name__ == '__main__':
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for name, duration, notes, character in CUES:
        samples = synthesize(duration, notes, character)
        target = OUTPUT / (name + '.wav')
        with wave.open(str(target), 'wb') as stream:
            stream.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
            stream.writeframes(samples.tobytes())
        print(f'{name}: {len(samples)/RATE:.2f}s, mono 16-bit PCM, peak <= 0.52')
