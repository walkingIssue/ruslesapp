# rustles.application.Web Architecture

## Responsibility

`rustles.application.Web` is the ASP.NET Core host for the shared Blazor UI.

## Main parts

- `Program.cs`: registers Razor components, interactive server rendering, static files, antiforgery, and the web `IFormFactor` implementation.
- `Components/App.razor`: the root application component.
- `Services/FormFactor.cs`: browser-side implementation of the shared form-factor abstraction.
- `appsettings*.json`: normal ASP.NET Core host configuration files.

## Design notes

This project behaves like a host adapter. That is a healthy boundary. It should stay thin and leave domain logic elsewhere.

## Current maturity

- The host restores and builds cleanly.
- It is successfully wired to `rustles.application.Shared`.
- It still contains default starter UI behavior.
- There is no API, backend orchestration, or domain service composition yet.

## Bottom line

This is a working web shell around the shared UI. It is the most usable entry point today, but it is still only a shell.
