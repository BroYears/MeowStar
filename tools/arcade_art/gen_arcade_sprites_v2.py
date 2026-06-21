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
    W, H = 250, 230
    d = (
        lin("woodTop", P["wood_l"], P["wood"]) +
        lin("woodSide", P["wood"], P["wood_dd"]) +
        lin("stone", "#9b9180", "#686056") +
        lin("iron", P["grill_l"], P["grill_d"]) +
        rad("em", P["ember_l"], P["ember"])
    )
    b = shadow(125, 210, 92, 16)
    # Log-cabin prep table frame.
    b += f'<rect x="50" y="116" width="150" height="62" rx="18" fill="url(#woodSide)" {OUTLINE}/>'
    b += f'<ellipse cx="125" cy="116" rx="84" ry="24" fill="url(#woodTop)" {OUTLINE}/>'
    for x in (70, 180):
        b += f'<rect x="{x}" y="158" width="18" height="46" rx="7" fill="{P["wood_dd"]}" {OUTLINE}/>'
    # Stone/iron fire bowl embedded in the wood.
    b += f'<ellipse cx="125" cy="102" rx="64" ry="25" fill="url(#stone)" {OUTLINE}/>'
    b += f'<ellipse cx="125" cy="98" rx="48" ry="17" fill="{P["grill_d"]}"/>'
    for (x, y) in [(101, 98), (125, 101), (149, 98), (113, 105), (137, 105)]:
        b += f'<circle cx="{x}" cy="{y}" r="7" fill="url(#em)"/>'
    for x in (92, 110, 128, 146, 164):
        b += f'<line x1="{x}" y1="78" x2="{x}" y2="112" stroke="url(#iron)" stroke-width="4" opacity="0.9" stroke-linecap="round"/>'
    b += f'<path d="M125 60 q-13 18 0 28 q13 -11 0 -28 Z" fill="{P["ember"]}" {OUTLINE}/>'
    b += f'<path d="M125 68 q-6 9 0 16 q6 -7 0 -16 Z" fill="{P["ember_l"]}"/>'
    # Cabin-kitchen detail: hanging herb hook on one side.
    b += f'<path d="M196 82 q18 16 12 34" stroke="{P["line"]}" stroke-width="3" fill="none" opacity="0.55" stroke-linecap="round"/>'
    b += f'<ellipse cx="204" cy="120" rx="6" ry="13" fill="{P["leaf"]}" opacity="0.9"/>'
    b += f'<ellipse cx="214" cy="122" rx="5" ry="11" fill="{P["leaf_d"]}" opacity="0.9"/>'
    return svg(W, H, b, d)

def juicer():
    W, H = 220, 230
    d = lin("woodTop", P["wood_l"], P["wood"]) + lin("woodSide", P["wood"], P["wood_dd"]) + lin("gls", "#ffffff", "#ffe1f0") + lin("juiceFill", P["juice"], P["juice_d"])
    b = shadow(110, 212, 78, 15)
    # Wooden counter base, matching the kitchen tables.
    b += f'<rect x="38" y="140" width="144" height="52" rx="18" fill="url(#woodSide)" {OUTLINE}/>'
    b += f'<ellipse cx="110" cy="140" rx="72" ry="20" fill="url(#woodTop)" {OUTLINE}/>'
    b += f'<rect x="58" y="178" width="16" height="28" rx="6" fill="{P["wood_dd"]}" {OUTLINE}/>'
    b += f'<rect x="146" y="178" width="16" height="28" rx="6" fill="{P["wood_dd"]}" {OUTLINE}/>'
    # Glass jar sunk into the counter.
    b += f'<path d="M72 64 h76 v60 q0 20 -20 20 h-36 q-20 0 -20 -20 Z" fill="url(#gls)" opacity="0.93" {OUTLINE}/>'
    b += f'<path d="M80 98 h60 v24 q0 14 -14 14 h-32 q-14 0 -14 -14 Z" fill="url(#juiceFill)"/>'
    b += f'<ellipse cx="110" cy="98" rx="30" ry="6" fill="{P["juice_d"]}" opacity="0.6"/>'
    # Cottage hand press.
    b += f'<rect x="101" y="38" width="18" height="32" rx="6" fill="{P["wood_dd"]}" {OUTLINE}/>'
    b += f'<path d="M78 52 h64 q12 0 18 -12" stroke="{P["wood_dd"]}" stroke-width="7" fill="none" stroke-linecap="round"/>'
    for (x, y, c) in [(86, 48, P["berry"]), (108, 42, P["berry_d"]), (130, 49, P["berry"]), (120, 55, P["berry_d"])]:
        b += f'<circle cx="{x}" cy="{y}" r="10" fill="{c}" {OUTLINE}/>'
    b += f'<ellipse cx="108" cy="28" rx="7" ry="3.5" fill="{P["berry_leaf"]}" transform="rotate(-18 108 28)"/>'
    return svg(W, H, b, d)

