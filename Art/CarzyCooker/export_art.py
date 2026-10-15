"""CarzyCooker authored models. Run with Blender 4.5 LTS --background --python.

The atlas layout is a production contract shared with the generated C# partial.
All silhouettes, materials, camera and poses are editable below. No downloaded art.
"""
import bpy
import csv
import math
import sys
from pathlib import Path
from mathutils import Vector
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path(__file__).resolve().parent
OUTPUT = ROOT / "Unity/Assets/Res/Entity/Product/Art"
LAYOUT = ROOT / "Unity/Assets/Scripts/Game/ET/Code/ModelView/Client/Product/GridKitchen/MonoGridKitchenBoard.ArtLayout.cs"
CELL, COLS = 192, 16
SAMPLE = "--sample" in sys.argv
OUTPUT.mkdir(parents=True, exist_ok=True)
PALETTE = {
    "cream": (0.94, 0.85, 0.66), "ivory": (0.96, 0.95, 0.84),
    "wood": (0.55, 0.29, 0.12), "woodlight": (0.80, 0.52, 0.27),
    "teal": (0.20, 0.48, 0.45), "tealdark": (0.08, 0.25, 0.25),
    "metal": (0.36, 0.48, 0.49), "dark": (0.07, 0.11, 0.13),
    "red": (0.82, 0.10, 0.07), "green": (0.22, 0.51, 0.10),
    "orange": (1.0, 0.41, 0.02), "yellow": (1.0, 0.74, 0.15),
    "blue": (0.20, 0.52, 0.72), "skin": (0.86, 0.54, 0.30),
    "pink": (0.80, 0.27, 0.39), "purple": (0.41, 0.29, 0.58),
    "water": (0.32, 0.72, 0.85), "soup": (0.87, 0.30, 0.10),
    "tile": (0.52, 0.68, 0.60),
    "coffee": (0.20, 0.075, 0.025), "latte": (0.67, 0.39, 0.19),
}
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE_NEXT"
scene.render.resolution_x = CELL
scene.render.resolution_y = CELL
scene.render.resolution_percentage = 100
scene.render.film_transparent = True
scene.render.image_settings.file_format = "PNG"
scene.render.image_settings.color_mode = "RGBA"
scene.view_settings.view_transform = "Standard"
scene.world.color = (0.3, 0.3, 0.3)
materials = {}
for key, color in PALETTE.items():
    material = bpy.data.materials.new(key)
    material.diffuse_color = (*color, 1)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = 0.72
    if key == "metal":
        bsdf.inputs["Metallic"].default_value = 0.35
    materials[key] = material

bpy.ops.object.camera_add(location=(0, -6, 6))
camera = bpy.context.object
camera.rotation_euler = (Vector((0, 0, 0)) - camera.location).to_track_quat("-Z", "Y").to_euler()
camera.data.type = "ORTHO"
camera.data.ortho_scale = 1.65
scene.camera = camera
for position, power, size in [((-3, -4, 7), 550, 5), ((4, 1, 5), 350, 5)]:
    bpy.ops.object.light_add(type="AREA", location=position)
    light = bpy.context.object
    light.data.energy, light.data.shape, light.data.size = power, "DISK", size
    light.rotation_euler = (Vector((0, 0, 0.3)) - light.location).to_track_quat("-Z", "Y").to_euler()

current_root = None

def finish(obj, name, material):
    obj.name = name
    obj.data.materials.append(materials[material])
    obj.parent = current_root
    return obj

def box(name, pos, size, mat, bevel=0.04):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        modifier = obj.modifiers.new("Soft edges", "BEVEL")
        modifier.width, modifier.segments = bevel, 3
        obj.modifiers.new("Weighted normals", "WEIGHTED_NORMAL")
    return finish(obj, name, mat)

def sphere(name, pos, size, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=12, location=pos)
    obj = bpy.context.object
    obj.scale = size
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return finish(obj, name, mat)

def cylinder(name, pos, radius, depth, mat):
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=radius, depth=depth, location=pos)
    obj = bpy.context.object
    modifier = obj.modifiers.new("Soft rim", "BEVEL")
    modifier.width, modifier.segments = 0.015, 2
    obj.modifiers.new("Weighted normals", "WEIGHTED_NORMAL")
    return finish(obj, name, mat)

