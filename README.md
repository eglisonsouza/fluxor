# Fluxor

Lightweight [**MediatR**](https://github.com/jbogard/MediatR)-style command pipeline for .NET with pluggable **behaviors** (middleware). Send commands through a single entry point, resolve handlers from DI, and wrap execution with cross-cutting concerns such as logging.

| | |
|---|---|
| **Package** | `fluxor` ([GitHub Packages](https://github.com/eglisonsouza/fluxor)) |
| **Target** | .NET 10 |
| **License** | MIT |

---

## Features

- **Command / handler pattern** — `ICommand<TResponse>` and `ICommandHandler<TCommand, TResponse>`
- **Pipeline behaviors** — decorate handlers (logging, validation, timing, etc.)
- **Microsoft.Extensions.DependencyInjection** — register with `AddCommandPipelines()`
- **Built-in logging behavior** — structured logs, timing, slow-command warnings
- **Query abstractions** — `IQuery<TResponse>` / `IQueryHandler<,>` for the same handler style (pipeline execution is command-focused today)

---

## Installation

The package is published to **GitHub Packages** (private feed). You need a GitHub Personal Access Token with `read:packages` (and `repo` if the repository is private).

### Visual Studio

1. **Tools** → **NuGet Package Manager** → **Package Manager Settings** → **Package sources**
2. Add source: `https://nuget.pkg.github.com/eglisonsouza/index.json`
3. Authenticate with your GitHub username and PAT
4. Install package **`fluxor`**

### `nuget.config` (solution or user level)

Copy [`nuget.config.example`](nuget.config.example), replace placeholders, then:

```bash
dotnet add package fluxor --version 0.1.0
```

### Project reference (local development)

```bash
dotnet add reference path/to/fluxor/src/Fluxor.csproj
```

---

## Quick start

### 1. Register services

```csharp
using Fluxor.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging();
builder.Services.AddCommandPipelines();

// Register your handlers
builder.Services.AddScoped<ICommandHandler<CreateOrderCommand, Guid>, CreateOrderHandler>();

var host = builder.Build();
```

`AddCommandPipelines()` registers:

- `ICommandPipeline` → `CommandPipeline`
- `IPipelineBehavior` → `LoggingBehavior` (included by default)

Register additional behaviors with `services.AddScoped<IPipelineBehavior, YourBehavior>();` — order is applied as a middleware chain.

### 2. Define a command and handler

```csharp
using Fluxor.Abstractions;

public sealed record CreateOrderCommand(string CustomerId, decimal Amount) : ICommand<Guid>;

public sealed class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    public Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();
        // persist order...
        return Task.FromResult(orderId);
    }
}
```

### 3. Execute through the pipeline

```csharp
using Fluxor.Abstractions;

var pipeline = host.Services.GetRequiredService<ICommandPipeline>();

var orderId = await pipeline.ExecuteAsync(
    new CreateOrderCommand("customer-1", 99.90m),
    cancellationToken);

Console.WriteLine($"Order created: {orderId}");
```

---

## How it works

```text
ExecuteAsync(command)
       │
       ▼
┌──────────────────┐
│ Behavior N       │  ← e.g. LoggingBehavior (outermost registered runs first)
└────────┬─────────┘
         ▼
┌──────────────────┐
│ Behavior 1       │
└────────┬─────────┘
         ▼
┌──────────────────┐
│ Command handler  │  ← resolved from DI by command type
└────────┬─────────┘
         ▼
      TResponse
```

1. `CommandPipeline` resolves `ICommandHandler<TCommand, TResponse>` from `IServiceProvider`.
2. Each `IPipelineBehavior` wraps the next delegate (similar to ASP.NET Core middleware).
3. The innermost delegate invokes `HandleAsync` on the handler.

If no handler is registered, `ExecuteAsync` throws `InvalidOperationException`.

---

## Pipeline behaviors

Behaviors implement `IPipelineBehavior`:

```csharp
public interface IPipelineBehavior
{
    Task<TResponse> HandleAsync<TResponse>(
        ICommand<TResponse> command,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken);
}
```

### Built-in: `LoggingBehavior`

- Logs command name and type (scoped)
- Warns when execution exceeds 500 ms
- Handles cancellation and exceptions

Requires `ILogger<LoggingBehavior>` (use `AddLogging()`).

### Custom behavior example

```csharp
using Fluxor.Abstractions;

public sealed class ValidationBehavior : IPipelineBehavior
{
    public async Task<TResponse> HandleAsync<TResponse>(
        ICommand<TResponse> command,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        // validate command...
        return await next();
    }
}
```

Register in DI:

```csharp
services.AddScoped<IPipelineBehavior, ValidationBehavior>();
```

---

## Queries

`IQuery<TResponse>` and `IQueryHandler<TQuery, TResponse>` mirror the command contracts so you can structure read models the same way. Wire query handlers in DI and invoke them directly, or extend the library with a query pipeline using the same pattern as `CommandPipeline`.

---

## Project structure

```text
fluxor/
├── src/
│   ├── Abstractions/          # ICommand, ICommandHandler, IQuery, IPipelineBehavior, ...
│   ├── Behaviors/             # LoggingBehavior
│   ├── Extensions/            # AddCommandPipelines()
│   ├── Pipeline/              # CommandPipeline
│   └── Fluxor.csproj
├── .github/
│   ├── workflows/
│   │   ├── ci.yml             # Build on develop + PRs; build on main
│   │   └── publish.yml        # Release on push to main
│   └── scripts/
│       └── calculate-version.sh
└── docs/
    ├── PACKAGING.md
    ├── VERSIONING.md
    └── TROUBLESHOOTING.md
```

---

## Development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Build

```bash
dotnet build src/Fluxor.csproj
```

### Pack locally

```bash
dotnet pack src/Fluxor.csproj -c Release -o ./artifacts -p:Version=0.1.0-preview
```

---

## CI/CD and releases

Fluxor uses **GitHub Actions** with a **GitFlow-style** branch model:

| Branch | Purpose | Automation |
|--------|---------|------------|
| `develop` | Daily work | **CI** — build and pack |
| `main` | Releases | **CI** + **Publish** — NuGet + git tag |

### Versioning

Versions follow [**Conventional Commits**](https://www.conventionalcommits.org/) on merges to `main`:

| Commit | Version bump |
|--------|----------------|
| `fix:` | Patch |
| `feat:` | Minor |
| `feat!:` / `BREAKING CHANGE:` | Major |
| `chore:`, `docs:`, `ci:`, … | No release |

Tags are created automatically as `vX.Y.Z` (e.g. `v0.2.0`).

Details: [docs/VERSIONING.md](docs/VERSIONING.md)

### Publishing

- **Feed:** `https://nuget.pkg.github.com/eglisonsouza/index.json`
- **Package id:** `fluxor` (lowercase, required by GitHub Packages)
- **Trigger:** push to `main`, or manual **Actions → Publish NuGet package** (enter version, branch `main`)

Details: [docs/PACKAGING.md](docs/PACKAGING.md)

### Troubleshooting Actions

If workflows do not start or show **“Failed to queue workflow run”**, check [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) and [GitHub Status](https://www.githubstatus.com/) (Actions outages affect all repositories).

---

## Comparison with MediatR

| | MediatR | Fluxor |
|---|---------|--------|
| Scope | Commands, queries, notifications, pipelines | Commands + behaviors (queries as contracts) |
| DI | Optional extensions | `Microsoft.Extensions.DependencyInjection` |
| Size | Full-featured | Small, focused library |

Fluxor is intended for projects that want a **simple command pipeline** without taking a dependency on the full MediatR surface area.

---

## Contributing

1. Create a branch from `develop`
2. Use [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, etc.)
3. Open a pull request into `develop`
4. Merge `develop` → `main` when ready to release

---

## License

MIT — see [PackageLicenseExpression](src/Fluxor.csproj) in the project file.

---

## Links

- Repository: https://github.com/eglisonsouza/fluxor
- [Versioning & branches](docs/VERSIONING.md)
- [GitHub Packages & Visual Studio](docs/PACKAGING.md)
- [Actions troubleshooting](docs/TROUBLESHOOTING.md)
