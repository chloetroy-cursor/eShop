# Slack request-ID triage — sandbox only

When a sandbox message contains `req-order-*`, extract exactly one request ID
and run `trace-request` read-only. Reply as a draft with the absolute window,
span/retry summary, monitor ID, release, and citations. Ignore duplicate
`channel + timestamp + request_id` deliveries. Never read or write customer
Slack and never change code.
