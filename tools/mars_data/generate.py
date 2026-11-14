#!/usr/bin/env python3
"""Build the Gale Crater biome pack for NasaValley.

The script parses the raw PDS products downloaded into `raw/` (see download.py),
classifies every cell into a biome, and emits:

  ../../data/mars/gale/terrain.bin   96x56, 3 bytes/cell: biome id, elevation byte, ice byte
  ../../src/NasaValley.Domain/Biomes.g.cs   biome definitions for game logic (generated C#)
  ../../data/mars/gale/meta.json     region + source attribution (human-readable)

Re-run after tweaking the thresholds.  Requires numpy only.
"""

import json
import os
import sys

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RAW_DIR = os.path.join(os.path.dirname(__file__), "raw")

OUT_DIR = os.path.join(ROOT, "data", "mars", "gale")
OUT_BIN = os.path.join(OUT_DIR, "terrain.bin")
OUT_CS = os.path.join(ROOT, "src", "NasaValley.Domain", "Biomes.g.cs")
OUT_META = os.path.join(OUT_DIR, "meta.json")

# Region: Gale Crater and surroundings.
W, H = 96, 56
LAT_TOP, LAT_BOT = -2.0, -8.0   # north-up
LON_LEFT, LON_RIGHT = 133.0, 143.4
TILE = 32

# Biome ids (kept stable; index == id byte in terrain.bin).
BIOMES = [
    {"id": 0, "name": "Gale floor",          "color": "#9b7046", "baseTempC": -42.0, "swingC": 64.0, "solar": 0.90, "dustTau": 0.42, "iceWt": 4.5, "nodeWeights": {"rock": 1.0, "regolith": 1.0, "iron": 0.6, "si": 0.4}},
    {"id": 1, "name": "Rocky basin",         "color": "#7d675a", "baseTempC": -48.0, "swingC": 34.0, "solar": 0.88, "dustTau": 0.30, "iceWt": 5.2, "nodeWeights": {"rock": 1.4, "regolith": 0.4, "iron": 1.3, "si": 0.9, "basalt": 0.5}},
    {"id": 2, "name": "Regolith plains",     "color": "#bd9068", "baseTempC": -30.0, "swingC": 74.0, "solar": 1.02, "dustTau": 0.36, "iceWt": 4.0, "nodeWeights": {"rock": 0.8, "regolith": 1.3, "iron": 0.6, "si": 0.5, "ice": 0.6}},
    {"id": 3, "name": "Basalt plains",       "color": "#74685f", "baseTempC": -34.0, "swingC": 60.0, "solar": 1.00, "dustTau": 0.26, "iceWt": 5.0, "nodeWeights": {"rock": 1.0, "regolith": 0.6, "iron": 1.2, "si": 1.6, "basalt": 1.2}},
    {"id": 4, "name": "High dusty plains",   "color": "#d9bda1", "baseTempC": -16.0, "swingC": 72.0, "solar": 1.15, "dustTau": 0.34, "iceWt": 3.4, "nodeWeights": {"rock": 0.5, "regolith": 1.5, "iron": 0.3, "si": 0.2, "ice": 0.3}},
    {"id": 5, "name": "Volcanic highlands",  "color": "#5b4d43", "baseTempC": -26.0, "swingC": 40.0, "solar": 0.82, "dustTau": 0.18, "iceWt": 6.3, "nodeWeights": {"basalt": 1.8, "rock": 1.2, "iron": 1.8, "si": 1.0, "ice": 0.8}},
]

BASALT_BIOMES = {1, 3, 5}


def load_pds_img(path):
    with open(path, "rb") as f:
        return np.fromfile(f, dtype=">i2").astype(np.int32)


def resize(arr, out_w, out_h):
    arr = arr.astype(np.float32)
    ih, iw = arr.shape
    y = np.linspace(0, ih - 1, out_h)
    x = np.linspace(0, iw - 1, out_w)
    # Along columns.
    tmp = np.empty((ih, out_w), np.float32)
    for j in range(ih):
        tmp[j] = np.interp(x, np.arange(iw, dtype=np.float32), arr[j])
    # Along rows.
    out = np.empty((out_h, out_w), np.float32)
    for i, yi in enumerate(y):
        y0 = int(yi)
        y1 = min(y0 + 1, ih - 1)
        frac = np.float32(yi - y0)
        out[i] = tmp[y0] * (1.0 - frac) + tmp[y1] * frac
    return out


