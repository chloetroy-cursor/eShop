.PHONY: demo-reset demo-telemetry demo-test-policy

demo-reset:
	python3 .cursor/skills/demo-reset/scripts/reset.py

demo-telemetry:
	python3 demo/incident/generate_telemetry.py

demo-test-policy:
	python3 demo/incident/check_retry_policy.py
