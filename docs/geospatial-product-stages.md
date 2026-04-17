# Geospatial Product Stages

## Purpose

This note captures the intended staged rollout for Rustles so the geospatial work stays attached to a realistic product sequence.

The goal is to prevent the project from trying to build the most ambitious version first.

## Chosen starting point

The current direction is:

- use a `10m` raster as the practical next geospatial asset target
- use that to support municipality-level chat first

This is not the final form of the platform. It is a tractable first stage.

Why `10m`:

- much more manageable than `5m` for nationwide storage
- still fine enough to support future local-area resolution work
- a better fit for real-world phone location accuracy than pretending `5m` is always meaningful

## Stage 1: Municipality Chat

### Goal

Build a functional chat application where users are resolved into a municipality and can interact within that municipality context.

### What this stage needs

- reliable municipality resolution
- stable municipality identity mapping
- room creation and membership at municipality scale
- message flow that actually works end to end
- realtime updates that are dependable
- enough testing to trust the basic behavior

### What this stage does not need

- precise local room placement
- venue-local overlays
- dynamic software surfaces
- the full "spatial runtime platform" concept

### Why this stage comes first

This is the smallest version of the idea that still behaves like a real product.

It gives:

- something users can actually try
- something that can be tested operationally
- something that can be explained to other people without a giant architecture preface

It also keeps the first geospatial problem narrow:

- resolve the user to the correct municipality

That is a much easier trust boundary than trying to resolve users to micro-locations on day one.

## Stage 2: Targeted Local Rooms

### Goal

Allow users to create and discover more fine-grained rooms within areas rather than only municipality-wide spaces.

### What changes here

The geospatial model becomes more precise and more user-facing.

Instead of:

- "you are in municipality X"

the system starts caring about:

- "you are near this place"
- "this room exists in this local area"
- "this user should be allowed to discover this room"

### Problems introduced at this stage

- phone location uncertainty
- border hopping between adjacent cells
- indoor vs outdoor ambiguity
- visibility radius design
- room placement semantics
- abuse and moderation of place-bound rooms
- stale or fake location claims

### Why `10m` is a useful starting point here

The municipality system can evolve into more local discovery without immediately requiring a nationwide `5m` raster.

`10m` is precise enough to start thinking about:

- local room indexing
- nearby discovery
- cell-key-based storage

without forcing the system into the file-size and correctness pain of an even finer nationwide grid.

This stage is where the project would likely need:

- exact fallback behavior near boundaries
- smoothing or hysteresis for user movement
- stronger thinking around room discovery rules

## Stage 3: Software Context Instead of Only Chat

### Goal

Expand the system from place-bound chat into place-bound software contexts.

This means the app is no longer only answering:

- "what room should I see here?"

It is answering:

- "what local software capabilities become active here?"

### Examples

- venue queue systems
- place-specific UI/runtime modules
- local event or service surfaces
- location-bound tools that are not primarily conversation-based

### Why this is a later stage

This is the broadest and most powerful version of the idea, but it depends on the earlier stages proving that:

- the geospatial layer is trustworthy
- realtime behavior works
- room and membership logic are stable
- users actually want place-bound interaction at all

Without those foundations, this stage is just an exciting abstraction.

## Why this staged model matters

The broad idea behind Rustles can easily pull the project into solving tomorrow's architecture before today's product exists.

This staged model is a guardrail against that.

It says:

1. first make municipality chat real
2. then make local rooms real
3. only then expand into general spatial software context

That sequence still preserves the broader vision, but it stops the broader vision from destroying execution.

## Funding reality

This sequence also makes the project more legible to funding or support conversations.

It is easier to explain and validate:

- a functioning municipality chat product

than:

- a future spatial runtime platform for place-bound digital experiences

The broader idea can still guide architecture, but the product has to earn the right to get there.

## Practical conclusion

The current direction should be treated as:

- `10m` georaster as the practical next raster target
- municipality chat as stage 1
- local targeted rooms as stage 2
- broader software context as stage 3

That is a realistic sequence, and it gives the geospatial work a clear reason to exist at each step.
