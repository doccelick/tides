# 0002. GHCR over ACR

## Status

Accepted

## Context

The API is deployed to Azure Container Apps as a container image, which needs a registry. Azure Container Registry (ACR) is Azure's own registry, but it costs money even at the Basic tier. GitHub Container Registry (GHCR) is free for public images and lives next to the code and the GitHub Actions workflows.

## Decision

Publish the API image to GHCR and have Container Apps pull it from there.

## Consequences

- No registry cost, which keeps the project on free tiers.
- CI pushes images with the workflow's built-in `GITHUB_TOKEN`, so no registry credential is stored.
- The image is public, so it must never contain secrets.
- Pulling from a registry outside Azure means one more external service for deployments to depend on.
