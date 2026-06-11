#!/usr/bin/env python3
"""
Arcade sprite set v2 — redesigned to match the concept art in Art/Concepts/.

Direction (from concepts): warm hand-drawn storybook. The hero is a CALICO cat
(white base + asymmetric orange & black tortoiseshell patches, big amber eyes) in
either a chef coat (white + red trim + toque) or an explorer vest (green, pouches).
Cozy log-cabin restaurant palette: deep warm wood, amber lantern glow, cream.

Renders SVG -> PNG via rsvg-convert into Assets/_Project/Resources/Arcade/.
Run:  python3 tools/arcade_art/gen_arcade_sprites_v2.py
"""
import os, subprocess, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "Arcade")

# ---- warm storybook palette ----
P = dict(
    # calico cat
    white="#fbf3e6", white_sh="#ece0cb", cream="#f6e7cf",
    ginger="#e8954a", ginger_d="#cf7430", ginger_l="#f4b878",
    black="#4a3f3a", black_d="#352c28",
    eye="#7a4a22", eye_l="#b07a3c", nose="#d98a8a", mouth="#6b4a3a",
    blush="#f4a9a0",
    # chef
    coat="#fdf6ea", coat_sh="#ece0c8", red="#d84b3c", red_d="#b53527",
    toque="#fdfaf2", toque_sh="#e8e0d0",
    # explorer vest
    vest="#7d8a4f", vest_d="#5e6b38", vest_l="#97a35f",
    buckle="#caa46a", pouch="#8a6a3e",
    # world — warm wood cabin
    wood="#c98f54", wood_d="#9c6a38", wood_l="#e3b577", wood_dd="#7c5128",
    grass="#86b94e", grass_d="#5f9434", grass_l="#a6d268",
    soil="#b98a52", soil_d="#946838",
    water="#67c2dd", water_d="#3fa3c6", water_l="#a6e3f0",
    # food
    fish="#86cfe6", fish_d="#4fb0d2", fish_belly="#fdf3df",
    grill_steel="#5a5560", grill_d="#3d3942", grill_l="#7e7884",
    ember="#ff7a35", ember_l="#ffd24a",
    berry="#c83a59", berry_d="#9c2440", berry_leaf="#5f9434",
    juice="#e06aa0", juice_d="#c44784",
    coin="#ffcf4a", coin_d="#e0a52a", bill="#5ec07a", bill_d="#3a9a57",
    # props
    leaf="#5f9434", leaf_d="#4a7a28", leaf_l="#86b94e",
    trunk="#a8743e", trunk_d="#7c5128",
    lantern="#ffcf6a", lantern_glow="#ffd98a",
    shadow="#2a1d12",
    line="#5a4632",   # warm outline
)

def lin(i, c0, c1, x1=0, y1=0, x2=0, y2=1):
    return (f'<linearGradient id="{i}" x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}">'
            f'<stop offset="0" stop-color="{c0}"/><stop offset="1" stop-color="{c1}"/></linearGradient>')

def rad(i, c0, c1, cx=0.5, cy=0.4, r=0.62):
    return (f'<radialGradient id="{i}" cx="{cx}" cy="{cy}" r="{r}">'
            f'<stop offset="0" stop-color="{c0}"/><stop offset="1" stop-color="{c1}"/></radialGradient>')

def svg(w, h, body, defs=""):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" '
            f'viewBox="0 0 {w} {h}">\n<defs>{defs}</defs>\n{body}\n</svg>')

def shadow(cx, cy, rx, ry, op=0.2):
    return f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="{P["shadow"]}" opacity="{op}"/>'

OUTLINE = f'stroke="{P["line"]}" stroke-width="3" stroke-linejoin="round"'

