#!/usr/bin/env python3
"""
Generates the 2.5D arcade sprite set as SVG, rendered to PNG via rsvg-convert.

Style goal: matches the existing storybook art (soft rounded shapes, warm palette,
subtle gradients, drop shadows, pink cheek blush) — see Art/Sprites/Customers/*.

Output: Assets/_Project/Resources/Arcade/*.png  (loaded at runtime via Resources.Load)

Run:  python3 tools/arcade_art/gen_arcade_sprites.py
Needs: rsvg-convert (brew install librsvg)
"""
import os
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "Arcade")

# ---- palette (warm storybook) ----
C = dict(
    grass="#7ec850", grass_d="#5fa83b", grass_l="#9bd96a",
    dirt="#caa46a", dirt_d="#a8824e",
    wood="#d8a463", wood_d="#b07d44", wood_l="#ecc488",
    water="#5cc6e8", water_d="#39a8d6", water_l="#9fe2f4",
    cream="#fbeed2", cream_d="#f0dcae",
    fish="#7fd4f2", fish_d="#4bb3e0", fish_belly="#fdf3df",
    grill_body="#4a4a52", grill_d="#33333a", grill_l="#6a6a74", ember="#ff7a3c", ember_l="#ffd24a",
    juice_body="#e87fb0", juice_d="#c85890", glass="#ffd9ec",
    berry="#c8284c", berry_d="#9c1838", berry_leaf="#5fa83b",
    money="#3ccf6e", money_d="#26a854", coin="#ffd24a", coin_d="#e8a82a",
    cat="#f6b860", cat_d="#e09a3e", cat_cream="#fdf3df",
    staff="#7fb8f0", staff_d="#5a96d8",
    nose="#3a2a22", eye="#3a2a22", blush="#f7a8b8",
    apron="#fef6e8", apron_d="#e8dcc0",
    hat="#ffffff", hat_d="#e6e6e6",
    pad_build="#ffd24a", pad_build_d="#e8a82a",
    pad_hire="#6fa8e8", pad_hire_d="#4a86d0",
    shadow="#000000",
    leaf="#5fa83b", leaf_d="#4a8830", trunk="#a8743e", trunk_d="#85591f",
)

def grad(id, c0, c1, x1=0, y1=0, x2=0, y2=1):
    return (f'<linearGradient id="{id}" x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}">'
            f'<stop offset="0" stop-color="{c0}"/><stop offset="1" stop-color="{c1}"/>'
            f'</linearGradient>')

def rgrad(id, c0, c1):
    return (f'<radialGradient id="{id}" cx="0.5" cy="0.42" r="0.6">'
            f'<stop offset="0" stop-color="{c0}"/><stop offset="1" stop-color="{c1}"/>'
            f'</radialGradient>')

def svg(w, h, body, defs=""):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" '
            f'viewBox="0 0 {w} {h}">\n<defs>{defs}</defs>\n{body}\n</svg>')

def ground_shadow(cx, cy, rx, ry, op=0.18):
    return f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="{C["shadow"]}" opacity="{op}"/>'

# ---------------- tiles (seamless-ish, drawn as one cell) ----------------
def tile_grass():
    d = grad("gg", C["grass_l"], C["grass"])
    body = f'<rect width="128" height="128" fill="url(#gg)"/>'
    # scattered blades + dots for texture
    for (x, y) in [(20,30),(54,18),(92,40),(110,80),(34,96),(72,104),(16,70),(98,16)]:
        body += (f'<path d="M{x} {y} q-3 -10 0 -16 q3 6 0 16 Z" fill="{C["grass_d"]}" opacity="0.5"/>'
                 f'<path d="M{x+5} {y} q3 -8 6 -12 q-1 6 -6 12 Z" fill="{C["grass_l"]}" opacity="0.6"/>')
    for (x,y,r) in [(44,60,3),(80,70,2.5),(24,44,2),(104,54,3),(64,30,2)]:
        body += f'<circle cx="{x}" cy="{y}" r="{r}" fill="{C["grass_d"]}" opacity="0.25"/>'
    return svg(128,128,body,d)

