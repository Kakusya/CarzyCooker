"""Editable native Blender source for the isolated handmade-bistro animation pack.

Run with Blender 4.5 --background --python build_bistro.py [-- --smoke].
All writes, including temporary renders, stay beside this source file.
This does not import or execute the production art exporter.
"""
import csv
import math
import random
import sys
from pathlib import Path

import bpy
import numpy as np
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Vector

BASE = Path(__file__).resolve().parent
CELL = 192
FRAMES = 8
FPS = 10
SMOKE = "--smoke" in sys.argv
TAU = math.tau
DIRS = ("north", "east", "south", "west")
ANGLES = (math.pi, math.pi / 2, 0, -math.pi / 2)
for folder in ("sprites", "frames", "review", "source"):
    (BASE / folder).mkdir(exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE_NEXT"
scene.eevee.taa_render_samples = 64
scene.render.resolution_x = scene.render.resolution_y = CELL
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.render.image_settings.file_format = "PNG"
scene.render.image_settings.color_mode = "RGBA"
scene.render.image_settings.color_depth = "8"
scene.render.fps = FPS
scene.view_settings.view_transform = "Standard"
scene.view_settings.look = "Medium High Contrast"
scene.view_settings.exposure = -0.35
scene.world.use_nodes = True
scene.world.node_tree.nodes["Background"].inputs[0].default_value = (.64, .70, .77, 1)
scene.world.node_tree.nodes["Background"].inputs[1].default_value = .32
scene.render.threads_mode = "FIXED"
scene.render.threads = 4

COLORS = {
    "cream": ((.87, .79, .60), .58), "porcelain": ((.93, .92, .79), .26),
    "celadon": ((.21, .43, .35), .32), "celadon_light": ((.38, .59, .46), .38),
    "rim": ((.075, .24, .23), .20), "wood": ((.47, .24, .095), .76),
    "wood_light": ((.73, .43, .18), .70), "metal": ((.42, .50, .48), .24),
    "dark": ((.045, .068, .058), .52), "skin": ((.79, .43, .24), .58),
    "cheek": ((.74, .23, .14), .70), "red": ((.76, .10, .035), .36),
    "crust": ((.56, .225, .045), .84), "crumb": ((.96, .66, .245), .86),
    "toast_mark": ((.36, .13, .025), .90), "butter": ((1, .79, .28), .35),
    "soup": ((.86, .195, .035), .23), "egg": ((1, .73, .23), .40),
    "herb": ((.13, .35, .045), .61), "juice": ((1, .39, .025), .18),
    "juice_light": ((1, .69, .15), .22), "tile": ((.48, .62, .50), .76),
    "water": ((.18, .52, .53), .19), "glaze": ((1, .95, .72), .16),
}
MATS = {}
for key, (color, rough) in COLORS.items():
    mat = bpy.data.materials.new(key)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1)
    shader.inputs["Roughness"].default_value = rough
    if key == "metal":
        shader.inputs["Metallic"].default_value = .65
    MATS[key] = mat
steam_mat = bpy.data.materials.new("Translucent warm steam")
steam_mat.diffuse_color = (.95, .91, .79, .34)
steam_mat.use_nodes = True
steam_bsdf = steam_mat.node_tree.nodes.get("Principled BSDF")
steam_bsdf.inputs["Base Color"].default_value = (.95, .91, .79, 1)
steam_bsdf.inputs["Alpha"].default_value = .34
steam_bsdf.inputs["Roughness"].default_value = 1
MATS["steam"] = steam_mat

bpy.ops.object.camera_add(location=(0, -6, 6.65))
camera = bpy.context.object
camera.name = "Fixed 45 degree sprite camera"
camera.data.type = "ORTHO"
scene.camera = camera
for position, power, size, color in [
    ((-3, -4, 7), 470, 4, (1, .88, .71)),
    ((4, -1, 4), 200, 5, (.76, .88, 1)),
    ((0, 4, 6), 250, 3, (1, .90, .74)),
]:
    bpy.ops.object.light_add(type="AREA", location=position)
    light = bpy.context.object
    light.data.energy, light.data.shape, light.data.size = power, "DISK", size
    light.data.color = color
    light.rotation_euler = (Vector((0, 0, .4)) - light.location).to_track_quat("-Z", "Y").to_euler()

