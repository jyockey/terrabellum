import os
import math
import json

def get_normal(v0, v1, v2):
    ax, ay, az = v1[0] - v0[0], v1[1] - v0[1], v1[2] - v0[2]
    bx, by, bz = v2[0] - v0[0], v2[1] - v0[1], v2[2] - v0[2]
    mag = math.sqrt((ay * bz - az * by)**2 + (az * bx - ax * bz)**2 + (ax * by - ay * bx)**2)
    return ((ay * bz - az * by) / mag, (az * bx - ax * bz) / mag, (ax * by - ay * bx) / mag) if mag > 0 else (0, 1, 0)

def dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]

metadata = {}

def normalize_dice(name, vertices, faces):
    """
    Normalizes dice so they match standard polyhedral set proportions.
    The d6 face-to-face width will be exactly 1.0.
    Others are scaled relative to the d6 based on Chessex standards.
    """
    # 1. Center the mesh
    min_v = [min(v[i] for v in vertices) for i in range(3)]
    max_v = [max(v[i] for v in vertices) for i in range(3)]
    center = [(max_v[i] + min_v[i]) / 2.0 for i in range(3)]
    vertices = [[v[i] - center[i] for i in range(3)] for v in vertices]

    # 2. Calculate current face-to-face distance (average distance to face centers * 2)
    face_centers = []
    for face in faces:
        face_centers.append([sum(vertices[i][j] for i in face)/len(face) for j in range(3)])
    
    avg_dist = sum(math.sqrt(sum(c[i]**2 for i in range(3))) for c in face_centers) / len(face_centers)
    current_f2f = avg_dist * 2.0

    # 3. Target face-to-face widths (relative to d6 = 1.0)
    # Based on Chessex 16mm set ratios:
    ratios = {
        "d4": 1.0,   # d4 is usually measured by edge, but we'll match height/volume feel
        "d6": 1.0,
        "d8": 1.0,
        "d10": 1.0,
        "d12": 1.125, # 18mm / 16mm
        "d20": 1.375  # 22mm / 16mm
    }
    
    target_f2f = ratios.get(name, 1.0)
    scale_factor = target_f2f / current_f2f
    
    return [[v[i] * scale_factor for i in range(3)] for v in vertices], target_f2f

