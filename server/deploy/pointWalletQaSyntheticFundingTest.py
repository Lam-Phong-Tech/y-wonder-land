#!/usr/bin/env python3
"""Regression tests for the audited QA synthetic USDT funding executor."""

from __future__ import annotations

import json
import os
import sqlite3
import subprocess
import sys
import tempfile
from datetime import datetime, timezone
from pathlib import Path


DEPLOY_ROOT = Path(__file__).resolve().parent
EXECUTOR = DEPLOY_ROOT / "pointWalletQaSyntheticFunding.py"
WEB_USER_ID = "web-user-wallet-qa"
GAME_PLAYER_ID = "game-player-wallet-qa"
OTHER_WEB_USER_ID = "web-user-other-qa"
OPERATION_ID = "point-qa-funding:" + "b" * 32
TRANSACTION_ID = "qaf_" + "b" * 32
OCCURRED_AT = "2026-07-17T09:00:00.000Z"
OCCURRED_AT_MS = int(
    datetime(2026, 7, 17, 9, 0, tzinfo=timezone.utc).timestamp() * 1000
)
WALLET_UPDATED_AT = 1_768_636_800_000
REPORT_KEY = "point-qa-funding-test-reference-key-at-least-32-chars"


def create_database(path: Path) -> None:
    db = sqlite3.connect(path)
    try:
        db.executescript('''
            pragma foreign_keys=on;
            create table "User" (
                "id" text primary key,
                "status" text not null,
                "emailVerified" datetime
            );
            create table "Wallet" (
                "id" text primary key,
                "userId" text not null unique references "User"("id"),
                "balanceGXL" real not null default 0,
                "lockedGXL" real not null default 0,
                "balanceUsdt" real not null default 0,
                "totalDepositedUsdt" real not null default 0,
                "totalWithdrawnUsdt" real not null default 0,
                "updatedAt" datetime not null
            );
            create table "Transaction" (
                "id" text primary key,
                "userId" text not null references "User"("id"),
                "type" text not null,
                "amount" real not null,
                "currency" text not null,
                "status" text not null,
                "externalRef" text,
                "metadata" text,
                "createdAt" datetime not null,
                "updatedAt" datetime not null
            );
            create table "GamePointLinkedAccount" (
                "userId" text primary key references "User"("id"),
                "gamePlayerId" text not null unique
            );
            create table "PointExchangeRateVersion" (
                "id" text primary key,
                "pair" text not null,
                "rateMicros" text not null,
                "isActive" integer not null
            );
            create table "GamePointConversion" ("id" text primary key, "userId" text not null);
            create table "GamePointDebit" ("id" text primary key, "userId" text not null);
            create table "GamePointSyncOutbox" ("id" text primary key, "userId" text not null);
            create trigger "GamePointLinkedAccount_require_zero_wallet"
            before insert on "GamePointLinkedAccount"
            when not exists (
                select 1 from "Wallet" where "userId"=new."userId"
                    and abs(coalesce("balanceGXL",0)) <= 0.000000001
                    and abs(coalesce("lockedGXL",0)) <= 0.000000001
            ) begin select raise(abort, 'GAME_POINT_LINK_REQUIRES_ZERO_LEGACY_BALANCE'); end;
            create trigger "Wallet_freeze_linked_point_update"
            before update of "balanceGXL","lockedGXL" on "Wallet"
            when exists (select 1 from "GamePointLinkedAccount" where "userId"=old."userId")
            begin select raise(abort, 'GAME_POINT_LEDGER_IS_AUTHORITATIVE'); end;
            create trigger "Wallet_require_zero_point_for_linked_insert"
            before insert on "Wallet"
            when exists (select 1 from "GamePointLinkedAccount" where "userId"=new."userId")
            begin select raise(abort, 'GAME_POINT_LEDGER_IS_AUTHORITATIVE'); end;
        ''')
        db.executemany(
            'insert into "User" ("id","status","emailVerified") values (?,?,?)',
            [
                (WEB_USER_ID, "ACTIVE", 1_768_636_800_000),
                (OTHER_WEB_USER_ID, "ACTIVE", 1_768_636_800_000),
            ],
        )
        db.executemany(
            'insert into "Wallet" '
            '("id","userId","balanceGXL","lockedGXL","balanceUsdt",'
            '"totalDepositedUsdt","totalWithdrawnUsdt","updatedAt") '
            'values (?,?,?,?,?,?,?,?)',
            [
                ("wallet-qa", WEB_USER_ID, 0, 0, 0, 0, 0, WALLET_UPDATED_AT),
                ("wallet-other", OTHER_WEB_USER_ID, 0, 0, 0, 0, 0, WALLET_UPDATED_AT),
            ],
        )
        db.execute(
            'insert into "GamePointLinkedAccount" ("userId","gamePlayerId") values (?,?)',
            (WEB_USER_ID, GAME_PLAYER_ID),
        )
        db.execute(
            'insert into "PointExchangeRateVersion" ("id","pair","rateMicros","isActive") '
            'values (?,?,?,1)',
            ("rate-265", "USDT_POINT", "26500000"),
        )
        db.commit()
    finally:
        db.close()


