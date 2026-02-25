# Claude Tokens

A native macOS menu bar app that tracks your [Claude Code](https://docs.anthropic.com/en/docs/claude-code) token usage and estimated costs in real time.

## Features

- **Menu bar only** — no dock icon, no main window, always one click away
- **Live cost tracking** — reads Claude Code's local data files and estimates costs per model (Opus, Sonnet, Haiku)
- **Rate limit display** — shows remaining token % via the Anthropic API with a dynamic menu bar icon
- **Per-project breakdown** — last-session costs for each Claude Code project
- **Privacy-first** — all data is read locally; nothing is sent except lightweight rate limit polling

## Setup

1. Build and publish:
   ```sh
   ./publish.sh
   ```
2. Copy to Applications:
   ```sh
   cp -R "publish/Claude Tokens.app" /Applications/
   ```
3. (Optional) Add to **System Settings > General > Login Items** for auto-start.
4. To enable rate limit display, add your Anthropic API key to `~/.claude-tokens/settings.json`.

## Prerequisites

- macOS (Apple Silicon)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Tech Stack

- C# / .NET 8
- [Avalonia UI](https://avaloniaui.net/) (tray icon & native menus)
- SkiaSharp (dynamic icon rendering)
