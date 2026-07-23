#!/usr/bin/env python3
"""Apply or compensate approved legacy Point residual normalization in SQLite."""

from __future__ import annotations

import argparse
import hashlib
import hmac
import json
import math
import os
import re
import sqlite3
import sys
from datetime import datetime
from decimal import ROUND_HALF_EVEN, Decimal, InvalidOperation
from pathlib import Path
from typing import Any


REPORT_DOMAIN = "ywonder-point-migration-report-v1"
POINT_ATTOS = Decimal("1000000000000000000")
POINT_MICROS = Decimal("1000000")
MICRO_POINT_ATTOS = Decimal("1000000000000")
SHA256_PATTERN = re.compile(r"^[a-f0-9]{64}$")
REF_PATTERN = re.compile(r"^[a-f0-9]{24}$")
OPERATION_PATTERN = re.compile(r"^point-remediation:[a-f0-9]{32}$")
APPROVAL_REFERENCE_PATTERN = re.compile(r"^OWNER_CHAT_APPROVAL_[A-Z0-9_-]+$")
FORBIDDEN_IDENTITY_KEYS = {
    "email", "phone", "playerId", "player_id", "sourceTransactionId", "transactionId",
    "userId", "user_id", "username", "webUserId", "web_user_id",
}
EXPECTED_CONSTRAINTS = {
    "authorizesOnlyListedOperations": True,
    "authorizesDatabaseMutation": True,
    "authorizesCompensatingRollback": True,
    "authorizesAccountLink": False,
    "authorizesBalanceMigration": False,
    "authorizesDeployment": False,
    "authorizesServiceRestart": False,
    "authorizesRealPayment": False,
    "requiresChecksummedBackup": True,
    "requiresFreshPreflight": True,
    "requiresAtomicWriteAndAudit": True,
    "requiresPostWriteReconciliation": True,
}
AUDIT_COLUMNS = {
    "eventId", "operationId", "remediationPlanSha256", "approvalSha256", "accountRef",
    "valueRef", "sourceKind", "sourceRecordId", "sourceField", "originalStorageType",
    "originalValueText", "originalValueHex", "sourcePointAttos", "roundedPointMicros",
    "residualPointAttos", "normalizedValueText", "action", "targetEventId", "occurredAt",
    "createdAt",
}
AUDIT_COLUMN_ORDER = (
    "eventId", "operationId", "remediationPlanSha256", "approvalSha256", "accountRef",
    "valueRef", "sourceKind", "sourceRecordId", "sourceField", "originalStorageType",
    "originalValueText", "originalValueHex", "sourcePointAttos", "roundedPointMicros",
    "residualPointAttos", "normalizedValueText", "action", "targetEventId", "occurredAt",
    "createdAt",
)


def fail(code: str) -> None:
    raise RuntimeError(code)


