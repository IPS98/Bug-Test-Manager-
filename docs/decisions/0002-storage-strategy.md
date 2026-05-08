# Decision 0002 - Storage Strategy

## Status

Proposed.

## Decision

Design the app to support both demo/local storage and future shared server storage.

Recommended options:

- Demo: SQLite or SQL Server LocalDB.
- Production: SQL Server on a company server.
- Attachments: file storage with metadata in the database.

## Reasoning

The first version needs to be easy to show at work. A local database is faster to set up for a demo.

The real target is a shared workflow where testers and developers can both edit data. That is better served by a server database than by a single local database file.

Images and other attachments can become large. Storing them as files keeps the database smaller and makes backup strategy clearer.

## Consequences

- The app needs configuration for database connection and attachment storage location.
- Infrastructure should hide the exact database provider behind interfaces.
- A server deployment plan will be needed before real multi-user production use.
