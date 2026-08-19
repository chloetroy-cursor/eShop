# Datadog alert webhook — draft artifact

Trigger on monitor `mon-ordering-publish-errors`. Deduplicate by
`monitor_id + triggered_at + service`, pin the alert's absolute window, then run
`incident-response` read-only. Produce a cited Slack/Jira draft; never send,
deploy, merge, or edit retry policy. On missing evidence, record `no evidence`
and stop. Customer production is out of scope.
