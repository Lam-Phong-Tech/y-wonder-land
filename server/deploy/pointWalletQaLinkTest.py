#!/usr/bin/env python3
"""Regression tests for the dedicated QA Point authority linker."""

from __future__ import annotations

import json
import os
import sqlite3
import subprocess
import sys
import tempfile
from pathlib import Path


DEPLOY_ROOT = Path(__file__).resolve().parent
EXECUTOR = DEPLOY_ROOT / "pointWalletQaLink.py"
WEB_USER_ID = "web-user-wallet-qa"
GAME_PLAYER_ID = "game-player-wallet-qa"
OTHER_WEB_USER_ID = "web-user-other-qa"
OPERATION_ID = "point-qa-link:" + "a" * 32
OCCURRED_AT = "2026-07-17T08:00:00.000Z"
REPORT_KEY = "point-qa-link-test-reference-key-at-least-32-chars"


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
                "balanceUsdt" real not null default 0
            );
            create table "Transaction" (
                "id" text primary key,
                "userId" text not null
            );
            create table "GamePointLinkedAccount" (
                "userId" text not null primary key references "User"("id"),
                "gamePlayerId" text not null,
                "linkedBy" text not null,
                "note" text,
                "linkedAt" datetime not null default current_timestamp
            );
            create unique index "GamePointLinkedAccount_gamePlayerId_key"
                on "GamePointLinkedAccount"("gamePlayerId");
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
                (WEB_USER_ID, "ACTIVE", "2026-07-17T07:40:00Z"),
                (OTHER_WEB_USER_ID, "ACTIVE", "2026-07-17T07:40:00Z"),
            ],
        )
        db.executemany(
            'insert into "Wallet" ("id","userId","balanceGXL","lockedGXL","balanceUsdt") '
            'values (?,?,?,?,?)',
            [
                ("wallet-qa", WEB_USER_ID, 0, 0, 0),
                ("wallet-other", OTHER_WEB_USER_ID, 0, 0, 0),
            ],
        )
        db.execute(
            'insert into "PointExchangeRateVersion" ("id","pair","rateMicros","isActive") '
            'values (?,?,?,1)',
            ("rate-1", "USDT_POINT", "26500000"),
        )
        db.commit()
    finally:
        db.close()


def run_linker(database: Path, action: str, output: Path, overrides: dict[str, str] | None = None,
               signed_point: int = 5000) -> subprocess.CompletedProcess[str]:
    env = {
        **os.environ,
        "POINT_QA_WEB_USER_ID": WEB_USER_ID,
        "POINT_QA_GAME_PLAYER_ID": GAME_PLAYER_ID,
        "POINT_QA_SIGNED_PLAYER_ID": GAME_PLAYER_ID,
        "POINT_MIGRATION_REPORT_KEY": REPORT_KEY,
    }
    if action == "apply":
        env["POINT_QA_LINK_APPLY_ACK"] = "I_APPROVE_QA_POINT_AUTHORITY_LINK"
    if action == "rollback":
        env["POINT_QA_LINK_ROLLBACK_ACK"] = "I_APPROVE_QA_POINT_AUTHORITY_UNLINK"
    env.update(overrides or {})
    return subprocess.run(
        [
            sys.executable,
            str(EXECUTOR),
            "--database", str(database),
            "--action", action,
            "--operation-id", OPERATION_ID,
            "--occurred-at", OCCURRED_AT,
            "--signed-game-point", str(signed_point),
            "--expected-game-point", "5000",
            "--expected-web-usdt", "0",
            "--output", str(output),
        ],
        env=env,
        text=True,
        capture_output=True,
        check=False,
    )


def assert_no_raw_identity(path: Path) -> None:
    text = path.read_text(encoding="utf-8")
    assert WEB_USER_ID not in text
    assert GAME_PLAYER_ID not in text
    value = json.loads(text)
    assert value["containsRawIdentities"] is False