def ring(name, pos, major, minor, mat, rotation=None):
    bpy.ops.mesh.primitive_torus_add(major_segments=24, minor_segments=8,
        major_radius=major, minor_radius=minor, location=pos)
    obj = bpy.context.object
    if rotation:
        obj.rotation_euler = rotation
    for poly in obj.data.polygons:
        poly.use_smooth = True
    return finish(obj, name, mat)

def cabinet(kind):
    if kind == 4:
        box("Tabletop", (0, 0, .58), (.99, .87, .12), "woodlight")
        for x in [-.34, .34]:
            for y in [-.29, .29]:
                box("Leg", (x, y, .26), (.10, .10, .53), "wood", .02)
        return
    if kind == 8:
        cylinder("Bin", (0, 0, .31), .30, .62, "tealdark")
        cylinder("Lid", (0, 0, .63), .33, .08, "metal")
        box("Lid handle", (0, 0, .70), (.22, .07, .07), "dark")
        return
    box("Cabinet", (0, 0, .32), (.85, .81, .64), "teal")
    box("Counter top", (0, 0, .67), (.97, .94, .11), "ivory")
    box("Kickboard", (0, -.43, .06), (.77, .03, .08), "tealdark", .01)
    box("Drawer", (0, -.421, .48), (.73, .035, .15), "tealdark", .02)
    box("Handle", (0, -.45, .49), (.28, .03, .045), "cream", .01)
    if kind == 1:
        box("Chopping board", (0, 0, .76), (.70, .57, .05), "woodlight", .025)
        box("Knife blade", (.20, .05, .80), (.27, .055, .05), "metal", .01)
        box("Knife handle", (.02, .05, .80), (.12, .07, .05), "dark", .01)
    elif kind == 2:
        box("Hob", (0, 0, .74), (.84, .78, .025), "dark", .02)
        for x in [-.23, .23]:
            for y in [-.22, .22]:
                ring("Burner", (x, y, .78), .12, .018, "metal")
    elif kind == 3:
        box("Oven hob", (0, 0, .76), (.81, .71, .07), "metal", .035)
        for x in [-.23, 0, .23]:
            box("Oven vent", (x, .12, .80), (.08, .27, .008), "tealdark", .01)
        box("Oven glass", (0, -.446, .29), (.64, .03, .28), "dark", .025)
        box("Warm window", (0, -.465, .28), (.51, .013, .15), "orange", .02)
        for x in [-.24, 0, .24]:
            sphere("Dial", (x, -.462, .51), (.04, .025, .04), "cream")
    elif kind == 5:
        box("Sink basin", (0, 0, .74), (.68, .59, .04), "metal", .08)
        box("Sink interior", (0, 0, .767), (.48, .39, .02), "tealdark", .065)
        cylinder("Tap riser", (0, .31, .86), .03, .32, "metal")
        box("Spout", (0, .19, 1.01), (.065, .26, .065), "metal", .02)
    elif kind in [6, 7]:
        box("Utensil tray", (0, 0, .78), (.70, .57, .08), "metal", .04)
        box("Tray interior", (0, 0, .829), (.58, .46, .02), "tealdark", .035)
        if kind == 6:
            sphere("Dirty pool sign", (.07, -.07, .845), (.10, .07, .014), "soup")
    elif kind == 9:
        box("Juicer base", (0, 0, .84), (.50, .45, .24), "orange", .05)
        cylinder("Juicer chamber", (0, 0, 1.04), .18, .20, "ivory")
        cylinder("Juicer lid", (0, 0, 1.17), .21, .05, "tealdark")
        box("Power button", (0, -.236, .89), (.10, .03, .08), "yellow", .02)
    elif kind == 13:
        box("Grinder body", (0, .04, .98), (.40, .40, .42), "tealdark", .05)
        cylinder("Bean hopper", (0, .04, 1.27), .23, .22, "cream")
        cylinder("Hopper lid", (0, .04, 1.40), .25, .035, "coffee")
        box("Ground coffee outlet", (0, -.19, .96), (.19, .22, .10), "metal", .02)
        sphere("Grind switch", (.15, -.17, 1.10), (.035, .025, .035), "orange")
    elif kind == 14:
        box("Coffee machine body", (0, .15, 1.00), (.71, .42, .48), "tealdark", .06)
        box("Coffee machine face", (0, -.08, 1.05), (.63, .035, .28), "metal", .025)
        cylinder("Group head", (0, -.17, .96), .10, .15, "dark")
        box("Drip tray", (0, -.20, .77), (.58, .42, .035), "metal", .02)
        for x in [-.17, 0, .17]:
            sphere("Coffee controls", (x, -.105, 1.11), (.035, .02, .035), "cream")
    elif kind == 15:
        box("Steam boiler", (-.14, .17, 1.02), (.39, .42, .53), "tealdark", .05)
        cylinder("Steam dial", (-.14, -.054, 1.15), .11, .025, "cream").rotation_euler.x = math.pi / 2
        box("Steam wand arm", (.12, .11, 1.20), (.29, .06, .055), "metal", .015)
        cylinder("Steam wand", (.24, .11, 1.04), .028, .30, "metal")
        sphere("Mode switch", (-.14, -.055, .95), (.05, .025, .04), "orange")
    elif kind == 12:
        box("Mixer motor", (0, .19, .96), (.49, .32, .43), "tealdark", .08)
        box("Mixer arm", (0, .01, 1.12), (.49, .45, .13), "cream", .045)
        cylinder("Whisk shaft", (0, -.10, .94), .026, .25, "metal")
        for angle in [0, math.pi / 3, -math.pi / 3]:
            ring("Whisk wire", (0, -.10, .86), .10, .013, "metal", (math.pi / 2, 0, angle))
        sphere("Mixer switch", (.20, .02, 1.03), (.04, .03, .04), "orange")
    elif kind == 11:
        box("Griddle rim", (0, 0, .75), (.86, .78, .08), "metal", .04)
        box("Hot flat surface", (0, 0, .80), (.74, .65, .03), "dark", .025)
        for x in [-.22, 0, .22]:
            sphere("Heat indicator", (x, -.46, .57), (.038, .025, .035), "orange")
        box("Spatula blade", (.25, .08, .84), (.18, .21, .018), "metal", .018)
        box("Spatula handle", (.25, -.10, .85), (.06, .20, .045), "wood", .015)
    elif kind == 10:
        box("Label roll", (0, .11, .83), (.32, .28, .23), "cream")
        box("Ticket", (.08, -.19, .75), (.35, .30, .025), "ivory", .01)
        for y in [-.24, -.17, -.10]:
            box("Print", (.08, y, .77), (.23, .016, .008), "tealdark", 0)

