"""Render the Markdown ASCII library as white ASCII text on fully transparent PNG backgrounds.

Requires Pillow. Run from any directory; paths resolve relative to this file.
"""
from pathlib import Path
import json
import re
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'art' / 'ASCII_SPRITES.md'
OUTPUT = ROOT / 'art' / 'sprites' / 'white-ascii'
SCALE = 3
CELL_W, CELL_H, PAD = 20, 38, 10
SIZE = (16 * CELL_W + 2 * PAD, 6 * CELL_H + 2 * PAD)
FONT_PATH = Path('C:/Windows/Fonts/consola.ttf')


def render(rows):
    size = tuple(v * SCALE for v in SIZE)
    frame = Image.new('RGBA', size, (255, 255, 255, 0))
    ink = ImageDraw.Draw(frame)
    font = ImageFont.truetype(str(FONT_PATH), 32 * SCALE)
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch != ' ':
                ink.text(((PAD + x * CELL_W) * SCALE,
                          (PAD + y * CELL_H + 28) * SCALE), ch,
                         font=font, fill=(255, 255, 255, 255), anchor='ls')
    alpha = frame.getchannel('A').resize(SIZE, Image.Resampling.LANCZOS)
    result = Image.new('RGBA', SIZE, (255, 255, 255, 0))
    result.putalpha(alpha)
    return result


def main():
    text = SOURCE.read_text(encoding='utf-8')
    clips = re.findall(r'^### ([a-z_]+)\n(.*?)(?=^### |\Z)', text, re.M | re.S)
    manifest = {'name': 'White ASCII (transparent)', 'source': 'art/ASCII_SPRITES.md', 'canvas': list(SIZE),
                'cell': [CELL_W, CELL_H], 'padding': PAD,
                'support_anchor': [PAD + 8 * CELL_W, PAD + 6 * CELL_H],
                'font': 'Consolas', 'font_size': 32,
                'style': 'white ASCII text, transparent background, no silhouette backing',
                'clips': {}}
    previews = []
    count = 0
    for name, content in clips:
        metadata = re.search(r'scope: (\S+) \| loop: (true|false) \| frame_ms: ([\d, ]+)', content)
        assert metadata, name
        times = [int(v) for v in metadata[3].split(',')]
        blocks = re.findall(r'^```text\n(.*?)^```', content, re.M | re.S)
        assert len(times) == len(blocks), name
        folder = OUTPUT / name
        folder.mkdir(parents=True, exist_ok=True)
        frames = []
        for i, (block, duration) in enumerate(zip(blocks, times), 1):
            rows = block.removesuffix('\n').split('\n')
            assert len(rows) == 6 and all(len(r) <= 16 for r in rows), (name, i)
            frame = render(rows)
            path = folder / f'{name}_{i:02d}.png'
            frame.save(path)
            with Image.open(path) as check:
                assert check.mode == 'RGBA' and check.size == SIZE
                assert check.getchannel('A').getextrema() == (0, 255)
                assert all(channel.getextrema() == (255, 255)
                           for channel in check.convert('RGB').split())
            frames.append({'file': path.relative_to(OUTPUT).as_posix(), 'duration_ms': duration})
            previews.append((f'{name} {i:02d}', frame))
            count += 1
        manifest['clips'][name] = {'scope': metadata[1], 'loop': metadata[2] == 'true', 'frames': frames}
    (OUTPUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    # A separate review sheet shows the transparency against a muted background.
    cols, tw, th = 6, 204, 176
    sheet = Image.new('RGB', (cols * tw, ((len(previews)+cols-1)//cols)*th), '#24303b')
    sd = ImageDraw.Draw(sheet)
    label_font = ImageFont.truetype(str(FONT_PATH), 13)
    for n, (label, frame) in enumerate(previews):
        x, y = (n % cols) * tw, (n // cols) * th
        small = frame.resize((204, 149), Image.Resampling.LANCZOS)
        sheet.paste(small, (x, y), small)
        sd.text((x + 6, y + 151), label, font=label_font, fill='#ffffff')
    sheet.save(OUTPUT / 'contact-sheet.png')
    print(f'Rendered {count} frames in {len(clips)} clips to {OUTPUT}')


if __name__ == '__main__':
    main()