def object_value(value: Any, field: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        fail(f"INVALID_{field}")
    return value


def array_value(value: Any, field: str) -> list[Any]:
    if not isinstance(value, list):
        fail(f"INVALID_{field}")
    return value


def exact_keys(value: dict[str, Any], expected: set[str], code: str) -> None:
    if set(value) != expected:
        fail(code)


def assert_no_raw_identity_fields(value: Any) -> None:
    if isinstance(value, list):
        for child in value:
            assert_no_raw_identity_fields(child)
        return
    if not isinstance(value, dict):
        return
    for key, child in value.items():
        if key in FORBIDDEN_IDENTITY_KEYS:
            fail("OPERATION_APPROVAL_CONTAINS_RAW_IDENTITY")
        assert_no_raw_identity_fields(child)


def sha256_value(value: Any, field: str) -> str:
    text = str(value or "").lower()
    if not SHA256_PATTERN.fullmatch(text):
        fail(f"INVALID_{field}_SHA256")
    return text


def integer_text(value: Any, field: str) -> str:
    text = str(value if value is not None else "").strip()
    if not re.fullmatch(r"-?(0|[1-9][0-9]*)", text):
        fail(f"INVALID_{field}")
    return text


def file_json(path: str, expected_sha256: str, field: str) -> tuple[dict[str, Any], str]:
    raw = Path(path).read_bytes()
    actual = hashlib.sha256(raw).hexdigest()
    if actual != sha256_value(expected_sha256, field):
        fail(f"{field}_SHA256_MISMATCH")
    value = json.loads(raw.decode("utf-8"))
    return object_value(value, field), actual


def public_ref(reference_key: str, kind: str, raw_value: str) -> str:
    message = f"{REPORT_DOMAIN}\0{kind}\0{raw_value}".encode()
    return hmac.new(reference_key.encode(), message, hashlib.sha256).hexdigest()[:24]


def parse_timestamp(value: str) -> str:
    text = str(value or "")
    try:
        parsed = datetime.fromisoformat(text.replace("Z", "+00:00"))
    except ValueError:
        fail("INVALID_OCCURRED_AT")
    if parsed.tzinfo is None:
        fail("INVALID_OCCURRED_AT")
    return text


def decimal_evidence(value: Any, field: str) -> dict[str, str]:
    if isinstance(value, float) and not math.isfinite(value):
        fail(f"INVALID_{field}")
    try:
        decimal = Decimal(str(value))
    except (InvalidOperation, ValueError):
        fail(f"INVALID_{field}")
    if not decimal.is_finite():
        fail(f"INVALID_{field}")
    point_attos = decimal * POINT_ATTOS
    if point_attos != point_attos.to_integral_value():
        fail(f"{field}_HAS_MORE_THAN_EIGHTEEN_DECIMALS")
    rounded_micros = (decimal * POINT_MICROS).to_integral_value(rounding=ROUND_HALF_EVEN)
    normalized_attos = rounded_micros * MICRO_POINT_ATTOS
    residual_attos = point_attos - normalized_attos
    if abs(residual_attos) > MICRO_POINT_ATTOS / 2:
        fail(f"INVALID_{field}_ROUNDING_RESIDUAL")
    return {
        "sourcePointAttos": str(int(point_attos)),
        "roundedPointMicros": str(int(rounded_micros)),
        "normalizedPointAttos": str(int(normalized_attos)),
        "residualPointAttos": str(int(residual_attos)),
        "normalizedValueText": format(rounded_micros / POINT_MICROS, "f"),
    }


def validate_approval(approval: dict[str, Any], plan: dict[str, Any], approval_sha256: str,
                      plan_sha256: str) -> None:
    assert_no_raw_identity_fields(approval)
    exact_keys(approval, {
        "schemaVersion", "mode", "approvedAt", "approvedByRole", "approvalReference",
        "remediationPlan", "executionOrder", "operations", "constraints",
    }, "UNEXPECTED_OPERATION_APPROVAL_FIELD")
    if approval.get("schemaVersion") != 1 or approval.get("mode") != \
            "POINT_WALLET_REMEDIATION_OPERATION_APPROVAL":
        fail("INVALID_OPERATION_APPROVAL")
    if approval.get("approvedByRole") != "PROJECT_OWNER":
        fail("INVALID_OPERATION_APPROVER_ROLE")
    if not APPROVAL_REFERENCE_PATTERN.fullmatch(str(approval.get("approvalReference") or "")):
        fail("INVALID_OPERATION_APPROVAL_REFERENCE")
    parse_timestamp(str(approval.get("approvedAt") or ""))
    source = object_value(approval.get("remediationPlan"), "APPROVED_REMEDIATION_PLAN")
    exact_keys(source, {"sha256", "plannerSha256", "generatedAt"},
               "UNEXPECTED_APPROVED_REMEDIATION_PLAN_FIELD")
    if sha256_value(source.get("sha256"), "APPROVED_REMEDIATION_PLAN") != plan_sha256:
        fail("APPROVED_REMEDIATION_PLAN_SHA_MISMATCH")
    if source.get("generatedAt") != plan.get("generatedAt"):
        fail("APPROVED_REMEDIATION_GENERATED_AT_MISMATCH")
    planner = object_value(plan.get("planner"), "PLAN_PLANNER")
    if sha256_value(source.get("plannerSha256"), "APPROVED_PLANNER") != \
            sha256_value(planner.get("sha256"), "PLAN_PLANNER"):
        fail("APPROVED_REMEDIATION_PLANNER_SHA_MISMATCH")
    if approval.get("executionOrder") != [
        "LEGACY_RESIDUAL_NORMALIZATION", "SYNTHETIC_CREDIT_REVERSAL"
    ]:
        fail("INVALID_REMEDIATION_EXECUTION_ORDER")
    constraints = object_value(approval.get("constraints"), "OPERATION_CONSTRAINTS")
    exact_keys(constraints, set(EXPECTED_CONSTRAINTS), "UNEXPECTED_OPERATION_CONSTRAINT")
    if constraints != EXPECTED_CONSTRAINTS:
        fail("INVALID_OPERATION_CONSTRAINT")
    operations = object_value(approval.get("operations"), "AUTHORIZED_OPERATIONS")
    exact_keys(operations, {"legacyResidualNormalization", "syntheticCreditReversal"},
               "UNEXPECTED_AUTHORIZED_OPERATION")
    residual = object_value(operations.get("legacyResidualNormalization"),
                            "AUTHORIZED_RESIDUAL_NORMALIZATION")
    exact_keys(residual, {
        "authorized", "action", "roundingMode", "accountCount", "valueCount",
        "totalResidualPointAttos", "normalizationDeltaPointAttos",
    }, "UNEXPECTED_RESIDUAL_AUTHORIZATION_FIELD")
    if residual.get("authorized") is not True or residual.get("action") != \
            "ROUND_HALF_EVEN_WITH_APPEND_ONLY_RESIDUAL_AUDIT" or \
            residual.get("roundingMode") != "ROUND_HALF_EVEN":
        fail("RESIDUAL_NORMALIZATION_NOT_AUTHORIZED")
    summary = object_value(plan.get("summary"), "PLAN_SUMMARY")
    expected = {
        "accountCount": summary.get("residualAccountCount"),
        "valueCount": summary.get("residualValueCount"),
        "totalResidualPointAttos": summary.get("totalResidualPointAttos"),
        "normalizationDeltaPointAttos": summary.get("normalizationDeltaPointAttos"),
    }
    for key, value in expected.items():
        if residual.get(key) != value:
            fail(f"AUTHORIZED_RESIDUAL_{key.upper()}_MISMATCH")
    synthetic = object_value(operations.get("syntheticCreditReversal"),
                             "AUTHORIZED_SYNTHETIC_CREDIT_REVERSAL")
    exact_keys(synthetic, {
        "authorized", "action", "accountCount", "sourceCount", "pointMicros",
    }, "UNEXPECTED_SYNTHETIC_AUTHORIZATION_FIELD")
    if synthetic.get("authorized") is not True or synthetic.get("action") != \
            "AUDITED_SYNTHETIC_CREDIT_REVERSAL":
        fail("SYNTHETIC_REVERSAL_NOT_AUTHORIZED")
    if synthetic.get("accountCount") != summary.get("syntheticReversalAccountCount") or \
            synthetic.get("sourceCount") != summary.get("syntheticReversalSourceCount") or \
            integer_text(synthetic.get("pointMicros"), "AUTHORIZED_SYNTHETIC_MICROS") != \
            integer_text(summary.get("syntheticReversalPointMicros"), "PLAN_SYNTHETIC_MICROS"):
        fail("AUTHORIZED_SYNTHETIC_SUMMARY_MISMATCH")
    sha256_value(approval_sha256, "OPERATION_APPROVAL")

    if plan.get("schemaVersion") != 1 or plan.get("mode") != "READ_ONLY_REMEDIATION_PLAN" or \
            plan.get("automaticExecutionAllowed") is not False or \
            plan.get("executionStatementsGenerated") != 0 or \
            plan.get("databaseMutationsPerformed") is not False or \
            plan.get("containsRawIdentities") is not False:
        fail("AUTHORIZED_REMEDIATION_PLAN_SAFETY_FLAGS_INVALID")
    authorization = object_value(plan.get("authorization"), "PLAN_AUTHORIZATION")
    expected_authorization = {
        "syntheticReversal": "NOT_AUTHORIZED",
        "residualNormalization": "NOT_AUTHORIZED",
        "accountLink": "DEFERRED",
        "balanceMigration": "NOT_AUTHORIZED",
        "deployment": "NOT_AUTHORIZED",
    }
    if any(authorization.get(key) != value for key, value in expected_authorization.items()):
        fail("SOURCE_PLAN_AUTHORIZATION_POSTURE_INVALID")


def validate_plan(plan: dict[str, Any]) -> list[dict[str, Any]]:
    if plan.get("schemaVersion") != 1 or plan.get("mode") != "READ_ONLY_REMEDIATION_PLAN":
        fail("INVALID_REMEDIATION_PLAN")
    if plan.get("automaticExecutionAllowed") is not False or \
            plan.get("executionStatementsGenerated") != 0 or \
            plan.get("databaseMutationsPerformed") is not False or \
            plan.get("containsRawIdentities") is not False:
        fail("REMEDIATION_PLAN_SAFETY_FLAGS_INVALID")
    authorization = object_value(plan.get("authorization"), "PLAN_AUTHORIZATION")
    if authorization.get("residualNormalization") != "NOT_AUTHORIZED":
        fail("SOURCE_PLAN_RESIDUAL_AUTHORIZATION_INVALID")
    plans = array_value(plan.get("legacyResidualPlans"), "LEGACY_RESIDUAL_PLANS")
    summary = object_value(plan.get("summary"), "PLAN_SUMMARY")
    if len(plans) != summary.get("residualAccountCount"):
        fail("RESIDUAL_PLAN_ACCOUNT_COUNT_MISMATCH")
    account_refs: set[str] = set()
    operation_ids: set[str] = set()
    total_values = 0
    total_residual = 0
    for account in plans:
        account_ref = str(account.get("accountRef") or "")
        operation_id = str(account.get("proposedOperationId") or "")
        if not REF_PATTERN.fullmatch(account_ref) or account_ref in account_refs:
            fail("INVALID_OR_DUPLICATE_RESIDUAL_ACCOUNT_REF")
        if not OPERATION_PATTERN.fullmatch(operation_id) or operation_id in operation_ids:
            fail("INVALID_OR_DUPLICATE_RESIDUAL_OPERATION_ID")
        account_refs.add(account_ref)
        operation_ids.add(operation_id)
        if account.get("policy") != "APPROVE_ROUND_HALF_EVEN_WITH_RESIDUAL_AUDIT" or \
                account.get("operationStatus") != "NOT_AUTHORIZED":
            fail("INVALID_RESIDUAL_PLAN_POLICY")
        values = array_value(account.get("values"), "RESIDUAL_PLAN_VALUES")
        if len(values) != account.get("valueCount"):
            fail("RESIDUAL_PLAN_VALUE_COUNT_MISMATCH")
        account_residual = 0
        max_abs_residual = 0
        for value in values:
            if value.get("proposedTreatment") != \
                    "ROUND_HALF_EVEN_WITH_APPEND_ONLY_RESIDUAL_AUDIT" or \
                    value.get("operationStatus") != "NOT_AUTHORIZED":
                fail("INVALID_RESIDUAL_VALUE_POLICY")
            source_attos = int(integer_text(value.get("sourcePointAttos"), "PLAN_SOURCE_ATTOS"))
            rounded_micros = int(integer_text(value.get("roundedPointMicros"),
                                              "PLAN_ROUNDED_MICROS"))
            normalized_attos = int(integer_text(value.get("normalizedPointAttos"),
                                                "PLAN_NORMALIZED_ATTOS"))
            residual_attos = int(integer_text(value.get("residualPointAttos"),
                                              "PLAN_RESIDUAL_ATTOS"))
            normalization_delta = int(integer_text(value.get("normalizationDeltaPointAttos"),
                                                    "PLAN_NORMALIZATION_DELTA_ATTOS"))
            if normalized_attos != rounded_micros * int(MICRO_POINT_ATTOS) or \
                    source_attos - normalized_attos != residual_attos or \
                    normalization_delta != -residual_attos or \
                    abs(residual_attos) > int(MICRO_POINT_ATTOS / 2):
                fail("RESIDUAL_PLAN_VALUE_ARITHMETIC_MISMATCH")
            account_residual += residual_attos
            max_abs_residual = max(max_abs_residual, abs(residual_attos))
        if int(integer_text(account.get("totalResidualPointAttos"),
                            "ACCOUNT_TOTAL_RESIDUAL_ATTOS")) != account_residual or \
                int(integer_text(account.get("normalizationDeltaPointAttos"),
                                 "ACCOUNT_NORMALIZATION_DELTA_ATTOS")) != -account_residual or \
                int(integer_text(account.get("maxAbsResidualPointAttos"),
                                 "ACCOUNT_MAX_RESIDUAL_ATTOS")) != max_abs_residual:
            fail("RESIDUAL_PLAN_ACCOUNT_ARITHMETIC_MISMATCH")
        total_values += len(values)
        total_residual += account_residual
    if total_values != summary.get("residualValueCount") or \
            int(integer_text(summary.get("totalResidualPointAttos"),
                             "SUMMARY_TOTAL_RESIDUAL_ATTOS")) != total_residual or \
            int(integer_text(summary.get("normalizationDeltaPointAttos"),
                             "SUMMARY_NORMALIZATION_DELTA_ATTOS")) != -total_residual:
        fail("RESIDUAL_PLAN_SUMMARY_ARITHMETIC_MISMATCH")
    return plans


def ensure_audit_schema(db: sqlite3.Connection) -> None:
    db.execute('''create table if not exists "PointLegacyResidualAudit" (
        "eventId" text primary key,
        "operationId" text not null,
        "remediationPlanSha256" text not null,
        "approvalSha256" text not null,
        "accountRef" text not null,
        "valueRef" text not null,
        "sourceKind" text not null,
        "sourceRecordId" text not null,
        "sourceField" text not null,
        "originalStorageType" text not null,
        "originalValueText" text not null,
        "originalValueHex" text not null,
        "sourcePointAttos" text not null,
        "roundedPointMicros" text not null,
        "residualPointAttos" text not null,
        "normalizedValueText" text not null,
        "action" text not null check ("action" in ('NORMALIZE','ROLLBACK')),
        "targetEventId" text,
        "occurredAt" text not null,
        "createdAt" text not null default (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
        unique ("operationId", "valueRef", "action")
    )''')
    db.execute('''create trigger if not exists "PointLegacyResidualAudit_no_update"
        before update on "PointLegacyResidualAudit"
        begin select raise(abort, 'POINT_RESIDUAL_AUDIT_IS_APPEND_ONLY'); end''')
    db.execute('''create trigger if not exists "PointLegacyResidualAudit_no_delete"
        before delete on "PointLegacyResidualAudit"
        begin select raise(abort, 'POINT_RESIDUAL_AUDIT_IS_APPEND_ONLY'); end''')
    columns = {str(row[1]) for row in db.execute('pragma table_info("PointLegacyResidualAudit")')}
    if columns != AUDIT_COLUMNS:
        fail("POINT_RESIDUAL_AUDIT_SCHEMA_MISMATCH")
    triggers = {str(row[0]) for row in db.execute(
        "select name from sqlite_master where type='trigger' and tbl_name='PointLegacyResidualAudit'"
    )}
    if triggers != {"PointLegacyResidualAudit_no_update", "PointLegacyResidualAudit_no_delete"}:
        fail("POINT_RESIDUAL_AUDIT_TRIGGER_MISMATCH")


def event_id(operation_id: str, value_ref: str, action: str) -> str:
    digest = hashlib.sha256(
        f"point-residual-audit-v1\0{operation_id}\0{value_ref}\0{action}".encode()
    ).hexdigest()[:32]
    return f"point-residual-audit:{digest}"


def source_candidates(db: sqlite3.Connection, reference_key: str) -> dict[str, dict[str, Any]]:
    candidates: dict[str, dict[str, Any]] = {}

    def add(candidate: dict[str, Any]) -> None:
        ref = candidate["valueRef"]
        if ref in candidates:
            fail("LEGACY_VALUE_REFERENCE_COLLISION")
        candidates[ref] = candidate

    for row in db.execute(
        'select "userId", "balanceGXL", typeof("balanceGXL"), '
        '"lockedGXL", typeof("lockedGXL") from "Wallet" order by "userId"'
    ):
        user_id = str(row[0])
        account_ref = public_ref(reference_key, "web-user", user_id)
        for field, value, storage_type, source_kind in (
            ("balanceGXL", row[1], row[2], "WEB_WALLET_BALANCE"),
            ("lockedGXL", row[3], row[4], "WEB_WALLET_LOCKED_BALANCE"),
        ):
            add({
                "valueRef": public_ref(reference_key, "legacy-value", f"{user_id}\0wallet\0{field}"),
                "accountRef": account_ref,
                "sourceKind": source_kind,
                "sourceField": field,
                "sourceRecordId": user_id,
                "value": value,
                "storageType": str(storage_type),
            })
    for row in db.execute(
        'select "id", "userId", "amount", typeof("amount") from "Transaction" '
        "where upper(trim(coalesce(\"currency\", ''))) in ('GXL','POINT') "
        'order by "userId", "createdAt", "id"'
    ):
        transaction_id, user_id = str(row[0]), str(row[1])
        add({
            "valueRef": public_ref(
                reference_key, "legacy-value", f"{user_id}\0transaction\0{transaction_id}\0amount"
            ),
            "transactionRef": public_ref(reference_key, "web-transaction", transaction_id),
            "accountRef": public_ref(reference_key, "web-user", user_id),
            "sourceKind": "WEB_TRANSACTION_AMOUNT",
            "sourceField": "amount",
            "sourceRecordId": transaction_id,
            "value": row[2],
            "storageType": str(row[3]),
        })
    return candidates


def selected_values(residual_plans: list[dict[str, Any]]) -> list[dict[str, Any]]:
    selected = []
    seen_refs: set[str] = set()
    for account in residual_plans:
        account_ref = str(account.get("accountRef") or "")
        operation_id = str(account.get("proposedOperationId") or "")
        if not REF_PATTERN.fullmatch(account_ref) or not OPERATION_PATTERN.fullmatch(operation_id):
            fail("INVALID_RESIDUAL_PLAN_IDENTITY")
        if account.get("operationStatus") != "NOT_AUTHORIZED":
            fail("INVALID_RESIDUAL_PLAN_OPERATION_STATUS")
        values = array_value(account.get("values"), "RESIDUAL_PLAN_VALUES")
        if len(values) != account.get("valueCount"):
            fail("RESIDUAL_PLAN_VALUE_COUNT_MISMATCH")
        for value in values:
            value_ref = str(value.get("valueRef") or "")
            if not REF_PATTERN.fullmatch(value_ref) or value_ref in seen_refs:
                fail("INVALID_OR_DUPLICATE_RESIDUAL_VALUE_REF")
            seen_refs.add(value_ref)
            selected.append({**value, "accountRef": account_ref, "operationId": operation_id})
    return selected


def verify_candidate(expected: dict[str, Any], candidate: dict[str, Any]) -> dict[str, str]:
    if candidate["accountRef"] != expected["accountRef"]:
        fail("RESIDUAL_ACCOUNT_REF_MISMATCH")
    if candidate["sourceKind"] != expected.get("sourceKind") or \
            candidate["sourceField"] != expected.get("sourceField"):
        fail("RESIDUAL_SOURCE_MISMATCH")
    if expected.get("transactionRef") is not None and \
            candidate.get("transactionRef") != expected.get("transactionRef"):
        fail("RESIDUAL_TRANSACTION_REF_MISMATCH")
    evidence = decimal_evidence(candidate["value"], "CURRENT_LEGACY_VALUE")
    for key in ("sourcePointAttos", "roundedPointMicros", "residualPointAttos"):
        if evidence[key] != integer_text(expected.get(key), f"PLAN_{key.upper()}"):
            fail(f"RESIDUAL_{key.upper()}_DRIFT")
    if evidence["normalizedPointAttos"] != integer_text(
            expected.get("normalizedPointAttos"), "PLAN_NORMALIZED_POINT_ATTOS"):
        fail("RESIDUAL_NORMALIZED_POINT_ATTOS_DRIFT")
    return evidence


def update_source(db: sqlite3.Connection, candidate: dict[str, Any], value: Any) -> None:
    if candidate["sourceKind"] in {"WEB_WALLET_BALANCE", "WEB_WALLET_LOCKED_BALANCE"}:
        field = candidate["sourceField"]
        if field not in {"balanceGXL", "lockedGXL"}:
            fail("INVALID_WALLET_SOURCE_FIELD")
        result = db.execute(
            f'update "Wallet" set "{field}"=? where "userId"=?',
            (value, candidate["sourceRecordId"]),
        )
    elif candidate["sourceKind"] == "WEB_TRANSACTION_AMOUNT":
        result = db.execute(
            'update "Transaction" set "amount"=? where "id"=?',
            (value, candidate["sourceRecordId"]),
        )
    else:
        fail("INVALID_RESIDUAL_SOURCE_KIND")
    if result.rowcount != 1:
        fail("RESIDUAL_SOURCE_UPDATE_COUNT_MISMATCH")


def original_hex(value: Any, storage_type: str) -> str:
    if storage_type == "real":
        return float(value).hex()
    if storage_type == "integer":
        return str(int(value))
    fail("UNSUPPORTED_RESIDUAL_STORAGE_TYPE")
    return ""


def restored_value(storage_type: str, encoded: str) -> Any:
    if storage_type == "real":
        return float.fromhex(encoded)
    if storage_type == "integer":
        return int(encoded)
    fail("UNSUPPORTED_RESIDUAL_STORAGE_TYPE")
    return None


def audit_row(value: tuple[Any, ...]) -> dict[str, Any]:
    if len(value) != len(AUDIT_COLUMN_ORDER):
        fail("POINT_RESIDUAL_AUDIT_ROW_SHAPE_MISMATCH")
    return dict(zip(AUDIT_COLUMN_ORDER, value, strict=True))


def verify_normalize_audit(expected: dict[str, Any], candidate: dict[str, Any], audit: dict[str, Any],
                           plan_sha256: str, approval_sha256: str) -> None:
    expected_values = {
        "eventId": event_id(expected["operationId"], expected["valueRef"], "NORMALIZE"),
        "operationId": expected["operationId"],
        "remediationPlanSha256": plan_sha256,
        "approvalSha256": approval_sha256,
        "accountRef": expected["accountRef"],
        "valueRef": expected["valueRef"],
        "sourceKind": expected["sourceKind"],
        "sourceRecordId": candidate["sourceRecordId"],
        "sourceField": expected["sourceField"],
        "sourcePointAttos": integer_text(expected.get("sourcePointAttos"), "PLAN_SOURCE_ATTOS"),
        "roundedPointMicros": integer_text(expected.get("roundedPointMicros"),
                                            "PLAN_ROUNDED_MICROS"),
        "residualPointAttos": integer_text(expected.get("residualPointAttos"),
                                            "PLAN_RESIDUAL_ATTOS"),
        "normalizedValueText": format(
            Decimal(integer_text(expected.get("roundedPointMicros"), "PLAN_ROUNDED_MICROS"))
            / POINT_MICROS,
            "f",
        ),
        "action": "NORMALIZE",
        "targetEventId": None,
    }
    for key, value in expected_values.items():
        if audit.get(key) != value:
            fail(f"NORMALIZE_AUDIT_{key.upper()}_MISMATCH")
    storage_type = str(audit.get("originalStorageType") or "")
    restored = restored_value(storage_type, str(audit.get("originalValueHex") or ""))
    restored_evidence = decimal_evidence(restored, "AUDIT_ORIGINAL_VALUE")
    text_evidence = decimal_evidence(str(audit.get("originalValueText") or ""),
                                     "AUDIT_ORIGINAL_VALUE_TEXT")
    for evidence in (restored_evidence, text_evidence):
        if evidence["sourcePointAttos"] != expected_values["sourcePointAttos"] or \
                evidence["roundedPointMicros"] != expected_values["roundedPointMicros"] or \
                evidence["residualPointAttos"] != expected_values["residualPointAttos"]:
            fail("NORMALIZE_AUDIT_ORIGINAL_VALUE_MISMATCH")
    parse_timestamp(str(audit.get("occurredAt") or ""))
    parse_timestamp(str(audit.get("createdAt") or ""))


def verify_rollback_audit(expected: dict[str, Any], normalize: dict[str, Any],
                          rollback: dict[str, Any], plan_sha256: str,
                          approval_sha256: str) -> None:
    copied_fields = (
        "operationId", "accountRef", "valueRef", "sourceKind", "sourceRecordId", "sourceField",
        "originalStorageType", "originalValueText", "originalValueHex", "sourcePointAttos",
        "roundedPointMicros", "residualPointAttos", "normalizedValueText",
    )
    for key in copied_fields:
        if rollback.get(key) != normalize.get(key):
            fail(f"ROLLBACK_AUDIT_{key.upper()}_MISMATCH")
    if rollback.get("eventId") != event_id(expected["operationId"], expected["valueRef"], "ROLLBACK") or \
            rollback.get("remediationPlanSha256") != plan_sha256 or \
            rollback.get("approvalSha256") != approval_sha256 or \
            rollback.get("action") != "ROLLBACK" or \
            rollback.get("targetEventId") != normalize.get("eventId"):
        fail("ROLLBACK_AUDIT_LINKAGE_MISMATCH")
    parse_timestamp(str(rollback.get("occurredAt") or ""))
    parse_timestamp(str(rollback.get("createdAt") or ""))


def apply_or_rollback(db: sqlite3.Connection, plan: dict[str, Any], plan_sha256: str,
                      approval_sha256: str, reference_key: str, action: str,
                      occurred_at: str) -> dict[str, Any]:
    residual_plans = validate_plan(plan)
    selected = selected_values(residual_plans)
    candidates = source_candidates(db, reference_key)
    for value in selected:
        candidate = candidates.get(value["valueRef"])
        if candidate is None:
            fail("APPROVED_RESIDUAL_SOURCE_MISSING")
        value["candidate"] = candidate

    ensure_audit_schema(db)
    normalize_rows = [audit_row(row) for row in db.execute(
        'select * from "PointLegacyResidualAudit" where "action"=\'NORMALIZE\' '
        'and "remediationPlanSha256"=? order by "valueRef"',
        (plan_sha256,),
    )]
    rollback_rows = [audit_row(row) for row in db.execute(
        'select * from "PointLegacyResidualAudit" where "action"=\'ROLLBACK\' '
        'and "remediationPlanSha256"=? order by "valueRef"',
        (plan_sha256,),
    )]
    if len(normalize_rows) not in (0, len(selected)) or len(rollback_rows) not in (0, len(selected)):
        fail("PARTIAL_RESIDUAL_AUDIT_STATE")

    events = []
    database_mutated = False
    if action == "apply":
        if rollback_rows:
            fail("RESIDUAL_NORMALIZATION_ALREADY_ROLLED_BACK")
        if normalize_rows:
            normalize_by_ref = {str(row["valueRef"]): row for row in normalize_rows}
            if len(normalize_by_ref) != len(selected):
                fail("DUPLICATE_NORMALIZE_AUDIT_VALUE_REF")
            for expected in selected:
                audit = normalize_by_ref.get(expected["valueRef"])
                if audit is None:
                    fail("NORMALIZE_AUDIT_VALUE_MISSING")
                verify_normalize_audit(
                    expected, expected["candidate"], audit, plan_sha256, approval_sha256
                )
                evidence = decimal_evidence(expected["candidate"]["value"], "IDEMPOTENT_NORMALIZED_VALUE")
                if evidence["roundedPointMicros"] != str(expected["roundedPointMicros"]) or \
                        evidence["residualPointAttos"] != "0":
                    fail("IDEMPOTENT_NORMALIZED_SOURCE_DRIFT")
            return {"idempotent": True, "databaseMutated": False, "events": []}
        for expected in selected:
            candidate = expected["candidate"]
            evidence = verify_candidate(expected, candidate)
            normalized = float(Decimal(evidence["roundedPointMicros"]) / POINT_MICROS)
            apply_event_id = event_id(expected["operationId"], expected["valueRef"], "NORMALIZE")
            db.execute(
                '''insert into "PointLegacyResidualAudit"
                ("eventId","operationId","remediationPlanSha256","approvalSha256","accountRef",
                 "valueRef","sourceKind","sourceRecordId","sourceField","originalStorageType",
                 "originalValueText","originalValueHex","sourcePointAttos","roundedPointMicros",
                 "residualPointAttos","normalizedValueText","action","targetEventId","occurredAt")
                values (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)''',
                (
                    apply_event_id, expected["operationId"], plan_sha256, approval_sha256,
                    expected["accountRef"], expected["valueRef"], candidate["sourceKind"],
                    candidate["sourceRecordId"], candidate["sourceField"], candidate["storageType"],
                    str(candidate["value"]), original_hex(candidate["value"], candidate["storageType"]),
                    evidence["sourcePointAttos"], evidence["roundedPointMicros"],
                    evidence["residualPointAttos"], evidence["normalizedValueText"], "NORMALIZE", None,
                    occurred_at,
                ),
            )
            update_source(db, candidate, normalized)
            after = {**candidate, "value": normalized}
            after_evidence = decimal_evidence(normalized, "NORMALIZED_LEGACY_VALUE")
            if after_evidence["roundedPointMicros"] != evidence["roundedPointMicros"] or \
                    after_evidence["residualPointAttos"] != "0":
                fail("NORMALIZED_SOURCE_POSTCONDITION_FAILED")
            database_mutated = True
            events.append({
                "eventRef": public_ref(reference_key, "residual-audit-event", apply_event_id),
                "operationId": expected["operationId"],
                "accountRef": expected["accountRef"],
                "valueRef": expected["valueRef"],
                "sourceKind": expected["sourceKind"],
                "sourceField": expected["sourceField"],
                "sourcePointAttos": evidence["sourcePointAttos"],
                "roundedPointMicros": evidence["roundedPointMicros"],
                "residualPointAttos": evidence["residualPointAttos"],
                "normalizationDeltaPointAttos": str(-int(evidence["residualPointAttos"])),
            })
    elif action == "rollback":
        if not normalize_rows:
            fail("RESIDUAL_NORMALIZATION_NOT_APPLIED")
        normalize_by_ref = {str(row["valueRef"]): row for row in normalize_rows}
        if len(normalize_by_ref) != len(selected):
            fail("DUPLICATE_NORMALIZE_AUDIT_VALUE_REF")
        for expected in selected:
            normalize = normalize_by_ref.get(expected["valueRef"])
            if normalize is None:
                fail("NORMALIZE_AUDIT_VALUE_MISSING")
            verify_normalize_audit(
                expected, expected["candidate"], normalize, plan_sha256, approval_sha256
            )
        if rollback_rows:
            rollback_by_ref = {str(row["valueRef"]): row for row in rollback_rows}
            if len(rollback_by_ref) != len(selected):
                fail("DUPLICATE_ROLLBACK_AUDIT_VALUE_REF")
            for expected in selected:
                normalize = normalize_by_ref[expected["valueRef"]]
                rollback = rollback_by_ref.get(expected["valueRef"])
                if rollback is None:
                    fail("ROLLBACK_AUDIT_VALUE_MISSING")
                verify_rollback_audit(
                    expected, normalize, rollback, plan_sha256, approval_sha256
                )
                current = decimal_evidence(
                    expected["candidate"]["value"], "IDEMPOTENT_ROLLBACK_CURRENT_VALUE"
                )
                if current["sourcePointAttos"] != str(normalize["sourcePointAttos"]) or \
                        current["residualPointAttos"] != str(normalize["residualPointAttos"]):
                    fail("IDEMPOTENT_ROLLBACK_SOURCE_DRIFT")
            return {"idempotent": True, "databaseMutated": False, "events": []}
        for expected in selected:
            candidate = expected["candidate"]
            audit = normalize_by_ref.get(expected["valueRef"])
            if audit is None:
                fail("NORMALIZE_AUDIT_VALUE_MISSING")
            current = decimal_evidence(candidate["value"], "ROLLBACK_CURRENT_VALUE")
            if current["roundedPointMicros"] != str(audit["roundedPointMicros"]) or \
                    current["residualPointAttos"] != "0":
                fail("ROLLBACK_CURRENT_SOURCE_DRIFT")
            restored = restored_value(
                str(audit["originalStorageType"]), str(audit["originalValueHex"])
            )
            update_source(db, candidate, restored)
            restored_evidence = decimal_evidence(restored, "ROLLBACK_RESTORED_VALUE")
            if restored_evidence["sourcePointAttos"] != str(audit["sourcePointAttos"]) or \
                    restored_evidence["residualPointAttos"] != str(audit["residualPointAttos"]):
                fail("ROLLBACK_RESTORED_SOURCE_MISMATCH")
            rollback_event_id = event_id(expected["operationId"], expected["valueRef"], "ROLLBACK")
            db.execute(
                '''insert into "PointLegacyResidualAudit"
                ("eventId","operationId","remediationPlanSha256","approvalSha256","accountRef",
                 "valueRef","sourceKind","sourceRecordId","sourceField","originalStorageType",
                 "originalValueText","originalValueHex","sourcePointAttos","roundedPointMicros",
                 "residualPointAttos","normalizedValueText","action","targetEventId","occurredAt")
                values (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)''',
                (
                    rollback_event_id, expected["operationId"], plan_sha256, approval_sha256,
                    expected["accountRef"], expected["valueRef"], candidate["sourceKind"],
                    candidate["sourceRecordId"], candidate["sourceField"],
                    str(audit["originalStorageType"]), str(audit["originalValueText"]),
                    str(audit["originalValueHex"]), str(audit["sourcePointAttos"]),
                    str(audit["roundedPointMicros"]), str(audit["residualPointAttos"]),
                    str(audit["normalizedValueText"]), "ROLLBACK", str(audit["eventId"]), occurred_at,
                ),
            )
            database_mutated = True
            events.append({
                "eventRef": public_ref(reference_key, "residual-audit-event", rollback_event_id),
                "operationId": expected["operationId"],
                "accountRef": expected["accountRef"],
                "valueRef": expected["valueRef"],
                "sourceKind": expected["sourceKind"],
                "sourceField": expected["sourceField"],
                "restoredSourcePointAttos": restored_evidence["sourcePointAttos"],
                "restoredResidualPointAttos": restored_evidence["residualPointAttos"],
            })
    else:
        fail("INVALID_RESIDUAL_ACTION")
    return {"idempotent": False, "databaseMutated": database_mutated, "events": events}


def write_receipt(path: str, receipt: dict[str, Any]) -> None:
    output = Path(path).resolve()
    flags = os.O_WRONLY | os.O_CREAT | os.O_EXCL
    descriptor = os.open(output, flags, 0o600)
    try:
        with os.fdopen(descriptor, "w", encoding="utf-8", newline="\n") as stream:
            json.dump(receipt, stream, ensure_ascii=True, indent=2)
            stream.write("\n")
    except Exception:
        try:
            output.unlink()
        except OSError:
            pass
        raise
    if os.name != "nt":
        os.chmod(output, 0o600)


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--database", required=True)
    parser.add_argument("--plan", required=True)
    parser.add_argument("--plan-sha256", required=True)
    parser.add_argument("--approval", required=True)
    parser.add_argument("--approval-sha256", required=True)
    parser.add_argument("--action", required=True, choices=("apply", "rollback"))
    parser.add_argument("--occurred-at", required=True)
    parser.add_argument("--output", required=True)
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    reference_key = os.environ.get("POINT_MIGRATION_REPORT_KEY", "")
    if len(reference_key.encode()) < 32:
        fail("POINT_MIGRATION_REPORT_KEY_TOO_SHORT")
    occurred_at = parse_timestamp(args.occurred_at)
    plan, plan_sha256 = file_json(args.plan, args.plan_sha256, "REMEDIATION_PLAN")
    approval, approval_sha256 = file_json(
        args.approval, args.approval_sha256, "OPERATION_APPROVAL"
    )
    validate_approval(approval, plan, approval_sha256, plan_sha256)
    database = Path(args.database).resolve(strict=True)
    if not database.is_file():
        fail("WEB_DATABASE_IS_NOT_A_FILE")
    db = sqlite3.connect(str(database), timeout=30, isolation_level=None)
    db.row_factory = sqlite3.Row
    try:
        db.execute("pragma foreign_keys=on")
        db.execute("pragma busy_timeout=30000")
        if db.execute("pragma foreign_key_check").fetchone() is not None:
            fail("WEB_DATABASE_FOREIGN_KEY_VIOLATION")
        db.execute("begin immediate")
        try:
            result = apply_or_rollback(
                db, plan, plan_sha256, approval_sha256, reference_key, args.action, occurred_at
            )
            if db.execute("pragma foreign_key_check").fetchone() is not None:
                fail("WEB_DATABASE_FOREIGN_KEY_VIOLATION_AFTER_OPERATION")
            db.commit()
        except Exception:
            db.rollback()
            raise
    finally:
        db.close()
    receipt = {
        "schemaVersion": 1,
        "generatedAt": occurred_at,
        "mode": "POINT_WALLET_RESIDUAL_NORMALIZATION_RECEIPT",
        "action": args.action.upper(),
        "idempotentReplay": result["idempotent"],
        "databaseMutationsPerformed": result["databaseMutated"],
        "containsRawIdentities": False,
        "sources": {
            "remediationPlanSha256": plan_sha256,
            "operationApprovalSha256": approval_sha256,
        },
        "summary": {
            "accountCount": plan["summary"]["residualAccountCount"],
            "valueCount": plan["summary"]["residualValueCount"],
            "totalResidualPointAttos": plan["summary"]["totalResidualPointAttos"],
            "normalizationDeltaPointAttos": plan["summary"]["normalizationDeltaPointAttos"],
        },
        "events": result["events"],
        "requiredNextGate": "FRESH_READ_ONLY_RECONCILIATION",
    }
    write_receipt(args.output, receipt)
    print(
        "[point-wallet-residual-normalization] "
        f"action={args.action} idempotent={str(result['idempotent']).lower()} "
        f"mutated={str(result['databaseMutated']).lower()} values={len(result['events'])}"
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main(sys.argv[1:]))
    except Exception as error:  # noqa: BLE001 - fail closed with a safe error code.
        print(f"[point-wallet-residual-normalization] {error}", file=sys.stderr)
        raise SystemExit(1)
