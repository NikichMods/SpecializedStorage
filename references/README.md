# Compile-time API references

`SpecializedStorage` needs Graveyard Keeper and NGUI type/member identities at compile time, but the public repository does not need full game assemblies to express that contract.

The projects in this directory are deliberately minimal **reference stubs**:

- `Assembly-CSharp/` declares only the game types, fields, properties, methods, and enum values directly required by production source.
- `Assembly-CSharp-firstpass/` declares only the NGUI surface directly required by production source.
- Their assembly names/versions are set to the verified identities used by Graveyard Keeper 1.407 and the accepted legacy build.

The bodies return inert/default values because these assemblies are never used at runtime. They exist only so the C# compiler emits references to the real game assemblies that are already loaded by Graveyard Keeper.

## Rules

- Never ship either reference-stub DLL with the mod.
- Never add game implementation code, decompiled method bodies, data tables, assets, or broad copied API surfaces here.
- Add a signature only after verifying the real declaring assembly and exact member/type shape.
- When these references change, treat it as a build/runtime compatibility boundary and verify the produced DLL's assembly/member references before handoff.
