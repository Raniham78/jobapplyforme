# Rani JobPilot

A Windows-first C#/.NET 8 prototype for assisted job-application form filling and local application tracking.

## What it does

- Opens an application URL in a dedicated Microsoft Edge profile using Playwright for .NET.
- Lets the user sign in manually and keeps the browser session separate from everyday browsing.
- Fills selected common HTML input fields when their labels can be matched confidently to the local profile.
- Can upload a PDF CV when a valid local path is configured and the form exposes a file input.
- Records application drafts and manually confirmed submissions in a local SQLite database.
- Never clicks the Submit button: the user reviews the form and submits manually.

## Requirements

- Windows 10/11
- .NET 8 SDK
- Microsoft Edge
- Network access for the first NuGet restore

No Docker, WSL, Hyper-V, Python service, Manus credits or external AI API are required.

## Open and run

Open `JobPilot.sln` with Visual Studio 2022, restore NuGet packages, then run the `JobPilot` project.

Or use PowerShell from the repository root:

```powershell
dotnet restore .\JobPilot.sln
dotnet build .\JobPilot.sln -c Release
dotnet run --project .\src\JobPilot\JobPilot.csproj
```

## First run

1. Choose menu option `3` to create/edit the local profile.
2. Set contact details and the full path to your English CV PDF. Save and close Notepad.
3. Choose menu option `1` and enter company, role and application URL.
4. Sign in manually if the site requests authentication.
5. Navigate to the form, return to the console and press Enter.
6. Review every field in the browser. Fill custom questions and declarations yourself.
7. Submit manually in the browser if everything is correct.
8. Return to the console and mark the application as submitted.

## Local data

The application creates `%LOCALAPPDATA%\RaniJobPilot` automatically.

- `profile.json`: personal contact details and local CV path
- `EdgeProfile`: browser session/cookies for the dedicated Edge profile
- `jobpilot.db`: SQLite application tracker

Never commit personal profile data, CV files, the SQLite database, browser cookies or the Edge profile to a public repository.

## Limitations

This is an early prototype. The current autofill adapter is conservative: it targets common standard HTML fields and selected Ashby-style labels. It does not guarantee support for every custom React control, dropdown, multi-step form, checkbox agreement or screening question. CV upload requires a file input that Playwright can target.

The project does not crawl job boards, rewrite CVs using AI, monitor job sites in the background or submit applications automatically. Field matching needs to be tested and improved against each recruiting platform's actual form markup.

## Roadmap

- Add a field-mapping preview and per-field confirmation.
- Improve Ashby selectors based on real form structures.
- Add adapters for Greenhouse and Lever.
- Add a Windows desktop UI instead of the current console menu.
- Add job discovery/import and duplicate detection.
- Add optional, reviewed CV/cover-letter tailoring.
