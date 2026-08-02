from __future__ import annotations

from datetime import datetime, timezone


class NewsService:
    """Provider boundary for the next build phase.

    A licensed market-news API, official economic feeds, or RSS adapters can be
    connected here without changing the dashboard contract.
    """

    def __init__(self, provider: str = "none"):
        self.provider = provider

    def latest(self) -> dict:
        return {
            "provider": self.provider,
            "enabled": self.provider != "none",
            "checked_at": datetime.now(timezone.utc).isoformat(),
            "events": [],
            "message": "News analysis is staged for the next build. Price and trend analysis remains fully available.",
        }