def table():
    W, H = 250, 190
    d = lin("top", P["wood_l"], P["wood"]) + lin("rim", P["wood"], P["wood_dd"]) + lin("cloth", "#fdf6ea", "#eadcc8")
    b = shadow(125, 168, 96, 16)
    for x in (48, 188):
        b += f'<rect x="{x}" y="100" width="16" height="58" rx="6" fill="{P["wood_dd"]}" {OUTLINE}/>'
    b += f'<ellipse cx="125" cy="103" rx="106" ry="34" fill="url(#rim)" {OUTLINE}/>'
    b += f'<ellipse cx="125" cy="88" rx="106" ry="34" fill="url(#top)" {OUTLINE}/>'
    # Cozy checkered runner.
    b += f'<path d="M58 86 q67 -18 134 0 q-22 18 -67 18 q-45 0 -67 -18 Z" fill="url(#cloth)" opacity="0.95" {OUTLINE}/>'
    for x in (82, 112, 142, 172):
        b += f'<path d="M{x} 72 q-2 16 0 31" stroke="{P["red"]}" stroke-width="2" opacity="0.28"/>'
    for y in (82, 94):
        b += f'<path d="M62 {y} q63 14 126 0" stroke="{P["red"]}" stroke-width="2" opacity="0.28" fill="none"/>'
    # Plate + cutlery makes it read as a restaurant table even when tiny.
    b += f'<ellipse cx="125" cy="84" rx="28" ry="10" fill="{P["cream"]}" {OUTLINE}/>'
    b += f'<ellipse cx="125" cy="82" rx="18" ry="6" fill="#e8d5b0"/>'
    b += f'<path d="M92 78 q-6 10 0 20 M158 76 q5 10 0 22" stroke="{P["line"]}" stroke-width="2.5" fill="none" opacity="0.45" stroke-linecap="round"/>'
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
    d = lin("gg", "#b6d879", P["grass"]) + rad("sun", "#d7ec98", P["grass"], 0.32, 0.22, 0.9)
    b = f'<rect width="128" height="128" fill="url(#gg)"/>'
    b += f'<circle cx="38" cy="30" r="82" fill="url(#sun)" opacity="0.34"/>'
    # Soft leafy clusters, kept sparse so the tile can repeat without looking noisy.
    for (x, y, s, c) in [(18, 28, 1.0, P["grass_d"]), (50, 18, 0.8, P["leaf"]),
                         (90, 36, 1.0, P["grass_d"]), (112, 84, 0.9, P["leaf"]),
                         (34, 98, 1.1, P["grass_d"]), (76, 108, 0.9, P["leaf"]),
                         (18, 70, 0.75, P["leaf_d"]), (104, 18, 0.75, P["leaf_d"])]:
        b += f'<path d="M{x} {y} q{-4*s} {-12*s} 0 {-18*s} q{4*s} {7*s} 0 {18*s} Z" fill="{c}" opacity="0.46"/>'
        b += f'<path d="M{x+6*s} {y+2*s} q{6*s} {-9*s} {10*s} {-14*s} q{-1*s} {7*s} {-10*s} {14*s} Z" fill="{P["grass_l"]}" opacity="0.42"/>'
    # Wildflowers echo the reference art without becoming a UI distraction.
    for (x, y, c) in [(45, 62, "#fff7d8"), (84, 70, "#f7d980"), (26, 46, "#d8e8ff"),
                      (107, 56, "#fff7d8"), (66, 32, "#f7d980"), (70, 98, "#d8e8ff")]:
        b += f'<circle cx="{x}" cy="{y}" r="2.2" fill="{c}" opacity="0.85"/>'
        b += f'<circle cx="{x+2}" cy="{y-1}" r="1.3" fill="#fff" opacity="0.55"/>'
    return svg(128, 128, b, d)

