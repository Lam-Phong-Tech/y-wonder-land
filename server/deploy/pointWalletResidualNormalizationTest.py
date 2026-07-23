#!/usr/bin/env python3
"""Regression tests for the approved SQLite residual normalization executor."""

from __future__ import annotations

import hashlib
import json
import os
import sqlite3
import subprocess
import sys
import tempfile
from pathlib import Path


DEPLOY_ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(DEPLOY_ROOT))
import pointWalletResidualNormalization as executor  # noqa: E402


REFERENCE_KEY = "residual-normalization-test-key-with-at-least-32-characters"
PLANNER_SHA256 = "d" * 64


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_json(path: Path, value: dict) -> str:
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")
    return sha256(path)


def create_database(path: Path) -> None:
    db = sqlite3.connect(path)
    try:
        db.executescript('''
            create table "User" ("id" text primary key);
            create table "Wallet" (
                "id" text primary key,
                "userId" text not null unique,
                "balanceGXL" real not null default 0,
                "lockedGXL" real not null default 0,
                "updatedAt" text not null
            );
            create table "Transaction" (
                "id" text primary key,
                "userId" text not null,
                "type" text not null,
                "amount" real not null,
                "currency" text not null,
                "status" text not null,
                "createdAt" text not null,
                "updatedAt" text not null
            );
            create table "GamePointSyncOutbox" (
                "userId" text not null,
                "sourceTransactionId" text not null,
                "pointAmount" real not null,
                "status" text not null,
                "attempts" integer not null
            );
        ''')
        db.execute('insert into "User" ("id") values (?)', ("raw-residual-user",))
        db.execute(
            'insert into "Wallet" ("id","userId","balanceGXL","lockedGXL","updatedAt") '
            'values (?,?,?,?,?)',
            ("wallet-1", "raw-residual-user", 0.6666666666666856, 0, "2026-07-16T00:00:00Z"),
        )
        db.execute(
            'insert into "Transaction" '
            '("id","userId","type","amount","currency","status","createdAt","updatedAt") '
            'values (?,?,?,?,?,?,?,?)',
            (
                "raw-residual-transaction", "raw-residual-user", "SWAP",
                316.6666666666667, "GXL", "SUCCESS",
                "2026-07-16T00:00:00Z", "2026-07-16T00:00:00Z",
            ),
        )
        db.commit()
    finally:
        db.close()


