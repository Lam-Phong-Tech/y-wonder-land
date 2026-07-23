#!/usr/bin/env python3
"""Validate, apply, or roll back one audited QA synthetic USDT funding entry."""

from __future__ import annotations

import argparse
import hashlib
import hmac
import json
import os
import re
import sqlite3
import sys
from datetime import datetime, timezone
from decimal import Decimal, InvalidOperation
from pathlib import Path
from typing import Any


REPORT_DOMAIN = "ywonder-point-qa-synthetic-funding-v1"
APPLY_ACK = "I_APPROVE_QA_SYNTHETIC_USDT_FUNDING"
ROLLBACK_ACK = "I_APPROVE_QA_SYNTHETIC_USDT_FUNDING_ROLLBACK"
FUNDING_TYPE = "QA_SYNTHETIC_USDT_FUNDING"
FUNDING_CURRENCY = "USDT"
FUNDING_STATUS = "SUCCESS"
FUNDING_KIND = "POINT_NO_MONEY_CANARY_USDT"
POINT_MICROS = 1_000_000
MAX_USDT_MICROS = 10_000_000 * POINT_MICROS
ID_PATTERN = re.compile(r"^[A-Za-z0-9._:-]{8,128}$")
OPERATION_PATTERN = re.compile(r"^point-qa-funding:[a-f0-9]{32}$")
INTEGER_PATTERN = re.compile(r"^[0-9]+$")
EXPECTED_TRIGGERS = {
    "GamePointLinkedAccount_require_zero_wallet",
    "Wallet_freeze_linked_point_update",
    "Wallet_require_zero_point_for_linked_insert",
}
EXPECTED_COLUMNS = {
    "User": {"id", "status", "emailVerified"},
    "Wallet": {
        "id", "userId", "balanceGXL", "lockedGXL", "balanceUsdt",
        "totalDepositedUsdt", "totalWithdrawnUsdt", "updatedAt",
    },
    "Transaction": {
        "id", "userId", "type", "amount", "currency", "status",
        "externalRef", "metadata", "createdAt", "updatedAt",
    },
    "GamePointLinkedAccount": {"userId", "gamePlayerId"},
    "PointExchangeRateVersion": {"id", "pair", "rateMicros", "isActive"},
    "GamePointConversion": {"id", "userId"},
    "GamePointDebit": {"id", "userId"},
    "GamePointSyncOutbox": {"id", "userId"},
}
METADATA_KEYS = {
    "schemaVersion", "synthetic", "kind", "operationId", "amountMicros",
    "rateVersionId", "rateMicros", "expectedPointMicros",
    "roundingRemainder", "occurredAt", "walletUpdatedAtBefore",
}


def fail(code: str) -> None:
    raise RuntimeError(code)


def required_env(name: str, pattern: re.Pattern[str] = ID_PATTERN) -> str:
    value = str(os.environ.get(name, "")).strip()
    if not pattern.fullmatch(value):
        fail(f"INVALID_{name}")
    return value


def integer_text(value: Any, field: str, positive: bool = False) -> int:
    text = str(value or "").strip()
    if not INTEGER_PATTERN.fullmatch(text):
        fail(f"INVALID_{field}")
    parsed = int(text)
    if positive and parsed < 1:
        fail(f"INVALID_{field}")
    return parsed


def decimal_value(value: Any, field: str) -> Decimal:
    try:
        parsed = Decimal(str(value))
    except (InvalidOperation, ValueError):
        fail(f"INVALID_{field}")
    if not parsed.is_finite():
        fail(f"INVALID_{field}")
    return parsed


def exact_decimal(actual: Any, expected: Decimal, code: str) -> None:
    if decimal_value(actual, code) != expected:
        fail(code)


def parse_timestamp(value: str) -> tuple[str, int]:
    text = str(value or "").strip()
    try:
        parsed = datetime.fromisoformat(text.replace("Z", "+00:00"))
    except ValueError:
        fail("INVALID_OCCURRED_AT")
    if parsed.tzinfo is None:
        fail("INVALID_OCCURRED_AT")
    utc = parsed.astimezone(timezone.utc)
    canonical = utc.isoformat(timespec="milliseconds").replace("+00:00", "Z")
    millis = int(utc.timestamp() * 1000)
    if millis < 1:
        fail("INVALID_OCCURRED_AT")
    return canonical, millis


