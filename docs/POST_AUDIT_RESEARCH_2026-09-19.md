# Post-Audit Engineering Research — 2026-09-19

## Scope

This document closes the focused post-audit engineering pass for **Specialized Storage** on **Graveyard Keeper 1.407**.

The review was opened because the accepted implementation temporarily projects a larger value into shared `ItemDefinition.stack_count` while vanilla inventory code handles moves/capacity, and because specialized saves may contain `Item.value` above the vanilla stack limit.

The purpose was to determine whether the accepted implementation should be kept, narrowly hardened, replaced, or blocked pending more runtime evidence.

## Accepted baseline under review

- Stable version: **1.2.0**
- Accepted production source: `dc8d3b1209510d0c57afa4d2bcd0382f56091332`
- Accepted DLL SHA-256: `1b30927d74a8787c00eccede0dc2fe5b735de66d8543f2f79e45820faf353599`
- Accepted CI run: `34617065129`
- Frozen refs: `candidate/1.2.0`, `baseline/1.2.0-accepted`

This audit did **not** change production source, gameplay rules, version metadata, accepted refs, or the accepted binary.

## Native inventory ownership

The current implementation keeps Graveyard Keeper's inventory code as the semantic owner of transfers.

Verified 1.407 path:

- `ChestGUI.GetMaxMoveCount(...)` delegates destination capacity to `MultiInventory.CanAddCount(...)`.
- `ChestGUI.MoveItem(...)` delegates the actual transfer to `MultiInventory.MoveItemTo(...)`.
- `MultiInventory.MoveItemTo(...)` calls destination `CanAddItem(...)`, removes from the source, then calls destination `AddItemNoCheck(...)`.
- The underlying inventory capacity/add paths read `ItemDefinition.stack_count`.

Therefore patching only `ChestGUI.GetMaxMoveCount` would not be sufficient: the real move path would still enforce the vanilla limit. Broader patches to generic `MultiInventory`/inventory methods would have a larger blast radius and would require reconstructing destination context. Reimplementing transfers would abandon native ownership entirely.

No verified narrower per-destination max-stack input or host extension seam was found.

## Scoped `ItemDefinition.stack_count` override

The accepted mechanism temporarily changes the relevant shared `ItemDefinition.stack_count` only around the native operation, then restores it.

Static review confirms:

- manual move/capacity hooks use Harmony finalizers for restoration;
- per-definition depth tracking handles nested use of the same definition;
- Quick Stack has its own outer scope/depth and reverse-order restoration;
- Quick Stack rejects a nested scope for a different active chest rather than silently mixing contexts;
- exception paths restore active state.

Historical runtime evidence also shows real nested behavior:

- manual move capacity checks reused an already active definition at depth 2;
- Quick Stack `CountPotentialMove` reused the active scope at depth 2;
- completed Quick Stack operations restored all temporarily changed definitions.

### Known compatibility property

The temporary value is shared process state. Code from another mod or a vanilla callback executing inside the same synchronous call stack can observe the projected `stack_count`.

This is a real architectural property, but no defect has been demonstrated from it. Replacing the mechanism solely to remove this theoretical exposure would currently require a broader or more fragile patch surface.

There is no evidence that relevant Graveyard Keeper inventory operations are asynchronous or concurrently mutate these definitions, so concurrency is not classified as a current bug.

## Save and uninstall safety

Specialized Storage does not introduce custom save data. A specialized stack is serialized as a normal `Item` whose `value` may exceed the vanilla `ItemDefinition.stack_count`.

Historical user runtime testing covered removal of `SpecializedStorage.dll` with existing over-vanilla stacks:

1. create/save specialized overstack;
2. remove the DLL;
3. load the save without Specialized Storage;
4. open/reopen the storage;
5. take part of the overstack;
6. add items back;
7. save and reload again while the mod remains absent.

Observed result:

- over-vanilla stacks survived deserialization without the mod;
- the storage remained usable;
- items could be taken normally;
- when items were added back, vanilla could redistribute them into vanilla-sized stacks when slots were available;
- untouched over-vanilla stacks could survive another save/reload without the DLL;
- no save corruption or forced destructive normalization was observed.

