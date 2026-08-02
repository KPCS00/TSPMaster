from pathlib import Path

import pytest

from app.services.analytics import AnalyticsService
from app.services.recommendations import RecommendationService
from app.services.tsp_data import TSPDataService


DATA_FILE = Path(__file__).resolve().parents[1] / "data" / "tsp_share_prices.csv"


@pytest.fixture(scope="module")
def services():
    data = TSPDataService(DATA_FILE)
    analytics = AnalyticsService(data)
    recommendations = RecommendationService(data, analytics)
    return data, analytics, recommendations


def test_data_validation(services):
    data, _, _ = services
    report = data.validation_report()
    assert report.rows == 5783
    assert report.duplicate_dates == 0
    assert report.invalid_dates == 0
    assert report.nonpositive_prices == 0
    assert report.status == "healthy"


def test_core_fund_metrics(services):
    _, analytics, _ = services
    metrics = analytics.metrics("c")
    assert metrics["latest_price"] > 0
    assert metrics["moving_average_50d"] is not None
    assert metrics["moving_average_200d"] is not None
    assert -1 < metrics["return_1y"] < 2


def test_recommendation_is_bounded(services):
    _, _, recommendations = services
    result = recommendations.recommendation("s")
    assert 0 <= result["score"] <= 100
    assert 0 <= result["confidence"] <= 1
    assert result["drivers"]
    assert result["risks"]


def test_portfolio_weights_and_output(services):
    _, analytics, _ = services
    result = analytics.portfolio(
        [
            {"fund_id": "g", "weight": 20},
            {"fund_id": "f", "weight": 10},
            {"fund_id": "c", "weight": 45},
            {"fund_id": "s", "weight": 15},
            {"fund_id": "i", "weight": 10},
        ]
    )
    assert result["annualized_volatility"] >= 0
    assert result["max_drawdown"] <= 0
    assert result["history"]
