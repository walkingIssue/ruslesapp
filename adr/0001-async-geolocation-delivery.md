# ADR 0001: Move Geolocation Requests to Async Processing and Push Delivery

- Status: Proposed
- Date: 2026-04-05

## Context

The current geolocation flow ties an HTTP request directly to the batching and resolution pipeline.

In practical terms, the request comes in, a promise-like object is created, the request is placed into a batch, the system waits for the batch window to open, the batch runs, and only then does the original HTTP caller get a response.

That model is simple to reason about at very small scale, but it creates a few problems:

- request lifetime is coupled to processing lifetime
- every in-flight geolocation request keeps server-side request state alive while it waits
- burst traffic can grow the number of waiting tasks quickly
- batching policy directly affects HTTP latency
- the web tier and the geolocation worker are more tightly coupled than they should be
- it makes push-based completion awkward, because the caller is still waiting for the original request

This is especially important if Rustles serves the web frontend, API, and SignalR endpoints from the same ASP.NET Core host. Even if each individual wait is not expensive, the design encourages request buildup in the place where we want the shortest possible lifetime.

## Decision

Geolocation resolution should move to an asynchronous request model.

The proposed shape is:

1. The client submits a geolocation request over HTTP.
2. The server validates and enqueues the work.
3. The server returns immediately with `202 Accepted` and a `requestId`.
4. A background worker owns batching and resolution.
5. When the result is ready, the server pushes it to the client through SignalR.
6. A polling endpoint may exist as a fallback for reconnects, retries, or clients that are temporarily offline.

## Why this is the preferred direction

- It keeps HTTP requests short-lived.
- It separates web request handling from batch execution.
- It gives us a cleaner place to apply backpressure and concurrency limits.
- It makes batching a processing concern instead of an HTTP concern.
- It aligns better with the future need to push room and message updates to connected clients.
- It avoids inventing a custom UDP or raw TCP subscriber system when SignalR already solves the application-level problem we actually have.

## Consequences

This is not a small tweak. It means a substantial refactor of the current geolocation API.

The current implementation assumes that the caller waits for the result inside the originating HTTP request. Moving away from that means we will need to change:

- the HTTP contract
- the batching entry point
- the lifetime and ownership of request state
- result tracking and correlation by `requestId`
- client update flow
- failure, timeout, and retry handling

We will also need to decide where short-lived request state lives:

- in memory only
- in a cache
- or persisted in the database for recovery and auditing

## Proposed implementation shape

- HTTP endpoint for submission
- background queue or channel for pending work
- background service that forms and executes batches
- result store keyed by `requestId`
- SignalR hub for completion and progress notifications
- optional polling endpoint for result lookup

## Tradeoffs

### Gains

- lower pressure on the request pipeline
- clearer separation of concerns
- better fit for bursty workloads
- cleaner path toward mobile and web clients using the same async flow

### Costs

- more moving parts
- more state management
- more client-side complexity
- harder local reasoning than the current request-response shape

## What this ADR does not decide

- whether request state should be persisted or only cached
- exact batch window and batch size rules
- whether progress updates are worth shipping before final completion
- whether the first version should support polling only before SignalR is added

## Follow-up work

Before implementation starts, we should define one concrete end-to-end flow:

1. submit request
2. enqueue request
3. batch and resolve
4. store result
5. notify client
6. handle reconnect or missed notification

That should be designed before changing the current geolocation API, because this proposal affects the contract from the edge all the way into the core batching logic.
