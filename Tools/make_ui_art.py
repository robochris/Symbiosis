"""Draws Shrunk's UI art into Assets/Resources/UI. Run: python Tools/make_ui_art.py

Everything is white (or white with alpha) so Unity tints it; the logo is the one piece drawn in colour.
Shapes are drawn 4x larger and scaled down, so their edges are smooth.
"""
import math
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Resources", "UI")
os.makedirs(ROOT, exist_ok=True)
SS = 4  # supersampling


def save(img, name):
    img.save(os.path.join(ROOT, name + ".png"))
    print("wrote", name)


def canvas(w, h):
    return Image.new("L", (w * SS, h * SS), 0)


def finish(mask, w, h, blur=0):
    mask = mask.resize((w, h), Image.LANCZOS)
    if blur:
        mask = mask.filter(ImageFilter.GaussianBlur(blur))
    out = Image.new("RGBA", (w, h), (255, 255, 255, 0))
    out.putalpha(mask)
    return out


def rounded(w, h, r, inset=0):
    m = canvas(w, h)
    d = ImageDraw.Draw(m)
    d.rounded_rectangle([inset * SS, inset * SS, (w - inset) * SS - 1, (h - inset) * SS - 1], r * SS, fill=255)
    return m


# ---- panels: a rounded fill, its edge, and a soft shadow (9-sliced in Unity) ----
save(finish(rounded(96, 96, 28), 96, 96), "panel")
edge = rounded(96, 96, 28)
inner = rounded(96, 96, 25, inset=3)
edge.paste(0, mask=inner)
save(finish(edge, 96, 96), "panel_edge")
# Small radius, for pills and bars.
save(finish(rounded(64, 64, 32), 64, 64), "pill")

shadow = Image.new("L", (160, 160), 0)
ImageDraw.Draw(shadow).rounded_rectangle([40, 40, 119, 119], 26, fill=200)
shadow = shadow.filter(ImageFilter.GaussianBlur(14))
s = Image.new("RGBA", (160, 160), (0, 0, 0, 0))
s.putalpha(shadow)
save(s, "shadow")

# ---- circles and rings ----
def circle(size, inset=0):
    m = canvas(size, size)
    ImageDraw.Draw(m).ellipse([inset * SS, inset * SS, (size - inset) * SS - 1, (size - inset) * SS - 1], fill=255)
    return m


save(finish(circle(256), 256, 256), "circle")
for name, thick in (("ring", 10), ("ring_thick", 22)):
    m = circle(256, 2)
    m.paste(0, mask=circle(256, 2 + thick))
    save(finish(m, 256, 256), name)

# A soft round glow, for behind buttons and knobs.
g = Image.new("L", (256, 256), 0)
ImageDraw.Draw(g).ellipse([48, 48, 207, 207], fill=255)
g = g.filter(ImageFilter.GaussianBlur(28))
glow = Image.new("RGBA", (256, 256), (255, 255, 255, 0))
glow.putalpha(g)
save(glow, "glow")

# ---- banner: solid in the middle, fading out to the left and right ----
w, h = 512, 8
b = Image.new("L", (w, h), 0)
for x in range(w):
    t = x / (w - 1)
    edge_fade = min(t, 1 - t) / 0.22
    b.paste(int(255 * max(0.0, min(1.0, edge_fade)) ** 1.4), [x, 0, x + 1, h])
ban = Image.new("RGBA", (w, h), (255, 255, 255, 0))
ban.putalpha(b)
save(ban, "banner")

# A vignette for full-screen cards: clear in the middle, dark at the edges.
w, h = 256, 144
v = Image.new("L", (w, h), 0)
px = v.load()
for y in range(h):
    for x in range(w):
        dx, dy = (x / (w - 1) - 0.5) * 2, (y / (h - 1) - 0.5) * 2
        r = math.sqrt(dx * dx * 0.8 + dy * dy)
        px[x, y] = int(255 * max(0.0, min(1.0, (r - 0.45) / 0.9)) ** 1.5)
vig = Image.new("RGBA", (w, h), (0, 0, 0, 0))
vig.putalpha(v)
save(vig, "vignette")