# ============================ CALICO CAT ============================
def calico_cat(outfit):
    """Chibi calico cat. outfit in {'chef','explorer','plain'}."""
    W, H = 220, 260
    d = (rad("head", "#fffaf0", P["white_sh"], 0.42, 0.4, 0.62)
         + rad("body", P["white"], P["white_sh"], 0.5, 0.35, 0.7)
         + lin("gpatch", P["ginger_l"], P["ginger_d"])
         + lin("bpatch", P["black"], P["black_d"])
         + lin("tail", P["ginger"], P["black"], 0, 0, 1, 1))
    b = shadow(110, 244, 60, 13)

    # ---- tail (tortoiseshell stripes) ----
    b += (f'<path d="M156 196 q44 4 50 -32 q4 -24 -14 -30 q10 18 2 34 q-10 20 -42 16 Z" '
          f'fill="url(#tail)" {OUTLINE}/>')
    for i, yy in enumerate((150, 162, 174)):
        b += f'<path d="M188 {yy} q10 -2 14 6" stroke="{P["black_d"]}" stroke-width="4" fill="none" opacity="0.6"/>'

    # ---- body (white base) ----
    b += (f'<path d="M60 150 q50 -30 100 0 q12 56 -10 74 q-40 14 -80 0 q-22 -18 -10 -74 Z" '
          f'fill="url(#body)" {OUTLINE}/>')
    # ginger patch on body (right side)
    b += f'<path d="M120 150 q30 -6 40 6 q8 30 -2 60 q-22 8 -40 4 q-2 -40 2 -70 Z" fill="url(#gpatch)" opacity="0.92"/>'

    # ---- feet ----
    b += f'<ellipse cx="86" cy="226" rx="16" ry="10" fill="{P["white"]}" {OUTLINE}/>'
    b += f'<ellipse cx="134" cy="226" rx="16" ry="10" fill="{P["white"]}" {OUTLINE}/>'

    # ---- ears (one ginger, one black) ----
    b += f'<path d="M64 86 q-12 -50 28 -54 q0 26 -12 52 Z" fill="url(#gpatch)" {OUTLINE}/>'
    b += f'<path d="M156 86 q12 -50 -28 -54 q0 26 12 52 Z" fill="url(#bpatch)" {OUTLINE}/>'
    b += f'<path d="M74 70 q-6 -26 16 -30 q-1 14 -8 28 Z" fill="{P["blush"]}" opacity="0.7"/>'

    # ---- head (white) ----
    b += f'<ellipse cx="110" cy="100" rx="62" ry="56" fill="url(#head)" {OUTLINE}/>'
    # ginger patch over left ear/cheek, black over right (asymmetric calico)
    b += f'<path d="M64 92 q-4 -30 24 -38 q20 8 22 30 q-22 16 -46 8 Z" fill="url(#gpatch)" opacity="0.95"/>'
    b += f'<path d="M156 92 q4 -28 -22 -36 q-16 6 -18 26 q18 14 40 10 Z" fill="url(#bpatch)" opacity="0.95"/>'
    # muzzle
    b += f'<ellipse cx="110" cy="118" rx="32" ry="24" fill="{P["white"]}"/>'

    # ---- eyes (big warm green, matching the cozy chef-cat reference) ----
    eye_outer = "#4f7a38"
    eye_inner = "#94b65c"
    for ex in (84, 136):
        b += f'<ellipse cx="{ex}" cy="100" rx="12" ry="14" fill="#ffffff"/>'
        b += f'<ellipse cx="{ex}" cy="101" rx="10" ry="12" fill="url(#eyeg_{ex})"/>'
        d += rad(f"eyeg_{ex}", eye_inner, eye_outer, 0.5, 0.4, 0.7)
        b += f'<circle cx="{ex}" cy="103" r="5.5" fill="{P["black_d"]}"/>'
        b += f'<circle cx="{ex+4}" cy="97" r="3.5" fill="#ffffff"/>'
        b += f'<circle cx="{ex-3}" cy="105" r="1.8" fill="#ffffff" opacity="0.8"/>'
    # brows hint
    b += f'<path d="M74 84 q10 -4 18 0 M128 84 q10 -4 18 0" stroke="{P["line"]}" stroke-width="2.5" fill="none" opacity="0.5" stroke-linecap="round"/>'

    # blush
    b += f'<ellipse cx="70" cy="116" rx="11" ry="7" fill="{P["blush"]}" opacity="0.8"/>'
    b += f'<ellipse cx="150" cy="116" rx="11" ry="7" fill="{P["blush"]}" opacity="0.8"/>'
    # nose + mouth
    b += f'<path d="M103 112 h14 l-7 8 Z" fill="{P["nose"]}" stroke="{P["mouth"]}" stroke-width="1.5"/>'
    b += f'<path d="M110 120 q-7 9 -15 5 M110 120 q7 9 15 5" stroke="{P["mouth"]}" stroke-width="3" fill="none" stroke-linecap="round"/>'
    # whiskers
    b += f'<path d="M50 110 q18 -2 30 2 M52 120 q16 0 28 4 M190 110 q-18 -2 -30 2 M188 120 q-16 0 -28 4" stroke="{P["line"]}" stroke-width="2" fill="none" opacity="0.4" stroke-linecap="round"/>'

    # ---- outfit ----
    if outfit == "chef":
        # coat over the body
        b += f'<path d="M78 156 q32 -16 64 0 q8 34 0 64 q-32 10 -64 0 q-8 -30 0 -64 Z" fill="{P["coat"]}" {OUTLINE}/>'
        b += f'<path d="M110 156 v68" stroke="{P["coat_sh"]}" stroke-width="3"/>'
        # red neckerchief
        b += f'<path d="M92 156 q18 14 36 0 l-6 14 h-24 Z" fill="{P["red"]}" {OUTLINE}/>'
        # tiny wooden spoon held beside the body; readable at close camera, harmless when small.
        b += f'<path d="M55 162 q-10 28 -18 48" stroke="{P["wood_dd"]}" stroke-width="6" fill="none" stroke-linecap="round"/>'
        b += f'<ellipse cx="53" cy="158" rx="8" ry="13" fill="{P["wood_l"]}" {OUTLINE} transform="rotate(28 53 158)"/>'
        # buttons
        for yy in (176, 192, 208):
            b += f'<circle cx="100" cy="{yy}" r="3" fill="{P["red_d"]}"/>'
        # toque
        b += f'<rect x="74" y="40" width="72" height="20" rx="8" fill="{P["toque"]}" {OUTLINE}/>'
        b += f'<path d="M76 46 q-12 -42 34 -42 q46 0 34 42 Z" fill="{P["toque"]}" {OUTLINE}/>'
        b += f'<circle cx="84" cy="22" r="15" fill="{P["toque"]}"/><circle cx="110" cy="12" r="17" fill="{P["toque"]}"/><circle cx="136" cy="22" r="15" fill="{P["toque"]}"/>'
        b += f'<ellipse cx="110" cy="34" rx="32" ry="8" fill="{P["toque_sh"]}" opacity="0.4"/>'
    elif outfit == "explorer":
        # green utility vest
        b += f'<path d="M80 156 q30 -14 60 0 q6 34 0 64 q-30 10 -60 0 q-6 -30 0 -64 Z" fill="{P["vest"]}" {OUTLINE}/>'
        b += f'<path d="M110 156 v66" stroke="{P["vest_d"]}" stroke-width="3"/>'
        # pouches
        b += f'<rect x="84" y="188" width="20" height="20" rx="4" fill="{P["pouch"]}" {OUTLINE}/>'
        b += f'<rect x="116" y="188" width="20" height="20" rx="4" fill="{P["pouch"]}" {OUTLINE}/>'
        # compass buckle
        b += f'<circle cx="110" cy="176" r="8" fill="{P["buckle"]}" {OUTLINE}/>'
        b += f'<circle cx="110" cy="176" r="3" fill="{P["vest_d"]}"/>'
    return svg(W, H, b, d)

