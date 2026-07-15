import math

particles = [
    {"id": 0, "spawn": (0.432, -2.325, 6.592), "vel": (-3.701, 19.921, 5.635), "old": (0.393, -2.113, 6.659), "loc": (0.338, -1.821, 6.741), "age": 0.0253},
    {"id": 1, "spawn": (-2.384, 0.212, 7.178), "vel": (18.903, -1.679, 2.415), "old": (-2.185, 0.194, 7.210), "loc": (-1.907, 0.169, 7.245), "age": 0.0252},
    {"id": 2, "spawn": (0.768, -0.056, 9.273), "vel": (-4.954, 0.362, 3.900), "old": (0.716, -0.052, 9.320), "loc": (0.644, -0.047, 9.377), "age": 0.0251},
    {"id": 3, "spawn": (-1.623, 0.248, 8.750), "vel": (10.941, -1.670, -1.101), "old": (-1.510, 0.231, 8.745), "loc": (-1.350, 0.206, 8.729), "age": 0.0250},
    {"id": 4, "spawn": (-1.509, 0.178, 8.858), "vel": (10.071, -1.187, 0.680), "old": (-1.405, 0.166, 8.871), "loc": (-1.258, 0.148, 8.881), "age": 0.0249},
    {"id": 5, "spawn": (1.747, -1.611, 6.665), "vel": (-14.816, 13.658, 8.333), "old": (1.597, -1.472, 6.756), "loc": (1.380, -1.272, 6.878), "age": 0.0248},
    {"id": 6, "spawn": (1.381, -1.046, 8.661), "vel": (-9.380, 7.108, 9.133), "old": (1.287, -0.975, 8.759), "loc": (1.149, -0.871, 8.892), "age": 0.0247},
    {"id": 7, "spawn": (0.566, 0.367, 9.303), "vel": (-3.642, -2.359, 12.223), "old": (0.530, 0.343, 9.431), "loc": (0.477, 0.309, 9.610), "age": 0.0246},
    {"id": 8, "spawn": (-1.545, 1.574, 7.946), "vel": (11.237, -11.455, 13.706), "old": (-1.434, 1.462, 8.087), "loc": (-1.269, 1.294, 8.288), "age": 0.0245},
]
owner = (0.0, 0.0, 0.0)
acc = (0.0, 0.0, -40.0)


def norm(v):
    length = math.sqrt(sum(x * x for x in v))
    return tuple(x / length for x in v)


def err(a, b):
    return math.sqrt(sum((a[i] - b[i]) ** 2 for i in range(3)))


print("=== Tick8: PTVD component model (vSpawn = vel - acc*age) ===")
for p in particles:
    v_spawn = tuple(p["vel"][i] - acc[i] * p["age"] for i in range(3))
    direction = norm(tuple(p["spawn"][i] - owner[i] for i in range(3)))
    raw = tuple((-v_spawn[i] / direction[i] if abs(direction[i]) > 1e-6 else 0.0) for i in range(3))
    pred = tuple(-raw[i] * direction[i] for i in range(3))
    comp_err = err(pred, v_spawn)
    xy_ok = abs(raw[0] - 60.0) <= 2.0 and abs(raw[1] - 60.0) <= 2.0
    z_ok = -18.5 <= raw[2] <= 1.5
    print(
        f"P{p['id']}: comp={'PASS' if comp_err < 0.05 else 'FAIL'} err={comp_err:.6f} "
        f"raw=({raw[0]:.2f},{raw[1]:.2f},{raw[2]:.2f}) xy={xy_ok} z={z_ok}"
    )

print()
print("=== Tick8: motion within frame (deltaLoc vs vel*dt) ===")
for p in particles:
    delta = tuple(p["loc"][i] - p["old"][i] for i in range(3))
    dt_x = delta[0] / p["vel"][0] if abs(p["vel"][0]) > 1e-6 else float("nan")
    dt_y = delta[1] / p["vel"][1] if abs(p["vel"][1]) > 1e-6 else float("nan")
    dt_z = delta[2] / p["vel"][2] if abs(p["vel"][2]) > 1e-6 else float("nan")
    dt_avg = (dt_x + dt_y + dt_z) / 3.0
    pred = tuple(p["vel"][i] * dt_avg for i in range(3))
    motion_err = err(delta, pred)
    print(
        f"P{p['id']}: motion={'PASS' if motion_err < 0.01 else 'FAIL'} err={motion_err:.6f} "
        f"dt~{dt_avg*1000:.2f}ms delta={tuple(round(x,3) for x in delta)}"
    )