def write_obj(name, vertices, faces):
    vertices, target_f2f = normalize_dice(name, vertices, faces)
    path = f"assets/models/dice/{name}.obj"
    os.makedirs(os.path.dirname(path), exist_ok=True)
    
    out_v, out_n, out_f, face_meta = [], [], [], []
    
    for f_idx, face in enumerate(faces):
        center = [sum(vertices[i][j] for i in face)/len(face) for j in range(3)]
        v0, v1, v2 = vertices[face[0]], vertices[face[1]], vertices[face[2]]
        normal = get_normal(v0, v1, v2)
        
        if dot(normal, center) < 0:
            normal = [-n for n in normal]
            face = list(face)
            face.reverse()

        ref = (0, 1, 0) if abs(normal[1]) < 0.9 else (0, 0, -1)
        up = [ref[i] - normal[i] * dot(ref, normal) for i in range(3)]
        mag = math.sqrt(sum(x*x for x in up))
        up = [x/mag for x in up]

        labels = []
        # Label offset is very small (0.01 units) to stay on surface
        offset = 0.01
        if name != "d4":
            labels.append({
                "text": str(f_idx + 1 if name != "d10" else (f_idx + 1) % 10),
                "pos": [center[i] + normal[i]*offset for i in range(3)],
                "up": up
            })
        else:
            for v_idx in face:
                v = vertices[v_idx]
                lp = [center[i] + (v[i] - center[i]) * 0.7 + normal[i]*offset for i in range(3)]
                lu = [v[i] - center[i] for i in range(3)]
                lmag = math.sqrt(sum(x*x for x in lu))
                lu = [x/lmag for x in lu]
                labels.append({"text": str(v_idx + 1), "vertex_idx": v_idx, "pos": lp, "up": lu})

        face_meta.append({
            "normal": normal,
            "labels": labels
        })

        for i in range(1, len(face) - 1):
            n_idx = len(out_n)
            out_n.append(normal)
            poly = [face[0], face[i], face[i+1]]
            face_indices = []
            for v_idx in poly:
                out_v.append(vertices[v_idx])
                face_indices.append((len(out_v), n_idx + 1))
            out_f.append(face_indices)

    with open(path, "w") as f:
        for v in out_v: f.write(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n")
        for n in out_n: f.write(f"vn {n[0]:.4f} {n[1]:.4f} {n[2]:.4f}\n")
        for poly in out_f: f.write("f " + " ".join(f"{v}//{n}" for v, n in poly) + "\n")
            
    metadata[name] = {
        "f2f_scale": target_f2f,
        "is_bottom_result": (name == "d4"),
        "faces": face_meta
    }
    print(f"Generated {path}")

phi = (1 + math.sqrt(5)) / 2

def generate_all():
    # D4
    v_d4 = [(0, 1, 0), (1, -0.33, 0), (-0.5, -0.33, 0.866), (-0.5, -0.33, -0.866)]
    f_d4 = [(1, 2, 3), (0, 2, 1), (0, 3, 2), (0, 1, 3)]
    write_obj("d4", v_d4, f_d4)

    # D6
    v_d6 = []
    for x in [-1,1]: 
        for y in [-1,1]:
            for z in [-1,1]: v_d6.append((x,y,z))
    f_d6 = [(0,2,3,1), (4,5,7,6), (0,1,5,4), (2,6,7,3), (0,4,6,2), (1,3,7,5)]
    write_obj("d6", v_d6, f_d6)

    # D8
    v_d8 = [(1,0,0), (-1,0,0), (0,1,0), (0,-1,0), (0,0,1), (0,0,-1)]
    f_d8 = [(0,2,4), (0,4,3), (0,3,5), (0,5,2), (1,4,2), (1,3,4), (1,5,3), (1,2,5)]
    write_obj("d8", v_d8, f_d8)

    # D10
    h_pole, h_ring, r_ring = 2.0, 0.5, 1.5
    v_d10 = [(0, h_pole, 0), (0, -h_pole, 0)]
    for i in range(5):
        a = i * 2 * math.pi / 5
        v_d10.append((r_ring * math.cos(a), h_ring, r_ring * math.sin(a)))
        v_d10.append((r_ring * math.cos(a + math.pi/5), -h_ring, r_ring * math.sin(a + math.pi/5)))
    f_d10 = []
    for i in range(5):
        t_curr = 2 + 2*i
        b_curr = 2 + 2*i + 1
        t_next = 2 + (2*i + 2) % 10
        f_d10.append((0, t_curr, b_curr, t_next))
        b_next = 2 + (2*i + 3) % 10
        f_d10.append((1, b_curr, t_next, b_next))
    write_obj("d10", v_d10, f_d10)

    # D12
    v_d12 = []
    for x in [-1,1]:
        for y in [-1,1]:
            for z in [-1,1]: v_d12.append((x, y, z))
    for y in [-1/phi, 1/phi]:
        for z in [-phi, phi]: v_d12.append((0, y, z))
    for x in [-1/phi, 1/phi]:
        for y in [-phi, phi]: v_d12.append((x, y, 0))
    for x in [-phi, phi]:
        for z in [-1/phi, 1/phi]: v_d12.append((x, 0, z))
    f_d12 = [
        (3, 11, 7, 15, 13), (7, 11, 9, 5, 19), (5, 9, 1, 12, 14),
        (1, 9, 11, 3, 17), (3, 13, 2, 16, 17), (1, 17, 16, 0, 12),
        (0, 8, 10, 2, 16), (0, 12, 14, 4, 8), (4, 14, 5, 19, 18),
        (4, 8, 10, 6, 18), (6, 10, 2, 13, 15), (6, 18, 19, 7, 15)
    ]
    write_obj("d12", v_d12, f_d12)

    # D20
    v_d20 = []
    for z in [-phi, phi]:
        for x in [-1, 1]: v_d20.append((x, 0, z))
    for x in [-phi, phi]:
        for y in [-1, 1]: v_d20.append((x, y, 0))
    for y in [-phi, phi]:
        for z in [-1, 1]: v_d20.append((0, y, z))
    f_d20 = [
        (0,10,5), (0,5,4), (0,4,8), (0,8,1), (0,1,10),
        (3,11,7), (3,7,6), (3,6,9), (3,9,2), (3,2,11),
        (1,10,7), (1,7,6), (1,6,8), (8,6,9), (8,9,4),
        (4,9,2), (4,2,5), (5,2,11), (5,11,10), (10,11,7)
    ]
    write_obj("d20", v_d20, f_d20)

if __name__ == "__main__":
    generate_all()
    with open("assets/models/dice/metadata.json", "w") as f: json.dump(metadata, f, indent=2)