def player(): return calico_cat("chef")       # chef calico = the player
def staff():  return calico_cat("explorer")   # explorer calico = hired staff

# ============================ GUESTS ============================
def guest_body(animal, base, shade, accent, outfit):
    """Shared world-guest template: transparent, chibi, same readable proportions."""
    W, H = 220, 260
    d = (rad("fur", base, shade, 0.48, 0.36, 0.72)
         + rad("belly", "#fff4df", "#ead7b8", 0.5, 0.42, 0.68)
         + lin("outfit", accent, shade)
         + lin("stripe", shade, P["line"]))
    b = shadow(110, 244, 58, 13)

    # Species-specific tail goes behind the body.
    if animal == "fox":
        b += f'<path d="M154 190 q48 -2 52 -42 q-22 18 -48 18 q14 12 -4 24 Z" fill="url(#fur)" {OUTLINE}/>'
        b += f'<path d="M192 150 q12 -4 14 -18 q-24 10 -32 28 Z" fill="#fff4df" opacity="0.95"/>'
    elif animal == "raccoon":
        b += f'<path d="M154 194 q46 4 46 -28 q-16 12 -42 8 q12 10 -4 20 Z" fill="url(#fur)" {OUTLINE}/>'
        b += f'<path d="M174 174 q12 10 24 2 M164 188 q14 8 28 2" stroke="{P["line"]}" stroke-width="5" fill="none" opacity="0.55"/>'
    elif animal == "cat":
        b += f'<path d="M156 198 q42 2 46 -32 q-18 14 -42 12 q10 12 -4 20 Z" fill="url(#fur)" {OUTLINE}/>'
    elif animal == "penguin":
        b += f'<ellipse cx="110" cy="206" rx="44" ry="36" fill="{shade}" opacity="0.25"/>'

    # Body + feet.
    b += f'<path d="M62 152 q48 -30 96 0 q12 54 -10 72 q-38 14 -76 0 q-22 -18 -10 -72 Z" fill="url(#fur)" {OUTLINE}/>'
    b += f'<ellipse cx="110" cy="196" rx="30" ry="32" fill="url(#belly)" opacity="0.95"/>'
    b += f'<ellipse cx="86" cy="226" rx="16" ry="10" fill="{base}" {OUTLINE}/><ellipse cx="134" cy="226" rx="16" ry="10" fill="{base}" {OUTLINE}/>'

    # Outfit/accessory: each guest has one tiny restaurant-readable costume note.
    if outfit == "scarf":
        b += f'<path d="M74 154 q36 16 72 0 l-7 17 q-29 12 -58 0 Z" fill="url(#outfit)" {OUTLINE}/>'
        b += f'<path d="M138 162 q12 8 12 30 l-17 -6 q3 -14 5 -24 Z" fill="url(#outfit)" {OUTLINE}/>'
    elif outfit == "vest":
        b += f'<path d="M82 158 q28 -12 56 0 q6 30 -2 58 h-52 q-8 -28 -2 -58 Z" fill="url(#outfit)" {OUTLINE}/>'
        b += f'<path d="M110 158 v60" stroke="{P["line"]}" stroke-width="2.5" opacity="0.45"/>'
    elif outfit == "bow":
        b += f'<path d="M92 156 q18 12 36 0 l-6 14 h-24 Z" fill="url(#outfit)" {OUTLINE}/>'
    elif outfit == "apron":
        b += f'<path d="M84 158 q26 -10 52 0 q6 30 -2 58 h-48 q-8 -28 -2 -58 Z" fill="#fdf6ea" {OUTLINE}/>'
        b += f'<rect x="98" y="184" width="24" height="18" rx="4" fill="url(#outfit)" opacity="0.9"/>'

    # Ears / species head silhouette.
    if animal == "rabbit":
        b += f'<path d="M74 82 q-20 -58 16 -70 q10 40 2 74 Z" fill="url(#fur)" {OUTLINE}/>'
        b += f'<path d="M146 82 q20 -58 -16 -70 q-10 40 -2 74 Z" fill="url(#fur)" {OUTLINE}/>'
        b += f'<path d="M82 66 q-6 -30 8 -42 q4 28 -1 46 Z" fill="{P["blush"]}" opacity="0.55"/>'
        b += f'<path d="M138 66 q6 -30 -8 -42 q-4 28 1 46 Z" fill="{P["blush"]}" opacity="0.55"/>'
    elif animal == "penguin":
        b += f'<path d="M62 90 q48 -52 96 0 q-10 -4 -48 -4 q-38 0 -48 4 Z" fill="{shade}" {OUTLINE}/>'
    else:
        b += f'<path d="M64 86 q-12 -48 28 -54 q0 26 -12 52 Z" fill="url(#fur)" {OUTLINE}/>'
        b += f'<path d="M156 86 q12 -48 -28 -54 q0 26 12 52 Z" fill="url(#fur)" {OUTLINE}/>'
        if animal == "fox":
            b += f'<path d="M74 68 q-6 -24 14 -30 q-1 14 -8 28 Z" fill="#fff0df" opacity="0.85"/>'
            b += f'<path d="M146 68 q6 -24 -14 -30 q1 14 8 28 Z" fill="#fff0df" opacity="0.85"/>'

    # Head.
    b += f'<ellipse cx="110" cy="102" rx="58" ry="52" fill="url(#fur)" {OUTLINE}/>'
    if animal == "penguin":
        b += f'<ellipse cx="110" cy="106" rx="42" ry="42" fill="#f7f2df"/>'
    elif animal == "raccoon":
        b += f'<path d="M64 100 q46 -28 92 0 q-10 20 -46 20 q-36 0 -46 -20 Z" fill="{P["line"]}" opacity="0.55"/>'
    elif animal == "fox":
        b += f'<path d="M72 120 q38 -34 76 0 q-24 28 -76 0 Z" fill="#fff2df" opacity="0.95"/>'
    elif animal == "cat":
        b += f'<path d="M72 88 q24 -18 34 -4 q-14 12 -34 4 Z" fill="{accent}" opacity="0.55"/>'
    elif animal == "bear":
        b += f'<ellipse cx="110" cy="120" rx="30" ry="22" fill="#d8a86a" opacity="0.9"/>'
    else:
        b += f'<ellipse cx="110" cy="120" rx="30" ry="22" fill="#fff4df" opacity="0.82"/>'

    # Eyes.
    for ex in (88, 132):
        b += f'<ellipse cx="{ex}" cy="102" rx="9" ry="11" fill="{P["black_d"]}"/>'
        b += f'<circle cx="{ex+3}" cy="98" r="3" fill="#fff"/>'
        b += f'<circle cx="{ex-2}" cy="106" r="1.5" fill="#fff" opacity="0.75"/>'

    # Nose/beak + mouth.
    if animal == "penguin":
        b += f'<path d="M102 112 h16 l-8 8 Z" fill="#df9140" {OUTLINE}/>'
    else:
        b += f'<path d="M103 114 h14 l-7 7 Z" fill="{P["nose"]}" stroke="{P["mouth"]}" stroke-width="1.4"/>'
        b += f'<path d="M110 122 q-7 8 -15 4 M110 122 q7 8 15 4" stroke="{P["mouth"]}" stroke-width="2.6" fill="none" stroke-linecap="round"/>'

    # Blush, shared for restaurant-friendly warmth.
    b += f'<ellipse cx="76" cy="118" rx="10" ry="6" fill="{P["blush"]}" opacity="0.72"/>'
    b += f'<ellipse cx="144" cy="118" rx="10" ry="6" fill="{P["blush"]}" opacity="0.72"/>'

    # Species-specific readable marks.
    if animal == "raccoon":
        b += f'<path d="M74 88 q16 -12 30 -4 M116 84 q16 -8 30 4" stroke="{P["line"]}" stroke-width="4" fill="none" opacity="0.55" stroke-linecap="round"/>'
    elif animal == "bear":
        b += f'<circle cx="70" cy="72" r="19" fill="url(#fur)" {OUTLINE}/><circle cx="150" cy="72" r="19" fill="url(#fur)" {OUTLINE}/>'
        b += f'<circle cx="70" cy="72" r="8" fill="{shade}" opacity="0.35"/><circle cx="150" cy="72" r="8" fill="{shade}" opacity="0.35"/>'
    elif animal == "cat":
        b += f'<path d="M58 110 q18 -2 30 2 M162 112 q-18 -2 -30 2" stroke="{P["line"]}" stroke-width="2" fill="none" opacity="0.4" stroke-linecap="round"/>'

    return svg(W, H, b, d)

