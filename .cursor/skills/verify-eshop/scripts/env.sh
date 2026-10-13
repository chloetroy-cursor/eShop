# Sourced by verify.sh and cleanup.sh. Keep in step with doctor.py.
#
# Agent sandboxes export PLAYWRIGHT_BROWSERS_PATH under the system temp dir, a per-session
# cache that is usually empty; the durable default (~/Library/Caches/ms-playwright or
# ~/.cache/ms-playwright) has the browsers. Drop the override when it points into temp.
case "${PLAYWRIGHT_BROWSERS_PATH:-}" in
  "") ;;
  "${TMPDIR:-/tmp}"*|/tmp/*|/var/folders/*) unset PLAYWRIGHT_BROWSERS_PATH ;;
esac
