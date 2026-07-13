# ADR-001: Navigation Host Allowlist — Minimum Privilege

**Date:** 2026-07-12
**Status:** ACCEPTED
**Supersedes:** `02-architecture.md` line 215 ("EndsWith .helltides.com")

## Context

Architecture document `02-architecture.md` specifies `uri.Host.EndsWith(".helltides.com")` as the navigation allowlist, which permits all subdomains including `app.helltides.com`, `map.helltides.com`, etc. During STAGE-01-TASK-06 implementation, the decision was made to restrict to only `helltides.com` and `www.helltides.com`.

## Decision

`DomainPolicy.IsAllowed()` uses exact host matching against `"helltides.com"` and `"www.helltides.com"` only. No wildcard subdomain matching.

## Rationale

1. **Minimum privilege:** No known subdomains are required for MVP (the live map at helltides.com does not redirect to subdomains).
2. **Security-first:** Each additional allowed host expands the attack surface. Subdomains should be added only when a concrete navigation requirement exists.
3. **Reversibility:** Adding a subdomain to the allowlist is a one-line code change + test. Removing a once-allowed subdomain later could break user workflows.

## Consequences

- If helltides.com redirects to a subdomain, the redirect will be blocked. A new task will explicitly add that subdomain to the allowlist.
- Architecture document `02-architecture.md` line 215 is superseded by this ADR for the allowlist scope. The rest of the navigation strategy (blocking popups, downloads, devtools) is unchanged.
