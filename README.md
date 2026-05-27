# Rinha de Backend 2026 (.NET)

This repository uses a small `package.json` only as a command runner to simplify local development, Docker orchestration, load testing, and utility tasks.

It is **not** a Node.js application.

---

# Requirements

Before running the commands, make sure you have installed:

- Docker Desktop
- .NET SDK
- Node.js + npm

---

# Available Commands

## Full Test Run

Runs the entire environment, executes the full k6 test suite, then shuts everything down.

```bash
npm run test
```

### What it does

Equivalent to:

```bash
npm run docker:up
npm run k6:test
npm run docker:down
npm run k6:test:stop
```

### Flow

1. Builds and starts the backend containers
2. Runs the k6 test profile
3. Stops the backend containers
4. Stops the k6 containers

---

# Smoke Test

Runs a lighter and faster validation suite.

```bash
npm run test:smoke
```

### What it does

Equivalent to:

```bash
npm run docker:up
npm run k6:smoke
npm run docker:down
npm run k6:smoke:stop
```

### Recommended for

- Quick validation
- CI checks
- Basic health verification
- Fast local feedback

---

# Docker Commands

## Start Containers

Builds and starts the backend stack.

```bash
npm run docker:up
```

### Internally runs

```bash
cd .\docker\ && docker compose up --build --no-deps -d
```

### Notes

- Rebuilds images before starting
- Runs detached (`-d`)
- Does not start dependencies (`--no-deps`)

---

## Stop Containers

Stops and removes the backend stack.

```bash
npm run docker:down
```

### Internally runs

```bash
cd .\docker\ && docker compose down
```

---

# k6 Load Tests

## Run Full Test Profile

Starts the full k6 test suite.

```bash
npm run k6:test
```

### Internally runs

```bash
cd .\test\ && docker compose --profile test up
```

---

## Stop Full Test Profile

Stops the full k6 test environment.

```bash
npm run k6:test:stop
```

### Internally runs

```bash
cd .\test\ && docker compose --profile test down
```

---

## Run Smoke Profile

Starts the smoke test profile.

```bash
npm run k6:smoke
```

### Internally runs

```bash
cd .\test\ && docker compose --profile smoke up
```

---

## Stop Smoke Profile

Stops the smoke test environment.

```bash
npm run k6:smoke:stop
```

### Internally runs

```bash
cd .\test\ && docker compose --profile smoke down
```

---

# Generate Data / Conversion Utilities

Runs the converter project.

```bash
npm run generate
```

### Internally runs

```bash
dotnet run --project src/RinhaNet.Converter
```

### Purpose

Used for generating or converting project data/assets required by the application.

---

# Project Structure

```text
/docker
  Docker compose files for the backend stack

/test
  k6 load testing environment and profiles

/src
  Main .NET source code
```

---

# Typical Workflow

## Full Performance Validation

```bash
npm run test
```

---

## Quick Validation

```bash
npm run test:smoke
```

---

## Manual Development Flow

Start backend:

```bash
npm run docker:up
```

Run smoke tests:

```bash
npm run k6:smoke
```

Stop everything:

```bash
npm run docker:down
npm run k6:smoke:stop
```

---

# Notes

- The commands are optimized for Windows shell usage (`.\` paths).
- Docker Compose profiles are used to separate full tests from smoke tests.
- npm is being used only as a task runner/orchestration layer.