def public_ref(reference_key: str, kind: str, raw_value: str) -> str:
    if len(reference_key) < 32:
        fail("POINT_MIGRATION_REPORT_KEY_TOO_SHORT")
    message = f"{REPORT_DOMAIN}\0{kind}\0{raw_value}".encode("utf-8")
    return hmac.new(reference_key.encode("utf-8"), message, hashlib.sha256).hexdigest()[:24]


def funding_transaction_id(operation_id: str) -> str:
    return "qaf_" + operation_id.rsplit(":", 1)[1]


def amount_from_micros(value: int) -> Decimal:
    return Decimal(value) / Decimal(POINT_MICROS)


def scalar(db: sqlite3.Connection, sql: str, args: tuple[Any, ...] = ()) -> Any:
    row = db.execute(sql, args).fetchone()
    return row[0] if row else None


def validate_schema(db: sqlite3.Connection) -> None:
    tables = {row[0] for row in db.execute("select name from sqlite_master where type='table'")}
    triggers = {row[0] for row in db.execute("select name from sqlite_master where type='trigger'")}
    for table, required in EXPECTED_COLUMNS.items():
        if table not in tables:
            fail("POINT_QA_FUNDING_SCHEMA_TABLE_MISSING")
        columns = {row[1] for row in db.execute(f'pragma table_info("{table}")')}
        if not required.issubset(columns):
            fail("POINT_QA_FUNDING_SCHEMA_COLUMN_MISSING")
    if not EXPECTED_TRIGGERS.issubset(triggers):
        fail("POINT_QA_FUNDING_SCHEMA_TRIGGER_MISSING")


def parse_metadata(raw: Any) -> dict[str, Any]:
    try:
        value = json.loads(str(raw or ""))
    except (TypeError, ValueError):
        fail("POINT_QA_FUNDING_IDEMPOTENCY_CONFLICT")
    if not isinstance(value, dict) or set(value) != METADATA_KEYS:
        fail("POINT_QA_FUNDING_IDEMPOTENCY_CONFLICT")
    return value


def validate_funding_row(
    row: sqlite3.Row,
    web_user_id: str,
    operation_id: str,
    occurred_at: str,
    occurred_at_ms: int,
    amount_micros: int,
    rate_id: str,
    rate_micros: int,
    expected_point_micros: int,
) -> dict[str, Any]:
    expected_amount = amount_from_micros(amount_micros)
    if (
        str(row["id"]) != funding_transaction_id(operation_id)
        or str(row["userId"]) != web_user_id
        or str(row["type"]) != FUNDING_TYPE
        or str(row["currency"]) != FUNDING_CURRENCY
        or str(row["status"]) != FUNDING_STATUS
        or str(row["externalRef"]) != operation_id
        or int(row["createdAt"]) != occurred_at_ms
        or int(row["updatedAt"]) != occurred_at_ms
    ):
        fail("POINT_QA_FUNDING_IDEMPOTENCY_CONFLICT")
    exact_decimal(row["amount"], expected_amount, "POINT_QA_FUNDING_IDEMPOTENCY_CONFLICT")
    metadata = parse_metadata(row["metadata"])
    expected = {
        "schemaVersion": 1,
        "synthetic": True,
        "kind": FUNDING_KIND,
        "operationId": operation_id,
        "amountMicros": str(amount_micros),
        "rateVersionId": rate_id,
        "rateMicros": str(rate_micros),
        "expectedPointMicros": str(expected_point_micros),
        "roundingRemainder": "0",
        "occurredAt": occurred_at,
    }
    for key, value in expected.items():
        if metadata.get(key) != value:
            fail("POINT_QA_FUNDING_IDEMPOTENCY_CONFLICT")
    if not isinstance(metadata.get("walletUpdatedAtBefore"), int) \
            or metadata["walletUpdatedAtBefore"] < 1:
        fail("POINT_QA_FUNDING_IDEMPOTENCY_CONFLICT")
    return metadata


