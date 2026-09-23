# Agent guidelines — Core

Technical rules for AI agents working in GamersCommunity.Core (shared .NET libraries).

## Never start servers

Do **not** start long-lived processes. One-shot builds/tests are OK.

## Role of this repo

- Hold **generic** building blocks reused by Platform, Gateway, and game Consumers (Rabbit helpers, logging, exceptions, EF helpers, etc.).
- When a game or Platform change is truly shared and would otherwise be copied into every `Program.cs` / host, prefer extracting it here so copies do not diverge.
- Do **not** put game-specific domain (LoL teams, WoW guilds, etc.) in Core.

## Game Template sync

If a Core change becomes a new default for every game host, also update **GamersCommunity.Games.Template** so new games inherit it.

## Commits / push

Only when the developer explicitly asks.
