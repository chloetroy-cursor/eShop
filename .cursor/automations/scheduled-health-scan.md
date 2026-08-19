# Scheduled health scan — draft artifact

Weekdays at 09:00 local time, query the last closed 30-minute absolute
window for `ordering-api`. Draft a follow-up only when error rate exceeds 0.15,
or p95 exceeds 500 ms and a release occurred within 60 minutes. Cite monitor,
window, release, and request IDs. No code changes or external sends.
