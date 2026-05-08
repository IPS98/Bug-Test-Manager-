# Decision 0001 - Technology Stack

## Status

Proposed.

## Decision

Use C#, .NET, WPF, MVVM, and MahApps.Metro for the desktop application.

Use .NET 9 for the first skeleton because the local machine currently has .NET SDK 9 installed. Plan to move to .NET 10 LTS after the SDK is installed.

Use Entity Framework Core for data access.

Use CommunityToolkit.Mvvm for common MVVM helpers.

## Reasoning

The project target is a Windows desktop application. WPF is suitable for Windows desktop tools and works well with MVVM.

As of May 7, 2026, Microsoft lists .NET 10 as the current LTS release supported until November 2028. The local development machine has .NET SDK 9.0.311 installed, so the first skeleton can be created with .NET 9 and upgraded later.

MahApps.Metro is already familiar to the project owner from another work project, so it is a good choice for a consistent and modern WPF interface.

Entity Framework Core keeps database access simpler and allows the project to support more than one database provider later.

CommunityToolkit.Mvvm reduces repetitive ViewModel code.

## Consequences

- The app will be Windows-only.
- The first skeleton targets .NET 9 on Windows.
- A future technical task should upgrade the target framework to .NET 10 when the SDK is available.
- The UI will use XAML.
- The team must keep ViewModels separate from Views.
- The database provider can be changed later more easily than with direct SQL everywhere.