def inspect_state(
    db: sqlite3.Connection,
    web_user_id: str,
    game_player_id: str,
    signed_player_id: str,
    signed_game_point: int,
    expected_game_point: int,
    operation_id: str,
    occurred_at: str,
    occurred_at_ms: int,
    baseline_web_usdt: Decimal,
    expected_real_deposited: Decimal,
    expected_total_deposited: Decimal,
    expected_total_withdrawn: Decimal,
    amount_micros: int,
    expected_rate_micros: int,
    expected_point_micros: int,
) -> dict[str, Any]:
    validate_schema(db)
    if signed_player_id != game_player_id:
        fail("POINT_QA_FUNDING_SIGNED_PLAYER_MISMATCH")
    if signed_game_point != expected_game_point or signed_game_point < 0:
        fail("POINT_QA_FUNDING_SIGNED_BALANCE_MISMATCH")

    users = db.execute(
        'select "status","emailVerified" from "User" where "id"=?', (web_user_id,)
    ).fetchall()
    if len(users) != 1:
        fail("POINT_QA_FUNDING_WEB_USER_NOT_UNIQUE")
    if str(users[0][0]) != "ACTIVE" or not users[0][1]:
        fail("POINT_QA_FUNDING_WEB_USER_NOT_ACTIVE_VERIFIED")

    links = db.execute(
        'select "gamePlayerId" from "GamePointLinkedAccount" where "userId"=?',
        (web_user_id,),
    ).fetchall()
    reverse_links = db.execute(
        'select "userId" from "GamePointLinkedAccount" where "gamePlayerId"=?',
        (game_player_id,),
    ).fetchall()
    if len(links) != 1 or len(reverse_links) != 1 \
            or str(links[0][0]) != game_player_id or str(reverse_links[0][0]) != web_user_id:
        fail("POINT_QA_FUNDING_LINK_MISMATCH")

    wallet = db.execute(
        'select "balanceGXL","lockedGXL","balanceUsdt","totalDepositedUsdt",'
        '"totalWithdrawnUsdt","updatedAt" from "Wallet" where "userId"=?',
        (web_user_id,),
    ).fetchall()
    if len(wallet) != 1:
        fail("POINT_QA_FUNDING_WALLET_NOT_UNIQUE")
    legacy_point, locked_point, web_usdt, total_deposited, total_withdrawn, wallet_updated_at = wallet[0]
    exact_decimal(legacy_point, Decimal(0), "POINT_QA_FUNDING_LEGACY_POINT_NOT_ZERO")
    exact_decimal(locked_point, Decimal(0), "POINT_QA_FUNDING_LOCKED_POINT_NOT_ZERO")
    exact_decimal(total_deposited, expected_total_deposited,
                  "POINT_QA_FUNDING_TOTAL_DEPOSITED_CHANGED")
    exact_decimal(total_withdrawn, expected_total_withdrawn,
                  "POINT_QA_FUNDING_TOTAL_WITHDRAWN_CHANGED")
    if not isinstance(wallet_updated_at, int) or wallet_updated_at < 1:
        fail("POINT_QA_FUNDING_WALLET_TIMESTAMP_INVALID")

    active_rates = db.execute(
        'select "id","rateMicros" from "PointExchangeRateVersion" '
        'where "pair"=? and "isActive"=1', ("USDT_POINT",)
    ).fetchall()
    if len(active_rates) != 1:
        fail("POINT_QA_FUNDING_ACTIVE_RATE_NOT_UNIQUE")
    rate_id = str(active_rates[0][0])
    rate_micros = integer_text(active_rates[0][1], "POINT_RATE_MICROS", positive=True)
    if rate_micros != expected_rate_micros:
        fail("POINT_QA_FUNDING_RATE_CHANGED")
    raw_quote = amount_micros * rate_micros
    point_micros, remainder = divmod(raw_quote, POINT_MICROS)
    if point_micros != expected_point_micros:
        fail("POINT_QA_FUNDING_QUOTE_CHANGED")
    if remainder != 0 or point_micros % POINT_MICROS != 0:
        fail("POINT_QA_FUNDING_REQUIRES_EXACT_WHOLE_POINT_QUOTE")

    transaction_id = funding_transaction_id(operation_id)
    own_rows = db.execute(
        'select * from "Transaction" where "id"=?', (transaction_id,)
    ).fetchall()
    operation_rows = db.execute(
        'select "id" from "Transaction" where "externalRef"=?', (operation_id,)
    ).fetchall()
    if len(own_rows) > 1 or len(operation_rows) > 1:
        fail("POINT_QA_FUNDING_IDEMPOTENCY_CONFLICT")
    if operation_rows and str(operation_rows[0][0]) != transaction_id:
        fail("POINT_QA_FUNDING_IDEMPOTENCY_CONFLICT")

    other_transactions = int(scalar(
        db,
        'select count(*) from "Transaction" where "userId"=? and "id"<>?',
        (web_user_id, transaction_id),
    ))
    activity = {
        "otherTransactionCount": other_transactions,
        "conversionCount": int(scalar(
            db, 'select count(*) from "GamePointConversion" where "userId"=?', (web_user_id,)
        )),
        "debitCount": int(scalar(
            db, 'select count(*) from "GamePointDebit" where "userId"=?', (web_user_id,)
        )),
        "outboxCount": int(scalar(
            db, 'select count(*) from "GamePointSyncOutbox" where "userId"=?', (web_user_id,)
        )),
    }
    if any(activity.values()):
        fail("POINT_QA_FUNDING_REQUIRES_EMPTY_OTHER_ACTIVITY")

    real_deposited = decimal_value(scalar(
        db,
        'select coalesce(sum("amount"),0) from "Transaction" '
        'where "userId"=? and "type"=? and "status"=?',
        (web_user_id, "USDT_DEPOSIT", "SUCCESS"),
    ), "REAL_DEPOSITED_USDT")
    if real_deposited != expected_real_deposited:
        fail("POINT_QA_FUNDING_REAL_DEPOSIT_PRINCIPAL_CHANGED")

    metadata = None
    funded = bool(own_rows)
    expected_balance = baseline_web_usdt
    if funded:
        metadata = validate_funding_row(
            own_rows[0], web_user_id, operation_id, occurred_at, occurred_at_ms,
            amount_micros, rate_id, rate_micros, expected_point_micros,
        )
        expected_balance += amount_from_micros(amount_micros)
        if wallet_updated_at != occurred_at_ms:
            fail("POINT_QA_FUNDING_WALLET_TIMESTAMP_MISMATCH")
    exact_decimal(web_usdt, expected_balance, "POINT_QA_FUNDING_WEB_USDT_MISMATCH")

    return {
        "funded": funded,
        "metadata": metadata,
        "walletUpdatedAt": wallet_updated_at,
        "webUsdt": format(decimal_value(web_usdt, "WEB_USDT"), "f"),
        "realDepositedUsdt": format(real_deposited, "f"),
        "totalDepositedUsdt": format(expected_total_deposited, "f"),
        "totalWithdrawnUsdt": format(expected_total_withdrawn, "f"),
        "rateId": rate_id,
        "rateMicros": str(rate_micros),
        "expectedPointMicros": str(point_micros),
        "expectedWholePoint": point_micros // POINT_MICROS,
        "roundingRemainder": str(remainder),
        "activity": activity,
    }