def vessel(kind):
    if kind in [54, 55]:
        cylinder("Coffee receiver" if kind == 54 else "Milk pitcher", (0, 0, .20), .25, .36, "teal" if kind == 54 else "metal")
        cylinder("Jug interior", (0, 0, .39), .21, .012, "cream")
        ring("Jug rim", (0, 0, .40), .235, .02, "ivory")
        ring("Jug handle", (.29, 0, .23), .11, .027, "tealdark", (math.pi / 2, 0, 0))
        box("Jug spout", (-.23, 0, .37), (.10, .12, .04), "teal" if kind == 54 else "metal", .015)
        return
    if kind == 59:
        cylinder("Hot drink cup", (0, 0, .19), .26, .35, "ivory")
        cylinder("Hot cup interior", (0, 0, .377), .22, .012, "cream")
        ring("Hot cup rim", (0, 0, .39), .245, .017, "teal")
        cylinder("Hot cup sleeve", (0, 0, .16), .268, .11, "teal")
        return
    if kind == 45:
        cylinder("Mixing bowl", (0, 0, .14), .37, .25, "teal")
        cylinder("Mixing bowl interior", (0, 0, .272), .32, .014, "cream")
        ring("Mixing bowl lip", (0, 0, .285), .35, .025, "ivory")
        return
    if kind == 47:
        cylinder("Reusable cake mold", (0, 0, .09), .28, .17, "metal")
        cylinder("Mold interior", (0, 0, .182), .235, .012, "tealdark")
        ring("Mold rim", (0, 0, .192), .265, .017, "metal")
        for i in range(10):
            angle = i * math.tau / 10
            box("Mold flute", (.273 * math.cos(angle), .273 * math.sin(angle), .095), (.018, .04, .13), "ivory", .006).rotation_euler.z = angle
        return
    if kind == 49:
        cylinder("Dessert plate", (0, 0, .06), .34, .055, "ivory")
        ring("Dessert plate rim", (0, 0, .097), .30, .017, "teal")
        return
    if kind == 10:
        cylinder("Plate", (0, 0, .08), .42, .07, "ivory")
        ring("Plate rim", (0, 0, .12), .37, .025, "cream")
        return
    radius = .34 if kind in [5, 6] else .23
    depth = .27 if kind == 5 else .42
    cylinder("Vessel body", (0, 0, depth / 2 + .02), radius, depth, "metal" if kind == 6 else "ivory")
    cylinder("Interior", (0, 0, depth + .023), radius * .86, .012, "tealdark" if kind == 6 else "cream")
    ring("Open rim", (0, 0, depth + .033), radius * .95, .025, "metal" if kind == 6 else "ivory")
    if kind in [6, 17]:
        for x in [-1, 1] if kind == 6 else [1]:
            ring("Handle", (x * (radius + .05), 0, depth * .58), .095, .029,
                "dark" if kind == 6 else "teal", (math.pi / 2, 0, 0))
    if kind == 19:
        for x in [-.10, 0, .10]:
            box("Cup stripe", (x, -.218, .23), (.035, .015, .22), "orange", .006)

