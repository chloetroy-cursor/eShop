---
name: incident-response
description: Orchestrates read-only triage of a real Datadog alert with three parallel specialists and a mandatory approval gate before Jira creation, remediation, or external writes.
---

# Incident response orchestrator

The user supplies an approved Datadog monitor ID or alert. Coordinate the
investigation; do not implement fixes or write externally.

1. Use Datadog read-only. Retrieve the approved monitor and pin an absolute
   investigation window from its alert time.
2. Never reveal notification targets, people, customer names, secrets, URLs, or
   extracted values. Stop if the monitor is not safe to discuss.
3. Launch these three subagents in one parallel tool-call message:
   - `telemetry-investigator`: monitor, logs, traces, and impact
   - `change-correlator`: deployments and other changes near onset
   - `hypothesis-challenger`: independently attempt to falsify the diagnosis
4. Separate observations, inference, contradictions, and unknowns. Cite the
   monitor ID, absolute window, and Datadog source for each claim.
5. Synthesize:

```text
Monitor / window / impact
Leading diagnosis and confidence
Evidence supporting and contradicting
Unknowns
Recommended next step
Approval required: yes
```

6. Draft a concise Jira ticket, but do not create it.
7. **Stop for explicit human approval.** Do not create Jira issues, edit code,
   deploy, merge, or send messages before approval.
