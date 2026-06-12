import sys, os
from rembg import remove, new_session
from PIL import Image

# u2net = general; isnet-general-use often crisper for illustrations
session = new_session("isnet-general-use")

def cutout(src, dst):
    img = Image.open(src).convert("RGBA")
    res = remove(img, session=session,
                 alpha_matting=True,
                 alpha_matting_foreground_threshold=240,
                 alpha_matting_background_threshold=15,
                 alpha_matting_erode_size=8)
    # crop to content bounding box so the sprite is tight
    bbox = res.getbbox()
    if bbox:
        res = res.crop(bbox)
    res.save(dst)
    print(f"  {os.path.basename(src)} -> {os.path.basename(dst)}  {res.size}")

jobs = [
    ("design/character/chef_nyastar_calico_1780279886144.png", "out_sprites/chef_nyastar.png"),
    ("design/character/baby_nyastar_calico_1780281940948.png", "out_sprites/baby_nyastar.png"),
    ("design/customer/rabbit_guest_1780280226323.png",          "out_sprites/customer_rabbit.png"),
    ("design/customer/bear_guest_1780280252649.png",            "out_sprites/customer_bear.png"),
]
print("배경 제거 시작...")
for s, d in jobs:
    cutout(s, d)
print("완료")