def run_executor(
    database: Path,
    action: str,
    output: Path,
    env_overrides: dict[str, str] | None = None,
    arg_overrides: dict[str, str] | None = None,
) -> subprocess.CompletedProcess[str]:
    env = {
        **os.environ,
        "POINT_QA_WEB_USER_ID": WEB_USER_ID,
        "POINT_QA_GAME_PLAYER_ID": GAME_PLAYER_ID,
        "POINT_QA_SIGNED_PLAYER_ID": GAME_PLAYER_ID,
        "POINT_MIGRATION_REPORT_KEY": REPORT_KEY,
    }
    if action == "apply":
        env["POINT_QA_FUNDING_APPLY_ACK"] = "I_APPROVE_QA_SYNTHETIC_USDT_FUNDING"
    if action == "rollback":
        env["POINT_QA_FUNDING_ROLLBACK_ACK"] = \
            "I_APPROVE_QA_SYNTHETIC_USDT_FUNDING_ROLLBACK"
    env.update(env_overrides or {})
    values = {
        "signed-game-point": "5000",
        "expected-game-point": "5000",
        "funding-usdt-micros": "2000000",
        "expected-rate-micros": "26500000",
        "expected-point-micros": "53000000",
        "expected-web-usdt": "0",
        "expected-real-deposited-usdt": "0",
        "expected-total-deposited-usdt": "0",
        "expected-total-withdrawn-usdt": "0",
    }
    values.update(arg_overrides or {})
    command = [
        sys.executable,
        str(EXECUTOR),
        "--database", str(database),
        "--action", action,
        "--operation-id", OPERATION_ID,
        "--occurred-at", OCCURRED_AT,
    ]
    for key, value in values.items():
        command.extend([f"--{key}", value])
    command.extend(["--output", str(output)])
    return subprocess.run(command, env=env, text=True, capture_output=True, check=False)


def fresh_database(root: Path, name: str) -> Path:
    path = root / f"{name}.sqlite"
    create_database(path)
    return path


def assert_no_raw_identity(path: Path) -> dict:
    text = path.read_text(encoding="utf-8")
    assert WEB_USER_ID not in text
    assert GAME_PLAYER_ID not in text
    value = json.loads(text)
    assert value["containsRawIdentities"] is False
    return value