def tile_wood():
    d = lin("wf", "#e7bd82", P["wood"]) + lin("darkGrain", P["wood_d"], P["wood_dd"])
    b = f'<rect width="128" height="128" fill="url(#wf)"/>'
    for i in range(5):
        y = i * 26 - 2
        b += f'<rect x="-2" y="{y}" width="132" height="28" fill="none" stroke="{P["wood_dd"]}" stroke-width="2" opacity="0.45"/>'
        off = 58 if i % 2 else 8
        b += f'<line x1="{off}" y1="{y}" x2="{off}" y2="{y+28}" stroke="{P["wood_dd"]}" stroke-width="2" opacity="0.38"/>'
        for gx in (18 + i * 5, 70 + i * 3):
            b += f'<path d="M{gx} {y+8} q18 5 36 0" stroke="url(#darkGrain)" stroke-width="1.4" fill="none" opacity="0.32" stroke-linecap="round"/>'
            b += f'<path d="M{gx+6} {y+18} q12 -4 26 0" stroke="{P["wood_l"]}" stroke-width="1" fill="none" opacity="0.38" stroke-linecap="round"/>'
    for (x, y) in [(26, 20), (92, 44), (56, 88), (114, 104)]:
        b += f'<circle cx="{x}" cy="{y}" r="3" fill="{P["wood_dd"]}" opacity="0.28"/>'
    return svg(128, 128, b, d)

def tile_path():
    d = lin("pp", "#cfad78", P["soil"]) + rad("wear", "#dec08a", P["soil_d"], 0.45, 0.4, 0.95)
    b = f'<rect width="128" height="128" fill="url(#pp)"/>'
    b += f'<path d="M-10 94 q34 -38 72 -34 q44 4 78 -32 v110 h-150 Z" fill="url(#wear)" opacity="0.42"/>'
    for (x, y, r) in [(30, 40, 5), (80, 30, 4), (100, 90, 6), (40, 100, 4), (64, 64, 5), (20, 80, 3), (112, 44, 3)]:
        b += f'<ellipse cx="{x}" cy="{y}" rx="{r+2}" ry="{r}" fill="{P["soil_d"]}" opacity="0.28"/>'
    for (x, y) in [(18, 24), (58, 28), (96, 64), (28, 112), (118, 104)]:
        b += f'<path d="M{x} {y} q-3 -8 0 -13 q3 5 0 13 Z" fill="{P["grass_d"]}" opacity="0.36"/>'
    return svg(128, 128, b, d)

def tile_water():
    d = lin("wt", "#b7edf3", P["water"]) + rad("deep", P["water_l"], P["water_d"], 0.65, 0.65, 0.9)
    b = f'<rect width="128" height="128" fill="url(#wt)"/>'
    b += f'<circle cx="88" cy="82" r="86" fill="url(#deep)" opacity="0.35"/>'
    for y in (24, 54, 84, 112):
        b += f'<path d="M-8 {y} q18 -8 36 0 t36 0 t36 0 t36 0" fill="none" stroke="#d8fbff" stroke-width="3" opacity="0.58" stroke-linecap="round"/>'
    for y in (38, 70, 100):
        b += f'<path d="M-6 {y} q16 8 32 0 t32 0 t32 0 t32 0" fill="none" stroke="{P["water_d"]}" stroke-width="2" opacity="0.34" stroke-linecap="round"/>'
    for (x, y) in [(30, 28), (102, 48), (66, 92)]:
        b += f'<ellipse cx="{x}" cy="{y}" rx="11" ry="3" fill="#ffffff" opacity="0.23"/>'
    return svg(128, 128, b, d)

