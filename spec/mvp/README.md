# Rustles MVP Spec

This folder defines the first version of Rustles that should work locally before deployment work becomes the main focus.

## MVP Goal

Allow a user to create an account, sign in, be resolved into a municipality, enter the municipality chat, and send or receive messages there.

## In Scope

- account registration
- sign in and sign out
- password recovery flow at the behavior level
- privacy notice
- municipality resolution from user location
- unresolved-location handling
- municipality chat access
- recent message history
- sending and receiving municipality messages
- blocking other users
- basic account management

## Explicitly Out Of Scope For This MVP

- exact vector fallback near municipality borders
- fine-grained local room placement
- location-scoped room discovery inside smaller areas
- broader spatial software-context behavior
- advanced moderation and abuse tooling
- sophisticated presence beyond a simple connected count or similar lightweight indicator

## Practical Meaning

This MVP should run locally, let us exercise the main end-to-end flow, and give us a real foundation for the web deployment pipeline.
