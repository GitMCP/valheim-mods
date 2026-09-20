"""Regression test for Njord's localization registration timing."""

import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PLUGIN = ROOT / "src" / "NjordWarehouseKeeper" / "NjordWarehouseKeeperPlugin.cs"


def method_body(source: str, signature: str, next_signature: str) -> str:
    match = re.search(
        re.escape(signature) + r"(?P<body>.*?)\n\s*}\s*\n\s*" + re.escape(next_signature),
        source,
        re.DOTALL,
    )
    if not match:
        raise AssertionError(f"Could not find method {signature}")
    return match.group("body")


def main() -> None:
    source = PLUGIN.read_text(encoding="utf-8")
    awake = method_body(source, "private void Awake()", "private void Update()")
    register_content = method_body(
        source,
        "private void RegisterContent()",
        "private void OnDestroy()",
    )

    registration = "Localizations.Register();"
    assert registration in awake, (
        "Localization catalogs must be registered in Awake before Valheim loads "
        "the vanilla localization table."
    )
    assert registration not in register_content, (
        "Localization catalogs must not be registered from RegisterContent, "
        "which runs after the localization table has been built."
    )

    subscription = "PrefabManager.OnVanillaPrefabsAvailable += RegisterContent;"
    assert awake.index(registration) < awake.index(subscription), (
        "Localization registration must precede the content callback subscription."
    )

    print("Localization registration timing is valid.")


if __name__ == "__main__":
    main()
