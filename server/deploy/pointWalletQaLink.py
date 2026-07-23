#!/usr/bin/env python3
"""Validate, apply, or roll back one dedicated QA Point authority link."""

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


REPORT_DOMAIN = "ywonder-point-qa-link-v1"
APPLY_ACK = "I_APPROVE_QA_POINT_AUTHORITY_LINK"
ROLLBACK_ACK = "I_APPROVE_QA_POINT_AUTHORITY_UNLINK"
LINKED_BY = "codex-point-qa-canary"
ID_PATTERN = re.compile(r"^[A-Za-z0-9._:-]{8,128}$")
OPERATION_PATTERN = re.compile(r"^point-qa-link:[a-f0-9]{32}$")
EXPECTED_TABLES = {
    "User",
    "Wallet",
    "Transaction",
    "GamePointLinkedAccount",
    "GamePointConversion",
    "GamePointDebit",
    "GamePointSyncOutbox",
    "PointExchangeRateVersion",
}
EXPECTED_TRIGGERS = {
    "GamePointLinkedAccount_require_zero_wallet",
    "Wallet_freeze_linked_point_update",
    "Wallet_require_zero_point_for_linked_insert",
}
EXPECTED_INDEXES = {"GamePointLinkedAccount_gamePlayerId_key"}


def fail(code: str) -> None:
    raise RuntimeError(code)


def required_env(name: str, pattern: re.Pattern[str] = ID_PATTERN) -> str:
    value = str(os.environ.get(name, "")).strip()
    if not pattern.fullmatch(value):
        fail(f"INVALID_{name}")
    return value


def parse_timestamp(value: str) -> str:
    text = str(value or "").strip()
    try:
        parsed = datetime.fromisoformat(text.replace("Z", "+00:00"))
    except ValueError:
        fail("INVALID_OCCURRED_AT")
    if parsed.tzinfo is None:
        fail("INVALID_OCCURRED_AT")
    return parsed.astimezone(timezone.utc).isoformat(timespec="milliseconds").replace("+00:00", "Z")


def decimal_value(value: Any, field: str) -> Decimal:
    try:
        parsed = Decimal(str(value))
    except (InvalidOperation, ValueError):
        fail(f"INVALID_{field}")
    if not parsed.is_finite():
        fail(f"INVALID_{field}")
    return parsed


def is_zero(value: Any) -> bool:
    return abs(decimal_value(value, "POINT_BALANCE")) <= Decimal("0.000000001")


def public_ref(reference_key: str, kind: str, raw_value: str) -> str:
    if len(reference_key) < 32:
        fail("POINT_MIGRATION_REPORT_KEY_TOO_SHORT")
    message = f"{REPORT_DOMAIN}\0{kind}\0{raw_value}".encode("utf-8")
    return hmac.new(reference_key.encode("utf-8"), message, hashlib.sha256).hexdigest()[:24]


def scalar(db: sqlite3.Connection, sql: str, args: tuple[Any, ...] = ()) -> Any:
    row = db.execute(sql, args).fetchone()
    return row[0] if row else None


def validate_schema(db: sqlite3.Connection) -> None:
    tables = {
        row[0] for row in db.execute("select name from sqlite_master where type='table'")
    }
    triggers = {
        row[0] for row in db.execute("select name from sqlite_master where type='trigger'")
    }
    indexes = {
        row[0] for row in db.execute("select name from sqlite_master where type='index'")
    }
    if not EXPECTED_TABLES.issubset(tables):
        fail("POINT_QA_LINK_SCHEMA_TABLE_MISSING")
    if not EXPECTED_TRIGGERS.issubset(triggers):
        fail("POINT_QA_LINK_SCHEMA_TRIGGER_MISSING")
    if not EXPECTED_INDEXES.issubset(indexes):
        fail("POINT_QA_LINK_SCHEMA_INDEX_MISSING")


