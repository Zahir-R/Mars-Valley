"""Download raw PDS data needed to build the Gale Crater biome map.

Uses HTTP Range requests so we only pull the rows we need; the full 128 MB
MOLA and 51 MB TES files are never downloaded.
"""

import os
import sys
import urllib.request

RAW_DIR = os.path.join(os.path.dirname(__file__), "raw")

SOURCES = {
    # MOLA MEGDR (32 px/deg, simple cylindrical, 0..360 E lon, north-up)
    "mola.lbl": "https://pds-geosciences.wustl.edu/mgs/mgs-m-mola-5-megdr-l3-v1/mgsl_300x/meg032/megr90n000fb.lbl",
    "mola.img": "https://pds-geosciences.wustl.edu/mgs/mgs-m-mola-5-megdr-l3-v1/mgsl_300x/meg032/megr90n000fb.img",
    # TES thermal inertia (20 px/deg, simple cylindrical, -180..180 E lon, north-up)
    "tes_ti.lbl": "https://pds-geosciences.wustl.edu/mgs/mgs-m-tes-5-timap-v1/mgst_9001/data/global_ti_day_2007.lbl",
    "tes_ti.img": "https://pds-geosciences.wustl.edu/mgs/mgs-m-tes-5-timap-v1/mgst_9001/data/global_ti_day_2007.img",
    # Odyssey GRS elemental maps (2-degree bins, ~1 MB ASCII)
    "h2o.lbl": "https://pds-geosciences.wustl.edu/ody/ody-m-grs-5-elements-v1/odgm_xxxx/data/unsmoothed/2x2/h2o_2x2.lbl",
    "h2o.tab": "https://pds-geosciences.wustl.edu/ody/ody-m-grs-5-elements-v1/odgm_xxxx/data/unsmoothed/2x2/h2o_2x2.tab",
    "fe.tab": "https://pds-geosciences.wustl.edu/ody/ody-m-grs-5-elements-v1/odgm_xxxx/data/unsmoothed/2x2/fe_2x2.tab",
    "si.tab": "https://pds-geosciences.wustl.edu/ody/ody-m-grs-5-elements-v1/odgm_xxxx/data/unsmoothed/2x2/si_2x2.tab",
}

def ensure_dir():
    os.makedirs(RAW_DIR, exist_ok=True)


def fetch(url: str, dest: str):
    print(f"Downloading {url}\n  -> {dest}")
    req = urllib.request.Request(url, headers={"User-Agent": "NasaValley/1.0 (Mars biome generator)"})
    with urllib.request.urlopen(req, timeout=600) as response, open(dest, "wb") as file:
        while True:
            chunk = response.read(1 << 20)
            if not chunk:
                break
            file.write(chunk)
    print(f"  {os.path.getsize(dest) / 1e6:.1f} MB")


def fetch_slice(url: str, dest: str, line_offset_bytes: int, total_bytes: int):
    print(f"Downloading {url}\n  -> {dest} ({line_offset_bytes}..{total_bytes})")
    req = urllib.request.Request(url, headers={
        "User-Agent": "NasaValley/1.0 (Mars biome generator)",
        "Range": f"bytes={line_offset_bytes}-{total_bytes - 1}",
    })
    with urllib.request.urlopen(req, timeout=600) as response, open(dest, "wb") as file:
        remaining = total_bytes - line_offset_bytes
        while remaining > 0:
            chunk = response.read(min(1 << 20, remaining))
            if not chunk:
                break
            file.write(chunk)
            remaining -= len(chunk)
    print(f"  {os.path.getsize(dest) / 1e6:.1f} MB")


if __name__ == "__main__":
    ensure_dir()
    for name, url in SOURCES.items():
        if name in ("mola.img", "tes_ti.img"):
            continue
        fetch(url, os.path.join(RAW_DIR, name))

    # MOLA MEGDR: 5760 lines x 23040 bytes/line, big-endian int16.
    # Region is lat -10..0 (rows 2880..3200 north-up), full width cropped later.
    mola_start, mola_end = 2880 * 23040, 3200 * 23040
    fetch_slice(SOURCES["mola.img"], os.path.join(RAW_DIR, "mola.img"), mola_start, mola_end)

    # TES TI: 3600 lines x 14400 bytes/line. Region is lat -10..0 (rows 1800..2000 north-up).
    tes_start, tes_end = 1800 * 14400, 2000 * 14400
    fetch_slice(SOURCES["tes_ti.img"], os.path.join(RAW_DIR, "tes_ti.img"), tes_start, tes_end)