def tile_wood():
    # warm kitchen plank floor
    d = grad("wf", C["wood_l"], C["wood"])
    body = f'<rect width="128" height="128" fill="url(#wf)"/>'
    for i in range(4):
        y = i*32
        body += f'<rect x="0" y="{y}" width="128" height="32" fill="none" stroke="{C["wood_d"]}" stroke-width="2" opacity="0.45"/>'
        # plank grain
        body += f'<line x1="0" y1="{y+16}" x2="128" y2="{y+16}" stroke="{C["wood_d"]}" stroke-width="1" opacity="0.18"/>'
        off = 64 if i % 2 else 0
        body += f'<line x1="{off}" y1="{y}" x2="{off}" y2="{y+32}" stroke="{C["wood_d"]}" stroke-width="2" opacity="0.4"/>'
    return svg(128,128,body,d)

def tile_path():
    d = grad("pp", C["dirt"], C["dirt_d"])
    body = f'<rect width="128" height="128" fill="url(#pp)"/>'
    for (x,y,r) in [(30,40,5),(80,30,4),(100,90,6),(40,100,4),(64,64,5),(20,80,3)]:
        body += f'<circle cx="{x}" cy="{y}" r="{r}" fill="{C["dirt_d"]}" opacity="0.3"/>'
    return svg(128,128,body,d)

def tile_water():
    d = grad("wt", C["water_l"], C["water"]) + grad("wt2", C["water"], C["water_d"])
    body = f'<rect width="128" height="128" fill="url(#wt)"/>'
    # gentle ripples
    for y in (28, 60, 92):
        body += (f'<path d="M0 {y} q16 -8 32 0 t32 0 t32 0 t32 0" fill="none" '
                 f'stroke="{C["water_l"]}" stroke-width="3" opacity="0.55" stroke-linecap="round"/>')
    for y in (44, 76, 108):
        body += (f'<path d="M0 {y} q16 8 32 0 t32 0 t32 0 t32 0" fill="none" '
                 f'stroke="{C["water_d"]}" stroke-width="2" opacity="0.35" stroke-linecap="round"/>')
    return svg(128,128,body,d)

def tile_berryfield():
    d = grad("bf", C["grass"], C["grass_d"])
    body = f'<rect width="128" height="128" fill="url(#bf)"/>'
    # little berry bushes dotted around
    for (x,y) in [(32,40),(88,36),(48,92),(96,96),(20,80)]:
        body += (f'<ellipse cx="{x}" cy="{y+6}" rx="16" ry="6" fill="{C["shadow"]}" opacity="0.12"/>'
                 f'<circle cx="{x}" cy="{y}" r="13" fill="{C["berry_leaf"]}"/>'
                 f'<circle cx="{x-4}" cy="{y-2}" r="3.5" fill="{C["berry"]}"/>'
                 f'<circle cx="{x+5}" cy="{y+1}" r="3.5" fill="{C["berry"]}"/>'
                 f'<circle cx="{x}" cy="{y+5}" r="3.5" fill="{C["berry_d"]}"/>')
    return svg(128,128,body,d)

# ---------------- facilities (2.5D, drawn facing the camera) ----------------
def grill():
    W,H=220,200
    d = grad("gb", C["grill_l"], C["grill_body"]) + grad("gl", C["grill_body"], C["grill_d"]) + rgrad("em", C["ember_l"], C["ember"])
    b = ground_shadow(110,184,84,16)
    # legs
    b += f'<rect x="44" y="120" width="14" height="56" rx="6" fill="{C["grill_d"]}"/>'
    b += f'<rect x="162" y="120" width="14" height="56" rx="6" fill="{C["grill_d"]}"/>'
    # basin (ellipse top + body)
    b += f'<rect x="34" y="78" width="152" height="56" rx="26" fill="url(#gl)"/>'
    b += f'<ellipse cx="110" cy="80" rx="78" ry="26" fill="url(#gb)"/>'
    b += f'<ellipse cx="110" cy="78" rx="64" ry="19" fill="{C["grill_d"]}"/>'
    # embers
    for (x,y) in [(86,76),(110,80),(134,75),(98,82),(122,82)]:
        b += f'<circle cx="{x}" cy="{y}" r="7" fill="url(#em)"/>'
    # grill bars
    for x in (80,98,116,134):
        b += f'<line x1="{x}" y1="64" x2="{x}" y2="92" stroke="{C["grill_l"]}" stroke-width="3" opacity="0.8"/>'
    # little flame
    b += f'<path d="M110 52 q-10 14 0 22 q10 -8 0 -22 Z" fill="{C["ember"]}"/>'
    b += f'<path d="M110 58 q-5 8 0 14 q5 -6 0 -14 Z" fill="{C["ember_l"]}"/>'
    return svg(W,H,b,d)