def fixture() -> tuple[dict, dict]:
    user_id = "raw-residual-user"
    transaction_id = "raw-residual-transaction"
    account_ref = executor.public_ref(REFERENCE_KEY, "web-user", user_id)
    wallet_ref = executor.public_ref(
        REFERENCE_KEY, "legacy-value", f"{user_id}\0wallet\0balanceGXL"
    )
    transaction_value_ref = executor.public_ref(
        REFERENCE_KEY,
        "legacy-value",
        f"{user_id}\0transaction\0{transaction_id}\0amount",
    )
    wallet = executor.decimal_evidence(0.6666666666666856, "FIXTURE_WALLET")
    transaction = executor.decimal_evidence(316.6666666666667, "FIXTURE_TRANSACTION")

    def planned_value(value_ref: str, evidence: dict[str, str], source_kind: str,
                      source_field: str, transaction_ref: str | None = None) -> dict:
        output = {
            "valueRef": value_ref,
            "sourceKind": source_kind,
            "sourceField": source_field,
            "roundedPointMicros": evidence["roundedPointMicros"],
            "sourcePointAttos": evidence["sourcePointAttos"],
            "normalizedPointAttos": evidence["normalizedPointAttos"],
            "residualPointAttos": evidence["residualPointAttos"],
            "normalizationDeltaPointAttos": str(-int(evidence["residualPointAttos"])),
            "proposedTreatment": "ROUND_HALF_EVEN_WITH_APPEND_ONLY_RESIDUAL_AUDIT",
            "operationStatus": "NOT_AUTHORIZED",
        }
        if transaction_ref:
            output["transactionRef"] = transaction_ref
        return output

    values = [
        planned_value(wallet_ref, wallet, "WEB_WALLET_BALANCE", "balanceGXL"),
        planned_value(
            transaction_value_ref,
            transaction,
            "WEB_TRANSACTION_AMOUNT",
            "amount",
            executor.public_ref(REFERENCE_KEY, "web-transaction", transaction_id),
        ),
    ]
    total_residual = sum(int(value["residualPointAttos"]) for value in values)
    operation_id = "point-remediation:" + "a" * 32
    plan = {
        "schemaVersion": 1,
        "generatedAt": "2026-07-16T15:56:41.000Z",
        "mode": "READ_ONLY_REMEDIATION_PLAN",
        "automaticExecutionAllowed": False,
        "executionStatementsGenerated": 0,
        "databaseMutationsPerformed": False,
        "containsRawIdentities": False,
        "planner": {"sha256": PLANNER_SHA256},
        "sources": {},
        "summary": {
            "syntheticReversalAccountCount": 1,
            "syntheticReversalSourceCount": 3,
            "syntheticReversalPointMicros": "3000000",
            "residualAccountCount": 1,
            "residualValueCount": 2,
            "totalResidualPointAttos": str(total_residual),
            "normalizationDeltaPointAttos": str(-total_residual),
            "currentBlockedAccountCount": 1,
            "expectedBlockedAccountCountAfterAuthorizedResidualNormalization": 0,
            "postExecutionGate": "FRESH_READ_ONLY_DRY_RUN_REQUIRED",
            "remediationPlanGate": "READY_FOR_SEPARATE_OPERATIONAL_APPROVAL",
        },
        "authorization": {
            "syntheticReversal": "NOT_AUTHORIZED",
            "residualNormalization": "NOT_AUTHORIZED",
            "accountLink": "DEFERRED",
            "balanceMigration": "NOT_AUTHORIZED",
            "deployment": "NOT_AUTHORIZED",
        },
        "syntheticReversalPlans": [],
        "legacyResidualPlans": [{
            "accountRef": account_ref,
            "policy": "APPROVE_ROUND_HALF_EVEN_WITH_RESIDUAL_AUDIT",
            "proposedOperationId": operation_id,
            "operationStatus": "NOT_AUTHORIZED",
            "valueCount": 2,
            "totalResidualPointAttos": str(total_residual),
            "normalizationDeltaPointAttos": str(-total_residual),
            "maxAbsResidualPointAttos": str(max(abs(int(value["residualPointAttos"])) for value in values)),
            "values": values,
            "requiredExecutionPreconditions": [],
        }],
    }
    approval = {
        "schemaVersion": 1,
        "mode": "POINT_WALLET_REMEDIATION_OPERATION_APPROVAL",
        "approvedAt": "2026-07-16T16:06:22.236Z",
        "approvedByRole": "PROJECT_OWNER",
        "approvalReference": "OWNER_CHAT_APPROVAL_TEST",
        "remediationPlan": {
            "sha256": "0" * 64,
            "plannerSha256": PLANNER_SHA256,
            "generatedAt": plan["generatedAt"],
        },
        "executionOrder": [
            "LEGACY_RESIDUAL_NORMALIZATION", "SYNTHETIC_CREDIT_REVERSAL"
        ],
        "operations": {
            "legacyResidualNormalization": {
                "authorized": True,
                "action": "ROUND_HALF_EVEN_WITH_APPEND_ONLY_RESIDUAL_AUDIT",
                "roundingMode": "ROUND_HALF_EVEN",
                "accountCount": 1,
                "valueCount": 2,
                "totalResidualPointAttos": str(total_residual),
                "normalizationDeltaPointAttos": str(-total_residual),
            },
            "syntheticCreditReversal": {
                "authorized": True,
                "action": "AUDITED_SYNTHETIC_CREDIT_REVERSAL",
                "accountCount": 1,
                "sourceCount": 3,
                "pointMicros": "3000000",
            },
        },
        "constraints": dict(executor.EXPECTED_CONSTRAINTS),
    }
    return plan, approval


def run_executor(root: Path, database: Path, plan_path: Path, plan_sha: str,
                 approval_path: Path, approval_sha: str, action: str, output_name: str) -> subprocess.CompletedProcess:
    env = {**os.environ, "POINT_MIGRATION_REPORT_KEY": REFERENCE_KEY}
    return subprocess.run(
        [
            sys.executable,
            str(DEPLOY_ROOT / "pointWalletResidualNormalization.py"),
            "--database", str(database),
            "--plan", str(plan_path),
            "--plan-sha256", plan_sha,
            "--approval", str(approval_path),
            "--approval-sha256", approval_sha,
            "--action", action,
            "--occurred-at", "2026-07-16T16:10:00.000Z",
            "--output", str(root / output_name),
        ],
        env=env,
        text=True,
        capture_output=True,
        check=False,
    )


def assert_sources(database: Path, wallet: float, transaction: float) -> None:
    db = sqlite3.connect(database)
    try:
        actual_wallet = db.execute('select "balanceGXL" from "Wallet"').fetchone()[0]
        actual_tx = db.execute('select "amount" from "Transaction"').fetchone()[0]
        assert executor.decimal_evidence(actual_wallet, "ASSERT_WALLET")["sourcePointAttos"] == \
            executor.decimal_evidence(wallet, "EXPECTED_WALLET")["sourcePointAttos"]
        assert executor.decimal_evidence(actual_tx, "ASSERT_TX")["sourcePointAttos"] == \
            executor.decimal_evidence(transaction, "EXPECTED_TX")["sourcePointAttos"]
    finally:
        db.close()