def guest_rabbit():
    return guest_body("rabbit", "#f7ecd5", "#d9c8aa", "#d7a74d", "vest")

def guest_raccoon():
    return guest_body("raccoon", "#9b927f", "#5f594f", "#6f9a4a", "scarf")

def guest_fox():
    return guest_body("fox", "#e88f3f", "#b85f26", "#d84b3c", "bow")

def guest_bear():
    return guest_body("bear", "#b27a45", "#7f512b", "#6f9a4a", "scarf")

def guest_penguin():
    return guest_body("penguin", "#47545d", "#28323a", "#6fa8e8", "apron")

def guest_vipcat():
    return guest_body("cat", "#f2c16c", "#c97936", "#caa46a", "vest")

# ============================ FACILITIES ============================
def grill():
    W, H = 230, 210
    d = lin("gs", P["grill_l"], P["grill_steel"]) + lin("gl", P["grill_steel"], P["grill_d"]) + rad("em", P["ember_l"], P["ember"])
    b = shadow(115, 192, 88, 16)
    b += f'<rect x="48" y="124" width="14" height="58" rx="6" fill="{P["grill_d"]}" {OUTLINE}/>'
    b += f'<rect x="168" y="124" width="14" height="58" rx="6" fill="{P["grill_d"]}" {OUTLINE}/>'
    b += f'<rect x="36" y="80" width="158" height="56" rx="26" fill="url(#gl)" {OUTLINE}/>'
    b += f'<ellipse cx="115" cy="82" rx="82" ry="27" fill="url(#gs)" {OUTLINE}/>'
    b += f'<ellipse cx="115" cy="80" rx="66" ry="19" fill="{P["grill_d"]}"/>'
    for (x, y) in [(88, 78), (115, 82), (142, 77), (102, 84), (128, 84)]:
        b += f'<circle cx="{x}" cy="{y}" r="8" fill="url(#em)"/>'
    for x in (82, 102, 122, 142):
        b += f'<line x1="{x}" y1="64" x2="{x}" y2="94" stroke="{P["grill_l"]}" stroke-width="3" opacity="0.85"/>'
    b += f'<path d="M115 50 q-11 16 0 24 q11 -10 0 -24 Z" fill="{P["ember"]}"/><path d="M115 56 q-6 9 0 15 q6 -7 0 -15 Z" fill="{P["ember_l"]}"/>'
    return svg(W, H, b, d)

