# Release Checklist

Use this checklist before committing, pushing, or sharing a demo build.

## Before Commit

1. Close any running `BugTestManager.App.exe`.
2. Run:

```powershell
dotnet build BugTestManager.sln
dotnet test tests\BugTestManager.Domain.Tests\BugTestManager.Domain.Tests.csproj --no-build
dotnet test tests\BugTestManager.Infrastructure.Tests\BugTestManager.Infrastructure.Tests.csproj --no-build
```

3. Open the app from Visual Studio.
4. Manually test:
   - create/edit a template item;
   - create a test session;
   - update a test case;
   - update a check to failed;
   - verify the parent test case becomes failed;
   - add an attachment.

## Before Push

1. Check `git status`.
2. Make sure no build output, database files, or local settings are staged.
3. Commit with a clear message.
4. Push the current branch to GitHub.

## Demo Publish Command

For a local demo executable folder, run:

```powershell
dotnet publish src\BugTestManager.App\BugTestManager.App.csproj -c Release -r win-x64 --self-contained false -o artifacts\publish\BugTestManager
```

The `artifacts` folder is ignored by Git.