def juicer():
    W,H=200,210
    d = grad("jb", C["juice_body"], C["juice_d"]) + grad("gs", "#ffffff", C["glass"])
    b = ground_shadow(100,192,72,15)
    # base
    b += f'<rect x="40" y="120" width="120" height="56" rx="22" fill="url(#jb)"/>'
    b += f'<ellipse cx="100" cy="122" rx="60" ry="18" fill="{C["juice_body"]}"/>'
    # glass jug with juice
    b += f'<path d="M64 60 h72 v52 q0 18 -18 18 h-36 q-18 0 -18 -18 Z" fill="url(#gs)" opacity="0.92" stroke="{C["juice_d"]}" stroke-width="3"/>'
    b += f'<path d="M70 92 h60 v18 q0 14 -14 14 h-32 q-14 0 -14 -14 Z" fill="{C["juice_body"]}"/>'
    b += f'<ellipse cx="100" cy="92" rx="30" ry="6" fill="{C["juice_d"]}" opacity="0.6"/>'
    # spout + berries on top
    b += f'<rect x="92" y="40" width="16" height="22" rx="6" fill="{C["juice_d"]}"/>'
    b += f'<circle cx="86" cy="40" r="9" fill="{C["berry"]}"/>'
    b += f'<circle cx="104" cy="36" r="9" fill="{C["berry_d"]}"/>'
    b += f'<circle cx="118" cy="42" r="8" fill="{C["berry"]}"/>'
    b += f'<ellipse cx="100" cy="26" rx="6" ry="3" fill="{C["berry_leaf"]}"/>'
    return svg(W,H,b,d)

def table():
    W,H=230,170
    d = grad("tt", C["wood_l"], C["wood"]) + grad("tg", C["wood"], C["wood_d"])
    b = ground_shadow(115,150,90,16)
    # legs
    b += f'<rect x="40" y="92" width="14" height="52" rx="5" fill="{C["wood_d"]}"/>'
    b += f'<rect x="176" y="92" width="14" height="52" rx="5" fill="{C["wood_d"]}"/>'
    # round table top (2.5D ellipse)
    b += f'<ellipse cx="115" cy="96" rx="98" ry="30" fill="url(#tg)"/>'
    b += f'<ellipse cx="115" cy="86" rx="98" ry="30" fill="url(#tt)"/>'
    b += f'<ellipse cx="115" cy="86" rx="82" ry="22" fill="none" stroke="{C["wood_d"]}" stroke-width="2" opacity="0.35"/>'
    # a little plate + cutlery hint
    b += f'<ellipse cx="115" cy="84" rx="26" ry="9" fill="{C["cream"]}"/>'
    b += f'<ellipse cx="115" cy="83" rx="18" ry="6" fill="{C["cream_d"]}"/>'
    return svg(W,H,b,d)

def pad(kind):
    # build / hire floor pad (glowing ring)
    W,H=200,120
    if kind=="build":
        c0,c1 = C["pad_build"], C["pad_build_d"]
    else:
        c0,c1 = C["pad_hire"], C["pad_hire_d"]
    d = rgrad("pad", c0, c1)
    b = f'<ellipse cx="100" cy="60" rx="92" ry="44" fill="url(#pad)" opacity="0.55"/>'
    b += f'<ellipse cx="100" cy="60" rx="92" ry="44" fill="none" stroke="{c1}" stroke-width="4" stroke-dasharray="14 10" opacity="0.9"/>'
    b += f'<ellipse cx="100" cy="60" rx="66" ry="30" fill="none" stroke="{c0}" stroke-width="3" opacity="0.7"/>'
    return svg(W,H,b,d)

