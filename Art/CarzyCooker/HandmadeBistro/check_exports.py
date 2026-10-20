"""Read-only contract check, executed by Blender's bundled Python / numpy."""
import csv
from collections import defaultdict
from pathlib import Path

import bpy
import numpy as np

base = Path(__file__).resolve().parent
with (base / "clips.csv").open(encoding="utf-8", newline="") as stream:
    rows = list(csv.DictReader(stream))
groups = defaultdict(list)
for row in rows:
    groups[(row["clip"], row["direction"])].append(row)

actions = ("idle", "walk", "carry", "chop", "serve")
for action in actions:
    for direction in ("north", "east", "south", "west"):
        assert len(groups[("chef_" + action, direction)]) == 8
for effect in ("soup_loop", "juice_loop", "steam_loop"):
    assert len(groups[(effect, "front")]) == 8
assert len(rows) == 203


def pixels(path):
    image = bpy.data.images.load(str(path), check_existing=False)
    size = tuple(image.size)
    data = np.empty(size[0] * size[1] * 4, dtype=np.float32)
    image.pixels.foreach_get(data)
    bpy.data.images.remove(image)
    return size, data.reshape(size[1], size[0], 4)


sheet_cache = {}
for (clip, direction), group in groups.items():
    sequence = []
    pivots = set()
    for row in group:
        size, frame = pixels(base / row["frame_file"])
        assert size == (192, 192), row["frame_file"]
        alpha = frame[:, :, 3]
        yy, xx = np.where(alpha > .08)
        assert len(xx) > 20 and alpha.min() == 0
        assert xx.min() > 1 and xx.max() < 190 and yy.min() > 1 and yy.max() < 190
        sequence.append(frame)
        sheet_file = row["sheet"]
        if sheet_file not in sheet_cache:
            sheet_cache[sheet_file] = pixels(base / sheet_file)
        sheet_size, sheet = sheet_cache[sheet_file]
        x, top = int(row["x_top_left"]), int(row["y_top_left"])
        bottom = sheet_size[1] - top - 192
        cell = sheet[bottom:bottom + 192, x:x + 192]
        assert np.max(np.abs(cell - frame)) < .02, f"Sheet / frame mismatch: {clip}/{direction}/{row['frame']}"
        pivot = (float(row["pivot_x_from_left"]), float(row["pivot_y_from_bottom"]))
        assert all(0 < value < 1 for value in pivot)
        pivots.add(pivot)
        if clip.startswith("chef_"):
            assert all(0 < float(row[key]) < 1 for key in ("hand_x_from_left", "hand_y_from_bottom"))
    assert len(pivots) == 1, f"Moving pivot: {clip}/{direction}"
    if len(sequence) > 1:
        differences = [float(np.abs(a - b).sum()) for a, b in zip(sequence, sequence[1:])]
        assert min(differences) > .05, f"Repeated neighboring frame: {clip}/{direction}"
        if group[0]["loop"] == "1":
            wrap = float(np.abs(sequence[0] - sequence[-1]).sum())
            assert wrap < max(differences) * 4 + 5, f"Large loop boundary: {clip}/{direction}"
    print(f"PASS {clip}/{direction}: {len(group)} frames; alpha, size, bounds, sheet match, fixed pivot", flush=True)

bpy.ops.wm.open_mainfile(filepath=str(base / "source" / "HandmadeBistro.blend"))
scene = bpy.context.scene
assert [marker.name for marker in sorted(scene.timeline_markers, key=lambda marker: marker.frame)] == list(actions)
knife = bpy.data.objects["Cutting knife blade"]
scene.frame_set(1)
assert knife.hide_render
scene.frame_set(28)
assert not knife.hide_render
torso = bpy.data.objects["Torso breathing / bob"]
scene.frame_set(9)
first = torso.location.copy()
scene.frame_set(11)
assert (torso.location - first).length > .001
print("PASS editable .blend: five action markers, sampled transforms, knife visibility", flush=True)
print(f"BISTRO CHECK COMPLETE frames={len(rows)} sheets={len(sheet_cache)} sequences={len(groups)}", flush=True)
