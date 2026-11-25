# 1. Simulated infrastructure

## Status

Accepted

## Context

The test suite must run on every platform without a database, message broker or network access.

## Decision

Components call cost primitives (`SimulatedDatabase`, `SimulatedStorage`, `SimulatedNetwork`, `Compute`)
that stand in for external systems.

## Consequences

Tests are hermetic and fast to set up. Timing of external calls is part of the code under test.