def food(kind):
    if kind in [5, 6, 10, 17, 19, 45, 47, 49, 54, 55, 59]:
        vessel(kind)
    elif kind in [1, 15]:
        sphere("Fruit", (0, 0, .27), (.31, .31, .27), "red" if kind == 1 else "orange")
        for angle in range(5):
            a = angle * math.tau / 5
            leaf = sphere("Leaf", (.09 * math.sin(a), .09 * math.cos(a), .53), (.05, .14, .015), "green")
            leaf.rotation_euler.z = -a
    elif kind in [2, 16]:
        for x, y in [(-.16, -.10), (.14, -.06), (0, .15)]:
            box("Cut fruit", (x, y, .13), (.24, .22, .20), "red" if kind == 2 else "orange", .045)
            box("Cut face", (x, y, .235), (.17, .15, .015), "pink" if kind == 2 else "yellow", .02)
    elif kind == 3:
        sphere("Egg", (0, 0, .27), (.22, .22, .29), "cream")
    elif kind in [4, 7, 11, 18]:
        color = {4: "yellow", 7: "soup", 11: "water", 18: "orange"}[kind]
        cylinder("Liquid surface", (0, 0, .03), .30, .045, color)
        if kind == 7:
            for x, y in [(-.1, .05), (.12, -.04), (0, -.15)]:
                sphere("Egg ribbon", (x, y, .06), (.08, .033, .013), "yellow")
            sphere("Herb", (.03, .13, .06), (.05, .07, .01), "green")
    elif kind in [8, 9]:
        box("Bread crust", (0, 0, .10), (.64, .53, .17), "woodlight" if kind == 8 else "wood", .10)
        box("Bread crumb", (0, 0, .19), (.52, .42, .025), "cream" if kind == 8 else "yellow", .07)
        if kind == 9:
            for x in [-.14, 0, .14]:
                box("Toast line", (x, 0, .207), (.02, .29, .008), "woodlight", .008)
    elif kind == 22:
        for i in range(7):
            a = i * math.tau / 7
            leaf = sphere("Lettuce leaf", (.13 * math.cos(a), .13 * math.sin(a), .17), (.19, .26, .14), "green")
            leaf.rotation_euler.z = a
            box("Leaf vein", (.13 * math.cos(a), .13 * math.sin(a), .29), (.025, .22, .012), "cream", .006).rotation_euler.z = a
    elif kind == 23:
        for x, y, a in [(-.17, -.09, -.4), (.13, -.08, .5), (.03, .15, -.2)]:
            sphere("Cut lettuce", (x, y, .09), (.19, .11, .065), "green").rotation_euler.z = a
            box("Lettuce rib", (x, y, .145), (.035, .14, .014), "cream", .007).rotation_euler.z = a
    elif kind == 24:
        sphere("Whole cucumber", (0, 0, .16), (.14, .43, .14), "green").rotation_euler.z = -.5
        for x, y in [(-.13, .30), (.13, -.30)]:
            sphere("Cucumber end", (x, y, .16), (.075, .055, .075), "cream")
    elif kind == 25:
        for x, y in [(-.17, -.07), (.15, -.06), (0, .15)]:
            cylinder("Cucumber skin", (x, y, .07), .145, .075, "green")
            cylinder("Cucumber flesh", (x, y, .111), .119, .012, "ivory")
            for dx in [-.035, .035]:
                sphere("Cucumber seed", (x + dx, y, .124), (.014, .04, .008), "cream")
    elif kind == 26:
        box("Cheese slice", (0, 0, .08), (.56, .48, .07), "yellow", .04)
        for x, y, radius in [(-.13, -.09, .055), (.13, .10, .035), (-.10, .12, .028)]:
            cylinder("Cheese dimples", (x, y, .12), radius, .008, "cream")
    elif kind in [27, 28]:
        for x, y, a in [(-.18, -.07, -.4), (.14, -.10, .5), (-.07, .17, .2), (.17, .15, -.6)]:
            sphere("Salad lettuce", (x, y, .09), (.19, .14, .06), "green").rotation_euler.z = a
        for x, y in [(-.15, -.07), (.13, .10)]:
            box("Salad tomato", (x, y, .17), (.15, .13, .11), "red", .03)
        for x, y in [(.16, -.11), (-.13, .16)]:
            cylinder("Salad cucumber skin", (x, y, .15), .095, .045, "green")
            cylinder("Salad cucumber flesh", (x, y, .177), .075, .008, "ivory")
        if kind == 28:
            for x, y, a in [(-.04, -.12, .3), (.09, .05, -.5), (-.12, .11, .4)]:
                box("Salad cheese", (x, y, .23), (.22, .085, .025), "yellow", .012).rotation_euler.z = a
    elif kind == 32:
        cylinder("Bun lower half", (-.04, -.02, .10), .30, .10, "woodlight")
        sphere("Bun crown", (.06, .07, .23), (.31, .28, .14), "woodlight")
        for x, y in [(-.10, .05), (.07, -.04), (.15, .12), (-.04, .17)]:
            sphere("Sesame", (x, y, .35), (.035, .015, .009), "ivory")
    elif kind in [33, 34]:
        cylinder("Beef patty", (0, 0, .08), .30, .12, "pink" if kind == 33 else "wood")
        if kind == 34:
            for x in [-.15, 0, .15]:
                box("Patty grill mark", (x, 0, .146), (.025, .38, .008), "dark", .006)
        else:
            for x, y in [(-.13, .10), (.10, -.09), (.13, .12)]:
                sphere("Raw marbling", (x, y, .146), (.035, .055, .007), "ivory")
    elif kind in [35, 36]:
        sphere("Sausage", (0, 0, .13), (.14, .38, .13), "pink" if kind == 35 else "woodlight")
        for y in [-.20, 0, .20]:
            box("Sausage score", (0, y, .247), (.18, .025, .013), "red" if kind == 35 else "wood", .008).rotation_euler.z = .4
    elif kind == 37:
        sphere("Fried egg white", (0, 0, .045), (.34, .28, .04), "ivory")
        sphere("Sunny yolk", (.04, .01, .11), (.13, .12, .075), "yellow")
    elif kind == 38:
        cylinder("Burger bun base", (0, 0, .08), .31, .10, "woodlight")
        cylinder("Burger cooked beef", (0, 0, .17), .30, .08, "wood")
        sphere("Burger bun crown", (0, 0, .29), (.33, .30, .13), "woodlight")
        for x, y in [(-.13, .04), (.10, -.06), (.14, .11), (-.05, .17)]:
            sphere("Burger sesame", (x, y, .40), (.035, .016, .010), "ivory")
    elif kind == 39:
        box("Breakfast toast crust", (-.16, .12, .07), (.33, .28, .09), "wood", .055)
        box("Breakfast toast crumb", (-.16, .12, .12), (.27, .23, .018), "yellow", .035)
        for x in [-.23, -.16, -.09]:
            box("Breakfast toast line", (x, .12, .135), (.012, .16, .008), "woodlight", .004)
        sphere("Breakfast sausage", (.19, .06, .10), (.085, .28, .08), "woodlight").rotation_euler.z = -.25
        for y in [-.08, .05, .18]:
            box("Breakfast sausage score", (.19, y, .175), (.12, .018, .008), "wood", .005)
        sphere("Breakfast egg white", (-.09, -.18, .045), (.22, .16, .03), "ivory")
        sphere("Breakfast yolk", (-.07, -.17, .09), (.085, .08, .05), "yellow")
    elif kind == 52:
        for x, y, angle in [(-.15, -.10, -.5), (.13, -.07, .5), (0, .14, -.2)]:
            sphere("Coffee bean", (x, y, .10), (.13, .085, .065), "coffee").rotation_euler.z = angle
            box("Bean crease", (x, y, .162), (.13, .014, .01), "wood", .004).rotation_euler.z = angle
    elif kind == 53:
        sphere("Coffee grounds", (0, 0, .075), (.29, .25, .07), "coffee")
        for x, y in [(-.17, .04), (.11, -.10), (.14, .08)]:
            sphere("Ground grain", (x, y, .14), (.023, .021, .012), "wood")
    elif kind in [56, 57, 58, 60, 61]:
        color = {56: "coffee", 57: "ivory", 58: "cream", 60: "latte", 61: "latte"}[kind]
        cylinder("Drink surface", (0, 0, .035), .29, .055, color)
        if kind == 56:
            ring("Espresso crema", (0, 0, .066), .245, .017, "woodlight")
        if kind in [58, 61]:
            for x, y in [(-.13, -.05), (.12, -.06), (0, .12), (0, 0)]:
                sphere("Milk foam cap", (x, y, .09), (.14, .13, .045), "ivory")
    elif kind == 43:
        sphere("Premix powder portion", (0, 0, .08), (.31, .25, .08), "cream")
        for x, y in [(-.15, .06), (.12, -.07), (.10, .11)]:
            sphere("Powder grain", (x, y, .15), (.045, .04, .012), "ivory")
    elif kind == 44:
        box("Milk carton", (0, 0, .23), (.34, .28, .44), "ivory", .035)
        box("Milk carton stripe", (0, -.149, .20), (.29, .015, .16), "blue", .008)
        box("Milk carton top", (0, 0, .47), (.37, .14, .09), "blue", .02)
        cylinder("Milk cap", (.09, -.04, .53), .04, .035, "ivory")
    elif kind == 46:
        cylinder("Sweet batter surface", (0, 0, .035), .29, .055, "cream")
        sphere("Batter swirl", (.04, 0, .07), (.16, .12, .02), "yellow")
    elif kind == 48:
        cylinder("Plain cake base", (0, 0, .12), .25, .20, "woodlight")
        sphere("Plain cake crown", (0, 0, .245), (.27, .25, .13), "yellow")
        for x, y in [(-.11, .04), (.10, .06), (.03, -.10)]:
            sphere("Baked cake crumb", (x, y, .355), (.016, .015, .008), "woodlight")
    else:
        box("Supply bag", (0, 0, .22), (.54, .38, .45), "woodlight", .06)
        box("Folded bag top", (0, 0, .45), (.57, .12, .08), "wood", .02)
        color = {12: "red", 13: "ivory", 14: "yellow", 20: "orange", 21: "teal", 29: "green", 30: "green", 31: "yellow", 40: "cream", 41: "pink", 42: "woodlight", 50: "cream", 51: "blue", 62: "coffee", 63: "tealdark"}[kind]
        box("Contents label", (0, -.202, .23), (.30, .012, .25), "ivory", .02)
        sphere("Contents symbol", (0, -.215, .25), (.083, .015, .083), color)

