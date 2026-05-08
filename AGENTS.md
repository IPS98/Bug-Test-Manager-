# AGENTS.md

This file defines working rules for AI coding agents contributing to this repository.

## Project Context

Bug & Test Manager is a Windows desktop application for managing test templates, test reports, bug reports, attachments, audit history, and PDF exports.

The project owner is a beginner programmer. Explanations should be clear, patient, and practical.

## Language Rules

- Repository documentation should be written in English.
- Application UI text must be written in English.
- Code comments must be written in English.
- Conversation with the project owner can be in Russian.

## Architecture Rules

- Use C#, .NET, WPF, and MVVM.
- Use MahApps.Metro for the WPF UI.
- Keep UI code in `BugTestManager.App`.
- Keep business entities and business rules in `BugTestManager.Domain`.
- Keep use cases and interfaces in `BugTestManager.Application`.
- Keep database, file storage, PDF, and external integrations in `BugTestManager.Infrastructure`.
- Do not put database queries directly in ViewModels.
- Do not put WPF references in Domain or Application projects.

## Repository Rules

- Do not hardcode secrets.
- Do not commit build outputs.
- Do not commit local user settings.
- Keep file and folder names clear.
- Keep commits focused and use clear commit messages.
- Verify meaningful changes with build and tests whenever possible.

## Planning Rule

Application code must not be implemented until the project owner approves the architecture.