def connect(database: Path, writable: bool) -> sqlite3.Connection:
    if not database.is_file() or database.is_symlink():
        fail("POINT_QA_FUNDING_DATABASE_INVALID")
    if writable:
        db = sqlite3.connect(str(database), timeout=15, isolation_level=None)
    else:
        db = sqlite3.connect(database.as_uri() + "?mode=ro", uri=True, timeout=15)
    db.execute("pragma foreign_keys=on")
    db.execute("pragma busy_timeout=15000")
    if not writable:
        db.execute("pragma query_only=on")
    db.row_factory = sqlite3.Row
    return db


def evidence(
    args: argparse.Namespace,
    state: dict[str, Any],
    mutation: bool,
    duplicate: bool,
    web_user_id: str,
    game_player_id: str,
    operation_id: str,
    occurred_at: str,
    reference_key: str,
) -> dict[str, Any]:
    transaction_id = funding_transaction_id(operation_id)
    return {
        "schemaVersion": 1,
        "mode": "POINT_WALLET_QA_SYNTHETIC_USDT_FUNDING",
        "action": args.action,
        "operationId": operation_id,
        "occurredAt": occurred_at,
        "databaseMutationPerformed": mutation,
        "duplicate": duplicate,
        "containsRawIdentities": False,
        "accountRef": public_ref(reference_key, "web-user", web_user_id),
        "gamePlayerRef": public_ref(reference_key, "game-player", game_player_id),
        "fundingTransactionRef": public_ref(reference_key, "funding-transaction", transaction_id),
        "activeRateRef": public_ref(reference_key, "point-rate", state["rateId"]),
        "funded": state["funded"],
        "funding": {
            "type": FUNDING_TYPE,
            "usdtMicros": str(args.funding_usdt_micros),
            "expectedPointMicros": state["expectedPointMicros"],
            "expectedWholePoint": state["expectedWholePoint"],
            "rateMicros": state["rateMicros"],
            "roundingRemainder": state["roundingRemainder"],
        },
        "balances": {
            "webUsdt": state["webUsdt"],
            "realDepositedUsdt": state["realDepositedUsdt"],
            "totalDepositedUsdt": state["totalDepositedUsdt"],
            "totalWithdrawnUsdt": state["totalWithdrawnUsdt"],
        },
        "withdrawalPrincipalUnchanged": True,
        "activity": state["activity"],
        "preconditionsPassed": True,
    }