def character(skin, pose):
    color = ["blue", "purple", "pink", "teal"][skin]
    stride = .11 if pose in [1, 2, 6, 7] else 0
    if pose in [2, 7]:
        stride = -stride
    for x in [-1, 1]:
        box("Shoe", (x * .12, x * stride, .065), (.17, .25, .13), "dark", .04)
        box("Trouser", (x * .12, x * stride * .4, .25), (.14, .15, .30), "tealdark", .035)
    sphere("Body", (0, 0, .51), (.26, .19, .29), "ivory" if skin < 2 else color)
    box("Apron", (0, -.18, .48), (.36, .055, .35), color, .05)
    box("Apron pocket", (0, -.218, .44), (.19, .012, .12), "cream", .02)
    sphere("Head", (0, 0, .91), (.26, .24, .27), "skin")
    sphere("Nose", (0, -.234, .9), (.055, .055, .06), "skin")
    for x in [-.09, .09]:
        sphere("Eye", (x, -.218, .98), (.025, .022, .035), "dark")
    if skin < 2:
        cylinder("Chef hat band", (0, 0, 1.15), .255, .11, "ivory")
        for x, y in [(-.15, 0), (.15, 0), (0, -.09), (0, .11)]:
            sphere("Chef hat puff", (x, y, 1.24), (.17, .16, .14), "ivory")
    else:
        sphere("Hair", (0, .045, 1.09), (.27, .23, .13), "wood" if skin == 2 else "dark")
    for side in [-1, 1]:
        y = -.22 if pose in [3, 4, 5, 6, 7] else side * stride
        z = .61 + (.08 if pose == 5 and side == 1 else 0)
        sphere("Sleeve", (side * .27, y * .5, .65), (.10, .105, .14), "ivory" if skin < 2 else color)
        sphere("Hand", (side * .25, y, z - .14), (.08, .08, .085), "skin")
    if pose in [4, 5]:
        box("Work spoon", (.25, -.29, .61 if pose == 4 else .79), (.04, .04, .29), "woodlight", .01)