This supports the current public uninstall guidance in `README.md`.

## `NormalizeExistingStacks` equivalence review

The highest-value static concern was the mod's merge-by-`id` normalization:

- destination and source with the same `id` are combined by moving only `value`;
- the emptied source object is removed.

This initially appeared risky because `Item` itself can carry instance state such as parameters, durability, unique/worker state, and nested inventory.

The concern is closed for Graveyard Keeper 1.407 because vanilla stack equivalence is also ID-based for stackable items.

Evidence from public decompile `Kupie/GYK_DECOMP` at commit
`6abf79199d92482af1c7573870dd9a20ec2270b9`:

- `LazyConsts` reports game version `1.407f`;
- `Item.CanAddCount(Item,...)` immediately delegates to `CanAddCount(item.id,...)`;
- `CanAddCount(string,...)` counts merge capacity by `item.id == item_id`;
- `Item.AddItem(Item,...)` identifies existing stackable destinations by matching `id` and increments their `value`;
- durability, per-instance params, worker state, and other instance fields are not part of vanilla stack-equivalence for `stack_count > 1`.

Accordingly, the current `NormalizeExistingStacks` merge criterion matches native 1.407 stacking semantics rather than creating a stricter or looser equivalence class.

No targeted hardening is justified here.

## Quick Stack compatibility

The accepted compatibility layer remains narrow:

- it soft-detects GYK Quick Stack;
- only verified methods/signatures are patched;
- the outer specialized scope derives relevant definitions from items already represented in the destination chest;
- nested `CanQuickStack` / `CountPotentialMove` calls reuse that scope;
- `TryQuickStack` completes with restoration.

Observed gameplay behavior is consistent with Quick Stack semantics:

- when an item is already represented in the specialized storage, matching inventory items are transferred and can use the specialized capacity;
- when no representative stack exists in that destination, Quick Stack does not create a new item stack there.

Historical logs cover Alchemy Rack, Household Utensils Rack, Scroll Shelf, and Mortuary Rack scopes, including nested depth and successful restoration.

The earlier question about creating a completely new destination stack is therefore not an unmet Specialized Storage requirement.

## Performance review

No post-audit performance defect was established.

Relevant costs:

- suitability catalog construction is lazy/one-time; accepted runtime evidence recorded about **35 ms** for 1157 definitions / 2102 crafts;
- Quick Stack reflection discovery occurs at startup, not per frame;
- marker refresh is tied to item-cell redraw and reuses created marker objects;
- there is no Specialized Storage per-frame `Update` patch.

No optimization or caching rewrite is justified without new profiling evidence.

## Alternatives considered

### Patch only `ChestGUI.GetMaxMoveCount`

Rejected. It changes UI/capacity calculation but not the actual native transfer/add path.

### Patch generic `CanAddCount` / `CanAddItem` / `AddItem`

Rejected for now. These are broader game-wide seams and require reliable destination-context propagation.

### Transpile every relevant `stack_count` read

Rejected for now. This increases version fragility, patch count, and hidden context requirements.

### Reimplement transfer/merge logic

Rejected. It duplicates host inventory semantics and has the largest behavioral/save compatibility surface.

### Clone/substitute `ItemDefinition`

Rejected absent a verified safe identity/substitution seam. It introduces additional state and definition-identity risks.

## Final verdict

**KEEP CURRENT IMPLEMENTATION**

The accepted 1.2.0 runtime architecture remains the preferred mechanism for Graveyard Keeper 1.407.

No production change, new DLL, version bump, CI run, migration, or release replacement is required as a result of this audit.

The previous audit classification "architectural / save-safety risk requiring focused research" is considered **closed by evidence** rather than fixed by code.

## Reopen conditions

Reopen this engineering question only if new evidence appears, for example:

- a concrete compatibility bug caused by another patch observing the temporary shared `stack_count`;
- a Graveyard Keeper version change alters inventory/serialization semantics;
- GYK Quick Stack changes its relevant method/signature/behavior;
- a verified narrower native destination-specific stack-limit seam is discovered;
- profiling identifies Specialized Storage as an actual runtime hot path.

Until then, additional architectural rewriting would increase complexity and blast radius without a demonstrated correctness benefit.
