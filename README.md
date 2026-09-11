# Specialized Storage

A quality-of-life mod for **Graveyard Keeper** that gives themed storage furniture increased stack capacity for appropriate items while leaving unrelated items at their vanilla limits.

## Supported storage

- **Alchemy Rack** — alchemy ingredients, reagents, and potions
- **Household Utensils Rack** — food, cooking ingredients, and kitchenware
- **Scroll Shelf** — paper, writing materials, stories, notes, prayers, and scrolls
- **Bookcase** — chapters, covers, books, and techbooks
- **Mortuary Rack** — body parts and embalming consumables

Classification primarily uses the game's item metadata and crafting relationships.

## Stack limits

`specialized stack = min(vanilla stack × 4, 200)`

Items with a vanilla stack size of 1 are unchanged.

## Increased-stack marker

Applicable cells in an opened specialized storage show a small three-bar marker in the lower-left corner. It indicates that the item can use the increased stack limit.

The marker is not shown in the player's inventory, ordinary storage, empty cells, unsupported items, or items whose vanilla stack size is 1. It does not interfere with quantity labels, mouse/gamepad input, drag-and-drop, or save data.

## Requirements and compatibility

- Graveyard Keeper 1.407
- BepInEx 5.x (tested with 5.4.23.5)
- GYK Quick Stack 1.0.0 is supported but optional

No configuration is required.

## Installation

Copy `Specialized Storage 1.2.0.dll` into:

`Graveyard Keeper/BepInEx/plugins/`

## Uninstalling

The mod does not add custom save data. Existing stacks above the vanilla limit may remain after uninstalling and can still be taken normally. Adding items back to an overstacked storage may split them into vanilla-sized stacks when free slots are available.

## Status

Current stable version: **1.2.0**.

## Development

- Canonical project: `SpecializedStorage.csproj`
- Runtime source: `src/`
- Verified build/test history: `docs/TEST_BUILD_LOG.md`
- Project-specific engineering rules: `AGENTS.md`