def tile_berryfield():
    d = lin("bf", "#8fc45a", P["grass_d"])
    b = f'<rect width="128" height="128" fill="url(#bf)"/>'
    for (x, y) in [(32, 40), (88, 36), (48, 92), (96, 96), (20, 80), (118, 66)]:
        b += (f'<ellipse cx="{x}" cy="{y+6}" rx="16" ry="6" fill="{P["shadow"]}" opacity="0.12"/>'
              f'<circle cx="{x}" cy="{y}" r="13" fill="{P["berry_leaf"]}" {OUTLINE}/>'
              f'<circle cx="{x-4}" cy="{y-2}" r="3.5" fill="{P["berry"]}"/>'
              f'<circle cx="{x+5}" cy="{y+1}" r="3.5" fill="{P["berry"]}"/>'
              f'<circle cx="{x}" cy="{y+5}" r="3.5" fill="{P["berry_d"]}"/>')
    for (x, y) in [(64, 26), (72, 68), (26, 108), (108, 114)]:
        b += f'<circle cx="{x}" cy="{y}" r="2" fill="#fff7d8" opacity="0.7"/>'
    return svg(128, 128, b, d)

# ============================ PADS ============================
def pad(kind):
    W, H = 210, 140
    if kind == "build":
        c0, c1 = P["coin"], P["coin_d"]
        icon = "hammer"
    else:
        c0, c1 = "#6fa8e8", "#4a86d0"
        icon = "paw"
    d = rad("pad", c0, c1) + lin("sign", P["wood_l"], P["wood_dd"])
    b = f'<ellipse cx="105" cy="86" rx="94" ry="42" fill="url(#pad)" opacity="0.42"/>'
    b += f'<ellipse cx="105" cy="86" rx="94" ry="42" fill="none" stroke="{c1}" stroke-width="4" stroke-dasharray="14 10" opacity="0.86"/>'
    b += f'<rect x="50" y="22" width="110" height="48" rx="12" fill="url(#sign)" {OUTLINE}/>'
    b += f'<rect x="70" y="66" width="12" height="32" rx="4" fill="{P["wood_dd"]}" {OUTLINE}/>'
    b += f'<rect x="128" y="66" width="12" height="32" rx="4" fill="{P["wood_dd"]}" {OUTLINE}/>'
    if icon == "hammer":
        b += f'<path d="M86 48 l28 -20 l10 10 l-30 20 Z" fill="{c0}" {OUTLINE}/>'
        b += f'<path d="M114 54 l22 22" stroke="{P["cream"]}" stroke-width="7" fill="none" stroke-linecap="round"/>'
    else:
        b += f'<circle cx="105" cy="48" r="12" fill="{c0}" {OUTLINE}/>'
        for (x, y) in [(88, 42), (97, 34), (113, 34), (122, 42)]:
            b += f'<circle cx="{x}" cy="{y}" r="5" fill="{c0}" {OUTLINE}/>'
    return svg(W, H, b, d)

# ============================ PROPS ============================
def tree():
    W, H = 210, 250
    d = rad("tl", "#b9d86f", P["leaf_d"], 0.4, 0.34, 0.75) + lin("tk", P["trunk"], P["trunk_d"]) + lin("autumn", "#f0b35a", "#c98235")
    b = shadow(105, 232, 62, 14)
    b += f'<path d="M94 142 q-12 34 -14 82 h36 q-2 -48 -14 -82 Z" fill="url(#tk)" {OUTLINE}/>'
    b += f'<path d="M100 154 q-26 -30 -50 -42 M110 154 q34 -28 56 -48" stroke="{P["trunk_d"]}" stroke-width="8" fill="none" stroke-linecap="round"/>'
    for (x, y, r, c) in [(104, 78, 58, "url(#tl)"), (58, 112, 39, "url(#tl)"),
                         (148, 112, 39, "url(#tl)"), (84, 56, 28, "url(#tl)"),
                         (132, 60, 30, "url(#tl)"), (68, 92, 22, "url(#autumn)"),
                         (158, 86, 20, "url(#autumn)")]:
        b += f'<circle cx="{x}" cy="{y}" r="{r}" fill="{c}" {OUTLINE}/>'
    for (x, y, c) in [(80, 78, P["grass_l"]), (122, 96, P["grass_l"]), (52, 126, "#f3c66c"), (150, 126, "#e49b4a")]:
        b += f'<path d="M{x} {y} q8 -12 20 -6 q-9 10 -20 6 Z" fill="{c}" opacity="0.65"/>'
    return svg(W, H, b, d)

