#!/usr/bin/env python3

from __future__ import annotations

import hashlib
import json
import os
import sqlite3
import subprocess
import sys
import tempfile
from pathlib import Path

from exportWebPointMigrationSnapshot import RAW_EXPORT_ACK, export_snapshot


def file_sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def create_fixture(path: Path) -> None:
    db = sqlite3.connect(path)
    db.executescript(
        """
        create table "User" ("id" text primary key, "email" text not null);
        create table "Wallet" (
          "userId" text primary key,
          "balanceGXL" real not null,
          "lockedGXL" real not null
        );
        create table "Transaction" (
          "id" text primary key,
          "userId" text not null,
          "type" text not null,
          "amount" real not null,
          "currency" text not null,
          "status" text not null,
          "createdAt" integer not null
        );
        create table "GamePointSyncOutbox" (
          "userId" text not null,
          "sourceTransactionId" text not null unique,
          "pointAmount" text not null,
          "status" text not null,
          "attempts" integer not null
        );
        create table "GamePointLinkedAccount" (
          "userId" text primary key,
          "gamePlayerId" text not null unique
        );
        """
    )
    db.execute('insert into "User" values (?, ?)', ("web-user-1", "must-not-leak@example.test"))
    db.execute('insert into "Wallet" values (?, ?, ?)', ("web-user-1", 12.25, 0))
    db.execute(
        'insert into "Transaction" values (?, ?, ?, ?, ?, ?, ?)',
        ("tx-point", "web-user-1", "REFERRAL", 12.25, "GXL", "SUCCESS", 1),
    )
    db.execute(
        'insert into "Transaction" values (?, ?, ?, ?, ?, ?, ?)',
        ("tx-usdt", "web-user-1", "DEPOSIT", 99, "USDT", "SUCCESS", 2),
    )
    db.execute(
        'insert into "GamePointSyncOutbox" values (?, ?, ?, ?, ?)',
        ("web-user-1", "source-1", "1.500000", "SENT", 2),
    )
    db.execute(
        'insert into "GamePointLinkedAccount" values (?, ?)',
        ("web-user-1", "game-player-1"),
    )
    db.commit()
    db.close()


def run() -> None:
    with tempfile.TemporaryDirectory(prefix="point-migration-web-export-") as temp:
        database = Path(temp) / "web.db"
        create_fixture(database)
        before = file_sha256(database)

        snapshot = export_snapshot(str(database))
        assert snapshot["wallets"] == [{
            "userId": "web-user-1",
            "pointMicros": "12250000",
            "pointLegacyResidualAttos": "0",
            "lockedPointMicros": "0",
            "lockedPointLegacyResidualAttos": "0",
        }]
        assert snapshot["schemaVersion"] == 2
        assert len(snapshot["transactions"]) == 1
        assert snapshot["transactions"][0]["currency"] == "GXL"
        assert snapshot["transactions"][0]["amountLegacyResidualAttos"] == "0"
        assert snapshot["outboxes"][0]["pointMicros"] == "1500000"
        assert snapshot["links"] == [{"userId": "web-user-1", "gamePlayerId": "game-player-1"}]
        assert "must-not-leak@example.test" not in json.dumps(snapshot)
        assert file_sha256(database) == before

        command = [sys.executable, str(Path(__file__).with_name("exportWebPointMigrationSnapshot.py")),
                   "--database", str(database)]
        denied_env = dict(os.environ)
        denied_env.pop("POINT_MIGRATION_RAW_EXPORT_ACK", None)
        denied = subprocess.run(
            command,
            capture_output=True,
            text=True,
            check=False,
            env=denied_env,
        )
        assert denied.returncode == 1
        assert "RAW_EXPORT_ACK_REQUIRED" in denied.stderr
        allowed_env = dict(os.environ)
        allowed_env["POINT_MIGRATION_RAW_EXPORT_ACK"] = RAW_EXPORT_ACK
        allowed = subprocess.run(
            command,
            capture_output=True,
            text=True,
            check=False,
            env=allowed_env,
        )
        assert allowed.returncode == 0, allowed.stderr
        assert json.loads(allowed.stdout) == snapshot
        assert file_sha256(database) == before

        db = sqlite3.connect(database)
        db.execute('update "Wallet" set "balanceGXL" = 0.6666666666666856')
        db.execute(
            'update "Transaction" set "amount" = 316.6666666666667 where "id" = ?',
            ("tx-point",),
        )
        db.commit()
        db.close()

        before_legacy_export = file_sha256(database)
        legacy_snapshot = export_snapshot(str(database))
        assert legacy_snapshot["wallets"][0]["pointMicros"] == "666667"
        assert legacy_snapshot["wallets"][0]["pointLegacyResidualAttos"] == "-333333314400"
        assert legacy_snapshot["transactions"][0]["amountMicros"] == "316666667"
        assert (
            legacy_snapshot["transactions"][0]["amountLegacyResidualAttos"]
            == "-333333300000"
        )
        assert file_sha256(database) == before_legacy_export

        db = sqlite3.connect(database)
        db.execute(
            'update "GamePointSyncOutbox" set "pointAmount" = ?',
            ("0.0000001",),
        )
        db.commit()
        db.close()
        try:
            export_snapshot(str(database))
        except RuntimeError as error:
            assert "HAS_MORE_THAN_SIX_DECIMALS" in str(error)
        else:
            raise AssertionError("Sub-micro settlement outbox value was not rejected")

    print("[web-point-migration-export] PASS: legacy residuals were preserved, settlement stayed exact, and SQLite stayed unchanged.")


if __name__ == "__main__":
    run()
