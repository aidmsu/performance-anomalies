# 2. Search indexing pipeline

## Status

Accepted

## Decision

The search index is rebuilt from the catalog snapshot. Analyzer stages run one after another over the
whole catalog so that every stage sees the output of the previous one.