# ---------------- characters (cute cats, front-facing) ----------------
def _cat(body_c, body_d, accessory):
    """Shared chibi cat. accessory: 'chef' (hat) or 'staff' (cap)."""
    W,H=200,240
    d = (rgrad("hd", "#ffffff", body_c).replace("hd",f"hd_{body_c[1:]}")
         + grad("bd", body_c, body_d).replace("bd",f"bd_{body_c[1:]}"))
    hd = f"hd_{body_c[1:]}"; bd = f"bd_{body_c[1:]}"
    b = ground_shadow(100,224,58,12)
    # body
    b += f'<path d="M58 150 q42 -26 84 0 q10 50 -8 66 h-68 q-18 -16 -8 -66 Z" fill="url(#{bd})"/>'
    b += f'<ellipse cx="100" cy="196" rx="30" ry="34" fill="{C["cat_cream"]}"/>'
    # feet
    b += f'<ellipse cx="78" cy="222" rx="14" ry="9" fill="{body_d}"/>'
    b += f'<ellipse cx="122" cy="222" rx="14" ry="9" fill="{body_d}"/>'
    # ears (drawn high & wide so they clearly poke above the head)
    b += f'<path d="M54 78 q-14 -52 30 -56 q-2 26 -14 52 Z" fill="url(#{bd})" stroke="{body_d}" stroke-width="2"/>'
    b += f'<path d="M146 78 q14 -52 -30 -56 q2 26 14 52 Z" fill="url(#{bd})" stroke="{body_d}" stroke-width="2"/>'
    b += f'<path d="M62 64 q-6 -28 18 -32 q-2 16 -10 30 Z" fill="{C["blush"]}" opacity="0.75"/>'
    b += f'<path d="M138 64 q6 -28 -18 -32 q2 16 10 30 Z" fill="{C["blush"]}" opacity="0.75"/>'
    # head
    b += f'<ellipse cx="100" cy="100" rx="60" ry="55" fill="url(#{hd})"/>'
    # muzzle
    b += f'<ellipse cx="100" cy="116" rx="30" ry="22" fill="{C["cat_cream"]}"/>'
    # eyes
    for ex in (80,120):
        b += f'<ellipse cx="{ex}" cy="98" rx="9" ry="11" fill="{C["eye"]}"/>'
        b += f'<circle cx="{ex+3}" cy="94" r="3" fill="#ffffff"/>'
    # blush
    b += f'<ellipse cx="68" cy="114" rx="10" ry="6" fill="{C["blush"]}" opacity="0.8"/>'
    b += f'<ellipse cx="132" cy="114" rx="10" ry="6" fill="{C["blush"]}" opacity="0.8"/>'
    # nose + mouth
    b += f'<path d="M94 110 h12 l-6 7 Z" fill="{C["nose"]}"/>'
    b += f'<path d="M100 117 q-6 8 -13 4 M100 117 q6 8 13 4" fill="none" stroke="{C["nose"]}" stroke-width="2.5" stroke-linecap="round"/>'
    if accessory=="chef":
        # chef hat (larger, sits across the crown)
        b += f'<rect x="64" y="40" width="72" height="20" rx="7" fill="{C["hat"]}" stroke="{C["hat_d"]}" stroke-width="2"/>'
        b += f'<path d="M66 46 q-12 -40 34 -40 q46 0 34 40 Z" fill="{C["hat"]}" stroke="{C["hat_d"]}" stroke-width="2"/>'
        b += f'<circle cx="80" cy="22" r="14" fill="{C["hat"]}"/><circle cx="100" cy="14" r="16" fill="{C["hat"]}"/><circle cx="120" cy="22" r="14" fill="{C["hat"]}"/>'
        b += f'<ellipse cx="100" cy="34" rx="30" ry="8" fill="{C["hat_d"]}" opacity="0.25"/>'
        # apron hint on body
        b += f'<path d="M82 158 q18 -10 36 0 l-4 48 h-28 Z" fill="{C["apron"]}" opacity="0.95"/>'
    else:
        # staff cap (visor)
        b += f'<path d="M64 60 q36 -26 72 0 q-36 -10 -72 0 Z" fill="{C["staff_d"]}"/>'
        b += f'<path d="M58 62 q42 -30 84 0 q-6 -8 -42 -8 q-36 0 -42 8 Z" fill="{body_c}" stroke="{C["staff_d"]}" stroke-width="2"/>'
        b += f'<ellipse cx="100" cy="52" rx="10" ry="5" fill="{C["staff_d"]}"/>'
        b += f'<path d="M86 158 q14 -8 28 0 l-3 48 h-22 Z" fill="{C["apron"]}" opacity="0.9"/>'
    return svg(W,H,b,d)

