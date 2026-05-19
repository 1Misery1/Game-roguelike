"""
从角色参考图中提取干净的游戏精灵，并同步更新 Unity .meta 文件。
流程：
  1. 黑色背景变透明（阈值 25，保留深色甲胄）
  2. 行密度分析找角色带（跳过底部武器/细节区域）
  3. 在角色带内按列密度分割各站立姿态
  4. 拼成横向精灵表并覆盖保存
  5. 更新对应 .meta：Multiple 模式，写入精确的切割坐标
"""
import numpy as np
from PIL import Image
from pathlib import Path
import re, sys, uuid

RESOURCES_DIR = Path("F:/Game/Assets/Resources")
BG_THRESHOLD  = 25     # R+G+B < 75 → 透明（保留深色甲胄）
GAP_COL       = 3      # 列间容差（像素）
MIN_CHAR_W    = 22
MIN_CHAR_H    = 25
MIN_CHAR_AREA = 200
LARGE_SKIP    = {"Commander", "Necromancer", "PoisonShaman", "Witch"}
TOP_NORM      = 0.10
TOP_LARGE     = 0.28


# ── 图像处理 ──────────────────────────────────────────────────────────────────

def load_no_bg(path: Path) -> np.ndarray:
    img = Image.open(path).convert("RGBA")
    d = np.array(img, dtype=np.uint8)
    brightness = d[:,:,0].astype(int) + d[:,:,1].astype(int) + d[:,:,2].astype(int)
    d[brightness < BG_THRESHOLD * 3, 3] = 0
    return d


def first_content_band(alpha: np.ndarray, top: int) -> tuple:
    h, w = alpha.shape
    row_den = np.sum(alpha[top:] > 0, axis=1)
    thresh = max(6, w * 0.07)
    bands, in_b, gap, b0 = [], False, 0, 0
    for i, v in enumerate(row_den):
        if v >= thresh:
            if not in_b: b0, in_b = i, True
            gap = 0
        else:
            if in_b:
                gap += 1
                if gap > 5:
                    bands.append((b0 + top, i - gap + top))
                    in_b = False
    if in_b:
        bands.append((b0 + top, len(row_den) - 1 + top))
    for by0, by1 in bands:
        if by1 - by0 >= 35:
            return by0, by1
    return top, h - 1


def col_runs(density: np.ndarray, min_v: int = 3, gap: int = GAP_COL):
    runs, in_r, g, xs = [], False, 0, 0
    for x, v in enumerate(density):
        if v >= min_v:
            if not in_r: xs, in_r = x, True
            g = 0
        else:
            if in_r:
                g += 1
                if g > gap:
                    runs.append((xs, x - g)); in_r = False
    if in_r: runs.append((xs, len(density) - 1))
    return runs


def find_bboxes(data: np.ndarray, char_name: str) -> list:
    h, w = data.shape[:2]
    alpha = data[:, :, 3]
    top = int(h * (TOP_LARGE if char_name in LARGE_SKIP else TOP_NORM))
    by0, by1 = first_content_band(alpha, top)
    if by1 - by0 < MIN_CHAR_H:
        return []
    band = alpha[by0:by1 + 1, :]
    col_den = np.sum(band > 0, axis=0)
    segs = col_runs(col_den, min_v=3, gap=GAP_COL)
    bboxes = []
    for sx, ex in segs:
        if ex - sx < MIN_CHAR_W: continue
        seg = band[:, sx:ex + 1]
        rows = np.where(np.any(seg > 0, axis=1))[0]
        if not rows.size: continue
        sy, ey = int(rows[0]) + by0, int(rows[-1]) + by0
        bw, bh = ex - sx, ey - sy
        area = int(np.sum(alpha[sy:ey + 1, sx:ex + 1] > 0))
        if bw > bh * 1.6 and bw > 55:
            inner = np.sum(band[:, sx:ex + 1] > 0, axis=0)
            sub = col_runs(inner, min_v=2, gap=1)
            if len(sub) > 1:
                for ix, iex in sub:
                    ax0, ax1 = sx + ix, sx + iex
                    if ax1 - ax0 < MIN_CHAR_W: continue
                    ib = band[:, ax0:ax1 + 1]
                    ir = np.where(np.any(ib > 0, axis=1))[0]
                    if not ir.size: continue
                    ia = int(np.sum(alpha[int(ir[0])+by0:int(ir[-1])+by0+1, ax0:ax1+1] > 0))
                    bboxes.append((ax0, int(ir[0])+by0, ax1, int(ir[-1])+by0, ia))
                continue
        bboxes.append((sx, sy, ex, ey, area))
    if not bboxes: return []
    max_h = max(ey - sy for _, sy, _, ey, _ in bboxes)
    result = []
    for sx, sy, ex, ey, area in bboxes:
        bw, bh = ex - sx, ey - sy
        if (bh < max_h * 0.60 or bh < MIN_CHAR_H or bw < MIN_CHAR_W
                or area < MIN_CHAR_AREA or (bw > 0 and bh / bw < 0.5)):
            continue
        result.append((sx, sy, ex, ey))
    result.sort(key=lambda b: b[0])
    return result