def bush():
    W, H = 160, 110
    d = lin("bsh", "#96c95e", P["grass_d"])
    b = shadow(80, 96, 58, 10)
    for (x, y, r) in [(44, 66, 30), (82, 52, 36), (118, 68, 30), (68, 76, 28), (102, 78, 26)]:
        b += f'<circle cx="{x}" cy="{y}" r="{r}" fill="url(#bsh)" {OUTLINE}/>'
    for (x, y, c) in [(58, 48, P["leaf_l"]), (90, 38, P["leaf_l"]), (120, 56, P["leaf"]), (40, 78, P["leaf_d"])]:
        b += f'<path d="M{x} {y} q8 -12 20 -6 q-8 11 -20 6 Z" fill="{c}" opacity="0.65"/>'
    for (x, y, c) in [(52, 64, P["berry"]), (84, 70, P["berry_d"]), (112, 72, P["berry"]), (74, 90, "#fff7d8"), (126, 88, "#d8e8ff")]:
        b += f'<circle cx="{x}" cy="{y}" r="4" fill="{c}" opacity="0.88"/>'
    return svg(W, H, b, d)

def fence():
    W, H = 180, 130
    d = lin("fn", P["wood_l"], P["wood_dd"])
    b = shadow(90, 120, 74, 8, 0.12)
    for x in (26, 90, 154):
        b += f'<rect x="{x-10}" y="38" width="20" height="82" rx="7" fill="url(#fn)" {OUTLINE}/>'
        b += f'<path d="M{x-10} 38 l10 -14 l10 14 Z" fill="{P["wood_l"]}" {OUTLINE}/>'
        b += f'<path d="M{x-4} 54 q4 18 0 38" stroke="{P["wood_dd"]}" stroke-width="1.4" fill="none" opacity="0.35"/>'
    for y in (56, 90):
        b += f'<rect x="8" y="{y}" width="164" height="13" rx="6" fill="url(#fn)" {OUTLINE}/>'
    # Leaves crawling on the fence, tying it back to the forest kitchen.
    b += f'<path d="M28 88 q32 -28 72 -18 q26 6 48 -12" stroke="{P["leaf_d"]}" stroke-width="3" fill="none" opacity="0.65"/>'
    for (x, y) in [(48, 76), (76, 68), (114, 68), (138, 60)]:
        b += f'<ellipse cx="{x}" cy="{y}" rx="7" ry="4" fill="{P["leaf"]}" transform="rotate(-24 {x} {y})"/>'
    return svg(W, H, b, d)

def lantern():
    W, H = 100, 165
    d = rad("lg", P["lantern_glow"], P["lantern"], 0.5, 0.5, 0.7)
    b = shadow(50, 154, 28, 8)
    b += f'<path d="M50 10 q-10 0 -10 12 h20 q0 -12 -10 -12 Z" fill="{P["wood_dd"]}"/>'
    b += f'<path d="M28 26 q22 -18 44 0" stroke="{P["wood_dd"]}" stroke-width="5" fill="none" stroke-linecap="round"/>'
    b += f'<rect x="33" y="32" width="34" height="11" rx="4" fill="{P["wood_dd"]}" {OUTLINE}/>'
    b += f'<rect x="30" y="42" width="40" height="76" rx="12" fill="url(#lg)" {OUTLINE}/>'
    b += f'<ellipse cx="50" cy="80" rx="10" ry="18" fill="{P["lantern_glow"]}"/>'
    b += f'<path d="M50 64 q-8 12 0 24 q8 -12 0 -24 Z" fill="#fff7d8" opacity="0.7"/>'
    b += f'<rect x="33" y="116" width="34" height="13" rx="4" fill="{P["wood_dd"]}" {OUTLINE}/>'
    b += f'<rect x="46" y="128" width="8" height="24" rx="3" fill="{P["wood_dd"]}" {OUTLINE}/>'
    # soft glow halo
    b = f'<circle cx="50" cy="80" r="48" fill="{P["lantern"]}" opacity="0.18"/>' + b
    return svg(W, H, b, d)

