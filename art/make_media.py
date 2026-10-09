# builds the animated icon.png and the readme pics from art/screenshots
# needs pillow. run from anywhere: python art/make_media.py
import math
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw

ART = Path(__file__).resolve().parent
ROOT = ART.parent
SHOTS = ART / "screenshots"
MEDIA = ROOT / "media"

# the two half full shots are from a different spot, these line them up with the others
# (output pixel -> source pixel, pillow AFFINE order)
HALF_STAND = (0.937, 0.032, 110.728, -0.006, 0.938, 15.039)
HALF_BOX = (0.9516, 0.0499, 77.578, -0.0499, 0.9516, 64.274)


def load(name, fix=None):
    im = Image.open(SHOTS / f"{name}.png").convert("RGB")
    if fix:
        im = im.transform(im.size, Image.AFFINE, fix, Image.BICUBIC)
    return im


STATES = {
    "stand_empty": load("button_not_ready"),
    "stand_half": load("button_not_ready_half_full", HALF_STAND),
    "stand_ready": load("button_ready"),
    "box_empty": load("button_not_ready_without_mod_installed"),
    "box_half": load("button_not_ready_without_mod_installed_half_full", HALF_BOX),
    "box_ready": load("button_ready_without_mod_installed"),
}

# framings in screenshot pixels
# the icon stand goes a bit past the top of the shot, shot() fills that in
ICON = {"stand": (575, -69, 1925, 1281), "box": (887, 410, 1933, 1456)}
TOUR = {"stand": (406, 248, 2128, 1325), "box": (787, 584, 2029, 1360)}
CLOSE = {"stand": (840, 222, 1740, 897), "box": (943, 610, 1876, 1310)}


def ease(t):
    return t * t * (3 - 2 * t)


def lerp_box(a, b, t):
    return tuple(x + (y - x) * t for x, y in zip(a, b))


def zoom(box, z):
    cx, cy = (box[0] + box[2]) / 2, (box[1] + box[3]) / 2
    hw, hh = (box[2] - box[0]) / 2 / z, (box[3] - box[1]) / 2 / z
    return (cx - hw, cy - hh, cx + hw, cy + hh)


PAD = 200
_padded = {}


def source(name):
    # the shot with its top few rows mirrored above it, only ever seen under the title
    if name not in _padded:
        im = STATES[name]
        w, h = im.size
        out = Image.new("RGB", (w, h + PAD))
        out.paste(im, (0, PAD))
        out.paste(im.crop((0, 0, w, PAD)).transpose(Image.FLIP_TOP_BOTTOM), (0, 0))
        _padded[name] = out
    return _padded[name]


def shot(a, b, t, box, size):
    box = (box[0], box[1] + PAD, box[2], box[3] + PAD)
    fa = source(a).resize(size, Image.LANCZOS, box=box)
    if b is None or t <= 0:
        return fa
    return Image.blend(fa, source(b).resize(size, Image.LANCZOS, box=box), t)


def shade(size):
    # darkens the top and bottom a bit so the title and badge read on the bright ivy
    w, h = size
    mask = Image.new("L", size, 0)
    d = ImageDraw.Draw(mask)
    for y in range(h):
        top = max(0.0, 1 - y / (h * 0.32)) * 190
        bottom = max(0.0, (y - h * 0.78) / (h * 0.22)) * 120
        d.line([(0, y), (w, y)], fill=int(max(top, bottom)))
    return mask


def where(state):
    return state.split("_")[0]


# the border is the progress bar again: red stub when there's not enough, yellow while it
# fills, green with a light running up it once it's ready. colours picked off the screenshots
RED, YELLOW, GREEN = (220, 30, 25), (214, 167, 7), (55, 215, 20)
SPARK, UNLIT, METAL = (200, 255, 160), (38, 40, 46), (16, 17, 21)
LEVEL = {"empty": 0.0, "half": 0.5, "ready": 1.0}


BAND, INSET, THICK = 8, 2, 4


def border_cells(size=256, count=21):
    # segments around the edge, ranked by how far they are from the bottom middle so they
    # fill up both sides at once
    w = h = size
    lo, hi = INSET, INSET + THICK - 1
    cells = []
    for i in range(count):
        a = round(lo + i * (w - 2 * lo) / count)
        b = round(lo + (i + 1) * (w - 2 * lo) / count) - 3
        cells.append((a, lo, b, hi))
        cells.append((a, h - 1 - hi, b, h - 1 - lo))
    side = count - 2
    for i in range(side):
        a = round(BAND + i * (h - 2 * BAND) / side) + 1
        b = round(BAND + (i + 1) * (h - 2 * BAND) / side) - 2
        cells.append((lo, a, hi, b))
        cells.append((w - 1 - hi, a, w - 1 - lo, b))

    def along(c):
        x, y = (c[0] + c[2]) / 2, (c[1] + c[3]) / 2
        dx = abs(x - w / 2)
        if y > h - BAND:
            return dx
        if y < BAND:
            return w / 2 + h + (w / 2 - dx)
        return w / 2 + (h - y)

    far = w + h
    return [(c, along(c) / far) for c in cells]


