"""Preserve deliberate two-sided polygons through FBX triangulation."""
def separate_duplicate_faces(mesh):
    seen = set()
    replacements = []
    coordinates = []
    first_new = len(mesh.vertices)
    for polygon in mesh.polygons:
        key = tuple(sorted(polygon.vertices))
        if key in seen:
            for loop_index in polygon.loop_indices:
                vertex_index = mesh.loops[loop_index].vertex_index
                replacements.append((loop_index, first_new + len(coordinates)))
                coordinates.append(tuple(mesh.vertices[vertex_index].co))
        seen.add(key)
    if coordinates:
        mesh.vertices.add(len(coordinates))
        for index, coordinate in enumerate(coordinates, first_new):
            mesh.vertices[index].co = coordinate
        for loop_index, vertex_index in replacements:
            mesh.loops[loop_index].vertex_index = vertex_index
        mesh.update(calc_edges=True)
    return len(coordinates)
