---
name: review
description: Self-review of current changes (git diff) against safety checklist, project patterns, and code style. Use before creating a PR. Optional argument — base branch for comparison (default HEAD).
---

# Review — pre-PR change review

Analyzes `git diff` against a quality checklist specific to Screen Translator (C# + WPF).

## Usage

**Arguments**: `$ARGUMENTS` — base branch for comparison (default `HEAD` for unstaged + staged).

## Steps

### 1. Collect diff

```bash
# No arguments — all uncommitted changes
git diff HEAD

# With argument — compare to branch
git diff $ARGUMENTS...HEAD
```

Also check changed files:
```bash
git diff --name-only HEAD
```

### 2. Checklist review

Analyze each changed file against these categories:

#### Security
- [ ] No hardcoded API keys, tokens, passwords
- [ ] No `.env` files or configs with secrets in commit
- [ ] No `config.json` with real API keys
- [ ] P/Invoke calls are safe (no buffer overflows, proper marshaling)

#### C# / WPF Patterns
- [ ] **DI pattern**: new services use dependency injection, not static singletons
- [ ] **Interfaces**: every service has an interface in `Services/Interfaces/`
- [ ] **TranslationRouter**: new providers added to router switch
- [ ] **Auto-save**: settings changes trigger `ScheduleAutoSave()`, not manual save
- [ ] **`_isLoading` flag**: used in change handlers to prevent save loops
- [ ] **Error handling**: try/catch in UI event handlers, no unhandled exceptions

#### C# Quality
- [ ] `dotnet build` passes without errors
- [ ] No unused `using` imports
- [ ] No `// TODO` or `// HACK` left behind
- [ ] Async methods properly awaited (no fire-and-forget without `_ =`)
- [ ] Nullable reference types handled (`?.`, `?? default`, `is not null`)

#### XAML Quality
- [ ] Bindings use `UpdateSourceTrigger=PropertyChanged` where needed
- [ ] DataTriggers match correct enum values
- [ ] No hardcoded strings that should be bindings

#### File Hygiene
- [ ] No backup files (*.bak, *.old, *.backup)
- [ ] No temp files (*.tmp, *.log)
- [ ] No bin/obj accidentally committed

### 3. Verdict

```
## Verdict: READY / NEEDS FIX / NEEDS DISCUSSION

### Issues Found

#### Critical (blocks PR)
- ...

#### Warnings (recommended to fix)
- ...

#### Notes (optional)
- ...
```