def juicer():
    W, H = 200, 220
    d = lin("jb", P["juice"], P["juice_d"]) + lin("gls", "#ffffff", "#ffe1f0")
    b = shadow(100, 202, 74, 15)
    b += f'<rect x="40" y="124" width="120" height="58" rx="22" fill="url(#jb)" {OUTLINE}/>'
    b += f'<ellipse cx="100" cy="126" rx="60" ry="18" fill="{P["juice"]}"/>'
    b += f'<path d="M64 62 h72 v54 q0 18 -18 18 h-36 q-18 0 -18 -18 Z" fill="url(#gls)" opacity="0.92" {OUTLINE}/>'
    b += f'<path d="M70 94 h60 v22 q0 14 -14 14 h-32 q-14 0 -14 -14 Z" fill="url(#jb)"/>'
    b += f'<ellipse cx="100" cy="94" rx="30" ry="6" fill="{P["juice_d"]}" opacity="0.6"/>'
    b += f'<rect x="92" y="42" width="16" height="22" rx="6" fill="{P["juice_d"]}" {OUTLINE}/>'
    b += f'<circle cx="84" cy="42" r="10" fill="{P["berry"]}" {OUTLINE}/><circle cx="106" cy="38" r="10" fill="{P["berry_d"]}" {OUTLINE}/><circle cx="120" cy="44" r="9" fill="{P["berry"]}" {OUTLINE}/>'
    b += f'<ellipse cx="100" cy="26" rx="7" ry="3" fill="{P["berry_leaf"]}"/>'
    return svg(W, H, b, d)

def table():
    W, H = 240, 180
    d = lin("tt", P["wood_l"], P["wood"]) + lin("tg", P["wood"], P["wood_dd"])
    b = shadow(120, 158, 94, 16)
    b += f'<rect x="42" y="96" width="14" height="54" rx="5" fill="{P["wood_dd"]}" {OUTLINE}/>'
    b += f'<rect x="184" y="96" width="14" height="54" rx="5" fill="{P["wood_dd"]}" {OUTLINE}/>'
    b += f'<ellipse cx="120" cy="100" rx="102" ry="32" fill="url(#tg)" {OUTLINE}/>'
    b += f'<ellipse cx="120" cy="88" rx="102" ry="32" fill="url(#tt)" {OUTLINE}/>'
    # plank grain
    for dx in (-60, -20, 20, 60):
        b += f'<path d="M{120+dx} 60 q-4 28 0 56" stroke="{P["wood_d"]}" stroke-width="2" fill="none" opacity="0.3"/>'
    # plate + food
    b += f'<ellipse cx="120" cy="86" rx="28" ry="10" fill="{P["cream"]}" {OUTLINE}/>'
    b += f'<ellipse cx="120" cy="84" rx="18" ry="6" fill="#e8d5b0"/>'
    return svg(W, H, b, d)