def wall():
    W, H = 180, 150
    d = lin("woodTop", P["wood_l"], P["wood"]) + lin("woodSide", P["wood"], P["wood_dd"])
    b = shadow(90, 140, 74, 8, 0.12)
    # Stacked horizontal logs
    for i in range(5):
        y = i * 28 + 10
        b += f'<rect x="10" y="{y}" width="160" height="30" rx="8" fill="url(#woodSide)" {OUTLINE}/>'
        b += f'<rect x="15" y="{y+2}" width="150" height="8" rx="3" fill="url(#woodTop)" opacity="0.3"/>'
    # Small window in the middle of the wall with warm glow
    b += f'<rect x="65" y="32" width="50" height="42" rx="6" fill="{P["wood_dd"]}" {OUTLINE}/>'
    b += f'<rect x="70" y="37" width="40" height="32" rx="3" fill="{P["lantern_glow"]}"/>'
    b += f'<line x1="90" y1="37" x2="90" y2="69" stroke="{P["wood_dd"]}" stroke-width="3"/>'
    b += f'<line x1="70" y1="53" x2="110" y2="53" stroke="{P["wood_dd"]}" stroke-width="3"/>'
    # Warm light beam emitting down from window
    b = f'<polygon points="70,53 40,140 140,140 110,53" fill="{P["lantern"]}" opacity="0.1"/>' + b
    return svg(W, H, b, d)

def carpet():
    W, H = 210, 140
    d = lin("cp", P["red"], P["red_d"])
    # Flat decorative rug (oval shape)
    b = f'<ellipse cx="105" cy="70" rx="96" ry="52" fill="url(#cp)" {OUTLINE}/>'
    b += f'<ellipse cx="105" cy="70" rx="82" ry="42" fill="none" stroke="{P["cream"]}" stroke-dasharray="8 6" stroke-width="3" opacity="0.8"/>'
    b += f'<ellipse cx="105" cy="70" rx="42" ry="22" fill="none" stroke="{P["cream"]}" stroke-width="2.5" opacity="0.6"/>'
    return svg(W, H, b, d)

def fireplace():
    W, H = 180, 200
    d = lin("st", "#8a8075", "#5c544c") + rad("fire", P["ember_l"], P["ember"])
    b = shadow(90, 190, 78, 10, 0.15)
    # Chimney body (stone blocks)
    b += f'<rect x="35" y="10" width="110" height="170" rx="12" fill="url(#st)" {OUTLINE}/>'
    # Hearth mantel
    b += f'<rect x="25" y="120" width="130" height="18" rx="5" fill="{P["wood_dd"]}" {OUTLINE}/>'
    # Fireplace opening
    b += f'<path d="M50 180 v-40 q0 -12 12 -12 h56 q12 0 12 12 v40 Z" fill="#2d2822" {OUTLINE}/>'
    # Burning wood and embers inside
    b += f'<rect x="68" y="166" width="44" height="10" rx="3" fill="{P["wood_dd"]}" transform="rotate(15 90 171)"/>'
    b += f'<rect x="68" y="166" width="44" height="10" rx="3" fill="{P["wood_dd"]}" transform="rotate(-15 90 171)"/>'
    for (x, y, r) in [(78, 164, 11), (90, 156, 14), (102, 164, 11), (90, 168, 8)]:
        b += f'<circle cx="{x}" cy="{y}" r="{r}" fill="url(#fire)"/>'
    # Sparks rising
    b += f'<circle cx="82" cy="138" r="3" fill="{P["ember_l"]}"/><circle cx="100" cy="142" r="2.5" fill="{P["ember_l"]}"/><circle cx="92" cy="128" r="2" fill="{P["ember_l"]}"/>'
    return svg(W, H, b, d)

