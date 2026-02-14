---
name: test
description: Run Screen Translator tests. Supports dotnet test with optional filter. Arguments — test name filter or 'all'.
---

# Test — run Screen Translator tests

## Usage

**Arguments**: `$ARGUMENTS`
- No arguments: run all tests
- Test name filter: `dotnet test --filter "MethodName"` pattern
- `all` — run all tests with verbose output

**Examples**:
```
/test                       → all tests
/test Translation           → tests matching "Translation"
/test --verbosity detailed  → verbose output
```

## Steps

### 1. Run tests

```bash
# All tests
dotnet test 2>&1

# Filtered tests
dotnet test --filter "$ARGUMENTS" 2>&1

# Verbose
dotnet test --verbosity detailed 2>&1
```

### 2. Analyze results

**On failure:**
1. Read the error message
2. Find file and line from stack trace
3. Determine problem type:
   - **Assertion failure** → logic error, check expectations
   - **Build error** → check imports, types, signatures
   - **NullReferenceException** → missing null check
   - **Timeout** → possible deadlock or infinite loop

**On success:**
- Show summary: test count, execution time
- Show warnings if any

### 3. Response format

```
## Test Results

### Summary: X passed, Y failed, Z skipped
Time: N seconds

### Failures (if any):
#### TestName
**File**: path/to/file.cs:42
**Error**: description
**Possible cause**: analysis
```

### 4. Recommendations

- After fixing a bug — suggest writing a test covering it
- New tests should follow patterns of existing tests
- Unit tests should not make real HTTP requests (use mocks)
