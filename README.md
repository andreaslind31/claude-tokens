# Claude Tokens

A macOS menu bar application that tracks your [Claude Code](https://docs.anthropic.com/en/docs/claude-code) token usage and estimated costs.

Built with C# / .NET 8 and [Avalonia UI](https://avaloniaui.net/).

## Features

- Lives in the macOS menu bar — no dock icon, no main window
- Displays daily and all-time token usage statistics
- Per-model breakdown (Opus, Sonnet, Haiku) with token counts and estimated costs
- Per-project last-session costs
- Auto-refreshes when Claude Code updates its data files
- Estimates costs from token counts using Anthropic's published pricing

## Menu Preview

```
  Today (Feb 9)
  ─────────────
  Cost: ~$12.34
  Messages: 3,182
  Sessions: 6
  Tool Calls: 369
  ─────────────
  Models                        →
    Opus 4.6
      In: 7.3K  Out: 26.6K
      Cache R: 51.0M  W: 4.5M
      ~$160.28
    Sonnet 4.5
      ...
  ─────────────
  Projects                      →
    backoffice-v2  $0.05
  ─────────────
  All Time
  Cost: ~$210.50
  Messages: 5,931
  Sessions: 21
  ─────────────
  Refresh
  Quit
```

## Prerequisites

- macOS
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

Install .NET 8 via the official install script:

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --install-dir ~/.dotnet
export PATH="$HOME/.dotnet:$PATH"
```

## Build & Run

```bash
dotnet run --project src/ClaudeTokens
```

## Data Sources

The app reads local files written by Claude Code:

| File | Contents |
|------|----------|
| `~/.claude/stats-cache.json` | Daily activity, per-model token aggregates, total sessions/messages |
| `~/.claude.json` | Per-project last-session costs and token breakdowns |

No network requests are made. All data stays local.

## Project Structure

```
src/ClaudeTokens/
  Program.cs                    Entry point (ShowInDock = false)
  App.axaml / App.axaml.cs      TrayIcon setup, no main window
  Models/
    StatsCache.cs               Deserialization for stats-cache.json
    ClaudeConfig.cs             Deserialization for .claude.json
    TokenSummary.cs             Aggregated view model
  Services/
    TokenDataService.cs         Reads/parses data files, estimates costs
    FileWatcherService.cs       Watches files for changes (debounced)
  ViewModels/
    MenuBarViewModel.cs         Builds the native dropdown menu
  Assets/
    icon.png                    16x16 menu bar icon
```

## Publishing

To build a self-contained binary:

```bash
dotnet publish src/ClaudeTokens/ClaudeTokens.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  -o publish/
```

To start at login, add the published app to **System Settings → General → Login Items**.
