"""Render an export comparison from the same source manifests; no Unity writes."""
import csv
from pathlib import Path

import bpy
import numpy as np

base = Path(__file__).resolve().parent
root = base.parents[2]
with (base.parent / "assets.csv").open(encoding="utf-8", newline="") as stream:
    original = list(csv.DictReader(stream))
with (base / "runtime-layout.csv").open(encoding="utf-8", newline="") as stream:
    runtime = list(csv.DictReader(stream))

image = bpy.data.images.load(str(root / "Unity/Assets/Res/Entity/Product/Art/RestaurantAtlas.png"))
pixels = np.empty(image.size[0] * image.size[1] * 4, dtype=np.float32)
image.pixels.foreach_get(pixels)
atlas = pixels.reshape(image.size[1], image.size[0], 4)
cell = 192
sheet = np.zeros((cell * 2, cell * 6, 4), dtype=np.float32)
sheet[:] = (.07, .12, .095, 1)
samples = [
    ("character", "0", "chef_idle", "south"), ("item", "1", "tomato", "north"),
    ("item", "3", "egg", "north"), ("item", "8", "bread", "north"),
    ("station", "1", "station_prep", "south"), ("station", "3", "station_oven", "south"),
]
for column, (category, definition, clip, direction) in enumerate(samples):
    record = next(row for row in original if row["category"] == category and row["definition_or_skin"] == definition and row["facing"] == "2" and row["pose"] == "0")
    index = int(record["index"])
    y, x = divmod(index, 16)
    old = atlas[y * cell:(y + 1) * cell, x * cell:(x + 1) * cell]
    record = next(row for row in runtime if row["clip"] == clip and row["direction"] == direction and row["frame"] == "0")
    render = bpy.data.images.load(str(base / record["frame_file"]))
    values = np.empty(cell * cell * 4, dtype=np.float32)
    render.pixels.foreach_get(values)
    new = values.reshape(cell, cell, 4)
    for row, source in ((1, old), (0, new)):
        destination = sheet[row * cell:(row + 1) * cell, column * cell:(column + 1) * cell]
        destination[:, :, :3] = source[:, :, :3] * source[:, :, 3:] + destination[:, :, :3] * (1 - source[:, :, 3:])
        assert np.count_nonzero(source[:, :, 3] > .1) > 20, record
    bpy.data.images.remove(render)
image = bpy.data.images.new("Coherence export comparison", width=cell * 6, height=cell * 2, alpha=True)
image.pixels.foreach_set(sheet.reshape(-1))
path = base / "review/coherence-style.png"
path.parent.mkdir(exist_ok=True)
image.filepath_raw, image.file_format = str(path), "PNG"
image.save()
print("COHERENCE REVIEW COMPLETE " + str(path))
