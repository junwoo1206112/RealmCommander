## Context

The project is a Unity RTS game using Mirror for networking. It has 8 distinct subsystems (Network, Spawning, Input, Selection, Command, Movement, Combat, AI) that interact in complex ways. Bug-fixing over the past sessions revealed that component interaction issues (stale singletons, missing DontDestroyOnLoad, conflicting input processing, formation spreading interfering with commands) are the primary source of bugs. A structured architecture document will:

- Make component boundaries explicit
- Document the data flow chain end-to-end
- Provide a reference for singleton lifecycle management
- Surface design tension points before they become bugs

## Goals / Non-Goals

**Goals:**
- Create a comprehensive system architecture document with phase-by-phase breakdown
- Document ALL singleton creation, DontDestroyOnLoad status, and destruction conditions
- Map data flows: Input → Selection → Command → Movement → Combat
- Document network authority model per subsystem
- Record all previously fixed bugs with root causes and resolutions
- Create a dependency graph showing component coupling

**Non-Goals:**
- Changing any code or fixing bugs (documentation only)
- Detailed class-level API documentation
- Performance profiling data
- Test coverage analysis

## Decisions

**1. Phase-based architecture decomposition**
The system decomposes into 8 sequential phases matching the data flow: NetworkFoundation → Spawning → PlayerInput → Selection → Command → Movement → Combat → AI. Each phase has one or more components with clear responsibilities.

**2. Two document layers: capability specs + design reference**
Two capability areas:
- `system-architecture` — full reference document covering all phases, data flows, and network authority
- `dependency-audit` — focused singleton lifecycle audit table

**3. Dependency graph as adjacency matrix**
Component coupling will be documented as an adjacency matrix in the `system-architecture` spec, showing which components depend on, create, or communicate with which others. This makes circular dependency and tight coupling visible at a glance.

**4. Network authority per subsystem documented separately**
Each phase doc lists which side (Server, Client, Host, or All) has authority for each operation. This prevents ambiguity about where validation and RPC routing should live.

## Risks / Trade-offs

- **Documentation drift** → Documents serve as references for future changes (updated when related changes are made)
- **Phase boundaries are somewhat arbitrary** → Boundaries follow the existing code layout and data flow. If the code is restructured, documents must be updated
- **No auto-generation** → Documents are hand-authored. Accuracy depends on careful review
