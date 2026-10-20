"""Extend the existing editable bistro source with the finite soup/toast sample.

Called by export_unity.py inside Blender; shares its materials, camera and lights.
The original chef animation and source file stay intact.
"""
import ast
import math
from pathlib import Path

import bpy
from mathutils import Vector


def export_sample(base, scene, template):
    # Reuse the authored geometry helpers without executing the original full render.
    source = ast.parse((base / "build_bistro.py").read_text(encoding="utf-8"))
    helpers = {"node", "finish", "box", "sphere", "cylinder", "ring", "line", "start", "descendants", "show_only", "view"}
    namespace = dict(bpy=bpy, math=math, Vector=Vector, scene=scene, camera=scene.camera,
                     MATS={mat.name: mat for mat in bpy.data.materials}, parent=None,
                     roots=[obj for obj in scene.objects if obj.type == "EMPTY" and obj.parent is None])
    functions = ast.Module(body=[node for node in source.body if isinstance(node, ast.FunctionDef) and node.name in helpers], type_ignores=[])
    exec(compile(functions, str(base / "build_bistro.py"), "exec"), namespace)
    start, sphere, box, cylinder, ring = (namespace[name] for name in ("start", "sphere", "box", "cylinder", "ring"))
    samples = []

    tomato = start("coherence_tomato")
    sphere("Ripe tomato", (0, 0, .22), (.27, .25, .23), "red")
    for index in range(5):
        angle = math.tau * index / 5
        leaf = sphere("Five pointed calyx", (.085 * math.cos(angle), .085 * math.sin(angle), .44), (.12, .035, .015), "herb")
        leaf.rotation_euler.z = angle
    samples.append(("tomato", tomato, 1, 1.0, .22))

    chunks = start("coherence_tomato_chunks")
    for index, (x, y) in enumerate([(-.17, -.10), (.12, -.12), (-.08, .13), (.18, .13)]):
        piece = box("Juicy tomato piece", (x, y, .09), (.22, .19, .16), "red", .045)
        piece.rotation_euler.z = index * .55
        for offset in (-.035, .035):
            sphere("Visible seed", (x + offset, y, .18), (.015, .035, .006), "butter")
    samples.append(("tomato_chunks", chunks, 1, 1.0, .10))

    egg = start("coherence_egg")
    sphere("Cream egg shell", (0, 0, .22), (.19, .19, .27), "cream")
    samples.append(("egg", egg, 1, .95, .22))

    liquid = start("coherence_egg_liquid")
    cylinder("Whisked egg surface", (0, 0, .03), .28, .035, "egg")
    for x, y in [(-.12, -.03), (.08, .10), (.12, -.11)]:
        ring("Whisk bubble", (x, y, .053), .025, .006, "glaze")
    samples.append(("egg_liquid", liquid, 1, 1.0, .04))

    bread = start("coherence_bread")
    box("Soft unbaked crust", (0, 0, .09), (.61, .51, .15), "wood_light", .09)
    box("Pale bread crumb", (0, 0, .17), (.49, .405, .025), "porcelain", .075)
    for x, y in [(-.16, -.09), (.03, .12), (.17, -.05), (-.08, .07)]:
        sphere("Soft crumb pore", (x, y, .188), (.014, .009, .003), "cream")
    samples.append(("bread", bread, 1, 1.0, .10))

    oven = start("coherence_oven")
    box("Sage enamel oven", (0, 0, .30), (.84, .79, .60), "celadon", .055)
    box("Cream oven top", (0, 0, .65), (.96, .89, .10), "porcelain", .045)
    box("Oven exhaust grille", (0, .22, .707), (.57, .21, .018), "metal", .015)
    for x in (-.18, 0, .18):
        box("Recognizable top vent", (x, .22, .723), (.045, .15, .014), "rim", .008)
    box("Oven window frame", (0, -.405, .29), (.68, .035, .39), "rim", .05)
    box("Dark oven glass", (0, -.430, .29), (.54, .022, .25), "dark", .04)
    box("Warm oven tray", (0, -.446, .23), (.42, .008, .025), "wood_light", .008)
    box("Oak door handle", (0, -.462, .46), (.48, .06, .04), "wood", .018)
    for x in (-.22, 0, .22):
        sphere("Brass oven dial", (x, -.421, .575), (.035, .025, .035), "butter")
    samples.append(("station_oven", oven, 4, 1.7, .42))

    rows, spans = [], []
    for name, root, directions, span, aim in samples:
        namespace["show_only"](root)
        namespace["view"](span, aim)
        for direction, angle in list(zip(("north", "east", "south", "west"), (math.pi, math.pi / 2, 0, -math.pi / 2)))[:directions]:
            root.rotation_euler.z = angle if directions == 4 else 0
            relative = f"coherence/{name}/{direction}.png"
            path = base / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            scene.render.filepath = str(path)
            bpy.ops.render.render(write_still=True)
            rows.append(dict(template, clip=name, direction=direction, frame="0", frame_file=relative,
                             hand_x_from_left="0.5", hand_y_from_bottom="0.5"))
            spans.append(span)
    bpy.ops.wm.save_as_mainfile(filepath=str(base / "source/RestaurantCoherence.blend"))
    return rows, spans
