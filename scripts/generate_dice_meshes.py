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

def write_obj(name, vertices, faces):
    path = f"assets/models/dice/{name}.obj"
    os.makedirs(os.path.dirname(path), exist_ok=True)
    
    out_v, out_n, out_f, face_meta = [], [], [], []
    
    for f_idx, face in enumerate(faces):
        # face is a list of vertex indices
        center = [sum(vertices[i][j] for i in face)/len(face) for j in range(3)]
        v0, v1, v2 = vertices[face[0]], vertices[face[1]], vertices[face[2]]
        normal = get_normal(v0, v1, v2)
        
        # Ensure normal points away from origin
        if dot(normal, center) < 0:
            normal = [-n for n in normal]
            face = list(face)
            face.reverse()

        # Label orientation (Up points toward nearest pole)
        ref = (0, 1, 0) if abs(normal[1]) < 0.9 else (0, 0, -1)
        up = [ref[i] - normal[i] * dot(ref, normal) for i in range(3)]
        mag = math.sqrt(sum(x*x for x in up))
        up = [x/mag for x in up]

        # Standard metadata (one label per face)
        labels = []
        if name != "d4":
            labels.append({
                "text": str(f_idx + 1 if name != "d10" else (f_idx + 1) % 10),
                "pos": [center[i] + normal[i]*0.2 for i in range(3)],
                "up": up
            })
        else:
            # D4 Special: 3 labels per face near vertices
            # Result is the vertex index
            for v_idx in face:
                v = vertices[v_idx]
                # Position is 70% toward the vertex from the face center
                lp = [center[i] + (v[i] - center[i]) * 0.7 + normal[i]*0.2 for i in range(3)]
                # Up for d4 label points toward that vertex
                lu = [v[i] - center[i] for i in range(3)]
                lmag = math.sqrt(sum(x*x for x in lu))
                lu = [x/lmag for x in lu]
                labels.append({"text": "SET_BY_CODE", "vertex_idx": v_idx, "pos": lp, "up": lu})

        face_meta.append({
            "normal": normal,
            "labels": labels
        })

        # Triangulate for OBJ
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
            
    metadata[name] = face_meta
    print(f"Generated {path}")

# --- Geometry Data ---
phi = (1 + math.sqrt(5)) / 2

def generate_all():
    # D4
    s = 30.0
    v_d4 = [(s,s,s), (s,-s,-s), (-s,s,-s), (-s,-s,s)]
    f_d4 = [(0,1,2), (0,2,3), (0,3,1), (1,3,2)]
    write_obj("d4", v_d4, f_d4)

    # D6
    s = 25.0
    v_d6 = []
    for x in [-s,s]: 
        for y in [-s,s]:
            for z in [-s,s]: v_d6.append((x,y,z))
    f_d6 = [(0,2,3,1), (4,5,7,6), (0,1,5,4), (2,6,7,3), (0,4,6,2), (1,3,7,5)]
    write_obj("d6", v_d6, f_d6)

    # D8
    s = 40.0
    v_d8 = [(s,0,0), (-s,0,0), (0,s,0), (0,-s,0), (0,0,s), (0,0,-s)]
    f_d8 = [(0,2,4), (0,4,3), (0,3,5), (0,5,2), (1,4,2), (1,3,4), (1,5,3), (1,2,5)]
    write_obj("d8", v_d8, f_d8)

    # D10
    s, h = 40.0, 50.0
    v_d10 = [(0, h, 0), (0, -h, 0)]
    for i in range(5):
        a = i * 2 * math.pi / 5
        v_d10.append((s * math.cos(a), 0, s * math.sin(a)))
    f_d10 = []
    for i in range(5):
        f_d10.append((0, 2+i, 2+(i+1)%5))
        f_d10.append((1, 2+(i+1)%5, 2+i))
    write_obj("d10", v_d10, f_d10)

    # D12
    s = 20.0
    v_d12 = []
    for x in [-1,1]:
        for y in [-1,1]:
            for z in [-1,1]: v_d12.append((x*s, y*s, z*s))
    for y in [-1/phi, 1/phi]:
        for z in [-phi, phi]: v_d12.append((0, y*s, z*s))
    for x in [-1/phi, 1/phi]:
        for y in [-phi, phi]: v_d12.append((x*s, y*s, 0))
    for x in [-phi, phi]:
        for z in [-1/phi, 1/phi]: v_d12.append((x*s, 0, z*s))
    f_d12 = [
        (0, 16, 2, 10, 8), (0, 8, 4, 14, 12), (0, 12, 1, 17, 16),
        (1, 9, 11, 3, 17), (1, 17, 16, 2, 13), (1, 12, 14, 5, 9),
        (2, 13, 3, 11, 10), (2, 10, 8, 18, 6), (3, 13, 2, 10, 11),
        (7, 15, 13, 3, 11), (7, 11, 10, 6, 18), (7, 18, 4, 14, 5)
    ]
    write_obj("d12", v_d12, f_d12)

    # D20
    s = 30.0
    v_d20 = []
    for y in [-1, 1]:
        for z in [-phi, phi]: v_d20.append((0, y*s, z*s))
    for x in [-1, 1]:
        for y in [-phi, phi]: v_d20.append((x*s, y*s, 0))
    for x in [-phi, phi]:
        for z in [-1, 1]: v_d20.append((x*s, 0, z*s))
    f_d20 = [
        (0,8,1), (0,1,4), (0,4,2), (0,2,10), (0,10,8),
        (1,8,9), (1,9,7), (1,7,4), (4,7,5), (4,5,2),
        (2,5,3), (2,3,10), (10,3,11), (10,11,8), (8,11,9),
        (6,11,3), (6,3,5), (6,5,7), (6,7,9), (6,9,11)
    ]
    write_obj("d20", v_d20, f_d20)

if __name__ == "__main__":
    generate_all()
    with open("assets/models/dice/metadata.json", "w") as f: json.dump(metadata, f, indent=2)