def run() -> None:
    with tempfile.TemporaryDirectory(prefix="point-residual-normalization-test-") as temp:
        root = Path(temp)
        database = root / "web.sqlite"
        create_database(database)
        plan, approval = fixture()
        plan_path = root / "plan.json"
        approval_path = root / "approval.json"
        plan_sha = write_json(plan_path, plan)
        approval["remediationPlan"]["sha256"] = plan_sha
        approval_sha = write_json(approval_path, approval)

        applied = run_executor(
            root, database, plan_path, plan_sha, approval_path, approval_sha, "apply", "apply.json"
        )
        assert applied.returncode == 0, applied.stderr
        assert_sources(database, 0.666667, 316.666667)
        receipt = json.loads((root / "apply.json").read_text(encoding="utf-8"))
        assert receipt["databaseMutationsPerformed"] is True
        assert receipt["idempotentReplay"] is False
        assert len(receipt["events"]) == 2
        raw_receipt = (root / "apply.json").read_text(encoding="utf-8")
        assert "raw-residual-user" not in raw_receipt
        assert "raw-residual-transaction" not in raw_receipt

        db = sqlite3.connect(database)
        try:
            assert db.execute(
                'select count(*) from "PointLegacyResidualAudit" where "action"=\'NORMALIZE\''
            ).fetchone()[0] == 2
            try:
                db.execute('delete from "PointLegacyResidualAudit"')
                raise AssertionError("append-only delete trigger did not fire")
            except sqlite3.DatabaseError as error:
                assert "POINT_RESIDUAL_AUDIT_IS_APPEND_ONLY" in str(error)
            db.rollback()
        finally:
            db.close()

        replay = run_executor(
            root, database, plan_path, plan_sha, approval_path, approval_sha, "apply", "replay.json"
        )
        assert replay.returncode == 0, replay.stderr
        replay_receipt = json.loads((root / "replay.json").read_text(encoding="utf-8"))
        assert replay_receipt["idempotentReplay"] is True
        assert replay_receipt["databaseMutationsPerformed"] is False

        rolled_back = run_executor(
            root, database, plan_path, plan_sha, approval_path, approval_sha,
            "rollback", "rollback.json"
        )
        assert rolled_back.returncode == 0, rolled_back.stderr
        assert_sources(database, 0.6666666666666856, 316.6666666666667)
        rollback_receipt = json.loads((root / "rollback.json").read_text(encoding="utf-8"))
        assert rollback_receipt["databaseMutationsPerformed"] is True
        assert len(rollback_receipt["events"]) == 2

        rollback_replay = run_executor(
            root, database, plan_path, plan_sha, approval_path, approval_sha,
            "rollback", "rollback-replay.json"
        )
        assert rollback_replay.returncode == 0, rollback_replay.stderr
        rollback_replay_receipt = json.loads(
            (root / "rollback-replay.json").read_text(encoding="utf-8")
        )
        assert rollback_replay_receipt["idempotentReplay"] is True
        assert rollback_replay_receipt["databaseMutationsPerformed"] is False
        assert_sources(database, 0.6666666666666856, 316.6666666666667)

        reapply = run_executor(
            root, database, plan_path, plan_sha, approval_path, approval_sha,
            "apply", "reapply-after-rollback.json"
        )
        assert reapply.returncode != 0
        assert "RESIDUAL_NORMALIZATION_ALREADY_ROLLED_BACK" in reapply.stderr

    with tempfile.TemporaryDirectory(prefix="point-residual-normalization-drift-") as temp:
        root = Path(temp)
        database = root / "web.sqlite"
        create_database(database)
        db = sqlite3.connect(database)
        db.execute('update "Wallet" set "balanceGXL"=?', (0.5,))
        db.commit()
        db.close()
        plan, approval = fixture()
        plan_path = root / "plan.json"
        approval_path = root / "approval.json"
        plan_sha = write_json(plan_path, plan)
        approval["remediationPlan"]["sha256"] = plan_sha
        approval_sha = write_json(approval_path, approval)
        drift = run_executor(
            root, database, plan_path, plan_sha, approval_path, approval_sha,
            "apply", "drift.json"
        )
        assert drift.returncode != 0
        assert "DRIFT" in drift.stderr
        db = sqlite3.connect(database)
        try:
            assert db.execute(
                "select count(*) from sqlite_master where type='table' and name='PointLegacyResidualAudit'"
            ).fetchone()[0] == 0
        finally:
            db.close()

    print("[point-wallet-residual-normalization] PASS: apply, audit, replay, rollback, and drift are fail-closed.")


if __name__ == "__main__":
    run()