def crop_sprite(data: np.ndarray, bbox, pad: int = 2) -> Image.Image:
    H, W = data.shape[:2]
    x0, y0, x1, y1 = bbox
    return Image.fromarray(data[max(0,y0-pad):min(H,y1+pad+1), max(0,x0-pad):min(W,x1+pad+1)])


def build_sheet(sprites: list) -> tuple:
    cw = ((max(s.width  for s in sprites) + 7) // 8) * 8
    ch = ((max(s.height for s in sprites) + 7) // 8) * 8
    sheet = Image.new("RGBA", (cw * len(sprites), ch), (0,0,0,0))
    for i, spr in enumerate(sprites):
        ox = i*cw + (cw-spr.width)//2
        oy =        (ch-spr.height)//2
        sheet.paste(spr, (ox, oy), spr)
    return sheet, cw, ch


# ── .meta 更新 ────────────────────────────────────────────────────────────────

def update_meta(meta_path: Path, char_name: str, n: int, cw: int, ch: int):
    if not meta_path.exists():
        return
    text = meta_path.read_text(encoding="utf-8")

    # 基础导入设置
    text = re.sub(r'textureType: \d+',        'textureType: 8',   text)
    text = re.sub(r'spriteMode: \d+',         'spriteMode: 2',    text)
    text = re.sub(r'filterMode: \d+',         'filterMode: 0',    text)
    text = re.sub(r'enableMipMap: \d+',       'enableMipMap: 0',  text)
    text = re.sub(r'alphaIsTransparency: \d+','alphaIsTransparency: 1', text)
    text = re.sub(r'isReadable: \d+',         'isReadable: 1',    text)
    text = re.sub(r'spritePixelsToUnits: \d+','spritePixelsToUnits: 32', text)

    # 构建精确的 sprite 切割条目
    entries = []
    for i in range(n):
        x_pos = i * cw
        sid = uuid.uuid4().hex[:32]
        entries.append(
            f"  - serializedVersion: 2\n"
            f"    name: {char_name}_{i:02d}\n"
            f"    rect:\n"
            f"      serializedVersion: 2\n"
            f"      x: {x_pos}\n"
            f"      y: 0\n"
            f"      width: {cw}\n"
            f"      height: {ch}\n"
            f"    alignment: 0\n"
            f"    pivot: {{x: 0.5, y: 0.5}}\n"
            f"    border: {{x: 0, y: 0, z: 0, w: 0}}\n"
            f"    outline: []\n"
            f"    physicsShape: []\n"
            f"    tessellationDetail: -1\n"
            f"    bones: []\n"
            f"    spriteID: {sid}\n"
            f"    internalID: {i + 1}\n"
            f"    vertices: []\n"
            f"    indices: \n"
            f"    edges: []\n"
            f"    weights: []"
        )
    sprites_yaml = "  sprites:\n" + "\n".join(entries)
    text = re.sub(r'  sprites: \[\]', sprites_yaml, text)
    meta_path.write_text(text, encoding="utf-8")


# ── 入口 ──────────────────────────────────────────────────────────────────────

def process_one(path: Path, dry: bool = False) -> int:
    name = path.parent.name
    data = load_no_bg(path)
    bboxes = find_bboxes(data, name)
    if not bboxes:
        print(f"  [SKIP] 未检测到: {name}")
        return 0
    sprites = [crop_sprite(data, bb) for bb in bboxes]
    sheet, cw, ch = build_sheet(sprites)
    poses = ["正面", "背面", "侧面", "侧面2"]
    print(f"  {name}: {len(sprites)} 姿态 | 格 {cw}x{ch} | 表 {sheet.width}x{sheet.height}")
    for i, (bb, spr) in enumerate(zip(bboxes, sprites)):
        label = poses[i] if i < len(poses) else f"姿{i}"
        print(f"    [{label}] {spr.width}x{spr.height}  bbox=({bb[0]},{bb[1]})-({bb[2]},{bb[3]})")
    if not dry:
        sheet.save(path)
        update_meta(Path(str(path) + ".meta"), name, len(sprites), cw, ch)
    return len(sprites)


dry    = "--dry" in sys.argv
target = next((a for a in sys.argv[1:] if not a.startswith("--")), None)
sheets = sorted(RESOURCES_DIR.rglob("sprite_sheet.png"))
sheets = [p for p in sheets if "_FullSheet" not in str(p)]
if target:
    sheets = [p for p in sheets if target.lower() in p.parent.name.lower()]

total = 0
for p in sheets:
    print(f"\n{p.parent.relative_to(RESOURCES_DIR)}")
    total += process_one(p, dry)

print(f"\n完成：提取 {total} 个精灵，处理 {len(sheets)} 张图")
