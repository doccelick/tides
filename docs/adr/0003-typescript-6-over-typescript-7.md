# 0003. TypeScript 6 over TypeScript 7

## Status

Accepted

## Context

The frontend's API types are generated from `api/src/Tides.Api/openapi.json`. The generators build their output with the TypeScript compiler API. TypeScript 7 is a port of the compiler to Go, and its npm package no longer exposes that API.

openapi-typescript 7.13 declares a peer dependency on TypeScript 5. @hey-api/openapi-ts 0.99 installs next to TypeScript 7 but fails at startup because `ts.SyntaxKind` is undefined. On TypeScript 6, @hey-api/openapi-ts installs and runs without any override.

## Decision

Stay on TypeScript 6 and generate the API types with @hey-api/openapi-ts.

## Consequences

- The generator is a normal devDependency, locked in `package-lock.json` and updated by Dependabot.
- `.github/dependabot.yml` ignores major TypeScript updates, so minor and patch releases of TypeScript 6 still arrive.
- The frontend does without TypeScript 7's faster type checking.
- Moving to TypeScript 7 needs a generator that runs on it, and the ignore rule removed.