def player():   return _cat(C["cat"], C["cat_d"], "chef")
def staff():    return _cat(C["staff"], C["staff_d"], "staff")

# ---------------- items ----------------
def item_fish():
    W,H=120,90
    d = grad("fs", C["fish"], C["fish_d"])
    b = ground_shadow(60,80,40,8)
    b += f'<ellipse cx="58" cy="44" rx="40" ry="24" fill="url(#fs)"/>'
    b += f'<ellipse cx="50" cy="50" rx="26" ry="13" fill="{C["fish_belly"]}"/>'
    b += f'<path d="M96 44 l22 -16 v32 Z" fill="{C["fish_d"]}"/>'
    b += f'<circle cx="34" cy="38" r="6" fill="#ffffff"/><circle cx="33" cy="38" r="3" fill="{C["eye"]}"/>'
    b += f'<path d="M62 30 q10 -2 16 4 q-10 0 -16 -4 Z" fill="{C["fish_d"]}" opacity="0.7"/>'
    return svg(W,H,b,d)

def item_grilledfish():
    W,H=120,90
    d = grad("gf", "#e8a85c", "#c47a2e")
    b = ground_shadow(60,80,40,8)
    b += f'<ellipse cx="58" cy="44" rx="40" ry="24" fill="url(#gf)"/>'
    # grill marks
    for x in (44,58,72):
        b += f'<line x1="{x-8}" y1="32" x2="{x+8}" y2="56" stroke="#8a4f1e" stroke-width="4" opacity="0.6" stroke-linecap="round"/>'
    b += f'<path d="M96 44 l22 -16 v32 Z" fill="#a8651f"/>'
    b += f'<circle cx="34" cy="38" r="4.5" fill="#3a2a22"/>'
    # parsley
    b += f'<circle cx="60" cy="22" r="5" fill="{C["leaf"]}"/><circle cx="68" cy="20" r="4" fill="{C["leaf_d"]}"/>'
    return svg(W,H,b,d)

def item_berry():
    W,H=100,100
    d = rgrad("br", "#e8506e", C["berry"])
    b = ground_shadow(50,88,32,7)
    b += f'<circle cx="40" cy="56" r="24" fill="url(#br)"/>'
    b += f'<circle cx="62" cy="50" r="20" fill="{C["berry_d"]}"/>'
    b += f'<circle cx="52" cy="62" r="22" fill="url(#br)"/>'
    b += f'<ellipse cx="46" cy="48" rx="6" ry="4" fill="#ffffff" opacity="0.6"/>'
    b += f'<path d="M48 32 q4 -12 16 -14 q-6 10 -16 14 Z" fill="{C["berry_leaf"]}"/>'
    return svg(W,H,b,d)

def item_juice():
    W,H=90,120
    d = grad("ju", C["juice_body"], C["juice_d"]) + grad("gl2","#ffffff",C["glass"])
    b = ground_shadow(45,108,28,7)
    b += f'<path d="M26 36 h38 l-5 64 q-1 8 -14 8 q-13 0 -14 -8 Z" fill="url(#gl2)" opacity="0.85" stroke="{C["juice_d"]}" stroke-width="2.5"/>'
    b += f'<path d="M29 56 h32 l-4 44 q-1 6 -12 6 q-11 0 -12 -6 Z" fill="url(#ju)"/>'
    b += f'<ellipse cx="45" cy="56" rx="16" ry="4" fill="{C["juice_d"]}" opacity="0.5"/>'
    # straw + berry
    b += f'<rect x="50" y="14" width="6" height="34" rx="3" fill="{C["berry_d"]}" transform="rotate(12 53 30)"/>'
    b += f'<circle cx="34" cy="30" r="7" fill="{C["berry"]}"/>'
    return svg(W,H,b,d)

