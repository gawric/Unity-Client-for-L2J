import re
import math

K = 1.8


def parse_l2_se0_tick7(path):
    with open(path, encoding="utf-8", errors="replace") as f:
        text = f.read()
    marker = "--- Tick 7 wallTime=2026-07-15 15:15:08.263 ---"
    idx = text.find(marker)
    if idx < 0:
        print("L2 tick7 not found")
        return
    chunk = text[idx : idx + 25000]
    entries = []
    for m in re.finditer(
        r"SpriteEmitter\[0\] Particle\[(\d+)\] Tick7.*?locLocal=\(([-0-9.]+), ([-0-9.]+), ([-0-9.]+)\).*?particleTime=([0-9.]+).*?size=\(([-0-9.]+)",
        chunk,
        re.S,
    ):
        slot = int(m.group(1))
        x, y, z = map(float, m.group(2, 3, 4))
        t = float(m.group(5))
        sz = float(m.group(6))
        entries.append((slot, x, y, z, math.hypot(x, y), t, sz))
    summarize("L2 SE0 Tick7", entries)


def parse_unity_se0_by_time(path, t_min=0.05, t_max=0.07):
    with open(path, encoding="utf-8", errors="replace") as f:
        text = f.read()
    entries = []
    for m in re.finditer(
        r"SpriteEmitter\[2\] Particle\[(\d+)\] Tick\d+.*?locLocal=\(([-0-9.]+), ([-0-9.]+), ([-0-9.]+)\).*?particleTime=([0-9.]+).*?size=\(([-0-9.]+)",
        text,
        re.S,
    ):
        slot = int(m.group(1))
        x, y, z = map(float, m.group(2, 3, 4))
        t = float(m.group(5))
        if t_min <= t <= t_max:
            sz = float(m.group(6))
            entries.append((slot, x, y, z, math.hypot(x, y), t, sz))
    summarize(f"Unity SE0 particleTime [{t_min},{t_max}]", entries)


def parse_l2_se0_by_time(path, t_min=0.05, t_max=0.07):
    with open(path, encoding="utf-8", errors="replace") as f:
        text = f.read()
    entries = []
    for m in re.finditer(
        r"SpriteEmitter\[0\] Particle\[(\d+)\] Tick\d+.*?layerIndex=2 kind=Sprite name=SpriteEmitter8.*?locLocal=\(([-0-9.]+), ([-0-9.]+), ([-0-9.]+)\).*?particleTime=([0-9.]+).*?size=\(([-0-9.]+)",
        text,
        re.S,
    ):
        slot = int(m.group(1))
        x, y, z = map(float, m.group(2, 3, 4))
        t = float(m.group(5))
        if t_min <= t <= t_max:
            sz = float(m.group(6))
            entries.append((slot, x, y, z, math.hypot(x, y), t, sz))
    summarize(f"L2 SE0 particleTime [{t_min},{t_max}]", entries)


def summarize(title, entries):
    if not entries:
        print(f"{title}: no entries")
        return
    hrs = [e[4] for e in entries]
    zs = [e[3] for e in entries]
    szs = [e[6] for e in entries]
    print(f"{title} (n={len(entries)})")
    print(
        f"  horizRadius UU: min={min(hrs):.3f} max={max(hrs):.3f} span={max(hrs)-min(hrs):.3f}"
    )
    print(
        f"  height Z UU: min={min(zs):.3f} max={max(zs):.3f} span={max(zs)-min(zs):.3f}"
    )
    print(
        f"  size UU: min={min(szs):.3f} max={max(szs):.3f} avg={sum(szs)/len(szs):.3f}"
    )
    print(
        f"  particleTime: min={min(e[5] for e in entries):.3f} max={max(e[5] for e in entries):.3f}"
    )
    print(
        f"  horizSpanM={((max(hrs)-min(hrs))/52.5*K):.4f} heightSpanM={((max(zs)-min(zs))/52.5*K):.4f} @K={K}"
    )


if __name__ == "__main__":
    l2 = r"C:\Users\hh-soft\source\repos\AutoLoginInterlude\Debug\ParticleSnapshot.log"
    unity = r"C:\Users\hh-soft\source\repos\AutoLoginInterlude\Debug\Unity_ParticleSnapshot.log"
    parse_l2_se0_by_time(l2)
    print()
    parse_unity_se0_by_time(unity)
