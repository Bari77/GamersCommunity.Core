# GamersCommunity.Core

`GamersCommunity.Core` is the core library of the **GamersCommunity** project.  
It provides the fundamental building blocks, utilities, and abstractions that other modules of the ecosystem rely on.

> Status: **Work in progress**. This repository is public, but external contributions are not currently solicited.

---

## ✨ Introduction

GamersCommunity aims to simplify the development of game-related services and tools by offering a clean, modular foundation for .NET applications.

This package focuses on solving common cross-cutting concerns:
- Shared domain primitives (errors, results, value objects).
- Consistent configuration & options binding.
- Dependency Injection helpers and extensions.
- Diagnostics: logging helpers and structured logging conventions.
- Utility helpers (guards, pagination, mapping helpers, etc.).

The library is designed to be **lightweight**, **extensible**, and **easy to integrate** in new or existing solutions.

---

## 🚀 Getting Started

### Requirements
- **.NET 10.0**
- Visual Studio 2022 / VS 2026 / Rider / VS Code

### Installation
Packages are published to **GitHub Packages** on each version tag (see [docs/RELEASE.md](../docs/RELEASE.md)).

```bash
dotnet add package GamersCommunity.Core
dotnet add package GamersCommunity.Core.Logging
```

Publish a release (maintainers):

```bash
git tag v9.5.0
git push origin v9.5.0
```

---

## 💬 Feedback

This project is under active development.  
If you encounter a bug, have a question, or want to suggest an improvement, please open an issue:

👉 **Issues:** https://github.com/Bari77/GamersCommunity.Core/issues

You can also reach out on your preferred social channel if applicable.

---

## 🤝 Contributing

While the repository is public, **external contributions are not actively requested** at this time.  
That said, **bug reports and feedback** are very welcome via the issues page.

If you still wish to propose a change:
- Fork the repository
- Create a feature branch
- Open a PR marked as **Draft** for discussion

> Note: The maintainers reserve the right to close PRs that fall outside the current roadmap.

---

## 🧩 Versioning & Breaking Changes

Until a stable **1.x** release, **breaking changes may occur** at any time as the API surface evolves.

---

## 📜 License

Unless stated otherwise in the repository, this project will be licensed under a permissive license (e.g., MIT).  
Check the `LICENSE` file once it is added.

---

## 🙌 Credits

Built with ❤️ by the GamersCommunity maintainers.
