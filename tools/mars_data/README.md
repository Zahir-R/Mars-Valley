# Mars data pipeline — Gale Crater

Derives the biome grid used by the game from real Planetary Data System products.

## Regenerate

```sh
python download.py   # downloads MOLA MEGDR slice (rows 2880-3200), TES TI slice, Odyssey H2O, FE, Si
python generate.py   # crops to lat -8..-2 / lon 133..143.4, resamples to 96x56, classifies biomes
```

`generate.py` writes:

- `data/mars/gale/terrain.bin` (96x56, 3 bytes/cell: biome id, elevation, ice)
- `src/NasaValley.Domain/Biomes.g.cs` (byte grid + `BiomeDefinition` records)
- `data/mars/gale/meta.json` (sources, DOIs, attribution)

## Classification

Elevation (MOLA) + thermal inertia (TES) → 6 biomes:

| id | biome | rule of thumb |
|----|-------|---------------|
| 0  | Gale floor | ele < −2700 m, TI < 285 |
| 1  | Rocky basin | ele < −2700 m, TI ≥ 285 |
| 2  | Regolith plains | −2700..−1500 m, TI < 285 |
| 3  | Basalt plains | −2700..−1500 m, TI ≥ 285 |
| 4  | High dusty plains | ≥ −1500 m, TI < 285 |
| 5  | Volcanic highlands | ≥ −1500 m, TI ≥ 285 |

TES units: J/(m²·K·√s). H2O from Odyssey GRS (2° bins, wt%). Climate values are calibrated
against the NASA GSFC PSG Mars Explorer (MY32, moderate dust). Data are NASA/PDS, public
domain; attribution is shipped in `meta.json`.
