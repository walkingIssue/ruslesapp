# Municipality Resolution

## Purpose

Turn location into a usable municipality result without making the user guess what the app is doing.

## Main UI States

- Default state:
  Explain that location is needed to place the user in municipality chat.
- Loading state:
  Show that municipality resolution is in progress.
- Success state:
  Show the resolved municipality clearly.
  Also explain what the resolved municipality means in product terms, not just geographic terms.
- Failure state:
  Show that the location could not be resolved and what the user can try next.
- Permission-needed state:
  Show a clear path to grant or retry location access.

## Validation And Feedback

- Do not silently fail when location access is missing.
- Do not silently map unresolved results to a municipality.
- If the location is unresolved, tell the user that the app could not confidently place them.

## Interaction Notes

- The product does not need exact border correctness for MVP.
- A municipality result can be treated as good enough unless the raster returns no result or the system explicitly marks the result as uncertain.
- The UI should separate "we need location permission" from "we tried and could not resolve this location."
- A successful resolution should feel like a handoff into the product, not just a background technical event.
- The success copy should clearly tell the user that they are now connected to local conversation in the resolved municipality.

## Open Questions

- Should MVP allow manual retry only, or also passive background re-resolution?
- Do we need a manual municipality override for internal testing?