# ============================ ITEMS ============================
def item_fish():
    W, H = 120, 90
    d = lin("fs", P["fish_l"] if "fish_l" in P else P["fish"], P["fish_d"])
    b = shadow(60, 80, 40, 8)
    b += f'<ellipse cx="58" cy="44" rx="40" ry="24" fill="url(#fs)" {OUTLINE}/>'
    b += f'<ellipse cx="50" cy="50" rx="26" ry="13" fill="{P["fish_belly"]}"/>'
    b += f'<path d="M96 44 l22 -16 v32 Z" fill="{P["fish_d"]}" {OUTLINE}/>'
    b += f'<circle cx="34" cy="38" r="6" fill="#fff" stroke="{P["line"]}" stroke-width="1.5"/><circle cx="33" cy="38" r="3" fill="{P["mouth"]}"/>'
    b += f'<path d="M62 30 q10 -2 16 4" stroke="{P["fish_d"]}" stroke-width="3" fill="none" opacity="0.7"/>'
    return svg(W, H, b, d)

def item_grilledfish():
    W, H = 120, 90
    d = lin("gf", "#e0a456", "#bd7a2e")
    b = shadow(60, 80, 40, 8)
    b += f'<ellipse cx="58" cy="44" rx="40" ry="24" fill="url(#gf)" {OUTLINE}/>'
    for x in (44, 58, 72):
        b += f'<line x1="{x-8}" y1="32" x2="{x+8}" y2="56" stroke="#7c451c" stroke-width="4" opacity="0.6" stroke-linecap="round"/>'
    b += f'<path d="M96 44 l22 -16 v32 Z" fill="#a8651f" {OUTLINE}/>'
    b += f'<circle cx="34" cy="38" r="4" fill="{P["mouth"]}"/>'
    b += f'<circle cx="60" cy="22" r="5" fill="{P["leaf"]}"/><circle cx="68" cy="20" r="4" fill="{P["leaf_d"]}"/>'
    return svg(W, H, b, d)

def item_berry():
    W, H = 100, 100
    d = rad("br2", "#e0506e", P["berry"])
    b = shadow(50, 88, 32, 7)
    b += f'<circle cx="40" cy="56" r="22" fill="url(#br2)" {OUTLINE}/>'
    b += f'<circle cx="62" cy="50" r="18" fill="{P["berry_d"]}" {OUTLINE}/>'
    b += f'<circle cx="52" cy="64" r="20" fill="url(#br2)" {OUTLINE}/>'
    b += f'<ellipse cx="46" cy="48" rx="6" ry="4" fill="#fff" opacity="0.6"/>'
    b += f'<path d="M48 32 q4 -12 16 -14 q-6 10 -16 14 Z" fill="{P["berry_leaf"]}" {OUTLINE}/>'
    return svg(W, H, b, d)

def item_juice():
    W, H = 90, 120
    d = lin("jj", P["juice"], P["juice_d"]) + lin("gg2", "#ffffff", "#ffe1f0")
    b = shadow(45, 108, 28, 7)
    b += f'<path d="M26 36 h38 l-5 64 q-1 8 -14 8 q-13 0 -14 -8 Z" fill="url(#gg2)" opacity="0.9" {OUTLINE}/>'
    b += f'<path d="M29 56 h32 l-4 44 q-1 6 -12 6 q-11 0 -12 -6 Z" fill="url(#jj)"/>'
    b += f'<ellipse cx="45" cy="56" rx="16" ry="4" fill="{P["juice_d"]}" opacity="0.5"/>'
    b += f'<rect x="50" y="14" width="6" height="34" rx="3" fill="{P["berry_d"]}" transform="rotate(12 53 30)"/>'
    b += f'<circle cx="34" cy="30" r="7" fill="{P["berry"]}" {OUTLINE}/>'
    return svg(W, H, b, d)

def money():
    W, H = 120, 95
    d = lin("mn", P["bill"], P["bill_d"]) + rad("co", P["coin"], P["coin_d"])
    b = shadow(60, 84, 42, 8)
    for yy, cc in [(60, P["bill_d"]), (52, P["bill"]), (44, P["bill"])]:
        b += f'<rect x="22" y="{yy}" width="76" height="20" rx="5" fill="{cc}" {OUTLINE}/>'
    b += f'<circle cx="60" cy="54" r="9" fill="url(#co)"/>'
    b += f'<ellipse cx="92" cy="64" rx="16" ry="16" fill="url(#co)" {OUTLINE}/>'
    b += f'<text x="92" y="70" font-family="Arial" font-size="18" font-weight="bold" fill="{P["coin_d"]}" text-anchor="middle">$</text>'
    return svg(W, H, b, d)

