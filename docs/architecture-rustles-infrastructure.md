# rustles.Infrastructure Architecture

## Responsibility

`rustles.Infrastructure` is the persistence foundation. It defines EF Core entity models, a `DbContext`, and a registration helper for SQL Server.

## Main parts

- `UserDataContext.cs`: `UsersDataContext`, the central EF Core context.
- `Model/*`: persistence-facing entity types such as `User`, `Room`, `Conversation`, `Message`, `Position`, and `Consent`.
- `Extensions/AddSqlUserData.cs`: DI registration extension for SQL Server-backed storage.

## Design notes

The project is following a normal infrastructure-layer shape for a .NET application. The code is intentionally thin, which is fine for this layer.

## Current maturity

- The project restores and builds cleanly.
- It is not yet integrated with the app hosts or the core project.
- `UsersDataContext` currently has no real model configuration.
- There are no migrations, repository services, or explicit mapping rules.
- The model layer looks like a first draft of the storage schema, not a production persistence model.

## Bottom line

This is a starter infrastructure project with a correct broad direction. It is not wrong; it is just early.
