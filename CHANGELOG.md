# Changelog

Only accepted user-facing releases are listed here.

## 1.2.0 — 2026-09-11

- Migrated Specialized Storage into the clean public repository.
- Replaced private checked-in game assembly references with minimal compile-time API reference stubs.
- Preserved the accepted 1.1.0 runtime behavior: storage classification, stack limits, marker appearance/position, input behavior, save behavior, and Quick Stack compatibility.

## 1.1.0 — 2026-08-18

- Added a small vanilla-like increased-stack marker to applicable specialized-storage cells.
- The marker represents potential capacity and appears even when the current stack is below the vanilla limit.
- Kept the vanilla quantity label and all mouse, gamepad, and drag-and-drop interaction unchanged.
- Embedded the point-filtered pixel-art marker directly in the DLL.

## 1.0.0 — 2026-08-16

- Initial stable release.
- Added specialized classification and increased stack limits for the Alchemy Rack, Household Utensils Rack, Scroll Shelf, Bookcase, and Mortuary Rack.
- Added optional compatibility with GYK Quick Stack.
