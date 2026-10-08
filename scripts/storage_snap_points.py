"""Derive placement points from the closed furniture's collision/support envelope."""
import math

CLEARANCE = .01

def apply(models, layouts, snaps):
    for name, layout in layouts.items():
        mount = layout['mount']
        # These families remain shelved, including their authored placement data.
        if layout.get('display') or mount == 'slope':
            continue
        parts = models[name]
        vertices = [v for part in parts for v in part['vertices']
                    if mount == 'corner' or part['material'] != 2]
        low = [min(v[axis] for v in vertices) for axis in range(3)]
        high = [max(v[axis] for v in vertices) for axis in range(3)]
        old = snaps[name]
        if mount == 'floor':
            # Rear support points must be behind every frame/closed-door part.
            points = [[x, y, low[2] - CLEARANCE]
                      for x in (low[0] - CLEARANCE, (low[0] + high[0]) / 2, high[0] + CLEARANCE)
                      for y in (low[1], high[1])]
            # Retain front-to-back floor snapping on racks, pallets and bins
            # which already supported it, moving their front anchors out of the frame.
            if any(p[2] > (low[2] + high[2]) / 2 + .0001 for p in old):
                points += [[x, low[1], high[2] + CLEARANCE]
                           for x in (low[0] - CLEARANCE, high[0] + CLEARANCE)]
        elif mount == 'wall':
            points = [[x, y, low[2] - CLEARANCE] for x, y, z in old]
            points.append([sum(p[0] for p in old) / len(old),
                           sum(p[1] for p in old) / len(old), low[2] - CLEARANCE])
        elif mount == 'ceiling':
            # Preserve the designed beam/plate positions, including single hooks.
            points = [[x, high[1] + CLEARANCE, z] for x, y, z in old]
            points.append([sum(p[0] for p in old) / len(old), high[1] + CLEARANCE,
                           sum(p[2] for p in old) / len(old)])
        elif mount == 'corner':
            # The cabinet has two 45-degree back faces. Offset each support plane
            # by 1cm along its normal rather than flattening the triangular body.
            left = min(v[0] + v[2] for v in vertices) - math.sqrt(2) * CLEARANCE
            right = min(-v[0] + v[2] for v in vertices) - math.sqrt(2) * CLEARANCE
            corner_x, corner_z = (left - right) / 2, (left + right) / 2
            outline = [(corner_x, corner_z)]
            outline += [(x, left - x) for x in (low[0], (low[0] + corner_x) / 2)]
            outline += [(x, right + x) for x in ((high[0] + corner_x) / 2, high[0])]
            points = [[x, y, z] for x, z in outline for y in (low[1], high[1])]
        else:
            raise ValueError('Unknown storage mount: ' + mount)
        unique = []
        for point in points:
            if not any(math.dist(point, saved) < 1e-6 for saved in unique):
                unique.append(point)
        if len(unique) > 16:
            raise ValueError('Storage snap count exceeds runtime limit: ' + name)
        snaps[name] = unique