def run() -> None:
    with tempfile.TemporaryDirectory(prefix="point-qa-link-test-") as temp:
        root = Path(temp)
        database = root / "web.sqlite"
        create_database(database)

        validated = run_linker(database, "validate", root / "validate.json")
        assert validated.returncode == 0, validated.stderr
        assert_no_raw_identity(root / "validate.json")

        no_ack = run_linker(
            database,
            "apply",
            root / "no-ack.json",
            {"POINT_QA_LINK_APPLY_ACK": ""},
        )
        assert no_ack.returncode != 0
        assert "POINT_QA_LINK_APPLY_NOT_APPROVED" in no_ack.stderr

        mismatch = run_linker(
            database,
            "validate",
            root / "mismatch.json",
            {"POINT_QA_SIGNED_PLAYER_ID": "different-game-player"},
        )
        assert mismatch.returncode != 0
        assert "POINT_QA_SIGNED_PLAYER_MISMATCH" in mismatch.stderr

        applied = run_linker(database, "apply", root / "apply.json")
        assert applied.returncode == 0, applied.stderr
        assert_no_raw_identity(root / "apply.json")
        apply_value = json.loads((root / "apply.json").read_text(encoding="utf-8"))
        assert apply_value["databaseMutationPerformed"] is True
        assert apply_value["linked"] is True

        duplicate = run_linker(database, "apply", root / "duplicate.json")
        assert duplicate.returncode == 0, duplicate.stderr
        duplicate_value = json.loads((root / "duplicate.json").read_text(encoding="utf-8"))
        assert duplicate_value["databaseMutationPerformed"] is False
        assert duplicate_value["duplicate"] is True

        db = sqlite3.connect(database)
        db.execute('insert into "Transaction" ("id","userId") values (?,?)', ("tx-1", WEB_USER_ID))
        db.commit()
        db.close()
        blocked_rollback = run_linker(database, "rollback", root / "blocked-rollback.json")
        assert blocked_rollback.returncode != 0
        assert "POINT_QA_LINK_REQUIRES_EMPTY_ACTIVITY" in blocked_rollback.stderr

        db = sqlite3.connect(database)
        db.execute('delete from "Transaction" where "userId"=?', (WEB_USER_ID,))
        db.commit()
        db.close()
        rolled_back = run_linker(database, "rollback", root / "rollback.json")
        assert rolled_back.returncode == 0, rolled_back.stderr
        rollback_value = json.loads((root / "rollback.json").read_text(encoding="utf-8"))
        assert rollback_value["databaseMutationPerformed"] is True
        assert rollback_value["linked"] is False

        for table, row_id in (
            ("GamePointConversion", "conversion-1"),
            ("GamePointDebit", "debit-1"),
            ("GamePointSyncOutbox", "outbox-1"),
        ):
            db = sqlite3.connect(database)
            db.execute(f'insert into "{table}" ("id","userId") values (?,?)', (row_id, WEB_USER_ID))
            db.commit()
            db.close()
            blocked = run_linker(
                database,
                "validate",
                root / f"blocked-{table}.json",
            )
            assert blocked.returncode != 0
            assert "POINT_QA_LINK_REQUIRES_EMPTY_ACTIVITY" in blocked.stderr
            db = sqlite3.connect(database)
            db.execute(f'delete from "{table}" where "userId"=?', (WEB_USER_ID,))
            db.commit()
            db.close()

        db = sqlite3.connect(database)
        db.execute('update "Wallet" set "balanceGXL"=1 where "userId"=?', (WEB_USER_ID,))
        db.commit()
        db.close()
        nonzero = run_linker(database, "validate", root / "nonzero.json")
        assert nonzero.returncode != 0
        assert "POINT_QA_LINK_REQUIRES_ZERO_LEGACY_POINT" in nonzero.stderr

        db = sqlite3.connect(database)
        db.execute('update "Wallet" set "balanceGXL"=0 where "userId"=?', (WEB_USER_ID,))
        db.execute(
            'insert into "GamePointLinkedAccount" '
            '("userId","gamePlayerId","linkedBy","note","linkedAt") values (?,?,?,?,?)',
            (OTHER_WEB_USER_ID, GAME_PLAYER_ID, "other", "other", OCCURRED_AT),
        )
        db.commit()
        db.close()
        conflict = run_linker(database, "validate", root / "conflict.json")
        assert conflict.returncode != 0
        assert "POINT_QA_GAME_PLAYER_ALREADY_LINKED_ELSEWHERE" in conflict.stderr

        wrong_balance = run_linker(database, "validate", root / "wrong-balance.json", signed_point=5001)
        assert wrong_balance.returncode != 0
        assert "POINT_QA_SIGNED_BALANCE_MISMATCH" in wrong_balance.stderr

    print("POINT_WALLET_QA_LINK_TEST=pass")


if __name__ == "__main__":
    run()