def run() -> None:
    with tempfile.TemporaryDirectory(prefix="point-qa-funding-test-") as temp:
        root = Path(temp)
        database = fresh_database(root, "happy")

        validated = run_executor(database, "validate", root / "validate.json")
        assert validated.returncode == 0, validated.stderr
        validate_value = assert_no_raw_identity(root / "validate.json")
        assert validate_value["funded"] is False
        assert validate_value["funding"]["expectedWholePoint"] == 53
        assert validate_value["withdrawalPrincipalUnchanged"] is True

        no_ack = run_executor(
            database,
            "apply",
            root / "no-ack.json",
            {"POINT_QA_FUNDING_APPLY_ACK": ""},
        )
        assert no_ack.returncode != 0
        assert "POINT_QA_FUNDING_APPLY_NOT_APPROVED" in no_ack.stderr

        applied = run_executor(database, "apply", root / "apply.json")
        assert applied.returncode == 0, applied.stderr
        apply_value = assert_no_raw_identity(root / "apply.json")
        assert apply_value["databaseMutationPerformed"] is True
        assert apply_value["funded"] is True
        assert apply_value["balances"]["webUsdt"] == "2.0"

        db = sqlite3.connect(database)
        wallet = db.execute(
            'select "balanceUsdt","totalDepositedUsdt","totalWithdrawnUsdt","updatedAt" '
            'from "Wallet" where "userId"=?', (WEB_USER_ID,)
        ).fetchone()
        tx = db.execute(
            'select "type","amount","currency","status","createdAt","updatedAt" '
            'from "Transaction" where "id"=?', (TRANSACTION_ID,)
        ).fetchone()
        real_deposit_count = db.execute(
            'select count(*) from "Transaction" where "userId"=? and "type"=? and "status"=?',
            (WEB_USER_ID, "USDT_DEPOSIT", "SUCCESS"),
        ).fetchone()[0]
        db.close()
        assert wallet == (2.0, 0.0, 0.0, OCCURRED_AT_MS)
        assert tx == ("QA_SYNTHETIC_USDT_FUNDING", 2.0, "USDT", "SUCCESS",
                      OCCURRED_AT_MS, OCCURRED_AT_MS)
        assert real_deposit_count == 0

        duplicate = run_executor(database, "apply", root / "duplicate.json")
        assert duplicate.returncode == 0, duplicate.stderr
        duplicate_value = assert_no_raw_identity(root / "duplicate.json")
        assert duplicate_value["databaseMutationPerformed"] is False
        assert duplicate_value["duplicate"] is True

        no_rollback_ack = run_executor(
            database,
            "rollback",
            root / "no-rollback-ack.json",
            {"POINT_QA_FUNDING_ROLLBACK_ACK": ""},
        )
        assert no_rollback_ack.returncode != 0
        assert "POINT_QA_FUNDING_ROLLBACK_NOT_APPROVED" in no_rollback_ack.stderr

        rolled_back = run_executor(database, "rollback", root / "rollback.json")
        assert rolled_back.returncode == 0, rolled_back.stderr
        rollback_value = assert_no_raw_identity(root / "rollback.json")
        assert rollback_value["funded"] is False
        db = sqlite3.connect(database)
        assert db.execute(
            'select "balanceUsdt","updatedAt" from "Wallet" where "userId"=?',
            (WEB_USER_ID,),
        ).fetchone() == (0.0, WALLET_UPDATED_AT)
        assert db.execute(
            'select count(*) from "Transaction" where "id"=?', (TRANSACTION_ID,)
        ).fetchone()[0] == 0
        db.close()

        rate_changed = run_executor(
            fresh_database(root, "rate-changed"),
            "validate",
            root / "rate-changed.json",
            arg_overrides={"expected-rate-micros": "25000000"},
        )
        assert rate_changed.returncode != 0
        assert "POINT_QA_FUNDING_RATE_CHANGED" in rate_changed.stderr

        quote_changed = run_executor(
            fresh_database(root, "quote-changed"),
            "validate",
            root / "quote-changed.json",
            arg_overrides={"expected-point-micros": "54000000"},
        )
        assert quote_changed.returncode != 0
        assert "POINT_QA_FUNDING_QUOTE_CHANGED" in quote_changed.stderr

        fractional = run_executor(
            fresh_database(root, "fractional"),
            "validate",
            root / "fractional.json",
            arg_overrides={
                "funding-usdt-micros": "1000000",
                "expected-point-micros": "26500000",
            },
        )
        assert fractional.returncode != 0
        assert "POINT_QA_FUNDING_EXPECTED_POINT_NOT_WHOLE" in fractional.stderr

        wrong_player = run_executor(
            fresh_database(root, "wrong-player"),
            "validate",
            root / "wrong-player.json",
            env_overrides={"POINT_QA_SIGNED_PLAYER_ID": "different-game-player"},
        )
        assert wrong_player.returncode != 0
        assert "POINT_QA_FUNDING_SIGNED_PLAYER_MISMATCH" in wrong_player.stderr

        activity_db = fresh_database(root, "activity")
        db = sqlite3.connect(activity_db)
        db.execute(
            'insert into "GamePointConversion" ("id","userId") values (?,?)',
            ("conversion-1", WEB_USER_ID),
        )
        db.commit()
        db.close()
        activity = run_executor(activity_db, "validate", root / "activity.json")
        assert activity.returncode != 0
        assert "POINT_QA_FUNDING_REQUIRES_EMPTY_OTHER_ACTIVITY" in activity.stderr

        tamper_db = fresh_database(root, "tamper")
        first = run_executor(tamper_db, "apply", root / "tamper-apply.json")
        assert first.returncode == 0, first.stderr
        db = sqlite3.connect(tamper_db)
        db.execute(
            'update "Transaction" set "metadata"=? where "id"=?',
            ('{"synthetic":false}', TRANSACTION_ID),
        )
        db.commit()
        db.close()
        tampered = run_executor(tamper_db, "apply", root / "tampered.json")
        assert tampered.returncode != 0
        assert "POINT_QA_FUNDING_IDEMPOTENCY_CONFLICT" in tampered.stderr

        consumed_db = fresh_database(root, "consumed")
        funded = run_executor(consumed_db, "apply", root / "consumed-apply.json")
        assert funded.returncode == 0, funded.stderr
        db = sqlite3.connect(consumed_db)
        db.execute(
            'insert into "GamePointConversion" ("id","userId") values (?,?)',
            ("conversion-after-funding", WEB_USER_ID),
        )
        db.execute(
            'insert into "Transaction" '
            '("id","userId","type","amount","currency","status","createdAt","updatedAt") '
            'values (?,?,?,?,?,?,?,?)',
            ("swap-after-funding", WEB_USER_ID, "SWAP", 53, "GXL", "PENDING",
             OCCURRED_AT_MS + 1, OCCURRED_AT_MS + 1),
        )
        db.execute(
            'update "Wallet" set "balanceUsdt"=0 where "userId"=?', (WEB_USER_ID,)
        )
        db.commit()
        db.close()
        blocked_rollback = run_executor(
            consumed_db, "rollback", root / "consumed-rollback.json"
        )
        assert blocked_rollback.returncode != 0
        assert "POINT_QA_FUNDING_REQUIRES_EMPTY_OTHER_ACTIVITY" in blocked_rollback.stderr
        db = sqlite3.connect(consumed_db)
        assert db.execute(
            'select count(*) from "Transaction" where "id"=?', (TRANSACTION_ID,)
        ).fetchone()[0] == 1
        db.close()

    print("POINT_WALLET_QA_SYNTHETIC_FUNDING_TEST=pass")


if __name__ == "__main__":
    run()