def counter():
    W, H = 200, 160
    d = lin("cTop", P["wood_l"], P["wood"]) + lin("cSide", P["wood"], P["wood_dd"])
    b = shadow(100, 148, 84, 12, 0.15)
    # Log counter body
    b += f'<rect x="25" y="58" width="150" height="82" rx="16" fill="url(#cSide)" {OUTLINE}/>'
    b += f'<ellipse cx="100" cy="58" rx="80" ry="20" fill="url(#cTop)" {OUTLINE}/>'
    # Little service bell on top
    b += f'<path d="M90 48 q10 -14 20 0 Z" fill="{P["buckle"]}" {OUTLINE}/>'
    b += f'<circle cx="100" cy="38" r="3" fill="{P["buckle"]}"/>'
    # Small notebook / register
    b += f'<polygon points="46,54 74,48 84,60 56,66" fill="{P["cream"]}" {OUTLINE}/>'
    b += f'<line x1="58" y1="53" x2="70" y2="50" stroke="{P["line"]}" stroke-width="2"/>'
    return svg(W, H, b, d)

def plant():
    W, H = 120, 180
    d = lin("pot", "#d9825b", "#9c5132") + lin("lf", "#7ebb55", "#4a8a2a")
    b = shadow(60, 170, 36, 8, 0.15)
    # Clay pot
    b += f'<rect x="42" y="128" width="36" height="42" rx="4" fill="url(#pot)" {OUTLINE}/>'
    b += f'<rect x="36" y="122" width="48" height="10" rx="3" fill="{P["wood"]}" {OUTLINE}/>'
    # Lush tropical leaves
    for (cx, cy, rx, ry, rot) in [(60, 96, 26, 44, 0), (46, 102, 22, 38, -32),
                                  (74, 102, 22, 38, 32), (32, 114, 18, 34, -62),
                                  (88, 114, 18, 34, 62)]:
        b += f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="url(#lf)" {OUTLINE} transform="rotate({rot} {cx} {cy})"/>'
        # Leaf rib detail
        b += f'<path d="M{cx} {cy+ry*0.7} Q{cx} {cy} {cx} {cy-ry*0.7}" stroke="{P["leaf_l"]}" stroke-width="2" fill="none" opacity="0.6" transform="rotate({rot} {cx} {cy})"/>'
    return svg(W, H, b, d)

def soup_pot():
    W, H = 230, 220
    d = lin("potSteel", P["grill_steel"], P["grill_d"]) + lin("soupFill", "#8f623e", "#5c3d25") + rad("fireGlow", P["ember_l"], P["ember"])
    b = shadow(115, 204, 82, 14, 0.15)
    # Stone campfire stand
    b += f'<rect x="45" y="148" width="140" height="50" rx="14" fill="#6b6257" {OUTLINE}/>'
    # Red embers underneath
    for (x, y) in [(80, 168), (115, 172), (150, 168), (100, 180), (130, 180)]:
        b += f'<circle cx="{x}" cy="{y}" r="8" fill="url(#fireGlow)"/>'
    # Large black iron cauldron (pot)
    b += f'<path d="M55 76 h120 c20 0 25 78 0 78 h-120 c-25 0 -20 -78 0 -78 Z" fill="url(#potSteel)" {OUTLINE}/>'
    # Outer rim
    b += f'<ellipse cx="115" cy="76" rx="60" ry="16" fill="url(#potSteel)" {OUTLINE}/>'
    # Soup surface
    b += f'<ellipse cx="115" cy="76" rx="52" ry="12" fill="url(#soupFill)"/>'
    # Bubbles on soup
    for (x, y, r) in [(84, 76, 5), (128, 73, 6), (144, 78, 4), (106, 79, 5)]:
        b += f'<circle cx="{x}" cy="{y}" r="{r}" fill="#baa698" opacity="0.4" stroke="{P["line"]}" stroke-width="1"/>'
    # Handles on the pot
    b += f'<path d="M48 94 q-18 -10 -12 -28" fill="none" stroke="url(#potSteel)" stroke-width="5" stroke-linecap="round"/>'
    b += f'<path d="M182 94 q18 -10 12 -28" fill="none" stroke="url(#potSteel)" stroke-width="5" stroke-linecap="round"/>'
    return svg(W, H, b, d)

