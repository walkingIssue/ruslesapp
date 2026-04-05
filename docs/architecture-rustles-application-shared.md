# rustles.application.Shared Architecture

## Responsibility

`rustles.application.Shared` is the shared Blazor UI layer used by both the MAUI host and the web host.

## Main parts

- `Routes.razor`: route registration.
- `Layout/*`: shared page chrome and navigation.
- `Pages/*`: shared Razor pages.
- `Services/IFormFactor.cs`: abstraction for device and platform info.
- `wwwroot/*`: shared CSS and static assets.

## Design notes

The project is set up the right way for code reuse. The host-specific projects only need to provide environment-specific services and boot the shared UI.

## Current maturity

- The shared structure is sound.
- The content is still template-grade: home, counter, and weather pages.
- There is no domain integration yet.
- The only real abstraction in use is `IFormFactor`.

## Bottom line

The architecture is good for reuse, but the feature layer has not been built yet. Right now this project is a scaffold, not a product UI.
