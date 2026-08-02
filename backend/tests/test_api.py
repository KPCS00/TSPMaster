import pytest
from fastapi.testclient import TestClient
from app.main import app

client = TestClient(app)


def test_health():
    response = client.get("/health")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "ok"
    assert "data_as_of" in data


def test_dashboard_endpoint():
    response = client.get("/api/v1/dashboard")
    assert response.status_code == 200
    data = response.json()
    assert "as_of" in data
    assert "market_regime" in data
    assert "recommendations" in data
    assert "metrics" in data
    assert "comparison" in data
    assert "data_quality" in data


def test_funds_endpoint():
    response = client.get("/api/v1/funds")
    assert response.status_code == 200
    data = response.json()
    assert len(data) > 0


def test_fund_history():
    response = client.get("/api/v1/funds/c/history?range=1y")
    assert response.status_code == 200
    data = response.json()
    assert "points" in data
    assert len(data["points"]) > 0


def test_fund_metrics():
    response = client.get("/api/v1/funds/c/metrics")
    assert response.status_code == 200
    data = response.json()
    assert "latest_price" in data


def test_compare():
    response = client.get("/api/v1/compare?funds=g,f,c&range=1y")
    assert response.status_code == 200
    data = response.json()
    assert "points" in data


def test_recommendations():
    response = client.get("/api/v1/recommendations")
    assert response.status_code == 200
    data = response.json()
    assert isinstance(data, list)


def test_portfolio_analyze():
    payload = {
        "holdings": [
            {"fund_id": "g", "weight": 20},
            {"fund_id": "f", "weight": 10},
            {"fund_id": "c", "weight": 45},
            {"fund_id": "s", "weight": 15},
            {"fund_id": "i", "weight": 10},
        ]
    }
    response = client.post("/api/v1/portfolio/analyze", json=payload)
    assert response.status_code == 200
    data = response.json()
    assert "annualized_volatility" in data


def test_data_quality():
    response = client.get("/api/v1/data-quality")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "healthy"


def test_news():
    response = client.get("/api/v1/news")
    assert response.status_code == 200
    data = response.json()
    assert "events" in data


def test_static_index():
    response = client.get("/")
    assert response.status_code == 200
    assert "TSPMaster" in response.text