# ---- icons, 128 px, white ----
def icon(draw_fn, size=128):
    m = canvas(size, size)
    draw_fn(ImageDraw.Draw(m), size * SS)
    return finish(m, size, size)


def heart_points(S, scale=1.0):
    pts = []
    for k in range(200):
        t = 2 * math.pi * k / 200
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((S * 0.5 + x * S * 0.027 * scale, S * 0.47 - y * S * 0.027 * scale))
    return pts


def heart(d, S):
    d.polygon(heart_points(S), fill=255)


save(icon(heart), "heart")
hm = canvas(128, 128)
ImageDraw.Draw(hm).polygon(heart_points(128 * SS), fill=255)
ImageDraw.Draw(hm).polygon(heart_points(128 * SS, 0.72), fill=0)
save(finish(hm, 128, 128), "heart_empty")


def coin(d, S):
    d.ellipse([S * 0.1, S * 0.1, S * 0.9, S * 0.9], fill=255)
    d.ellipse([S * 0.24, S * 0.24, S * 0.76, S * 0.76], fill=0)
    d.ellipse([S * 0.32, S * 0.32, S * 0.68, S * 0.68], fill=255)


save(icon(coin), "coin")


def sample(d, S):
    # A drop.
    cx, cy, r = S * 0.5, S * 0.62, S * 0.27
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=255)
    d.polygon([(cx - r * 0.93, cy - r * 0.35), (cx + r * 0.93, cy - r * 0.35), (cx, S * 0.08)], fill=255)
    d.ellipse([cx - r * 0.55, cy - r * 0.2, cx - r * 0.15, cy + r * 0.25], fill=0)


save(icon(sample), "sample")


def pause(d, S):
    d.rounded_rectangle([S * 0.28, S * 0.22, S * 0.43, S * 0.78], S * 0.04, fill=255)
    d.rounded_rectangle([S * 0.57, S * 0.22, S * 0.72, S * 0.78], S * 0.04, fill=255)


save(icon(pause), "icon_pause")


def camera(d, S):
    d.rounded_rectangle([S * 0.12, S * 0.3, S * 0.88, S * 0.78], S * 0.08, fill=255)
    d.rounded_rectangle([S * 0.36, S * 0.2, S * 0.64, S * 0.36], S * 0.04, fill=255)
    d.ellipse([S * 0.33, S * 0.37, S * 0.67, S * 0.71], fill=0)
    d.ellipse([S * 0.41, S * 0.45, S * 0.59, S * 0.63], fill=255)


save(icon(camera), "icon_camera")


def bolt(d, S):
    d.polygon([(S * 0.58, S * 0.06), (S * 0.22, S * 0.56), (S * 0.47, S * 0.56), (S * 0.4, S * 0.94), (S * 0.78, S * 0.42), (S * 0.53, S * 0.42)], fill=255)


save(icon(bolt), "icon_power")


def dodge(d, S):
    # Two chevrons, a dash forward.
    for x0 in (0.18, 0.46):
        d.polygon([(S * x0, S * 0.22), (S * (x0 + 0.2), S * 0.22), (S * (x0 + 0.38), S * 0.5), (S * (x0 + 0.2), S * 0.78), (S * x0, S * 0.78), (S * (x0 + 0.18), S * 0.5)], fill=255)


save(icon(dodge), "icon_dodge")


def hand(d, S):
    # A pointing tap: a dot with two rings, "press here".
    d.ellipse([S * 0.38, S * 0.38, S * 0.62, S * 0.62], fill=255)
    for r, w in ((0.27, 0.05), (0.4, 0.04)):
        d.ellipse([S * (0.5 - r), S * (0.5 - r), S * (0.5 + r), S * (0.5 + r)], outline=255, width=int(S * w))


save(icon(hand), "icon_use")


def skull(d, S):
    d.ellipse([S * 0.18, S * 0.12, S * 0.82, S * 0.7], fill=255)
    d.rounded_rectangle([S * 0.32, S * 0.5, S * 0.68, S * 0.86], S * 0.06, fill=255)
    d.ellipse([S * 0.29, S * 0.34, S * 0.45, S * 0.52], fill=0)
    d.ellipse([S * 0.55, S * 0.34, S * 0.71, S * 0.52], fill=0)
    for x in (0.41, 0.5, 0.59):
        d.rectangle([S * (x - 0.015), S * 0.72, S * (x + 0.015), S * 0.86], fill=0)