def money():
    W,H=120,90
    d = grad("mn", C["money"], C["money_d"]) + rgrad("co", C["coin"], C["coin_d"])
    b = ground_shadow(60,80,42,8)
    # stack of bills
    for i,(yy,cc) in enumerate([(58,C["money_d"]),(50,C["money"]),(42,C["money"])]):
        b += f'<rect x="22" y="{yy}" width="76" height="20" rx="5" fill="{cc}" stroke="{C["money_d"]}" stroke-width="2"/>'
    b += f'<circle cx="60" cy="52" r="9" fill="url(#co)"/>'
    # coins
    b += f'<ellipse cx="92" cy="62" rx="16" ry="16" fill="url(#co)"/>'
    b += f'<text x="92" y="68" font-family="Arial" font-size="18" font-weight="bold" fill="{C["coin_d"]}" text-anchor="middle">$</text>'
    return svg(W,H,b,d)

# ---------------- props ----------------
def tree():
    W,H=180,220
    d = grad("tl", C["leaf"], C["leaf_d"]) + grad("tk", C["trunk"], C["trunk_d"])
    b = ground_shadow(90,204,56,14)
    b += f'<rect x="78" y="130" width="24" height="70" rx="10" fill="url(#tk)"/>'
    b += f'<circle cx="90" cy="92" r="58" fill="url(#tl)"/>'
    b += f'<circle cx="54" cy="110" r="36" fill="url(#tl)"/>'
    b += f'<circle cx="126" cy="110" r="36" fill="url(#tl)"/>'
    b += f'<circle cx="74" cy="74" r="14" fill="{C["grass_l"]}" opacity="0.5"/>'
    return svg(W,H,b,d)

def bush():
    W,H=140,90
    d = grad("bsh", C["grass"], C["grass_d"])
    b = ground_shadow(70,80,52,10)
    b += f'<circle cx="44" cy="54" r="28" fill="url(#bsh)"/>'
    b += f'<circle cx="96" cy="54" r="28" fill="url(#bsh)"/>'
    b += f'<circle cx="70" cy="44" r="32" fill="url(#bsh)"/>'
    b += f'<circle cx="60" cy="36" r="9" fill="{C["grass_l"]}" opacity="0.5"/>'
    return svg(W,H,b,d)

def fence():
    W,H=160,120
    d = grad("fn", C["wood_l"], C["wood_d"])
    b = ""
    for x in (24, 80, 136):
        b += f'<rect x="{x-9}" y="34" width="18" height="78" rx="6" fill="url(#fn)" stroke="{C["wood_d"]}" stroke-width="2"/>'
        b += f'<path d="M{x-9} 34 l9 -12 l9 12 Z" fill="{C["wood_l"]}" stroke="{C["wood_d"]}" stroke-width="2"/>'
    for y in (52, 84):
        b += f'<rect x="10" y="{y}" width="140" height="12" rx="5" fill="url(#fn)" stroke="{C["wood_d"]}" stroke-width="2"/>'
    return svg(W,H,b,d)

SPRITES = {
    "tile_grass": tile_grass, "tile_wood": tile_wood, "tile_path": tile_path,
    "tile_water": tile_water, "tile_berryfield": tile_berryfield,
    "grill": grill, "juicer": juicer, "table": table,
    "pad_build": lambda: pad("build"), "pad_hire": lambda: pad("hire"),
    "player": player, "staff": staff,
    "item_fish": item_fish, "item_grilledfish": item_grilledfish,
    "item_berry": item_berry, "item_juice": item_juice, "money": money,
    "tree": tree, "bush": bush, "fence": fence,
}

def main():
    os.makedirs(OUT, exist_ok=True)
    tmp = os.path.join(os.path.dirname(__file__), "_svg_tmp")
    os.makedirs(tmp, exist_ok=True)
    failed = []
    for name, fn in SPRITES.items():
        svg_path = os.path.join(tmp, name + ".svg")
        png_path = os.path.join(OUT, name + ".png")
        with open(svg_path, "w") as f:
            f.write(fn())
        r = subprocess.run(["rsvg-convert", svg_path, "-o", png_path],
                           capture_output=True, text=True)
        if r.returncode != 0:
            failed.append((name, r.stderr.strip()))
            print(f"  FAIL {name}: {r.stderr.strip()}")
        else:
            print(f"  ok   {name}.png")
    if failed:
        print(f"\n{len(failed)} failed")
        sys.exit(1)
    print(f"\nDone: {len(SPRITES)} sprites -> {OUT}")

if __name__ == "__main__":
    main()
