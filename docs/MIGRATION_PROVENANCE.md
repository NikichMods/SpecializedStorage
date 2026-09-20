# Public Repository Migration Provenance

## Accepted legacy source

- Private legacy repository: `NikichMods/SpecializedStorage-legacy-private`
- Accepted legacy version: **1.1.0**
- Accepted source tag: `v1.1.0`
- Accepted source commit: `1bb576af395a269d1d4e1a584a68fa13e1c12eb3`
- Accepted production source blob: `e83504499fb33b0ad7610325d3868366bad11b14`
- Accepted marker-resource blob: `f009a6ff58ad8879cc3f8a4f7184e47da042764d`
- Accepted player-tested DLL SHA-256: `2d8876dfc475ff217142b423ac8c94a03d491c7376853ed1e31916b07169eabf`

The accepted runtime source and marker resource remained unchanged on the legacy `main` line after the 1.1.0 acceptance point.

## Public migration

The public repository starts with fresh Git history. Version **1.2.0** is the first public clean-build candidate because the build/reference architecture changes and therefore produces a new DLL even though no gameplay behavior change is intended.

The legacy build used private compile-time copies of Graveyard Keeper/BepInEx/Unity assemblies. The public build replaces those with package references plus minimal hand-authored API reference projects whose purpose is only to preserve the compile-time member/assembly identities used by production source.

The stable BepInEx GUID remains `nikich.graveyardkeeper.specializedstorage`.