save(icon(skull), "icon_kills")


def flag(d, S):
    d.rectangle([S * 0.22, S * 0.1, S * 0.3, S * 0.92], fill=255)
    d.polygon([(S * 0.3, S * 0.12), (S * 0.84, S * 0.28), (S * 0.3, S * 0.46)], fill=255)


save(icon(flag), "icon_floor")


# ---- the logo: SYMBIOSIS, letters bobbing a little, cyan running into leaf green ----
def logo():
    font_path = os.path.join(os.environ.get("WINDIR", r"C:\Windows"), "Fonts", "ariblk.ttf")
    word = "SYMBIOSIS"
    sizes = [250] * len(word)
    bob = [0, -10, 4, -6, 8, -4, 6, -10, 0]
    W, H = 2000, 440
    fill = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(fill)
    x = 40
    base = 340
    spots = []
    for ch, size, dy in zip(word, sizes, bob):
        f = ImageFont.truetype(font_path, size)
        l, t, r, b = d.textbbox((0, 0), ch, font=f, anchor="ls")
        d.text((x, base + dy), ch, font=f, fill=255, anchor="ls")
        spots.append((x, r - l))
        x += (r - l) + 10
    right = x
    # Trim the canvas to the word.
    W2 = right + 40
    fill = fill.crop((0, 0, W2, H))
    # A thick dark outline, a drop shadow, and a top-to-bottom colour.
    outline = fill.filter(ImageFilter.MaxFilter(23))
    shadow = outline.filter(ImageFilter.GaussianBlur(10))
    out = Image.new("RGBA", fill.size, (0, 0, 0, 0))
    sh = Image.new("RGBA", fill.size, (0, 0, 0, 0))
    sh.putalpha(shadow.point(lambda v: int(v * 0.55)))
    out.alpha_composite(sh, (0, 14))
    ol = Image.new("RGBA", fill.size, (14, 22, 38, 255))
    ol.putalpha(outline)
    out.alpha_composite(ol)
    grad = Image.new("RGBA", fill.size)
    gp = grad.load()
    # Cyan on the left running into leaf green on the right; lighter at the top of each letter.
    left, right = (90, 214, 245), (120, 220, 110)
    for yy in range(fill.size[1]):
        lift = 1.0 - max(0.0, min(1.0, (yy - 60) / 280))
        for xx in range(fill.size[0]):
            u = xx / max(1, fill.size[0] - 1)
            c = [left[i] + (right[i] - left[i]) * u for i in range(3)]
            c = tuple(int(c[i] + (255 - c[i]) * 0.55 * lift) for i in range(3))
            gp[xx, yy] = c + (255,)
    grad.putalpha(fill)
    out.alpha_composite(grad)
    # A shine along the upper half of the letters.
    shine = fill.copy()
    sp = shine.load()
    for yy in range(fill.size[1]):
        k = 1.0 if yy < 190 else 0.0
        for xx in range(fill.size[0]):
            sp[xx, yy] = int(sp[xx, yy] * 0.28 * k)
    shine_img = Image.new("RGBA", fill.size, (255, 255, 255, 0))
    shine_img.putalpha(shine)
    out.alpha_composite(shine_img)
    bbox = out.getbbox()
    out = out.crop((max(0, bbox[0] - 8), max(0, bbox[1] - 8), min(out.width, bbox[2] + 8), min(out.height, bbox[3] + 8)))
    save(out, "logo")


logo()


# ---- a vertical fade: clear at the top, solid at the bottom, for under the title ----
fu = Image.new("L", (8, 256), 0)
for yy in range(256):
    t = yy / 255
    fu.paste(int(255 * (t ** 1.6)), [0, yy, 8, yy + 1])
fade = Image.new("RGBA", (8, 256), (255, 255, 255, 0))
fade.putalpha(fu)
save(fade, "fade_up")
