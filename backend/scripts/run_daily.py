from __future__ import annotations

import json
from pathlib import Path

from app.config import get_settings
from app.services.analytics import AnalyticsService
from app.services.news import NewsService
from app.services.recommendations import RecommendationService
from app.services.tsp_data import TSPDataService


def main() -> None:
    settings = get_settings()
    data = TSPDataService(settings.data_file)
    analytics = AnalyticsService(data)
    recommendations = RecommendationService(data, analytics)
    report = {
        "as_of": str(data.latest_date()),
        "recommendations": recommendations.core_recommendations(),
        "news": NewsService(settings.news_provider).latest(),
        "data_quality": data.validation_report().__dict__,
    }
    output = Path(__file__).resolve().parents[1] / "data" / "daily_report.json"
    output.write_text(json.dumps(report, indent=2, default=str), encoding="utf-8")
    print(f"Wrote {output}")


if __name__ == "__main__":
    main()
