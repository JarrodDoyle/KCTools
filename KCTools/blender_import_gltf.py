import bpy
import sys

argv = sys.argv
input_path = argv[argv.index("--") + 1]

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete()
bpy.ops.outliner.orphans_purge()
bpy.ops.import_scene.gltf(filepath=input_path)

for window in bpy.context.window_manager.windows:
    for area in window.screen.areas: # iterate through areas in current screen
        if area.type == 'VIEW_3D':
            for space in area.spaces: # iterate through spaces in current VIEW_3D area
                if space.type == 'VIEW_3D': # check if space is a 3D view
                    space.shading.type = 'MATERIAL'