parent = None
roots = []


def node(name, location=(0, 0, 0), owner=None):
    obj = bpy.data.objects.new(name, None)
    scene.collection.objects.link(obj)
    obj.parent = owner
    obj.location = location
    return obj


def finish(obj, name, mat):
    obj.name = name
    obj.data.materials.append(MATS[mat])
    obj.parent = parent
    return obj


def box(name, position, size, mat, bevel=.035):
    bpy.ops.mesh.primitive_cube_add(size=1, location=position)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new("Hand softened edges", "BEVEL")
        mod.width, mod.segments = bevel, 3
        obj.modifiers.new("Weighted normals", "WEIGHTED_NORMAL")
    return finish(obj, name, mat)


def sphere(name, position, size, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=12, location=position)
    obj = bpy.context.object
    obj.scale = size
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return finish(obj, name, mat)


def cylinder(name, position, radius, depth, mat):
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=radius, depth=depth, location=position)
    obj = bpy.context.object
    mod = obj.modifiers.new("Soft ceramic edge", "BEVEL")
    mod.width, mod.segments = .012, 3
    obj.modifiers.new("Weighted normals", "WEIGHTED_NORMAL")
    return finish(obj, name, mat)


def ring(name, position, radius, thickness, mat, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_segments=32, minor_segments=8,
        major_radius=radius, minor_radius=thickness, location=position, rotation=rotation)
    obj = bpy.context.object
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return finish(obj, name, mat)


