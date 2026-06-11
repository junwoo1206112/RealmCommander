## Why

The project has grown organically through iterative bug-fixing without a clear architectural map. Multiple bugs (RPC serialization, selection flow, movement conflicts, missing DontDestroyOnLoad, auto-acquisition interference) stem from component interaction issues that a structured architecture document would surface. Creating a living architecture map will make design flaws visible, guide future development, and prevent regression.

## What Changes

- Create a system architecture document with phase-by-phase component maps
- Document all cross-component data flows (input → selection → command → movement → combat)
- Identify and document all singleton lifetimes and dependencies
- Generate a dependency graph showing which components depend on which
- Outline the network authority model (server-authoritative vs client-authoritative)
- Document known design issues and their resolutions

## Capabilities

### New Capabilities
- `system-architecture`: Comprehensive architecture documentation covering all subsystems, their relationships, data flows, and design decisions
- `dependency-audit`: Singleton lifecycle audit showing creation order, DontDestroyOnLoad status, and cross-scene persistence

### Modified Capabilities

- None

## Impact

- `openspec/changes/project-architecture-docs/` — architecture documents
- No code changes — this is purely documentation
- Artifacts serve as reference for all future changes
