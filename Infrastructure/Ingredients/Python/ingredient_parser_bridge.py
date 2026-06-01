"""Bridge for Prepora to parse freehand ingredient lines via ingredient_parser_nlp."""

from __future__ import annotations

import json
from fractions import Fraction

from ingredient_parser import parse_ingredient


def _ensure_nltk_data() -> None:
    import nltk

    for resource in ("averaged_perceptron_tagger_eng",):
        try:
            nltk.data.find(f"taggers/{resource}")
        except LookupError:
            raise RuntimeError(
                "Missing NLTK resource "
                f"'{resource}'. Install it once in the parser environment with: "
                f"python -c \"import nltk; nltk.download('{resource}')\""
            )


def _quantity_to_float(value) -> float | None:
    if value is None:
        return None
    if isinstance(value, Fraction):
        return float(value)
    return float(value)


def _unit_to_str(unit) -> str | None:
    if unit is None or unit == "":
        return None
    return str(unit)


def parse_line(line: str) -> dict:
    parsed = parse_ingredient(line)

    name_parts = [part.text for part in (parsed.name or []) if part.text]
    name = " ".join(name_parts).strip() or None

    quantity = None
    unit = None
    if parsed.amount:
        amount = parsed.amount[0]
        quantity = _quantity_to_float(amount.quantity)
        unit = _unit_to_str(amount.unit)

    size = parsed.size.text.strip() if parsed.size and parsed.size.text else None

    notes: list[str] = []
    if parsed.preparation and parsed.preparation.text:
        notes.append(parsed.preparation.text.strip())
    if parsed.comment and parsed.comment.text:
        notes.append(parsed.comment.text.strip())
    note = ", ".join(notes) if notes else None

    return {
        "name": name,
        "quantity": quantity,
        "unit": unit,
        "size": size,
        "note": note,
        "branded": False,
    }


def parse_ingredients_json(lines_json: str) -> str:
    # _ensure_nltk_data()
    print("Lines JSON: ", lines_json)
    lines = json.loads(lines_json)
    ingredients: list[dict] = []
    errors: list[dict] = []

    for index, raw_line in enumerate(lines):
        line = (raw_line or "").strip()
        if not line:
            continue
        try:
            ingredients.append(parse_line(line))
        except Exception as exc:  # noqa: BLE001 - surface per-line failures to .NET
            errors.append({"index": index, "line": line, "error": str(exc)})

    return json.dumps({"ingredients": ingredients, "errors": errors})
