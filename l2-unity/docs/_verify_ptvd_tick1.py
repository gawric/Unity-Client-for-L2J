import math

particles = [
    {"id": 0, "spawn": (-0.809, 1.039, 9.007), "vel": (5.331, -6.847, 14.569), "age": 0.010445},
    {"id": 1, "spawn": (2.348, -0.280, 6.587), "vel": (-20.126, 2.404, 1.182), "age": 0.010345},
    {"id": 2, "spawn": (-1.479, 0.154, 8.884), "vel": (9.849, -1.027, 14.852), "age": 0.010245},
    {"id": 3, "spawn": (0.897, -2.214, 6.772), "vel": (-7.497, 18.500, 3.468), "age": 0.010145},
    {"id": 4, "spawn": (-2.396, -0.110, 7.086), "vel": (19.215, 0.885, 9.720), "age": 0.010045},
    {"id": 5, "spawn": (-0.574, -2.131, 7.944), "vel": (4.176, 15.506, 2.452), "age": 0.009945},
    {"id": 6, "spawn": (0.805, 1.021, 9.017), "vel": (-5.302, -6.725, 5.802), "age": 0.009845},
    {"id": 7, "spawn": (0.798, 0.944, 9.057), "vel": (-5.238, -6.197, 8.063), "age": 0.009745},
    {"id": 8, "spawn": (-1.539, -1.265, 8.339), "vel": (10.769, 8.852, 16.429), "age": 0.009645},
]
owner = (0.0, 0.0, 0.0)
acc = (0.0, 0.0, -40.0)


def norm(v):
    length = math.sqrt(sum(x * x for x in v))
    return tuple(x / length for x in v)


print("Tick1 PTVD inference from vSpawn (component model)")
for p in particles:
    spawn = p["spawn"]
    age = p["age"]
    vel = p["vel"]
    v_spawn = tuple(vel[i] - acc[i] * age for i in range(3))
    direction = norm(tuple(spawn[i] - owner[i] for i in range(3)))
    raw = tuple((-v_spawn[i] / direction[i] if abs(direction[i]) > 1e-6 else 0.0) for i in range(3))
    pred = tuple(-raw[i] * direction[i] for i in range(3))
    err = math.sqrt(sum((pred[i] - v_spawn[i]) ** 2 for i in range(3)))
    xy_ok = abs(raw[0] - 60.0) <= 2.0 and abs(raw[1] - 60.0) <= 2.0
    z_ok = -18.5 <= raw[2] <= 1.5
    comp_ok = err < 0.05
    print(
        f"P{p['id']}: comp={comp_ok} err={err:.6f} raw=({raw[0]:.2f},{raw[1]:.2f},{raw[2]:.2f}) "
        f"xy={xy_ok} z={z_ok}"
    )