def item_mushroom():
    W, H = 100, 100
    d = rad("mshCap", "#c55ecf", "#7c3585") + lin("mshStem", P["white"], P["white_sh"])
    b = shadow(50, 88, 30, 6)
    # First mushroom (smaller, rotated)
    b += f'<path d="M32 78 q-10 -14 -12 -26 q24 -6 28 8 Z" fill="url(#mshStem)" {OUTLINE}/>'
    b += f'<ellipse cx="22" cy="50" rx="18" ry="12" fill="url(#mshCap)" {OUTLINE} transform="rotate(-15 22 50)"/>'
    # Second mushroom (larger, main)
    b += f'<path d="M56 82 q0 -22 -14 -32 q28 -6 32 14 Z" fill="url(#mshStem)" {OUTLINE}/>'
    b += f'<ellipse cx="64" cy="46" rx="24" ry="16" fill="url(#mshCap)" {OUTLINE} transform="rotate(10 64 46)"/>'
    # Spots on cap
    b += f'<circle cx="56" cy="40" r="3.5" fill="{P["white"]}" opacity="0.85"/>'
    b += f'<circle cx="72" cy="48" r="4.5" fill="{P["white"]}" opacity="0.85"/>'
    b += f'<circle cx="18" cy="48" r="3" fill="{P["white"]}" opacity="0.85"/>'
    return svg(W, H, b, d)

def item_soup():
    W, H = 100, 100
    d = lin("spBowl", P["wood_l"], P["wood_dd"]) + lin("spSurf", "#8f623e", "#5c3d25")
    b = shadow(50, 88, 34, 7)
    # Wooden bowl
    b += f'<path d="M22 46 h56 q6 34 -28 34 q-34 0 -28 -34 Z" fill="url(#spBowl)" {OUTLINE}/>'
    b += f'<ellipse cx="50" cy="46" rx="28" ry="8" fill="url(#spBowl)" {OUTLINE}/>'
    # Soup inside
    b += f'<ellipse cx="50" cy="46" rx="24" ry="6" fill="url(#spSurf)"/>'
    # Spoon in bowl
    b += f'<line x1="68" y1="22" x2="52" y2="48" stroke="{P["white"]}" stroke-width="4" stroke-linecap="round"/>'
    b += f'<ellipse cx="52" cy="48" rx="6" ry="4" fill="{P["white"]}" transform="rotate(30 52 48)"/>'
    # Garnish green leaf float
    b += f'<ellipse cx="42" cy="46" rx="3" ry="5" fill="{P["leaf"]}" transform="rotate(-30 42 46)"/>'
    return svg(W, H, b, d)

def tile_cave():
    d = lin("cv", "#3f3047", "#271c2c") + rad("glow", "#674873", "#271c2c", 0.4, 0.4, 0.95)
    b = f'<rect width="128" height="128" fill="url(#cv)"/>'
    b += f'<circle cx="50" cy="50" r="76" fill="url(#glow)" opacity="0.45"/>'
    # Small cave stones and sparkling crystal details
    for (x, y, r, c) in [(18, 32, 4, "#503e59"), (92, 24, 6, "#1f1424"),
                         (38, 98, 5, "#1f1424"), (112, 104, 4, "#503e59")]:
        b += f'<ellipse cx="{x}" cy="{y}" rx="{r+1}" ry="{r}" fill="{c}" opacity="0.6"/>'
    # Sparkling crystal shards (glistening yellow-purple)
    for (x, y) in [(56, 42), (78, 86), (28, 70), (102, 60)]:
        b += f'<polygon points="{x},{y-6} {x+3},{y} {x},{y+6} {x-3},{y}" fill="#d59bf2" opacity="0.8"/>'
        b += f'<polygon points="{x},{y-3} {x+1.5},{y} {x},{y+3} {x-1.5},{y}" fill="#fff"/>'
    return svg(128, 128, b, d)

# Player and staff now use PDF-concept cutouts directly in Resources/Arcade.
# Keep this generator focused on the remaining SVG-native arcade sprites so a
# refresh does not overwrite the higher-fidelity character art.
SPRITES = {
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
    "wall": wall, "carpet": carpet, "fireplace": fireplace,
    "counter": counter, "plant": plant, "soup_pot": soup_pot,
    "item_mushroom": item_mushroom, "item_soup": item_soup,
    "tile_cave": tile_cave,
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
