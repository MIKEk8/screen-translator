---
name: improve
description: Multi-perspective quality analysis of code, UI, documentation or any artifact. Evaluates from 14 perspectives and outputs a prioritized list of improvements. Argument — what to analyze (file, component, system).
---

# Improve — multi-perspective quality analysis

Systematic evaluation of any artifact (code, UI, documentation, architecture) from 14 expert perspectives.

## Usage

**Arguments**: `$ARGUMENTS` — what to analyze:
- File or directory path: `/improve src/ScreenTranslator.Core/Services/`
- Component name: `/improve translation service`
- No argument: analyze what's being discussed in current conversation

## Steps

### 1. Identify the target

From `$ARGUMENTS` or conversation context:
- **Artifact type**: UI/XAML, backend C#, services, models, documentation, architecture
- **Files**: read all relevant files (use Glob + Read)
- **Context**: purpose, users, dependencies

### 2. Evaluate perspectives

For each perspective: briefly assess current state and suggest **1-5 specific improvements** (if any). Skip perspectives that don't apply.

#### Perspectives

**Group: Users**

| # | Perspective | Focus | Applies to |
|---|-----------|-------|-----------|
| 1 | Visual Designer | Visual hierarchy, typography, colors, spacing, consistency | XAML, UI |
| 2 | UX Designer | Navigation, feedback, predictability, cognitive load | UI, desktop |
| 3 | Desktop User | Hotkeys, window management, tray integration, performance | Desktop app |
| 4 | Accessibility | Keyboard navigation, contrast, screen readers | XAML |

**Group: Content Quality**

| # | Perspective | Focus | Applies to |
|---|-----------|-------|-----------|
| 5 | Technical Writer | Completeness, structure, terminology | Docs, comments |
| 6 | Data Steward | Data freshness, coverage, consistency | Config, models |
| 7 | Product Manager | Use-case coverage, user value, priorities | Everything |

**Group: Technical Quality**

| # | Perspective | Focus | Applies to |
|---|-----------|-------|-----------|
| 8 | C# Developer | LINQ, async/await, null safety, patterns, idiomatic C# | C# code |
| 9 | Architect | Scalability, coupling, separation of concerns, SOLID, DI | Architecture |
| 10 | Performance Engineer | Memory allocation, async overhead, startup time | C#, XAML |
| 11 | QA / Tester | Testability, edge cases, error handling, regression risks | Code |
| 12 | Security Engineer | API key handling, injection, P/Invoke safety | Code, config |

**Group: Maintenance**

| # | Perspective | Focus | Applies to |
|---|-----------|-------|-----------|
| 13 | New Developer (Onboarding) | Code clarity, docs, naming, README | Code, docs |
| 14 | DevOps | Build, CI/CD, binary size, self-contained publish | Infrastructure |

### 3. Format per perspective

```
### [#N] Perspective: [Name]

**Assessment**: [Good / OK / Needs attention] — one sentence.

**Improvements**:
- [P1/P2/P3] Specific improvement with rationale
```

Priorities: **P1** critical, **P2** important, **P3** nice-to-have.

### 4. Summary table

```
## Improvement Summary

| # | Priority | Perspective | Improvement | Complexity |
|---|----------|------------|------------|-----------|
| 1 | P1 | Security | ... | Low |
| 2 | P1 | UX | ... | Medium |

Complexity: Low (< 30 min), Medium (1-4 hours), High (> 4 hours)
```

Sort: P1 first, then P2, then P3. Within priority — easiest first.

### 5. Recommendation

```
## Recommendation

**Overall score**: [1-10] / 10
**Quick wins** (do now): #1, #3, #5
**Strategic improvements** (plan): #2, #8
```
