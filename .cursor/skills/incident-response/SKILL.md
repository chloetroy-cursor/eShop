---
name: incident-response
description: Orchestrates evidence-first Datadog triage and autonomous local remediation with three parallel specialists.
---

# Incident response orchestrator

The user supplies a Datadog monitor ID or alert. Coordinate the investigation,
then implement and verify reversible local remediation without a handoff.

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

6. If the diagnosis supports a code change, route the remediation through the
   matching pstack playbook. Reproduce first, make the smallest root-cause fix,
   run the relevant harness, inspect the diff, commit, push, and update the
   draft PR. RabbitMQ retry-policy edits are in scope.
7. Draft a concise Jira ticket. Create or send it only when the operator names
   and authorizes the destination.
8. Production deploys and merges require an explicit deploy/merge/land/ship
   request. Do not stop local remediation while waiting for either action.
