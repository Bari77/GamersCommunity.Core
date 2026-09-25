# Agent guidelines — Core

Shared module: [`AgentKit/`](AgentKit/) → [GamersCommunity.AgentKit](https://github.com/Bari77/GamersCommunity.AgentKit)

- [`AgentKit/AGENTS.base.md`](AgentKit/AGENTS.base.md)
- [`AgentKit/ENGINEERING_STANDARDS.md`](AgentKit/ENGINEERING_STANDARDS.md)
- [`AgentKit/POLICY.md`](AgentKit/POLICY.md)
- Optional: [`AGENTS.override.md`](AGENTS.override.md)

## Repo-specific

- Hold **generic** .NET building blocks only (RPC, hosting, seeds framework, realtime, Platform clients, etc.).
- Do **not** put game-specific domain (LoL teams, WoW guilds, …) here.
- If a Core change becomes the default for every game host, also update **GamersCommunity.Games.Template**.