def line(name, points, radius, mat):
    curve = bpy.data.curves.new(name, "CURVE")
    curve.dimensions = "3D"
    curve.bevel_depth, curve.bevel_resolution = radius, 2
    spline = curve.splines.new("BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for point, co in zip(spline.bezier_points, points):
        point.co = co
        point.handle_left_type = point.handle_right_type = "AUTO"
    obj = bpy.data.objects.new(name, curve)
    scene.collection.objects.link(obj)
    return finish(obj, name, mat)


def start(name):
    global parent
    parent = node(name)
    roots.append(parent)
    return parent


def descendants(root):
    for child in root.children:
        yield child
        yield from descendants(child)


def show_only(root):
    for group in roots:
        for obj in descendants(group):
            obj.hide_render = group != root


def view(span, aim):
    camera.data.ortho_scale = span
    camera.location = (0, -6, 6 + aim)
    camera.rotation_euler = (Vector((0, 0, aim)) - camera.location).to_track_quat("-Z", "Y").to_euler()


chef = start("Chef · editable transform rig")
torso = node("Torso breathing / bob", (0, 0, .48), chef)
parent = torso
sphere("Cream cotton jacket", (0, 0, .015), (.255, .18, .25), "porcelain")
box("Sage apron bib", (0, -.163, .02), (.35, .048, .32), "celadon", .06)
box("Soft apron skirt", (0, -.135, -.15), (.39, .07, .18), "celadon", .045)
box("Apron pocket", (0, -.197, -.065), (.17, .018, .11), "celadon_light", .025)
line("Pocket stitch", [(-.065, -.21, -.04), (-.06, -.212, -.095), (.06, -.212, -.095), (.065, -.21, -.04)], .004, "cream")
for side in (-1, 1):
    strap = box("Apron strap", (side * .125, -.153, .14), (.035, .04, .19), "celadon")
    strap.rotation_euler.y = side * .12
    sphere("Brass apron button", (side * .12, -.185, .10), (.014, .01, .014), "butter")
    box("Apron waist tie", (side * .20, .11, -.03), (.04, .13, .05), "celadon", .015)
box("Paprika neckerchief", (0, -.13, .245), (.17, .06, .055), "red", .025)
scarf = box("Neckerchief tail", (.03, -.17, .195), (.06, .025, .10), "red", .01)
scarf.rotation_euler.y = -.2
head = node("Head / hat follow through", (0, -.005, .43), torso)
parent = head
sphere("Head", (0, 0, 0), (.255, .224, .25), "skin")
sphere("Nose", (0, -.217, -.015), (.052, .05, .055), "skin")
eyes = []
for side in (-1, 1):
    sphere("Ear", (side * .248, -.015, -.01), (.045, .035, .07), "skin")
    sphere("Rosy cheek", (side * .137, -.185, -.055), (.043, .012, .023), "cheek")
    eyes.append(sphere("Eye", (side * .085, -.202, .045), (.023, .017, .032), "dark"))
    sphere("Eye sparkle", (side * .085 - .006, -.218, .055), (.006, .004, .008), "porcelain")
    line("Eyebrow", [(side * .12, -.191, .104), (side * .085, -.207, .12), (side * .055, -.21, .11)], .009, "wood")
line("Little smile", [(-.043, -.216, -.074), (0, -.23, -.089), (.043, -.216, -.074)], .009, "wood")
cylinder("Chef hat band", (0, 0, .23), .24, .10, "porcelain")
ring("Hat seam", (0, 0, .205), .239, .007, "cream")
for x, y, z, size in [(-.14, 0, .33, .16), (.14, 0, .34, .17), (0, -.10, .355, .18), (0, .10, .36, .18)]:
    sphere("Soft folded toque", (x, y, z), (size, size * .88, size * .77), "porcelain")

legs, arms, hands = [], [], []
for side in (-1, 1):
    leg = node("Left leg" if side < 0 else "Right leg", (side * .115, 0, .34), chef)
    parent = leg
    box("Trouser leg", (0, 0, -.09), (.145, .16, .25), "rim", .04)
    box("Comfortable shoe", (0, -.048, -.27), (.18, .25, .12), "dark", .045)
    box("Shoe sole", (0, -.05, -.315), (.182, .251, .025), "wood", .012)
    legs.append(leg)
    arm = node("Left shoulder" if side < 0 else "Right shoulder", (side * .245, 0, .16), torso)
    parent = arm
    sphere("Jacket sleeve", (side * .015, 0, -.065), (.087, .091, .15), "porcelain")
    ring("Rolled cuff", (side * .018, 0, -.15), .062, .017, "cream")
    hand = sphere("Hand socket", (side * .02, 0, -.245), (.073, .065, .075), "skin")
    arms.append(arm)
    hands.append(hand)
parent = arms[1]
knife = box("Cutting knife blade", (.02, -.10, -.25), (.033, .19, .095), "metal", .01)
knife_handle = box("Knife wooden handle", (.02, .025, -.245), (.045, .12, .045), "wood", .012)


def pose(action, frame, key=False):
    phase = TAU * frame / FRAMES
    walk = action in ("walk", "carry")
    bob = .018 * (1 - math.cos(phase * 2)) if walk else .008 * math.sin(phase)
    torso.location.z = .48 + bob
    torso.rotation_euler.y = .025 * math.sin(phase) if walk else 0
    head.rotation_euler = (.015 * math.sin(phase), .018 * math.sin(phase - .5), -.02 * math.sin(phase))
    for index, leg in enumerate(legs):
        leg.rotation_euler.x = (1 if index == 0 else -1) * .44 * math.sin(phase) if walk else 0
        leg.location.z = .34
    for index, arm in enumerate(arms):
        sign = 1 if index == 0 else -1
        if action == "idle":
            arm.rotation_euler = (.025 * math.sin(phase), sign * .045, 0)
        elif action == "walk":
            arm.rotation_euler = (-sign * .36 * math.sin(phase), sign * .06, 0)
        elif action == "carry":
            arm.rotation_euler = (-.92 + .035 * math.sin(phase * 2), sign * -.14, 0)
        elif action == "chop":
            arm.rotation_euler = ((-.9 + .38 * math.sin(phase)) if index == 1 else -.73, sign * -.15, 0)
        else:
            reach = math.sin(math.pi * frame / (FRAMES - 1))
            arm.rotation_euler = (-.75 - .52 * reach, sign * -.17, 0)
    knife.hide_render = knife_handle.hide_render = action != "chop"
    for eye in eyes:
        eye.scale.z = .032 * (.12 if action == "idle" and frame == 6 else 1)
    if key:
        for obj in [torso, head, *legs, *arms, *eyes]:
            obj.keyframe_insert(data_path="location")
            obj.keyframe_insert(data_path="rotation_euler")
            obj.keyframe_insert(data_path="scale")
        knife.keyframe_insert(data_path="hide_render")
        knife_handle.keyframe_insert(data_path="hide_render")


def vessel(name, radius, height, pot=False, cup=False):
    root = start(name)
    cylinder("Enamel body", (0, 0, height * .5), radius, height, "celadon" if pot else "porcelain")
    cylinder("Empty interior", (0, 0, height + .006), radius * .87, .012, "cream" if not pot else "rim")
    ring("Blue green enamel rim", (0, 0, height + .012), radius * .96, .021, "rim")
    ring("Foot ring", (0, 0, .025), radius * .82, .014, "cream")
    if pot:
        for side in (-1, 1):
            ring("Wood handle", (side * (radius + .06), 0, height * .68), .08, .027, "wood", (math.pi / 2, 0, 0))
            box("Handle collar", (side * (radius + .005), 0, height * .68), (.07, .10, .08), "metal", .02)
    if cup:
        for x in (-.08, 0, .08):
            box("Painted juice stripe", (x, -radius + .004, height * .52), (.022, .015, height * .55), "juice", .008)
    return root


bowl = vessel("bowl_empty", .33, .235)
pot = vessel("pot_empty", .35, .36, pot=True)
cup = vessel("cup_empty", .23, .40, cup=True)
plate = start("plate_empty")
cylinder("Plate body", (0, 0, .04), .42, .065, "porcelain")
ring("Enamel plate rim", (0, 0, .08), .383, .025, "rim")
ring("Fine glaze accent", (0, 0, .085), .35, .006, "celadon_light")

soup = start("soup_surface")
cylinder("Tomato soup", (0, 0, .015), .285, .025, "soup")
ribbons = []
for index, (x, y) in enumerate([(-.12, .08), (.065, .13), (.12, -.07), (-.10, -.14)]):
    ribbons.append(line("Soft egg ribbon", [(x - .05, y, .034), (x, y + .023, .04), (x + .055, y - .008, .035)], .017, "egg"))
for x, y, angle in [(.03, -.015, .7), (-.08, .16, -.4), (.17, .07, .5)]:
    leaf = sphere("Fresh herb", (x, y, .044), (.032, .018, .008), "herb")
    leaf.rotation_euler.z = angle
soup_bubbles = [sphere("Soup bubble", (0, 0, .035), (.02, .02, .01), "juice_light") for _ in range(4)]
line("Wet surface highlight", [(-.21, -.09, .035), (-.19, -.14, .037), (-.14, -.17, .035)], .008, "glaze")

juice = start("juice_surface")
cylinder("Orange juice surface", (0, 0, .015), .19, .026, "juice")
juice_ring = ring("Soft liquid meniscus", (0, 0, .029), .175, .009, "juice_light")
juice_glints = []
for x, y in [(-.09, -.04), (.07, .08), (.10, -.07)]:
    juice_glints.append(sphere("Pulp / reflection", (x, y, .032), (.022, .009, .005), "juice_light"))

steam = start("steam_loop")
puffs = [sphere("Warm steam wisp", (0, 0, .1), (.07, .035, .11), "steam") for _ in range(5)]


def effect_pose(action, frame):
    phase = TAU * frame / FRAMES
    if action == "soup":
        for i, obj in enumerate(ribbons):
            obj.location.z = .002 * math.sin(phase + i)
        for i, obj in enumerate(soup_bubbles):
            t = (frame / FRAMES + i / 4) % 1
            a = i * 2.4
            obj.location = (.18 * math.cos(a), .18 * math.sin(a), .035 + .025 * math.sin(t * math.pi))
            obj.scale = (.008 + .022 * t, .008 + .022 * t, .005 + .008 * math.sin(t * math.pi))
    elif action == "juice":
        juice_ring.scale = (1 + .045 * math.sin(phase), 1 - .025 * math.sin(phase), 1)
        for i, obj in enumerate(juice_glints):
            obj.location.z = .032 + .005 * math.sin(phase + i)
            obj.rotation_euler.z = .35 * math.sin(phase + i)
    else:
        for i, obj in enumerate(puffs):
            t = (frame / FRAMES + i / len(puffs)) % 1
            obj.location = (.07 * math.sin(TAU * t + i), .01 * i, .07 + .55 * t)
            size = .038 + .05 * math.sin(math.pi * t)
            fade = math.sin(math.pi * t)
            obj.scale = (size * fade, .025 * fade, (.045 + .075 * t) * fade)


toast = start("toast")
box("Golden baked crust", (0, 0, .095), (.61, .51, .15), "crust", .09)
box("Honey colored crumb", (0, 0, .172), (.49, .405, .021), "crumb", .075)
rng = random.Random(27)
for i in range(23):
    x, y = rng.uniform(-.20, .20), rng.uniform(-.15, .15)
    sphere("Bread pore", (x, y, .184), (.006 + rng.random() * .008, .004, .002), "crust")
for x in (-.13, .02, .17):
    stripe = box("Toasted golden mark", (x, -.005, .187), (.015, .26, .002), "toast_mark", .005)
    stripe.rotation_euler.z = -.16
butter = box("Melting butter", (-.06, -.02, .205), (.13, .11, .026), "butter", .016)
butter.rotation_euler.z = .25


def station(kind):
    root = start("station_" + kind)
    box("Painted timber base", (0, 0, .29), (.83, .78, .58), "celadon", .045)
    for x in (-.36, .36):
        box("Raised panel edge", (x, -.399, .29), (.025, .018, .45), "celadon_light", .008)
    box("Inset cabinet door", (0, -.395, .27), (.64, .028, .35), "celadon_light", .025)
    box("Oak kickboard", (0, -.41, .055), (.78, .03, .07), "wood", .014)
    box("Brass drawer pull", (0, -.43, .41), (.24, .035, .025), "butter", .01)
    box("Counter slab", (0, 0, .63), (.96, .89, .10), "porcelain", .045)
    if kind == "prep":
        box("Thick oak cutting board", (0, -.02, .71), (.73, .59, .07), "wood_light", .045)
        for x in (-.19, .04, .21):
            line("Wood grain", [(x, -.25, .749), (x + .025, 0, .75), (x - .01, .23, .75)], .004, "wood")
        ring("Hanging hole", (.28, .18, .75), .025, .006, "wood")
    elif kind == "hob":
        box("Sage enamel hob", (0, 0, .71), (.81, .74, .055), "rim", .03)
        for x in (-.22, .22):
            for y in (-.20, .20):
                ring("Burner ring", (x, y, .75), .125, .018, "metal")
                for angle in (0, math.pi / 2):
                    rail = box("Pan support", (x, y, .756), (.30, .025, .02), "dark", .006)
                    rail.rotation_euler.z = angle
        for x in (-.23, 0, .23):
            sphere("Round cooker dial", (x, -.445, .57), (.04, .025, .04), "rim")
    else:
        box("Basin rim", (0, -.02, .70), (.71, .58, .055), "metal", .075)
        box("Empty basin", (0, -.02, .73), (.54, .42, .025), "rim", .065)
        line("Curved tap", [(0, .29, .70), (0, .29, .98), (0, .10, 1.02), (0, .05, .92)], .025, "metal")
        box("Tap lever", (.10, .27, .82), (.14, .035, .025), "metal", .01)
        sphere("Soap bottle", (.30, .28, .80), (.06, .06, .12), "cream")
        box("Soap pump", (.30, .26, .94), (.09, .035, .025), "rim", .008)
    return root


prep, hob, sink = [station(kind) for kind in ("prep", "hob", "sink")]

window = start("window")
box("Window oak frame", (0, 0, .50), (.86, .11, .94), "wood_light", .03)
box("Blue morning glass", (0, -.065, .50), (.72, .025, .80), "water", .015)
for x in (-.18, .18):
    box("Glass reflection", (x, -.082, .51), (.06, .008, .69), "porcelain", .008)
box("Window mullion", (0, -.09, .50), (.035, .03, .83), "cream", .01)
box("Window crossbar", (0, -.09, .50), (.75, .03, .035), "cream", .01)
box("Wide oak sill", (0, -.09, .045), (.99, .29, .06), "wood_light", .02)

plant = start("herb_pot")
cylinder("Terracotta herb pot", (0, 0, .13), .16, .25, "crust")
ring("Pot lip", (0, 0, .25), .16, .025, "wood_light")
cylinder("Dark soil", (0, 0, .255), .145, .01, "wood")
for i in range(7):
    a = i * 2.4
    leaf = sphere("Basil leaf", (.09 * math.cos(a), .09 * math.sin(a), .35 + .025 * i), (.06, .10, .025), "herb")
    leaf.rotation_euler = (.4 * math.cos(a), .4 * math.sin(a), a)

static = [
    ("bowl_empty", bowl, 1, 1.15, .14), ("pot_empty", pot, 1, 1.3, .18),
    ("cup_empty", cup, 1, 1.05, .19), ("plate_empty", plate, 1, 1.15, .04),
    ("toast", toast, 1, 1.0, .10), ("station_prep", prep, 4, 1.7, .42),
    ("station_hob", hob, 4, 1.7, .42), ("station_sink", sink, 4, 1.7, .42),
    ("window", window, 1, 1.4, .43), ("herb_pot", plant, 1, .95, .25),
]


def save_pixels(name, pixels, path):
    image = bpy.data.images.new(name, width=pixels.shape[1], height=pixels.shape[0], alpha=True)
    image.pixels.foreach_set(pixels.reshape(-1))
    image.filepath_raw = str(path)
    image.file_format = "PNG"
    image.save()
    bpy.data.images.remove(image)


def render(path):
    path.parent.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    image = bpy.data.images.load(str(path), check_existing=False)
    pixels = np.empty(CELL * CELL * 4, dtype=np.float32)
    image.pixels.foreach_get(pixels)
    bpy.data.images.remove(image)
    return pixels.reshape(CELL, CELL, 4)


def validate_frame(pixels, label):
    alpha = pixels[:, :, 3]
    yy, xx = np.where(alpha > .08)
    assert len(xx) > 20, f"Empty frame: {label}"
    assert alpha.min() == 0 and alpha.max() > .1, f"Missing transparency: {label}"
    assert xx.min() > 1 and xx.max() < CELL - 2 and yy.min() > 1 and yy.max() < CELL - 2, f"Clipped frame: {label}"


clips = []
records = []
for action in ("idle", "walk", "carry", "chop", "serve"):
    clips.append(("chef_" + action, chef, action, FRAMES, 4, 1.95, .66, action != "serve"))
for name, group, span, aim in [("soup", soup, 1.0, .03), ("juice", juice, .80, .025), ("steam", steam, 1.05, .30)]:
    clips.append((name + "_loop", group, name, FRAMES, 1, span, aim, True))
for name, group, dirs, span, aim in static:
    clips.append((name, group, "static", 1, dirs, span, aim, False))

if SMOKE:
    clips = [clips[1], clips[5], clips[8], clips[12]]

contact = []
for clip, group, action, count, directions, span, aim, looping in clips:
    show_only(group)
    view(span, aim)
    sheet = np.zeros((CELL * directions, CELL * count, 4), dtype=np.float32)
    for direction in range(directions):
        group.rotation_euler.z = ANGLES[direction] if directions == 4 else 0
        sequence = []
        for frame in range(count):
            if group == chef:
                pose(action, frame)
            elif action in ("soup", "juice", "steam"):
                effect_pose(action, frame)
            bpy.context.view_layer.update()
            path = BASE / "frames" / clip / (DIRS[direction] if directions == 4 else "front") / f"{frame:03}.png"
            pixels = render(path)
            validate_frame(pixels, f"{clip}/{direction}/{frame}")
            # numpy / Blender pixel arrays start at the bottom; sheet rows in the CSV start at the top.
            row = directions - direction - 1
            sheet[row * CELL:(row + 1) * CELL, frame * CELL:(frame + 1) * CELL] = pixels
            sequence.append(pixels)
            if group == chef:
                foot = world_to_camera_view(scene, camera, Vector((0, 0, 0)))
                hand = world_to_camera_view(scene, camera, (hands[0].matrix_world.translation + hands[1].matrix_world.translation) * .5)
                pivot, socket = (foot.x, foot.y), (hand.x, hand.y)
            else:
                pivot, socket = (.5, .5), (0, 0)
            records.append([clip, DIRS[direction] if directions == 4 else "front", frame,
                f"sprites/{clip}.png", frame * CELL, direction * CELL, CELL, CELL,
                FPS if count > 1 else 0, int(looping), round(pivot[0], 5), round(pivot[1], 5),
                round(socket[0], 5), round(socket[1], 5), str(path.relative_to(BASE)).replace("\\", "/")])
            print(f"FRAME {clip} {direction + 1}/{directions} {frame + 1}/{count}", flush=True)
        if count > 1:
            differences = [float(np.abs(sequence[i] - sequence[i - 1]).sum()) for i in range(1, count)]
            assert min(differences) > .05, f"Duplicate neighboring frames: {clip}/{direction}"
            if looping:
                wrap = float(np.abs(sequence[0] - sequence[-1]).sum())
                assert wrap < max(differences) * 4 + 5, f"Loop discontinuity: {clip}/{direction}"
        if direction == min(2, directions - 1):
            contact.append(sequence[0])
    save_pixels(clip, sheet, BASE / "sprites" / f"{clip}.png")
    print(f"PASS clip={clip} frames={count * directions} alpha/bounds/variation/loop", flush=True)

with (BASE / ("smoke-clips.csv" if SMOKE else "clips.csv")).open("w", newline="", encoding="utf-8") as stream:
    writer = csv.writer(stream)
    writer.writerow(["clip", "direction", "frame", "sheet", "x_top_left", "y_top_left", "width", "height",
        "fps", "loop", "pivot_x_from_left", "pivot_y_from_bottom", "hand_x_from_left", "hand_y_from_bottom", "frame_file"])
    writer.writerows(records)

cols = 4
rows = math.ceil(len(contact) / cols)
contact_pixels = np.zeros((CELL * rows, CELL * cols, 4), dtype=np.float32)
for index, pixels in enumerate(contact):
    row, col = divmod(index, cols)
    dest_row = rows - row - 1
    contact_pixels[dest_row * CELL:(dest_row + 1) * CELL, col * CELL:(col + 1) * CELL] = pixels
save_pixels("Contact sheet", contact_pixels, BASE / "review" / ("smoke.png" if SMOKE else "contact-sheet.png"))

if not SMOKE:
    # Assemble an art display only. No game scene, layout or Unity asset is written.
    stage = start("Bistro art display · not gameplay")
    box("Warm oak foundation", (0, 0, -.085), (3.24, 3.24, .16), "wood_light", .08)
    for x in range(5):
        for y in range(5):
            box("Sage square tile", ((x - 2) * .62, (y - 2) * .62, .012), (.60, .60, .05), "tile", .012)
    box("Cream back wall", (0, 1.55, 1.02), (3.24, .13, 2.05), "cream", .03)
    box("Oak wall skirting", (0, 1.455, .14), (3.13, .06, .18), "wood_light", .015)
    box("Left low return wall", (-1.55, .55, .42), (.13, 2.1, .84), "cream", .025)
    box("Return wall cap", (-1.55, .55, .85), (.18, 2.16, .06), "wood_light", .02)

    def clone(source, location, scale=1, rotation=0):
        def copy_branch(original, owner):
            duplicate = original.copy()
            duplicate.animation_data_clear()
            scene.collection.objects.link(duplicate)
            duplicate.parent = owner
            duplicate.hide_render = False
            for child in original.children:
                copy_branch(child, duplicate)
            return duplicate
        root = copy_branch(source, stage)
        root.location = location
        root.rotation_euler.z = rotation
        root.scale = (scale, scale, scale)
        return root

    clone(window, (-.55, 1.44, 1.04), .87)
    clone(prep, (-.98, .98, .04))
    clone(hob, (0, .98, .04))
    clone(sink, (.98, .98, .04))
    clone(pot, (0, .98, .80))
    clone(plant, (-1.0, 1.32, 1.07), .6)
    clone(plate, (-.98, .84, .80), .82)
    clone(toast, (-.98, .84, .87), .82)
    clone(cup, (.97, .64, .80), .85)
    parent = stage
    box("Menu frame", (.74, 1.45, 1.56), (.63, .055, .69), "wood", .025)
    box("Chalk board", (.74, 1.414, 1.56), (.55, .025, .60), "rim", .01)
    for i, width in enumerate((.32, .38, .27, .35)):
        box("Menu chalk stroke", (.72, 1.396, 1.77 - i * .13), (width, .006, .012), "cream", .004)
    line("Hanging utensil rail", [(-1.25, 1.40, .94), (-.70, 1.40, .94)], .015, "metal")
    for x in (-1.15, -.95, -.75):
        line("Hanging spoon handle", [(x, 1.39, .92), (x, 1.39, .74)], .012, "wood")
        sphere("Wooden spoon bowl", (x, 1.39, .70), (.035, .017, .05), "wood_light")
    # Foreground cafe table and folded linen create a material / food comparison.
    box("Dining table top", (.66, -.88, .58), (1.14, .70, .09), "wood_light", .045)
    for x in (.22, 1.10):
        for y in (-1.10, -.66):
            box("Table leg", (x, y, .29), (.07, .07, .58), "wood", .016)
    box("Folded tea towel", (.97, -.84, .64), (.28, .33, .025), "porcelain", .015)
    for x in (.90, .96, 1.02):
        box("Woven tea towel stripe", (x, -.84, .655), (.018, .30, .003), "celadon", .003)
    clone(bowl, (.43, -.90, .65), .9)
    for x, y in [(-1.08, -.90), (1.0, -.05)]:
        cylinder("Wooden stool", (x, y, .33), .22, .07, "wood_light")
        for dx, dy in [(-.12, -.12), (.12, -.12), (-.12, .12), (.12, .12)]:
            box("Stool leg", (x + dx, y + dy, .16), (.045, .045, .32), "wood", .01)
    clone(plant, (-1.26, -.06, .04), .95)
    show_only(stage)
    scene.render.resolution_x, scene.render.resolution_y = 1024, 768
    stage_span = 5.6
    view(stage_span, .85)
    scene.render.filepath = str(BASE / "review" / "bistro-corner.png")
    bpy.ops.render.render(write_still=True)
    markers = [
        ("chef", (-.25, -.22, .04), 1.95),
        ("pot_soup", (0, .98, 1.19), 1.0),
        ("pot_steam", (0, .98, 1.53), 1.05),
        ("cup_juice", (.97, .64, 1.16), .80 * .85),
        ("bowl_soup", (.43, -.90, .89), 1.0 * .9),
    ]
    bpy.context.view_layer.update()
    with (BASE / "stage.csv").open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream)
        writer.writerow(["layer", "x_from_left", "y_from_top", "sprite_size_pixels"])
        for name, point, span in markers:
            p = world_to_camera_view(scene, camera, Vector(point))
            writer.writerow([name, round(p.x * 1024, 3), round((1 - p.y) * 768, 3), round(span / stage_span * 1024, 3)])
    scene.render.resolution_x = scene.render.resolution_y = CELL
    show_only(chef)
    view(1.95, .66)
    chef.rotation_euler.z = 0
    for clip_index, action in enumerate(("idle", "walk", "carry", "chop", "serve")):
        for frame in range(FRAMES):
            scene.frame_set(clip_index * FRAMES + frame + 1)
            pose(action, frame, key=True)
        marker = scene.timeline_markers.new(action, frame=clip_index * FRAMES + 1)
    for group, action in ((soup, "soup"), (juice, "juice"), (steam, "steam")):
        for frame in range(FRAMES):
            scene.frame_set(frame + 1)
            effect_pose(action, frame)
            for obj in descendants(group):
                obj.keyframe_insert(data_path="location")
                obj.keyframe_insert(data_path="rotation_euler")
                obj.keyframe_insert(data_path="scale")
    scene.frame_start, scene.frame_end = 1, FRAMES * 5
    scene.frame_set(1)
    knife.hide_render = knife_handle.hide_render = True
    for index, group in enumerate(root for root in roots if root != chef):
        group.location = (2.4 * (1 + index % 6), 3.2 * (index // 6), 0)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(BASE / "source" / "HandmadeBistro.blend"))
print(f"BISTRO EXPORT COMPLETE clips={len(clips)} frames={len(records)} isolated={BASE}", flush=True)