def environment(kind):
    if kind in [0, 1]:
        box("Floor", (0, 0, -.035), (1.55, 1.55, .065), "tile" if kind == 0 else "woodlight", .01)
        if kind == 0:
            for p in [-.765, .765]:
                box("Tile grout", (p, 0, .002), (.014, 1.55, .008), "ivory", 0)
                box("Tile grout", (0, p, .002), (1.55, .014, .008), "ivory", 0)
        else:
            for p in [-.5, 0, .5]:
                box("Plank seam", (p, 0, .002), (.018, 1.55, .008), "wood", 0)
    elif kind == 2:
        box("Low wall", (0, 0, .10), (1.52, 1.52, .23), "wood", .035)
        box("Wall cap", (0, 0, .23), (1.57, 1.57, .05), "cream", .025)
    elif kind == 3:
        cylinder("Flower pot", (0, 0, .17), .18, .32, "woodlight")
        for x, y, z in [(0, 0, .55), (-.13, 0, .46), (.1, .03, .48)]:
            sphere("Leaf bunch", (x, y, z), (.19, .16, .23), "green")
    elif kind == 4:
        box("Window frame", (0, 0, .56), (.85, .10, .90), "woodlight")
        box("Window glass", (0, -.061, .56), (.70, .015, .74), "water", .01)
        box("Crossbar", (0, -.08, .56), (.04, .025, .76), "cream", .01)
        box("Crossbar", (0, -.08, .56), (.72, .025, .04), "cream", .01)
    elif kind == 5:
        box("Chair seat", (0, 0, .34), (.50,.48,.08), "woodlight", .035)
        box("Chair back", (0,.21,.62), (.49,.075,.48), "woodlight", .04)
        box("Seat cushion", (0,0,.39), (.43,.40,.035), "teal", .025)
        for x in [-.17,.17]:
            for y in [-.16,.16]:
                box("Chair leg", (x,y,.16), (.06,.06,.34), "wood", .015)