def inspect_state(
    db: sqlite3.Connection,
    web_user_id: str,
    game_player_id: str,
    signed_player_id: str,
    signed_game_point: int,
    expected_game_point: int,
    expected_web_usdt: Decimal,
) -> dict[str, Any]:
    validate_schema(db)
    if signed_player_id != game_player_id:
        fail("POINT_QA_SIGNED_PLAYER_MISMATCH")
    if signed_game_point != expected_game_point or signed_game_point < 0:
        fail("POINT_QA_SIGNED_BALANCE_MISMATCH")

    user = db.execute(
        'select "status","emailVerified" from "User" where "id"=?',
        (web_user_id,),
    ).fetchall()
    if len(user) != 1:
        fail("POINT_QA_WEB_USER_NOT_UNIQUE")
    if str(user[0][0]) != "ACTIVE" or not user[0][1]:
        fail("POINT_QA_WEB_USER_NOT_ACTIVE_VERIFIED")

    wallet_rows = db.execute(
        'select "balanceGXL","lockedGXL","balanceUsdt" from "Wallet" where "userId"=?',
        (web_user_id,),
    ).fetchall()
    if len(wallet_rows) != 1:
        fail("POINT_QA_WALLET_NOT_UNIQUE")
    legacy_point, locked_point, web_usdt = wallet_rows[0]
    if not is_zero(legacy_point) or not is_zero(locked_point):
        fail("POINT_QA_LINK_REQUIRES_ZERO_LEGACY_POINT")
    if decimal_value(web_usdt, "WEB_USDT") != expected_web_usdt:
        fail("POINT_QA_WEB_USDT_BASELINE_MISMATCH")

    activity_counts = {
        "webTransactionCount": int(scalar(
            db, 'select count(*) from "Transaction" where "userId"=?', (web_user_id,)
        )),
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
    if any(activity_counts.values()):
        fail("POINT_QA_LINK_REQUIRES_EMPTY_ACTIVITY")

    active_rates = db.execute(
        'select "id","rateMicros" from "PointExchangeRateVersion" '
        'where "pair"=? and "isActive"=1',
        ("USDT_POINT",),
    ).fetchall()
    if len(active_rates) != 1:
        fail("POINT_QA_ACTIVE_RATE_NOT_UNIQUE")
    rate_id, rate_micros = active_rates[0]
    rate_text = str(rate_micros or "").strip()
    if not re.fullmatch(r"[1-9][0-9]*", rate_text):
        fail("POINT_QA_ACTIVE_RATE_INVALID")

    user_links = db.execute(
        'select "gamePlayerId","linkedBy","note","linkedAt" '
        'from "GamePointLinkedAccount" where "userId"=?',
        (web_user_id,),
    ).fetchall()
    player_links = db.execute(
        'select "userId" from "GamePointLinkedAccount" where "gamePlayerId"=?',
        (game_player_id,),
    ).fetchall()
    if len(user_links) > 1 or len(player_links) > 1:
        fail("POINT_QA_LINK_NOT_UNIQUE")
    if user_links and str(user_links[0][0]) != game_player_id:
        fail("POINT_QA_WEB_USER_ALREADY_LINKED_ELSEWHERE")
    if player_links and str(player_links[0][0]) != web_user_id:
        fail("POINT_QA_GAME_PLAYER_ALREADY_LINKED_ELSEWHERE")

    return {
        "legacyPoint": format(decimal_value(legacy_point, "LEGACY_POINT"), "f"),
        "legacyPointLocked": format(decimal_value(locked_point, "LOCKED_POINT"), "f"),
        "webUsdt": format(decimal_value(web_usdt, "WEB_USDT"), "f"),
        "signedGamePoint": signed_game_point,
        "rateId": str(rate_id),
        "rateMicros": rate_text,
        "activity": activity_counts,
        "userLink": tuple(user_links[0]) if user_links else None,
        "playerLinkCount": len(player_links),
    }


def assert_exact_link(state: dict[str, Any], game_player_id: str, operation_id: str,
                      occurred_at: str) -> None:
    row = state["userLink"]
    if not row:
        fail("POINT_QA_LINK_NOT_FOUND")
    if tuple(map(str, row)) != (game_player_id, LINKED_BY, operation_id, occurred_at):
        fail("POINT_QA_LINK_OWNERSHIP_MISMATCH")


def evidence(
    action: str,
    mutation: bool,
    duplicate: bool,
    state: dict[str, Any],
    web_user_id: str,
    game_player_id: str,
    operation_id: str,
    occurred_at: str,
    reference_key: str,
) -> dict[str, Any]:
    return {
        "schemaVersion": 1,
        "mode": "POINT_WALLET_QA_AUTHORITY_LINK",
        "action": action,
        "operationId": operation_id,
        "occurredAt": occurred_at,
        "databaseMutationPerformed": mutation,
        "duplicate": duplicate,
        "containsRawIdentities": False,
        "accountRef": public_ref(reference_key, "web-user", web_user_id),
        "gamePlayerRef": public_ref(reference_key, "game-player", game_player_id),
        "activeRateRef": public_ref(reference_key, "point-rate", state["rateId"]),
        "baseline": {
            "legacyPoint": state["legacyPoint"],
            "legacyPointLocked": state["legacyPointLocked"],
            "webUsdt": state["webUsdt"],
            "signedGamePoint": state["signedGamePoint"],
            "rateMicros": state["rateMicros"],
        },
        "activity": state["activity"],
        "linked": state["userLink"] is not None,
        "preconditionsPassed": True,
    }


def write_output(path_text: str | None, value: dict[str, Any]) -> None:
    serialized = json.dumps(value, sort_keys=True, indent=2) + "\n"
    if not path_text:
        sys.stdout.write(serialized)
        return
    path = Path(path_text)
    if path.exists() or path.is_symlink():
        fail("POINT_QA_LINK_OUTPUT_EXISTS")
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("x", encoding="utf-8", newline="\n") as handle:
        handle.write(serialized)
    try:
        path.chmod(0o600)
    except OSError:
        pass


def connect(database: Path, writable: bool) -> sqlite3.Connection:
    if not database.is_file() or database.is_symlink():
        fail("POINT_QA_LINK_DATABASE_INVALID")
    if writable:
        db = sqlite3.connect(str(database), timeout=15, isolation_level=None)
    else:
        db = sqlite3.connect(database.resolve().as_uri() + "?mode=ro", uri=True, timeout=15)
    db.execute("pragma foreign_keys=on")
    db.execute("pragma busy_timeout=15000")
    if not writable:
        db.execute("pragma query_only=on")
    db.row_factory = sqlite3.Row
    return db


def execute(args: argparse.Namespace) -> dict[str, Any]:
    web_user_id = required_env("POINT_QA_WEB_USER_ID")
    game_player_id = required_env("POINT_QA_GAME_PLAYER_ID")
    signed_player_id = required_env("POINT_QA_SIGNED_PLAYER_ID")
    reference_key = str(os.environ.get("POINT_MIGRATION_REPORT_KEY", ""))
    operation_id = str(args.operation_id).strip().lower()
    if not OPERATION_PATTERN.fullmatch(operation_id):
        fail("INVALID_POINT_QA_LINK_OPERATION_ID")
    occurred_at = parse_timestamp(args.occurred_at)
    expected_web_usdt = decimal_value(args.expected_web_usdt, "EXPECTED_WEB_USDT")
    database = Path(args.database).resolve()
    writable = args.action in {"apply", "rollback"}

    if args.action == "apply" and os.environ.get("POINT_QA_LINK_APPLY_ACK") != APPLY_ACK:
        fail("POINT_QA_LINK_APPLY_NOT_APPROVED")
    if args.action == "rollback" and os.environ.get("POINT_QA_LINK_ROLLBACK_ACK") != ROLLBACK_ACK:
        fail("POINT_QA_LINK_ROLLBACK_NOT_APPROVED")

    db = connect(database, writable)
    mutation = False
    duplicate = False
    try:
        if writable:
            db.execute("begin immediate")
        before = inspect_state(
            db,
            web_user_id,
            game_player_id,
            signed_player_id,
            args.signed_game_point,
            args.expected_game_point,
            expected_web_usdt,
        )

        if args.action == "validate":
            if before["userLink"] is not None:
                fail("POINT_QA_LINK_ALREADY_EXISTS")
            final = before
        elif args.action == "apply":
            if before["userLink"] is None:
                db.execute(
                    'insert into "GamePointLinkedAccount" '
                    '("userId","gamePlayerId","linkedBy","note","linkedAt") values (?,?,?,?,?)',
                    (web_user_id, game_player_id, LINKED_BY, operation_id, occurred_at),
                )
                mutation = True
            else:
                assert_exact_link(before, game_player_id, operation_id, occurred_at)
                duplicate = True
            final = inspect_state(
                db,
                web_user_id,
                game_player_id,
                signed_player_id,
                args.signed_game_point,
                args.expected_game_point,
                expected_web_usdt,
            )
            assert_exact_link(final, game_player_id, operation_id, occurred_at)
        else:
            assert_exact_link(before, game_player_id, operation_id, occurred_at)
            deleted = db.execute(
                'delete from "GamePointLinkedAccount" '
                'where "userId"=? and "gamePlayerId"=? and "linkedBy"=? and "note"=? and "linkedAt"=?',
                (web_user_id, game_player_id, LINKED_BY, operation_id, occurred_at),
            ).rowcount
            if deleted != 1:
                fail("POINT_QA_LINK_ROLLBACK_DELETE_MISMATCH")
            mutation = True
            final = inspect_state(
                db,
                web_user_id,
                game_player_id,
                signed_player_id,
                args.signed_game_point,
                args.expected_game_point,
                expected_web_usdt,
            )
            if final["userLink"] is not None:
                fail("POINT_QA_LINK_ROLLBACK_VERIFY_FAILED")

        if writable:
            db.commit()
        return evidence(
            args.action,
            mutation,
            duplicate,
            final,
            web_user_id,
            game_player_id,
            operation_id,
            occurred_at,
            reference_key,
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
    parser.add_argument("--expected-web-usdt", default="0")
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
        print("POINT_QA_LINK_SQLITE_ERROR", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