def classify(height, ti):
    """Return biome id for one cell."""
    if height < -2700:
        if ti < 285:
            return 0        # Gale floor
        return 1            # Rocky basin
    if height < -1500:
        if ti < 285:
            return 2        # Regolith plains
        return 3            # Basalt plains
    if ti < 285:
        return 4            # High dusty plains
    return 5                # Volcanic highlands


def main():
    os.makedirs(OUT_DIR, exist_ok=True)

    # ---- MOLA ----
    mola = load_pds_img(os.path.join(RAW_DIR, "mola.img"))
    mola = mola.reshape(320, 11520)  # rows 2880..3200 of the global map
    # Global row = (90 - lat) * 32; our slice starts at global row 2880 (lat 0).
    r_top = round((90 - LAT_TOP) * 32) - 2880
    r_bot = round((90 - LAT_BOT) * 32) - 2880
    c_left = round(LON_LEFT * 32)
    c_right = round(LON_RIGHT * 32)
    mola = mola[r_top:r_bot, c_left:c_right]
    print(f"MOLA crop: {mola.shape}  h=[{mola.min()}, {mola.max()}] mean={mola.mean():.0f}")

    # ---- TES thermal inertia ----
    tes = load_pds_img(os.path.join(RAW_DIR, "tes_ti.img")).reshape(200, 7200)
    t_top = int((90 - LAT_TOP) * 20) - 1800
    t_bot = int((90 - LAT_BOT) * 20) - 1800
    tc = int(LON_LEFT * 20)
    tr = int(LON_RIGHT * 20)
    tes = tes[t_top:t_bot, tc:tr]
    print(f"TES crop:  {tes.shape}  ti=[{tes.min()}, {tes.max()}] mean={tes.mean():.0f}")

    # ---- Resize to the world grid ----
    h_grid = resize(mola, W, H)
    ti_grid = resize(tes, W, H)
    print(f"grid: h=[{h_grid.min():.0f}, {h_grid.max():.0f}] ti=[{ti_grid.min():.0f}, {ti_grid.max():.0f}]")

    # ---- Classify ----
    biome_ids = np.empty((H, W), dtype=np.uint8)
    for y in range(H):
        for x in range(W):
            biome_ids[y, x] = classify(h_grid[y, x], ti_grid[y, x])

    id_counts = np.bincount(biome_ids.flatten(), minlength=len(BIOMES))
    print("cell distribution:")
    for b, c in zip(BIOMES, id_counts):
        print(f"  [{b['id']}] {b['name']:<20} {c:6d}  {100.0 * c / biome_ids.size:5.1f}%")

    elev_min, elev_max = h_grid.min(), h_grid.max()

    def elev_byte(h):
        return int(np.clip(round((h - elev_min) * 255.0 / (elev_max - elev_min)), 0, 255))

    # ---- terrain.bin: id, elevation, ice ----
    with open(OUT_BIN, "wb") as f:
        for y in range(H):
            for x in range(W):
                f.write(bytes([biome_ids[y, x],
                               elev_byte(h_grid[y, x]),
                               int(np.clip(BIOMES[biome_ids[y, x]]["iceWt"] * 20, 0, 100))]))
    print(f"wrote {OUT_BIN} ({W * H * 3} bytes)")

    # ---- Biomes.g.cs ----
    lines = [
        "// <auto-generated /> DO NOT EDIT BY HAND.",
        "// Generated by tools/mars_data/generate.py from PDS data.",
        "// Source data: MGS MOLA MEGDR, MGS TES thermal inertia, Odyssey GRS H2O, PSG Mars Explorer.",
        "using System.Collections.Generic;",
        "",
        "namespace NasaValley.Domain;",
        "",
        "/// <summary>Gale Crater biome definitions (id = byte in the grid, north-up 96x56 cells).</summary>",
        "public static class GeneratedBiomes",
        "{",
        "    public const int Width = 96;",
        "    public const int Height = 56;",
        "    public const int TileSize = 32;",
        "    public const int WorldWidth = Width * TileSize;",
        "    public const int WorldHeight = Height * TileSize;",
        "",
        "    /// <summary>96x56 biome index per cell, row-major, north-to-south.</summary>",
        "    public static readonly byte[] Grid =",
        "    {",
    ]

    for y in range(H):
        row = biome_ids[y]
        lines.append("        " + ", ".join(f"{int(v)}" for v in row) + ",")
    lines += [
        "    };",
        "",
        "    public static readonly IReadOnlyDictionary<int, BiomeDefinition> ById = new Dictionary<int, BiomeDefinition>",
        "    {",
    ]

    for b in BIOMES:
        weights = ", ".join(f'{{ \"{k}\", {v} }}' for k, v in b["nodeWeights"].items())
        lines += [
            f'        [{b["id"]}] = new BiomeDefinition(',
            f'            Index: {b["id"]},',
            f'            Id: "{b["name"].lower().replace(" ", "_")}",',
            f'            Name: \"{b["name"]}\",',
            f'            Color: \"{b["color"]}\",',
            f'            BaseTempC: {b["baseTempC"]}f,',
            f'            DayNightSwingC: {b["swingC"]}f,',
            f'            SolarFactor: {b["solar"]}f,',
            f'            DustTau: {b["dustTau"]}f,',
            f'            IceFraction: {round(b["iceWt"] / 100.0, 3)}f,',
            f'            NodeWeights: new Dictionary<string, double> {{ {weights} }}),',
        ]
    lines += [
        "    };",
        "",
        "    public static BiomeDefinition Get(int index) => ById[index];",
        "",
        "    /// <summary>Biome id for a world position in tile coordinates.</summary>",
        "    public static BiomeDefinition At(int tileX, int tileY)",
        "    {",
        "        if (tileX < 0) tileX = 0; else if (tileX >= Width) tileX = Width - 1;",
        "        if (tileY < 0) tileY = 0; else if (tileY >= Height) tileY = Height - 1;",
        "        return ById[Grid[tileY * Width + tileX]];",
        "    }",
        "",
        "    /// <summary>Biome id for a world position in pixel coordinates.</summary>",
        "    public static BiomeDefinition AtWorld(float x, float y) =>",
        "        At((int)(x / TileSize), (int)(y / TileSize));",
        "}",
        "",
        "",
        "public sealed record BiomeDefinition(int Index, string Id, string Name, string Color, float BaseTempC, float DayNightSwingC, float SolarFactor, float DustTau, float IceFraction, IReadOnlyDictionary<string, double> NodeWeights);",
    ]
    with open(OUT_CS, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    print(f"wrote {OUT_CS}")

    meta = {
        "region": "Gale Crater",
        "bbox": {"south": LAT_BOT, "north": LAT_TOP, "west": LON_LEFT, "east": LON_RIGHT},
        "grid": {"width": W, "height": H},
        "elevation_m": {"min": float(elev_min), "max": float(elev_max)},
        "sources": [
            {"name": "MGS MOLA MEGDR gridded topographic map (32 px/deg)", "url": "https://pds-geosciences.wustl.edu/mgs/mgs-m-mola-5-megdr-l3-v1/mgsl_300x/meg032/megr90n000fb.lbl", "doi": "10.17189/1520531"},
            {"name": "MGS TES derived thermal inertia maps (20 px/deg)", "url": "https://pds-geosciences.wustl.edu/mgs/mgs-m-tes-5-timap-v1/mgst_9001/data/global_ti_day_2007.lbl", "doi": "10.17189/1518557"},
            {"name": "2001 Mars Odyssey GRS gamma-ray spectrometer H2O concentration (2-degree bins)", "url": "https://pds-geosciences.wustl.edu/ody/ody-m-grs-5-elements-v1/odgm_xxxx/data/unsmoothed/2x2/h2o_2x2.tab", "doi": "10.17189/1518984"},
            {"name": "NASA GSFC Mars OSSE / EMARS climatology (PSG Mars Explorer), MY32 Moderate dust", "url": "https://psg.gsfc.nasa.gov/apps/mars/", "doi": "10.1029/2022EA002253"},
            {"name": "USGS Astrogeology", "url": "https://astrogeology.usgs.gov/"},
        ],
        "attribution": "NASA/JPL/GSFC/USGS/PDS. Data are used to drive gameplay; values simplified for gameplay.",
    }
    with open(OUT_META, "w", encoding="utf-8") as f:
        json.dump(meta, f, indent=2)
    print(f"wrote {OUT_META}")


if __name__ == "__main__":
    try:
        main()
    except FileNotFoundError as e:
        sys.exit(f"Missing raw data file: {e}. Run tools/mars_data/download.py first.")