def draw_border(im, cells, level, color, t, flash):
    d = ImageDraw.Draw(im)
    w, h = im.size
    for k in range(BAND):
        d.rectangle((k, k, w - 1 - k, h - 1 - k), outline=METAL)
    for box, rank in cells:
        lit = rank <= level and level > 0.02
        c = color if lit else UNLIT
        if level <= 0.02 and rank < 0.04:
            pulse = 0.6 + 0.4 * math.sin(t * math.tau / 1.4)
            c = mix(UNLIT, RED, pulse)
        if lit and color == GREEN:
            run = (t * 0.9) % 1.4
            c = mix(c, SPARK, max(0.0, 1 - abs(rank - run) / 0.12))
            c = mix(c, SPARK, flash)
        elif lit and level - rank < 0.04:
            c = mix(c, (255, 230, 120), 0.5 + 0.5 * math.sin(t * math.tau / 0.6))
        d.rectangle(box, fill=c)


def mix(a, b, t):
    t = min(1.0, max(0.0, t))
    return tuple(round(x + (y - x) * t) for x, y in zip(a, b))


def icon():
    # stills held, short fades and a pan between the stand and the box. starts on the lit
    # button so anything that only shows the first frame still looks right
    timeline = [("stand_ready", 2.0), ("box_empty", 1.1), ("box_half", 1.1),
                ("box_ready", 2.0), ("stand_empty", 1.1), ("stand_half", 1.1)]
    fps, fade, pan = 12, 0.35, 0.7
    size = (256, 256)
    # title down and badge up so they clear the border
    art = Image.open(ART / "overlay.png").convert("RGBA")
    overlay = Image.new("RGBA", size)
    overlay.alpha_composite(art.crop((0, 0, 256, 130)), (0, 6))
    overlay.alpha_composite(art.crop((0, 130, 256, 256)), (0, 124))
    mask = shade(size)
    black = Image.new("RGB", size, (0, 0, 0))
    cells = border_cells()

    def finish(im):
        im = Image.composite(black, im, mask).convert("RGBA")
        im.alpha_composite(overlay)
        return im.convert("RGB")

    frames = []
    clock = 0.0
    flash = 0.0
    for i, (state, hold) in enumerate(timeline):
        nxt = timeline[(i + 1) % len(timeline)][0]
        a, b = ICON[where(state)], ICON[where(nxt)]
        level, next_level = LEVEL[state.split("_")[1]], LEVEL[nxt.split("_")[1]]
        color = GREEN if level >= 1 else YELLOW
        still = finish(shot(state, None, 0, a, size))
        for f in range(round(hold * fps)):
            im = still.copy()
            draw_border(im, cells, level, color, clock, flash)
            flash *= 0.55
            frames.append(im)
            clock += 1 / fps
        steps = round((pan if a != b else fade) * fps)
        for s in range(1, steps):
            t = ease(s / steps)
            im = finish(shot(state, nxt, t, lerp_box(a, b, t), size))
            draw_border(im, cells, level + (next_level - level) * t, color, clock, 0.0)
            frames.append(im)
            clock += 1 / fps
        if next_level >= 1:
            flash = 1.0

    # only what changed goes in each frame, the rest is see-through so the border-only
    # frames cost next to nothing
    out = [frames[0].convert("RGBA")]
    for prev, cur in zip(frames, frames[1:]):
        r, g, b = ImageChops.difference(prev, cur).split()
        same = ImageChops.lighter(ImageChops.lighter(r, g), b).point(lambda v: 255 if v == 0 else 0)
        delta = cur.convert("RGBA")
        delta.paste((0, 0, 0, 0), mask=same)
        out.append(delta)
    out[0].save(ROOT / "icon.png", save_all=True, append_images=out[1:],
                duration=round(1000 / fps), loop=0, disposal=0, blend=1, optimize=True)
    frames[0].save(ART / "icon-still.png", optimize=True)
    return frames


def tour():
    # slow push in on the stand while it fills, swing over to the box, same again
    fps, size = 20, (600, 375)
    fade, pan, push = 0.5, 0.9, 1.16
    run = [("empty", 1.3), ("half", 1.3), ("ready", 2.2)]
    seg = sum(h for _, h in run) + fade * (len(run) - 1)

    def at(where_, t):
        # state blend and crop for time t inside one segment
        z = 1 + (push - 1) * (t / seg)
        box = zoom(TOUR[where_], z)
        clock = 0.0
        for i, (name, hold) in enumerate(run):
            if t < clock + hold or i == len(run) - 1:
                return f"{where_}_{name}", None, 0.0, box
            clock += hold
            if t < clock + fade:
                return f"{where_}_{name}", f"{where_}_{run[i + 1][0]}", ease((t - clock) / fade), box
            clock += fade

    frames = []
    for side, other in (("stand", "box"), ("box", "stand")):
        for f in range(round(seg * fps)):
            a, b, t, box = at(side, f / fps)
            frames.append(shot(a, b, t, box, size))
        end = zoom(TOUR[side], push)
        for f in range(1, round(pan * fps)):
            t = ease(f / (pan * fps))
            frames.append(shot(f"{side}_ready", f"{other}_empty", t, lerp_box(end, TOUR[other], t), size))

    frames[0].save(MEDIA / "tour.webp", save_all=True, append_images=frames[1:],
                   duration=int(1000 / fps), loop=0, quality=70, method=6)


def stills():
    # one strip per row so it scales with the page: not enough, getting there, ready
    w, h, gap = 480, 360, 8
    for side, name in (("stand", "with_mod"), ("box", "without_mod")):
        strip = Image.new("RGB", (w * 3 + gap * 2, h), METAL)
        for i, level in enumerate(("empty", "half", "ready")):
            strip.paste(shot(f"{side}_{level}", None, 0, CLOSE[side], (w, h)), (i * (w + gap), 0))
        strip.save(MEDIA / f"{name}.jpg", quality=90, optimize=True)


if __name__ == "__main__":
    MEDIA.mkdir(exist_ok=True)
    icon()
    tour()
    stills()