def write_output(path_text: str | None, value: dict[str, Any]) -> None:
    serialized = json.dumps(value, sort_keys=True, indent=2) + "\n"
    if not path_text:
        sys.stdout.write(serialized)
        return
    path = Path(path_text)
    if path.exists() or path.is_symlink():
        fail("POINT_QA_FUNDING_OUTPUT_EXISTS")
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("x", encoding="utf-8", newline="\n") as handle:
        handle.write(serialized)
    try:
        path.chmod(0o600)
    except OSError:
        pass


def execute(args: argparse.Namespace) -> dict[str, Any]:
    web_user_id = required_env("POINT_QA_WEB_USER_ID")
    game_player_id = required_env("POINT_QA_GAME_PLAYER_ID")
    signed_player_id = required_env("POINT_QA_SIGNED_PLAYER_ID")
    reference_key = str(os.environ.get("POINT_MIGRATION_REPORT_KEY", ""))
    operation_id = str(args.operation_id).strip().lower()
    if not OPERATION_PATTERN.fullmatch(operation_id):
        fail("INVALID_POINT_QA_FUNDING_OPERATION_ID")
    occurred_at, occurred_at_ms = parse_timestamp(args.occurred_at)
    amount_micros = integer_text(args.funding_usdt_micros, "FUNDING_USDT_MICROS", positive=True)
    expected_rate_micros = integer_text(args.expected_rate_micros, "EXPECTED_RATE_MICROS", positive=True)
    expected_point_micros = integer_text(args.expected_point_micros, "EXPECTED_POINT_MICROS", positive=True)
    if amount_micros > MAX_USDT_MICROS:
        fail("POINT_QA_FUNDING_AMOUNT_TOO_LARGE")
    if expected_point_micros % POINT_MICROS != 0:
        fail("POINT_QA_FUNDING_EXPECTED_POINT_NOT_WHOLE")

    baseline_web_usdt = decimal_value(args.expected_web_usdt, "EXPECTED_WEB_USDT")
    expected_real_deposited = decimal_value(
        args.expected_real_deposited_usdt, "EXPECTED_REAL_DEPOSITED_USDT"
    )
    expected_total_deposited = decimal_value(
        args.expected_total_deposited_usdt, "EXPECTED_TOTAL_DEPOSITED_USDT"
    )
    expected_total_withdrawn = decimal_value(
        args.expected_total_withdrawn_usdt, "EXPECTED_TOTAL_WITHDRAWN_USDT"
    )
    if min(baseline_web_usdt, expected_real_deposited,
           expected_total_deposited, expected_total_withdrawn) < 0:
        fail("POINT_QA_FUNDING_NEGATIVE_BASELINE")

    if args.action == "apply" and os.environ.get("POINT_QA_FUNDING_APPLY_ACK") != APPLY_ACK:
        fail("POINT_QA_FUNDING_APPLY_NOT_APPROVED")
    if args.action == "rollback" \
            and os.environ.get("POINT_QA_FUNDING_ROLLBACK_ACK") != ROLLBACK_ACK:
        fail("POINT_QA_FUNDING_ROLLBACK_NOT_APPROVED")

    database = Path(args.database).resolve()
    writable = args.action in {"apply", "rollback"}
    db = connect(database, writable)
    mutation = False
    duplicate = False
    inspect_args = (
        db, web_user_id, game_player_id, signed_player_id,
        args.signed_game_point, args.expected_game_point,
        operation_id, occurred_at, occurred_at_ms,
        baseline_web_usdt, expected_real_deposited,
        expected_total_deposited, expected_total_withdrawn,
        amount_micros, expected_rate_micros, expected_point_micros,
    )
    try:
        if writable:
            db.execute("begin immediate")
        before = inspect_state(*inspect_args)
        transaction_id = funding_transaction_id(operation_id)
        funding_amount = amount_from_micros(amount_micros)

        if args.action == "validate":
            if before["funded"]:
                fail("POINT_QA_FUNDING_ALREADY_EXISTS")
            final = before
        elif args.action == "apply":
            if before["funded"]:
                duplicate = True
            else:
                metadata = json.dumps({
                    "schemaVersion": 1,
                    "synthetic": True,
                    "kind": FUNDING_KIND,
                    "operationId": operation_id,
                    "amountMicros": str(amount_micros),
                    "rateVersionId": before["rateId"],
                    "rateMicros": before["rateMicros"],
                    "expectedPointMicros": before["expectedPointMicros"],
                    "roundingRemainder": "0",
                    "occurredAt": occurred_at,
                    "walletUpdatedAtBefore": before["walletUpdatedAt"],
                }, sort_keys=True, separators=(",", ":"))
                updated = db.execute(
                    'update "Wallet" set "balanceUsdt"=?,"updatedAt"=? '
                    'where "userId"=? and abs("balanceUsdt"-?)<=0.000000001',
                    (float(baseline_web_usdt + funding_amount), occurred_at_ms,
                     web_user_id, float(baseline_web_usdt)),
                ).rowcount
                if updated != 1:
                    fail("POINT_QA_FUNDING_WALLET_UPDATE_MISMATCH")
                db.execute(
                    'insert into "Transaction" '
                    '("id","userId","type","amount","currency","status","externalRef",'
                    '"metadata","createdAt","updatedAt") values (?,?,?,?,?,?,?,?,?,?)',
                    (transaction_id, web_user_id, FUNDING_TYPE, float(funding_amount),
                     FUNDING_CURRENCY, FUNDING_STATUS, operation_id, metadata,
                     occurred_at_ms, occurred_at_ms),
                )
                mutation = True
            final = inspect_state(*inspect_args)
            if not final["funded"]:
                fail("POINT_QA_FUNDING_APPLY_VERIFY_FAILED")
        else:
            if not before["funded"] or not before["metadata"]:
                fail("POINT_QA_FUNDING_NOT_FOUND")
            restored_timestamp = before["metadata"]["walletUpdatedAtBefore"]
            deleted = db.execute(
                'delete from "Transaction" where "id"=? and "userId"=? '
                'and "type"=? and "externalRef"=?',
                (transaction_id, web_user_id, FUNDING_TYPE, operation_id),
            ).rowcount
            if deleted != 1:
                fail("POINT_QA_FUNDING_ROLLBACK_DELETE_MISMATCH")
            updated = db.execute(
                'update "Wallet" set "balanceUsdt"=?,"updatedAt"=? '
                'where "userId"=? and abs("balanceUsdt"-?)<=0.000000001',
                (float(baseline_web_usdt), restored_timestamp, web_user_id,
                 float(baseline_web_usdt + funding_amount)),
            ).rowcount
            if updated != 1:
                fail("POINT_QA_FUNDING_ROLLBACK_WALLET_MISMATCH")
            mutation = True
            final = inspect_state(*inspect_args)
            if final["funded"]:
                fail("POINT_QA_FUNDING_ROLLBACK_VERIFY_FAILED")

        if writable:
            db.commit()
        return evidence(
            args, final, mutation, duplicate, web_user_id, game_player_id,
            operation_id, occurred_at, reference_key,
        )
    except Exception:
        if writable:
            db.rollback()
        raise
    finally:
        db.close()


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--database", required=True)
    parser.add_argument("--action", required=True, choices=("validate", "apply", "rollback"))
    parser.add_argument("--operation-id", required=True)
    parser.add_argument("--occurred-at", required=True)
    parser.add_argument("--signed-game-point", required=True, type=int)
    parser.add_argument("--expected-game-point", required=True, type=int)
    parser.add_argument("--funding-usdt-micros", required=True)
    parser.add_argument("--expected-rate-micros", required=True)
    parser.add_argument("--expected-point-micros", required=True)
    parser.add_argument("--expected-web-usdt", default="0")
    parser.add_argument("--expected-real-deposited-usdt", default="0")
    parser.add_argument("--expected-total-deposited-usdt", default="0")
    parser.add_argument("--expected-total-withdrawn-usdt", default="0")
    parser.add_argument("--output")
    return parser.parse_args()


def main() -> int:
    try:
        args = parse_args()
        result = execute(args)
        write_output(args.output, result)
        return 0
    except RuntimeError as error:
        print(str(error), file=sys.stderr)
        return 1
    except sqlite3.Error:
        print("POINT_QA_FUNDING_SQLITE_ERROR", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
