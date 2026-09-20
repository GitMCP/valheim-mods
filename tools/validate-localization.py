#!/usr/bin/env python3
"""Validate embedded Valheim localization catalogs."""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any


PLACEHOLDER = re.compile(r"\{\d+\}")


class DuplicateKeyError(ValueError):
    pass


def object_pairs(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        if key in result:
            raise DuplicateKeyError(f"duplicate key '{key}'")
        result[key] = value
    return result


def load_catalog(path: Path) -> dict[str, str]:
    try:
        value = json.loads(
            path.read_text(encoding="utf-8-sig"),
            object_pairs_hook=object_pairs,
        )
    except (OSError, json.JSONDecodeError, DuplicateKeyError) as error:
        raise ValueError(f"{path.name}: invalid JSON: {error}") from error

    if not isinstance(value, dict) or not value:
        raise ValueError(f"{path.name}: expected a non-empty JSON object")

    invalid = [
        key
        for key, item in value.items()
        if not isinstance(key, str) or not isinstance(item, str)
    ]
    if invalid:
        raise ValueError(
            f"{path.name}: keys and values must be strings: {', '.join(map(str, invalid))}"
        )
    return value


def placeholders(value: str) -> tuple[str, ...]:
    return tuple(sorted(PLACEHOLDER.findall(value)))


def validate(directory: Path, reference_name: str) -> list[str]:
    if not directory.is_dir():
        raise ValueError(f"localization directory does not exist: {directory}")

    catalogs = sorted(directory.glob("*.json"))
    if not catalogs:
        raise ValueError(f"no localization catalogs found in {directory}")

    reference_path = directory / reference_name
    if not reference_path.is_file():
        raise ValueError(f"reference catalog not found: {reference_path}")

    loaded = {path.name: load_catalog(path) for path in catalogs}
    reference = loaded[reference_name]
    reference_keys = set(reference)
    errors: list[str] = []

    for name, catalog in loaded.items():
        missing = sorted(reference_keys - set(catalog))
        extra = sorted(set(catalog) - reference_keys)
        if missing:
            errors.append(f"{name}: missing keys: {', '.join(missing)}")
        if extra:
            errors.append(f"{name}: unexpected keys: {', '.join(extra)}")

        for key in sorted(reference_keys & set(catalog)):
            expected = placeholders(reference[key])
            actual = placeholders(catalog[key])
            if expected != actual:
                errors.append(
                    f"{name}:{key}: placeholders {actual!r}, expected {expected!r}"
                )

    return errors


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    parser.add_argument("--reference", default="English.json")
    args = parser.parse_args()

    try:
        errors = validate(args.directory, args.reference)
    except ValueError as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    count = len(list(args.directory.glob("*.json")))
    print(f"Validated {count} localization catalog(s) against {args.reference}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
