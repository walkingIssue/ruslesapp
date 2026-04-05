# rustles.application Architecture

## Responsibility

`rustles.application` is the MAUI shell. Its job is to host the shared Blazor UI on native platforms and provide device-specific services to that shared layer.

## Main parts

- `MauiProgram.cs`: configures the MAUI app, Blazor WebView, fonts, logging, and `IFormFactor`.
- `App.xaml` and `MainPage.xaml`: the MAUI application shell.
- `Platforms/*`: platform-specific entry points and manifests for Android, iOS, MacCatalyst, Tizen, and Windows.
- `Services/FormFactor.cs`: the native implementation of the shared UI's device information abstraction.

## Design notes

This project is intentionally thin. That is the right shape for a host layer. It should compose services and UI, not carry business logic.

## Current maturity

- The host wiring is valid after the rename.
- The project still looks very close to the default MAUI Blazor template.
- It does not yet reference `rustles.core` or `rustles.Infrastructure`.
- Full build verification is still dependent on MAUI workloads being installed on the machine.

## Bottom line

This is a clean shell project, but it is still just a shell. The architecture is fine; the missing part is application composition.
