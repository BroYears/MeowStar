#!/usr/bin/env python3
"""
Rough 2.5D composite of the arcade scene so we can eyeball layout/sizing without
launching Unity. NOT a Unity renderer — it just projects world (x,z) to screen with
a fake camera pitch and pastes the sprites. Output: tools/arcade_art/_preview.png
"""
import os
from PIL import Image

HERE = os.path.dirname(__file__)
ART = os.path.abspath(os.path.join(HERE, "..", "..", "Assets", "_Project", "Resources", "Arcade"))

W, H = 1100, 720
PPU_PX = 46          # screen pixels per world unit (x)
PITCH = 0.62         # z foreshortening (cos-ish)
CX, CY = W // 2, int(H * 0.60)

bg = Image.new("RGBA", (W, H), (176, 219, 242, 255))  # sky

def proj(x, z):
    return CX + x * PPU_PX, CY - z * PPU_PX * PITCH

def load(key):
    p = os.path.join(ART, key + ".png")
    return Image.open(p).convert("RGBA") if os.path.exists(p) else None

def paste_ground(key, cx, cz, w_units, h_units):
    img = load(key)
    if not img: return
    tw = int(w_units * PPU_PX)
    th = int(h_units * PPU_PX * PITCH)
    tile = img.resize((max(1, int(1.7*PPU_PX)), max(1, int(1.7*PPU_PX*PITCH))))
    patch = Image.new("RGBA", (tw, th), (0,0,0,0))
    for yy in range(0, th, tile.height):
        for xx in range(0, tw, tile.width):
            patch.alpha_composite(tile, (xx, yy))
    sx, sy = proj(cx, cz)
    bg.alpha_composite(patch, (int(sx - tw/2), int(sy - th/2)))

def paste_stand(key, x, z, h_units, items):
    img = load(key)
    if not img: return
    s = (h_units * PPU_PX) / img.height
    img2 = img.resize((max(1,int(img.width*s)), max(1,int(img.height*s))))
    sx, sy = proj(x, z)
    items.append((z, img2, int(sx - img2.width/2), int(sy - img2.height)))

# grounds (draw first)
paste_ground("tile_grass", 0, 0, 17, 10)
paste_ground("tile_wood", -0.4, 0, 5.2, 6.4)
paste_ground("tile_water", -6.6, -1.2, 2.4, 6.4)
paste_ground("tile_berryfield", -6.6, 3.6, 2.6, 2.6)

# standing things, painter-sorted by z (far first)
items = []
paste_stand("tree", -8.0, -4.4, 2.6, items)
paste_stand("tree", 7.6, -4.6, 2.4, items)
paste_stand("tree", 8.0, 4.6, 2.7, items)
paste_stand("bush", -8.2, 1.6, 1.0, items)
paste_stand("lantern", 6.2, -3.4, 1.4, items)
paste_stand("lantern", -5.2, -3.4, 1.4, items)
paste_stand("fence", 0.5, -4.7, 1.4, items)
paste_stand("fence", 3.6, -4.7, 1.4, items)
paste_stand("grill", -1.2, -1.6, 1.7, items)
paste_stand("juicer", -1.2, 1.8, 1.7, items)
paste_stand("table", 4.4, -2.4, 1.05, items)
paste_stand("table", 4.4, 0.0, 1.05, items)
paste_stand("table", 4.4, 2.4, 1.05, items)
paste_stand("player", -2.8, 0.0, 2.0, items)
paste_stand("staff", -3.6, -3.0, 1.9, items)
paste_stand("cust_fox", 4.4, -1.6, 1.5, items)
paste_stand("cust_penguin", 4.4, 0.8, 1.5, items)

for _, img, x, y in sorted(items, key=lambda t: t[0], reverse=True):
    bg.alpha_composite(img, (x, y))

out = os.path.join(HERE, "_preview.png")
bg.convert("RGB").save(out)
print("wrote", out)
