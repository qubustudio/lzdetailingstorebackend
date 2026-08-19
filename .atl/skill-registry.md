# Skill Registry — LzDetailingStore Backend

## Project Context
- **Stack**: .NET 10, C# 14, ASP.NET Core, Entity Framework Core 10, PostgreSQL 17
- **Architecture**: Clean Architecture + Vertical Slices + Lightweight CQRS (MediatR)
- **Test Framework**: xUnit + FluentAssertions + Testcontainers
- **Last Updated**: 2026-08-11

## Skills Available

| Skill | Trigger | Scope | Path |
|-------|---------|-------|------|
| sdd-init | SDD initialization, project bootstrap | Project | `.agent/skills/sdd-init/` |
| sdd-explore | Investigation, architecture analysis | Change | `.agent/skills/sdd-explore/` |
| sdd-propose | Change proposals, PRD creation | Change | `.agent/skills/sdd-propose/` |
| sdd-spec | Requirements, acceptance criteria | Change | `.agent/skills/sdd-spec/` |
| sdd-design | Technical architecture decisions | Change | `.agent/skills/sdd-design/` |
| sdd-tasks | Implementation task breakdown | Change | `.agent/skills/sdd-tasks/` |
| sdd-apply | Code implementation from tasks | Change | `.agent/skills/sdd-apply/` |
| sdd-verify | Spec validation, test verification | Change | `.agent/skills/sdd-verify/` |
| sdd-archive | Change closure and archival | Change | `.agent/skills/sdd-archive/` |

## Conventions Detected

| Convention | Source | Value |
|-----------|--------|-------|
| Language | steering/conventions.md | C# 14 |
| Naming | steering/conventions.md | PascalCase classes, _camelCase fields |
| Architecture | steering/architecture.md | Clean Architecture + Vertical Slices |
| Testing | steering/conventions.md | xUnit + FluentAssertions |
| API Style | steering/api-design.md | Minimal APIs + REST |
| DB Migrations | steering/architecture.md | EF Core Migrations (code-first) |
| Commit Style | steering/conventions.md | Conventional Commits |

## Testing Capabilities

| Capability | Status | Tool |
|-----------|--------|------|
| Unit Tests | Planned (project not scaffolded) | xUnit |
| Integration Tests | Planned | Testcontainers + PostgreSQL |
| Architecture Tests | Planned | NetArchTest |
| API Tests | Planned | WebApplicationFactory |
| Coverage | Planned | Coverlet |
| Test Command | `dotnet test` | — |
| Strict TDD | false (no test runner available yet) | — |

## Notes

- Project is in documentation/design phase — no source code exists yet
- Strict TDD will be enabled once the solution is scaffolded and xUnit is configured
- Testing capabilities will be updated after `sdd-apply` creates the project structure
