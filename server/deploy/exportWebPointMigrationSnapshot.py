#!/usr/bin/env python3
"""Export the minimum sensitive web ledger data for Point reconciliation."""

from __future__ import annotations

import argparse
import json
import math
import os
import sqlite3
import sys
from decimal import ROUND_HALF_EVEN, Decimal, InvalidOperation
from pathlib import Path
from urllib.parse import quote


RAW_EXPORT_ACK = "I_UNDERSTAND_THIS_OUTPUT_CONTAINS_RAW_WALLET_IDENTITIES"
POINT_MICROS = Decimal("1000000")
POINT_ATTOS = Decimal("1000000000000000000")
MICRO_POINT_ATTOS = Decimal("1000000000000")


def fail(message: str) -> None:
    raise RuntimeError(message)


def table_names(db: sqlite3.Connection) -> set[str]:
    return {
        str(row[0])
        for row in db.execute("select name from sqlite_master where type='table'")
    }


def require_columns(db: sqlite3.Connection, table: str, required: set[str]) -> None:
    columns = {str(row[1]) for row in db.execute(f'pragma table_info("{table}")')}
    missing = sorted(required - columns)
    if missing:
        fail(f"WEB_SCHEMA_MISSING_{table.upper()}_COLUMNS:{','.join(missing)}")


def required_text(value: object, field: str) -> str:
    text = "" if value is None else str(value).strip()
    if not text or len(text) > 256 or any(char in text for char in ("\r", "\n", "\0")):
        fail(f"INVALID_{field.upper()}")
    return text


def optional_text(value: object, field: str, max_length: int = 256) -> str:
    text = "" if value is None else str(value).strip()
    if len(text) > max_length or any(char in text for char in ("\r", "\n", "\0")):
        fail(f"INVALID_{field.upper()}")
    return text


def decimal_micros(value: object, field: str) -> str:
    if isinstance(value, float) and not math.isfinite(value):
        fail(f"INVALID_{field.upper()}")
    try:
        decimal = Decimal(str(value))
    except (InvalidOperation, ValueError):
        fail(f"INVALID_{field.upper()}")
    if not decimal.is_finite():
        fail(f"INVALID_{field.upper()}")
    micros = decimal * POINT_MICROS
    integral = micros.to_integral_value()
    if micros != integral:
        fail(f"{field.upper()}_HAS_MORE_THAN_SIX_DECIMALS")
    return str(int(integral))


def legacy_decimal_micros(value: object, field: str) -> tuple[str, str]:
    """Quantize legacy SQLite REAL Point to micros while preserving exact residual evidence."""
    if isinstance(value, float) and not math.isfinite(value):
        fail(f"INVALID_{field.upper()}")
    try:
        decimal = Decimal(str(value))
    except (InvalidOperation, ValueError):
        fail(f"INVALID_{field.upper()}")
    if not decimal.is_finite():
        fail(f"INVALID_{field.upper()}")

    point_attos = decimal * POINT_ATTOS
    integral_attos = point_attos.to_integral_value()
    if point_attos != integral_attos:
        fail(f"{field.upper()}_HAS_MORE_THAN_EIGHTEEN_DECIMALS")

    micros = decimal * POINT_MICROS
    rounded_micros = micros.to_integral_value(rounding=ROUND_HALF_EVEN)
    residual_attos = integral_attos - (rounded_micros * MICRO_POINT_ATTOS)
    if abs(residual_attos) > MICRO_POINT_ATTOS / 2:
        fail(f"INVALID_{field.upper()}_ROUNDING_RESIDUAL")
    return str(int(rounded_micros)), str(int(residual_attos))


def non_negative_integer(value: object, field: str) -> str:
    text = str(value).strip()
    if not text.isdigit():
        fail(f"INVALID_{field.upper()}")
    return text


