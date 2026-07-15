import math

# Reconstruct spawn velocity from post-UpdateParticles log:
#   vSpawn = loggedVelocity - acceleration * particleTime
# (UpdateParticles applies vel += acc*dt before logging at end of tick)

particles = [
    {"id": 0, "spawn": (-1.752, -1.530, 6.410), "vel": (15.417, 13.465, 5.808)},
    {"id": 1, "spawn": (0.861, -1.562, 8.606), "vel": (-5.878, 10.660, 8.553)},
    {"id": 2, "spawn": (1.831, -1.381, 7.708), "vel": (-13.659, 10.302, 6.834)},
    {"id": 3, "spawn": (0.482, 1.191, 9.027), "vel": (-3.169, -7.838, 7.050)},
    {"id": 4, "spawn": (-1.764, -0.548, 8.532), "vel": (12.123, 3.764, 7.755)},
    {"id": 5, "spawn": (-0.380, 1.672, 8.679), "vel": (2.574, -11.339, 2.220)},
    {"id": 6, "spawn": (0.296, 0.646, 9.292), "vel": (-1.904, -4.161, 14.229)},
    {"id": 7, "spawn": (-1.672, 0.154, 8.715), "vel": (11.306, -1.044, 4.504)},
    {"id": 8, "spawn": (2.044, -1.228, 6.727), "vel": (-17.181, 10.325, 3.124)},
]
owner = (0.0, 0.0, 0.0)


def norm(v):
    l = math.sqrt(sum(x * x for x in v))
    return tuple(x / l for x in v)


def dot(a, b):
    return sum(x * y for x, y in zip(a, b))


print("=== Fingerprint: SE0 from m_u004_b ===")
print("accel Z=-40, lifetime 1.0-1.8, maxParticles=12, PTVD_StartPositionAndOwner")
print()

ages = {0: 0.0113, 1: 0.0112, 2: 0.0111, 3: 0.0110, 4: 0.0109, 5: 0.0108, 6: 0.0107, 7: 0.0106, 8: 0.0105}

print("=== PTVD component on vSpawn: v = -raw * dir, dir=norm(spawn-owner) ===")
for p in particles:
    s = p["spawn"]
    dir_so = norm(tuple(s[i] - owner[i] for i in range(3)))
    acc = (0.0, 0.0, -40.0)
    age = ages[p["id"]]
    v_logged = p["vel"]
    v_spawn = tuple(v_logged[i] - acc[i] * age for i in range(3))
    rz = -v_spawn[2] / dir_so[2] if abs(dir_so[2]) > 1e-6 else float("nan")
    pred = (-60 * dir_so[0], -60 * dir_so[1], -rz * dir_so[2])
    err = math.sqrt(sum((pred[i] - v_spawn[i]) ** 2 for i in range(3)))
    ok = "PASS" if err < 0.05 else "FAIL"
    print(
        f"P{p['id']}: err={err:.4f} {ok} vSpawn=({v_spawn[0]:.3f},{v_spawn[1]:.3f},{v_spawn[2]:.3f}) "
        f"pred=({pred[0]:.3f},{pred[1]:.3f},{pred[2]:.3f}) rz={rz:.3f}"
    )

print()
print("=== OLD (wrong): compare logged velocity directly without acc correction ===")
for p in particles:
    s = p["spawn"]
    dir_so = norm(tuple(s[i] - owner[i] for i in range(3)))
    rz = -p["vel"][2] / dir_so[2] if abs(dir_so[2]) > 1e-6 else float("nan")
    pred = (-60 * dir_so[0], -60 * dir_so[1], -rz * dir_so[2])
    err = math.sqrt(sum((pred[i] - p["vel"][i]) ** 2 for i in range(3)))
    ok = "PASS" if err < 0.05 else "FAIL"
    print(f"P{p['id']}: err={err:.4f} {ok}")

print()
print("=== PTVD projection on logged velocity (should FAIL) ===")
for p in particles:
    s = p["spawn"]
    dir_so = norm(tuple(s[i] - owner[i] for i in range(3)))
    best = None
    for rz100 in range(-1800, 101):
        rz = rz100 / 100.0
        rawv = (60.0, 60.0, rz)
        d = dot(rawv, dir_so)
        pred = tuple(dir_so[i] * d for i in range(3))
        err = math.sqrt(sum((pred[i] - p["vel"][i]) ** 2 for i in range(3)))
        if best is None or err < best[0]:
            best = (err, rz, pred)
    ok = "PASS" if best[0] < 0.05 else "FAIL"
    print(
        f"P{p['id']}: err={best[0]:.4f} {ok} rz={best[1]:.3f} "
        f"pred=({best[2][0]:.3f},{best[2][1]:.3f},{best[2][2]:.3f})"
    )

print()
print("=== Motion: delta loc vs velocity*particleTime (tick3) ===")
for p in particles[:3]:
    # user provided oldLocal=startLocation at tick3
    s = p["spawn"]
    # approximate loc from first 3 in user message
    locs = {
        0: (-1.578, -1.378, 6.475),
        1: (0.795, -1.442, 8.702),
        2: (1.679, -1.266, 7.784),
    }
    loc = locs[p["id"]]
    delta = tuple(loc[i] - s[i] for i in range(3))
    times = {0: 0.0113, 1: 0.0112, 2: 0.0111}
    t = times[p["id"]]
    pred = tuple(p["vel"][i] * t for i in range(3))
    err = math.sqrt(sum((pred[i] - delta[i]) ** 2 for i in range(3)))
    print(f"P{p['id']}: delta={tuple(round(x,4) for x in delta)} vel*t={tuple(round(x,4) for x in pred)} err={err:.5f}")