def effect(kind):
    if kind == 0:
        for x, z in [(-.12, .17), (.08, .33), (-.03, .50)]:
            sphere("Steam", (x, 0, z), (.12, .06, .08), "ivory")
    elif kind == 1:
        sphere("Stain", (0, 0, .01), (.28, .23, .018), "soup")
        for x, y in [(-.14, .17), (.2, .10)]:
            sphere("Crumb", (x, y, .03), (.06, .05, .02), "wood")
    elif kind == 2:
        box("Order ticket", (0, 0, .03), (.38, .48, .04), "ivory", .015)
        for y in [-.12, 0, .12]:
            box("Ticket line", (0, y, .055), (.27, .025, .01), "teal", .005)
    elif kind == 3:
        ring("Upgrade ring", (0, 0, .02), .30, .045, "yellow")
    elif kind == 4:
        for x in [-.17, .04, .20]:
            sphere("Splash", (x, 0, .18 + x), (.06, .035, .13), "water")

records = []
for i, name in enumerate(["kitchen_floor", "dining_floor", "low_wall", "plant", "window", "chair"]):
    records.append((name, "environment", i, 0, 0))
station_start = len(records)
for kind in range(16):
    for facing in range(4):
        records.append((f"station_{kind}_{facing}", "station", kind, facing, 0))
item_start = len(records)
for kind in range(1, 64):
    records.append((f"item_{kind}", "item", kind, 2, 0))