def build_snapshot(db: sqlite3.Connection) -> dict[str, object]:
    if db.execute("pragma foreign_key_check").fetchone() is not None:
        fail("WEB_DATABASE_FOREIGN_KEY_VIOLATION")
    tables = table_names(db)
    required_tables = {"User", "Wallet", "Transaction", "GamePointSyncOutbox"}
    missing_tables = sorted(required_tables - tables)
    if missing_tables:
        fail(f"WEB_SCHEMA_MISSING_TABLES:{','.join(missing_tables)}")

    require_columns(db, "User", {"id"})
    require_columns(db, "Wallet", {"userId", "balanceGXL", "lockedGXL"})
    require_columns(
        db,
        "Transaction",
        {"id", "userId", "type", "amount", "currency", "status", "createdAt"},
    )
    require_columns(
        db,
        "GamePointSyncOutbox",
        {"userId", "sourceTransactionId", "pointAmount", "status", "attempts"},
    )
    if "GamePointLinkedAccount" in tables:
        require_columns(db, "GamePointLinkedAccount", {"userId", "gamePlayerId"})

    users = [
        {"userId": required_text(row[0], "web_user_id")}
        for row in db.execute('select "id" from "User" order by "id"')
    ]
    wallets = []
    for row in db.execute(
        'select "userId", "balanceGXL", "lockedGXL" from "Wallet" order by "userId"'
    ):
        point_micros, point_residual_attos = legacy_decimal_micros(row[1], "wallet_point")
        locked_micros, locked_residual_attos = legacy_decimal_micros(
            row[2], "wallet_locked_point"
        )
        wallets.append({
            "userId": required_text(row[0], "wallet_user_id"),
            "pointMicros": point_micros,
            "pointLegacyResidualAttos": point_residual_attos,
            "lockedPointMicros": locked_micros,
            "lockedPointLegacyResidualAttos": locked_residual_attos,
        })

    transactions = []
    for row in db.execute(
        'select "id", "userId", "type", "amount", "currency", "status" '
        'from "Transaction" '
        "where upper(trim(coalesce(\"currency\", ''))) in ('GXL', 'POINT') "
        'order by "userId", "createdAt", "id"'
    ):
        amount_micros, amount_residual_attos = legacy_decimal_micros(
            row[3], "web_transaction_amount"
        )
        transactions.append({
            "transactionId": required_text(row[0], "web_transaction_id"),
            "userId": required_text(row[1], "web_transaction_user_id"),
            "type": optional_text(row[2], "web_transaction_type", 128) or "UNKNOWN",
            "amountMicros": amount_micros,
            "amountLegacyResidualAttos": amount_residual_attos,
            "currency": optional_text(row[4], "web_transaction_currency", 32) or "UNKNOWN",
            "status": optional_text(row[5], "web_transaction_status", 64) or "UNKNOWN",
        })
    outboxes = [
        {
            "userId": required_text(row[0], "outbox_user_id"),
            "sourceTransactionId": required_text(row[1], "outbox_source_transaction_id"),
            "pointMicros": decimal_micros(row[2], "outbox_point_amount"),
            "status": optional_text(row[3], "outbox_status", 32).upper() or "UNKNOWN",
            "attempts": non_negative_integer(row[4], "outbox_attempts"),
        }
        for row in db.execute(
            'select "userId", "sourceTransactionId", "pointAmount", "status", "attempts" '
            'from "GamePointSyncOutbox" order by "userId", "sourceTransactionId"'
        )
    ]
    links = []
    if "GamePointLinkedAccount" in tables:
        links = [
            {
                "userId": required_text(row[0], "link_user_id"),
                "gamePlayerId": required_text(row[1], "link_game_player_id"),
            }
            for row in db.execute(
                'select "userId", "gamePlayerId" from "GamePointLinkedAccount" order by "userId"'
            )
        ]

    return {
        "schemaVersion": 2,
        "users": users,
        "wallets": wallets,
        "transactions": transactions,
        "outboxes": outboxes,
        "links": links,
    }


def export_snapshot(database_path: str) -> dict[str, object]:
    path = Path(database_path).resolve(strict=True)
    if not path.is_file():
        fail("WEB_DATABASE_IS_NOT_A_FILE")
    uri_path = quote(path.as_posix(), safe="/:")
    db = sqlite3.connect(f"file:{uri_path}?mode=ro", uri=True, timeout=10)
    try:
        db.execute("pragma query_only=on")
        if db.execute("pragma query_only").fetchone()[0] != 1:
            fail("WEB_DATABASE_QUERY_ONLY_NOT_ACTIVE")
        changes_before = db.total_changes
        db.execute("begin")
        snapshot = build_snapshot(db)
        if db.total_changes != changes_before:
            fail("WEB_DATABASE_CHANGED_DURING_EXPORT")
        db.rollback()
        return snapshot
    finally:
        db.close()


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(add_help=True)
    parser.add_argument("--database", required=True)
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    if os.environ.get("POINT_MIGRATION_RAW_EXPORT_ACK") != RAW_EXPORT_ACK:
        fail("RAW_EXPORT_ACK_REQUIRED")
    args = parse_args(argv)
    snapshot = export_snapshot(args.database)
    json.dump(snapshot, sys.stdout, ensure_ascii=True, separators=(",", ":"))
    sys.stdout.write("\n")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main(sys.argv[1:]))
    except Exception as error:  # noqa: BLE001 - CLI must fail closed with one safe code.
        print(f"[web-point-migration-export] {error}", file=sys.stderr)
        raise SystemExit(1)
