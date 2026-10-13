#!/usr/bin/env bash
# Read-only views of state behind the storefront, for proving side effects.
#
#   side-effects.sh orders [buyer-name]   latest rows in ordering.orders (orderingdb)
#   side-effects.sh queues                RabbitMQ queue depth and consumer counts
#   side-effects.sh basket                every raw Redis basket entry (keyed by identity GUID)
#
# Containers are found by Aspire's DCP label, so unrelated Postgres/RabbitMQ containers
# on the machine are never touched.
set -euo pipefail

DCP_LABEL="com.microsoft.developer.usvc-dev.name"

container() {
  local prefix="$1"
  local name
  name=$(docker ps --filter "label=${DCP_LABEL}" --format '{{.Names}}' | grep "^${prefix}-" | head -1 || true)
  if [[ -z "$name" ]]; then
    echo "no running ${prefix}-* container with label ${DCP_LABEL}; is the AppHost up?" >&2
    exit 2
  fi
  printf '%s' "$name"
}

case "${1:-}" in
  orders)
    buyer="${2:-}"
    where=""
    # buyers."Name" holds the Identity `name` claim ("Bob"), not the login ("bob").
    if [[ -n "$buyer" ]]; then
      where="WHERE lower(b.\"Name\") = lower('${buyer//\'/\'\'}')"
    fi
    # Aspire generates the Postgres password per machine; read it from the container.
    # SQL goes over stdin so no quoting survives two shells.
    docker exec -i "$(container postgres)" sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" psql -U postgres -d orderingdb -At -F "	" -f -' <<SQL
SELECT o."Id", o."OrderStatus", to_char(o."OrderDate", 'YYYY-MM-DD HH24:MI:SS'), b."Name", o."Address_Street"
FROM ordering.orders o JOIN ordering.buyers b ON b."Id" = o."BuyerId"
${where}
ORDER BY o."Id" DESC LIMIT 10;
SQL
    ;;
  queues)
    docker exec "$(container eventbus)" rabbitmqctl list_queues --quiet name messages consumers
    ;;
  basket)
    # Basket.API keys baskets by the user's identity GUID, not the username, so list
    # every key and let the caller correlate; there are only a handful in a dev stack.
    docker exec "$(container redis)" sh -c '
      export REDISCLI_AUTH="$REDIS_PASSWORD"
      keys=$(redis-cli --scan --pattern "*")
      if [ -z "$keys" ]; then echo "no basket keys"; exit 0; fi
      for key in $keys; do printf "%s\n" "$key"; redis-cli GET "$key"; echo; done'
    ;;
  *)
    sed -n '2,8p' "$0" >&2
    exit 64
    ;;
esac