# ============================ TILES ============================
def tile_grass():
    d = lin("gg", P["grass_l"], P["grass"])
    b = f'<rect width="128" height="128" fill="url(#gg)"/>'
    for (x, y) in [(20, 30), (54, 18), (92, 40), (110, 80), (34, 96), (72, 104), (16, 70), (98, 16)]:
        b += f'<path d="M{x} {y} q-3 -10 0 -16 q3 6 0 16 Z" fill="{P["grass_d"]}" opacity="0.5"/>'
        b += f'<path d="M{x+5} {y} q3 -8 6 -12 q-1 6 -6 12 Z" fill="{P["grass_l"]}" opacity="0.6"/>'
    for (x, y) in [(44, 60), (80, 70), (24, 44), (104, 54), (64, 30)]:
        b += f'<circle cx="{x}" cy="{y}" r="2.5" fill="{P["grass_d"]}" opacity="0.25"/>'
        b += f'<circle cx="{x+3}" cy="{y-2}" r="1.5" fill="#fff7d8" opacity="0.5"/>'  # tiny flowers
    return svg(128, 128, b, d)

def tile_wood():
    d = lin("wf", P["wood_l"], P["wood"])
    b = f'<rect width="128" height="128" fill="url(#wf)"/>'
    for i in range(4):
        y = i * 32
        b += f'<rect x="0" y="{y}" width="128" height="32" fill="none" stroke="{P["wood_dd"]}" stroke-width="2" opacity="0.5"/>'
        b += f'<line x1="0" y1="{y+10}" x2="128" y2="{y+10}" stroke="{P["wood_dd"]}" stroke-width="1" opacity="0.2"/>'
        b += f'<line x1="0" y1="{y+22}" x2="128" y2="{y+22}" stroke="{P["wood_l"]}" stroke-width="1" opacity="0.3"/>'
        off = 64 if i % 2 else 0
        b += f'<line x1="{off}" y1="{y}" x2="{off}" y2="{y+32}" stroke="{P["wood_dd"]}" stroke-width="2" opacity="0.45"/>'
    return svg(128, 128, b, d)

def tile_path():
    d = lin("pp", P["soil"], P["soil_d"])
    b = f'<rect width="128" height="128" fill="url(#pp)"/>'
    for (x, y, r) in [(30, 40, 5), (80, 30, 4), (100, 90, 6), (40, 100, 4), (64, 64, 5), (20, 80, 3)]:
        b += f'<circle cx="{x}" cy="{y}" r="{r}" fill="{P["soil_d"]}" opacity="0.35"/>'
    return svg(128, 128, b, d)

def tile_water():
    d = lin("wt", P["water_l"], P["water"])
    b = f'<rect width="128" height="128" fill="url(#wt)"/>'
    for y in (28, 60, 92):
        b += f'<path d="M0 {y} q16 -8 32 0 t32 0 t32 0 t32 0" fill="none" stroke="{P["water_l"]}" stroke-width="3" opacity="0.6" stroke-linecap="round"/>'
    for y in (44, 76, 108):
        b += f'<path d="M0 {y} q16 8 32 0 t32 0 t32 0 t32 0" fill="none" stroke="{P["water_d"]}" stroke-width="2" opacity="0.35" stroke-linecap="round"/>'
    return svg(128, 128, b, d)

def tile_berryfield():
    d = lin("bf", P["grass"], P["grass_d"])
    b = f'<rect width="128" height="128" fill="url(#bf)"/>'
    for (x, y) in [(32, 40), (88, 36), (48, 92), (96, 96), (20, 80)]:
        b += (f'<ellipse cx="{x}" cy="{y+6}" rx="16" ry="6" fill="{P["shadow"]}" opacity="0.12"/>'
              f'<circle cx="{x}" cy="{y}" r="13" fill="{P["berry_leaf"]}" {OUTLINE}/>'
              f'<circle cx="{x-4}" cy="{y-2}" r="3.5" fill="{P["berry"]}"/>'
              f'<circle cx="{x+5}" cy="{y+1}" r="3.5" fill="{P["berry"]}"/>'
              f'<circle cx="{x}" cy="{y+5}" r="3.5" fill="{P["berry_d"]}"/>')
    return svg(128, 128, b, d)

# ============================ PADS ============================
def pad(kind):
    W, H = 200, 120
    if kind == "build":
        c0, c1 = P["coin"], P["coin_d"]
    else:
        c0, c1 = "#6fa8e8", "#4a86d0"
    d = rad("pad", c0, c1)
    b = f'<ellipse cx="100" cy="60" rx="92" ry="44" fill="url(#pad)" opacity="0.5"/>'
    b += f'<ellipse cx="100" cy="60" rx="92" ry="44" fill="none" stroke="{c1}" stroke-width="4" stroke-dasharray="14 10" opacity="0.9"/>'
    b += f'<ellipse cx="100" cy="60" rx="66" ry="30" fill="none" stroke="{c0}" stroke-width="3" opacity="0.7"/>'
    return svg(W, H, b, d)

