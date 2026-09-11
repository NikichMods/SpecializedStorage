# Test Build Log

This file records numbered binaries and the evidence used to accept or reject them. Numbered artifacts are immutable once handed to the user.

## Legacy accepted baseline

### 1.1.0 — accepted marker release

- **Behavior:** specialized classification for Alchemy Rack, Household Utensils Rack, Scroll Shelf, Bookcase, and Mortuary Rack; suitable items use `min(vanilla × 4, 200)`; stack-size-1 and unrelated items remain vanilla.
- **UI:** accepted 7×5 three-bar marker, final lower-left placement `(-18, -17)`.
- **Interaction result:** marker appearance, mouse, gamepad, clicking, dragging, and repeated storage use were accepted; supplied log had no Specialized Storage exceptions.
- **Quick Stack:** optional compatibility retained.
- **Accepted source tag:** `v1.1.0` at `1bb576af395a269d1d4e1a584a68fa13e1c12eb3`.
- **Accepted production source blob:** `e83504499fb33b0ad7610325d3868366bad11b14`.
- **Accepted marker blob:** `f009a6ff58ad8879cc3f8a4f7184e47da042764d`.
- **Accepted player-tested DLL:** 29,696 bytes; SHA-256 `2d8876dfc475ff217142b423ac8c94a03d491c7376853ed1e31916b07169eabf`.
- **Result:** **accepted**.

A later repository-normalization CI build from unchanged production source/project/resource succeeded at legacy run `33955208231`, commit `e1448f18720ca678fd53ca26ccfc92bf5223c0f3`, but produced different compiler bytes (SHA-256 `ffb0c608c5093434ef177a6eb938dfbb14de3bef7f9471c2df5444988e06ff84`). That did not replace the exact player-tested 1.1.0 binary.

## 1.2.0 — clean public build/reference migration

- **Goal:** reproduce accepted 1.1.0 gameplay behavior from a clean public repository without checked-in game assemblies.
- **Runtime changes intended:** none.
- **Version change:** 1.1.0 → 1.2.0 because the clean public build produces a new binary and 1.1.0 is immutable.
- **Production behavior preserved:** storage IDs/classification, stack multiplier/cap, legacy-overstack handling, recipe-derived suitability cache, stack normalization/temporary override behavior, marker pixels/layout/depth, Quick Stack scope, and Harmony patch targets.
- **Build architecture change:** package references plus minimal compile-time `Assembly-CSharp` / `Assembly-CSharp-firstpass` API reference projects under `references/`; reference-stub DLLs are compile-only and are not shipped.
- **Verified legacy assembly identities:** `Assembly-CSharp` version `11.0.0.0`; `Assembly-CSharp-firstpass` version `0.0.0.0`.
- **Verified API constants used by production:** BagType Alchemy=2, Potions=6, Food=8; ItemType Preach=20, BodyUniversalPart=270; CraftType MixedCraft=3, AlchemyDecompose=5; UIWidget.Pivot.BottomLeft=6.
- **Initial public build:** run `34616727882` compiled but was rejected before handoff because SDK default compile-item discovery also compiled reference-stub source into the production assembly.
- **Accepted source commit:** `dc8d3b1209510d0c57afa4d2bcd0382f56091332`; frozen as `candidate/1.2.0` and `baseline/1.2.0-accepted`.
- **Accepted CI:** run `34617065129` succeeded with 0 warnings / 0 errors; artifact `SpecializedStorage-1.2.0` (`10270612472`).
- **Package-boundary verification:** `SpecializedStorage.dll` is the only shipped DLL; compile-only `Assembly-CSharp.dll` / `Assembly-CSharp-firstpass.dll` are not copied to output. Production compile items are explicitly limited to `src/*.cs`, so API stub source is not compiled into the mod. Post-build inspection confirms external `Assembly-CSharp` / `Assembly-CSharp-firstpass` references remain in the production DLL.
- **Accepted DLL:** 30,208 bytes; SHA-256 `1b30927d74a8787c00eccede0dc2fe5b735de66d8543f2f79e45820faf353599`.
- **In-game result (2026-09-11):** user confirmed that the mod looked and worked correctly. The supplied game log shows `Specialized Storage 1.2.0 loaded` and a successful suitability-cache build (`definitions=1157`, `alchemy=135`, `kitchen=225`, `crafts=2102`, `time_ms=35`) with no Specialized Storage-tagged warnings or errors.
- **Result:** **accepted stable release**.