character_start = len(records)
for skin in range(4):
    for facing in range(4):
        for pose in range(8):
            records.append((f"character_{skin}_{facing}_{pose}", "character", skin, facing, pose))
effect_start = len(records)
for kind in range(5):
    records.append((f"effect_{kind}", "effect", kind, 2, 0))
rows = math.ceil(len(records) / COLS)
atlas = np.zeros((rows * CELL, COLS * CELL, 4), dtype=np.float32)
models = {}
sample_names = {"station_1_2", "station_2_2", "item_1", "item_2", "item_5", "item_6", "item_7", "character_0_2_0"}
sample_index = 0
sample_pixels = np.zeros((CELL * 2, CELL * 4, 4), dtype=np.float32)

for index, (name, group, kind, facing, pose) in enumerate(records):
    if SAMPLE and name not in sample_names:
        continue
    key = (group, kind, pose)
    if key not in models:
        current_root = bpy.data.objects.new(f"{group}_{kind}_pose_{pose}", None)
        scene.collection.objects.link(current_root)
        {"environment": environment, "station": cabinet, "item": food,
            "effect": effect}.get(group, lambda k: character(k, pose))(kind)
        models[key] = current_root
    for root in models.values():
        for child in root.children:
            child.hide_render = root != models[key]
    current_root = models[key]
    current_root.rotation_euler.z = [math.pi, math.pi / 2, 0, -math.pi / 2][facing] if group in ["station", "character"] else 0
    camera.data.ortho_scale = 1.7 if group == "station" else 1.65 if group == "environment" else 1.85 if group == "character" else 1.15
    aim_z = .42 if group == "station" else .62 if group == "character" else .12 if group in ["item", "effect"] else .14
    camera.location = (0, -6, 6 + aim_z)
    camera.rotation_euler = (Vector((0, 0, aim_z)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    render_temp = ROOT / "Unity/Temp/CarzyCookerArt"
    render_temp.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(render_temp / "current_render.png")
    bpy.ops.render.render(write_still=True)
    rendered = bpy.data.images.load(scene.render.filepath, check_existing=False)
    pixels = np.empty(CELL * CELL * 4, dtype=np.float32)
    rendered.pixels.foreach_get(pixels)
    pixels = pixels.reshape(CELL, CELL, 4)
    bpy.data.images.remove(rendered)
    row, col = divmod(index, COLS)
    atlas[row * CELL:(row + 1) * CELL, col * CELL:(col + 1) * CELL] = pixels
    if name in sample_names:
        sr, sc = divmod(sample_index, 4)
        sample_pixels[sr * CELL:(sr + 1) * CELL, sc * CELL:(sc + 1) * CELL] = pixels
        sample_index += 1
    print(f"ART {index + 1}/{len(records)} {name}", flush=True)

def save_image(name, pixels, path):
    image = bpy.data.images.new(name, width=pixels.shape[1], height=pixels.shape[0], alpha=True)
    image.pixels.foreach_set(pixels.reshape(-1))
    image.filepath_raw, image.file_format = str(path), "PNG"
    image.save()
    bpy.data.images.remove(image)

save_image("Art sample", sample_pixels, SOURCE / "sample-sheet.png")
if not SAMPLE:
    save_image("Restaurant atlas", atlas, OUTPUT / "RestaurantAtlas.png")
    with (SOURCE / "assets.csv").open("w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(["index", "name", "category", "definition_or_skin", "facing", "pose"])
        for index, record in enumerate(records):
            writer.writerow([index, *record])
    LAYOUT.write_text("// <auto-generated> Art/CarzyCooker/export_art.py; do not edit. </auto-generated>\n"
        "namespace ET.Client\n{\n    public partial class MonoGridKitchenBoard\n    {\n"
        f"        private const int ArtCell = {CELL}, ArtColumns = {COLS}, ArtRows = {rows}, ArtCount = {len(records)};\n"
        "        private const int ArtPoseCount = 8;\n"
        f"        private const int ArtItemCount = {character_start - item_start};\n"
        f"        private const int ArtStationStart = {station_start}, ArtItemStart = {item_start}, ArtCharacterStart = {character_start}, ArtEffectStart = {effect_start};\n"
        "    }\n}\n", encoding="utf-8")
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / "RestaurantModels.blend"))
print("ART EXPORT COMPLETE", flush=True)