# ============================ PROPS ============================
def tree():
    W, H = 190, 230
    d = rad("tl", P["leaf_l"], P["leaf_d"], 0.4, 0.35, 0.7) + lin("tk", P["trunk"], P["trunk_d"])
    b = shadow(95, 214, 58, 14)
    b += f'<rect x="82" y="138" width="26" height="74" rx="11" fill="url(#tk)" {OUTLINE}/>'
    b += f'<circle cx="95" cy="96" r="60" fill="url(#tl)" {OUTLINE}/>'
    b += f'<circle cx="56" cy="116" r="38" fill="url(#tl)" {OUTLINE}/>'
    b += f'<circle cx="134" cy="116" r="38" fill="url(#tl)" {OUTLINE}/>'
    b += f'<circle cx="78" cy="76" r="14" fill="{P["leaf_l"]}" opacity="0.5"/>'
    # a couple of apples
    b += f'<circle cx="70" cy="120" r="6" fill="{P["red"]}"/><circle cx="120" cy="100" r="6" fill="{P["red"]}"/>'
    return svg(W, H, b, d)

def bush():
    W, H = 140, 95
    d = lin("bsh", P["grass"], P["grass_d"])
    b = shadow(70, 84, 52, 10)
    b += f'<circle cx="44" cy="56" r="28" fill="url(#bsh)" {OUTLINE}/>'
    b += f'<circle cx="96" cy="56" r="28" fill="url(#bsh)" {OUTLINE}/>'
    b += f'<circle cx="70" cy="44" r="32" fill="url(#bsh)" {OUTLINE}/>'
    b += f'<circle cx="60" cy="36" r="9" fill="{P["leaf_l"]}" opacity="0.5"/>'
    b += f'<circle cx="50" cy="56" r="4" fill="{P["berry"]}"/><circle cx="88" cy="58" r="4" fill="{P["berry"]}"/>'
    return svg(W, H, b, d)

def fence():
    W, H = 160, 120
    d = lin("fn", P["wood_l"], P["wood_dd"])
    b = ""
    for x in (24, 80, 136):
        b += f'<rect x="{x-9}" y="34" width="18" height="78" rx="6" fill="url(#fn)" {OUTLINE}/>'
        b += f'<path d="M{x-9} 34 l9 -12 l9 12 Z" fill="{P["wood_l"]}" {OUTLINE}/>'
    for y in (52, 84):
        b += f'<rect x="10" y="{y}" width="140" height="12" rx="5" fill="url(#fn)" {OUTLINE}/>'
    return svg(W, H, b, d)

def lantern():
    W, H = 90, 150
    d = rad("lg", P["lantern_glow"], P["lantern"], 0.5, 0.5, 0.7)
    b = shadow(45, 142, 26, 8)
    b += f'<path d="M45 8 q-8 0 -8 10 h16 q0 -10 -8 -10 Z" fill="{P["wood_dd"]}"/>'
    b += f'<rect x="30" y="22" width="30" height="10" rx="4" fill="{P["wood_dd"]}" {OUTLINE}/>'
    b += f'<rect x="28" y="30" width="34" height="70" rx="10" fill="url(#lg)" {OUTLINE}/>'
    b += f'<ellipse cx="45" cy="66" rx="9" ry="16" fill="{P["lantern_glow"]}"/>'
    b += f'<rect x="30" y="98" width="30" height="12" rx="4" fill="{P["wood_dd"]}" {OUTLINE}/>'
    # soft glow halo
    b = f'<circle cx="45" cy="66" r="40" fill="{P["lantern"]}" opacity="0.18"/>' + b
    return svg(W, H, b, d)

# World characters are emitted together so the player, staff, and every customer
# share one transparent chibi sprite language in the runtime scene.
SPRITES = {
    "player": player, "staff": staff,
    "cust_rabbit": guest_rabbit, "cust_raccoon": guest_raccoon,
    "cust_fox": guest_fox, "cust_bear": guest_bear,
    "cust_penguin": guest_penguin, "cust_vipcat": guest_vipcat,
    "grill": grill, "juicer": juicer, "table": table,
    "item_fish": item_fish, "item_grilledfish": item_grilledfish,
    "item_berry": item_berry, "item_juice": item_juice, "money": money,
    "tile_grass": tile_grass, "tile_wood": tile_wood, "tile_path": tile_path,
    "tile_water": tile_water, "tile_berryfield": tile_berryfield,
    "pad_build": lambda: pad("build"), "pad_hire": lambda: pad("hire"),
    "tree": tree, "bush": bush, "fence": fence, "lantern": lantern,
}

def main():
    os.makedirs(OUT, exist_ok=True)
    tmp = os.path.join(os.path.dirname(__file__), "_svg_v2")
    os.makedirs(tmp, exist_ok=True)
    failed = []
    for name, fn in SPRITES.items():
        sp = os.path.join(tmp, name + ".svg")
        png = os.path.join(OUT, name + ".png")
        open(sp, "w").write(fn())
        r = subprocess.run(["rsvg-convert", sp, "-o", png], capture_output=True, text=True)
        if r.returncode != 0:
            failed.append((name, r.stderr.strip())); print(f"  FAIL {name}: {r.stderr.strip()}")
        else:
            print(f"  ok   {name}.png")
    print(f"\n{'FAILED '+str(len(failed)) if failed else 'Done'}: {len(SPRITES)} sprites -> {OUT}")
    if failed: sys.exit(1)

if __name__ == "__main__":
    main()
