from __future__ import annotations

from statistics import mean

from .analytics import AnalyticsService
from .tsp_data import CORE_FUNDS, TSPDataService


def _clip(value: float | None, low: float, high: float) -> float:
    if value is None:
        return 0.0
    return max(low, min(high, value))


class RecommendationService:
    """Transparent signal engine. It does not place trades or use an LLM for calculations."""

    def __init__(self, data: TSPDataService, analytics: AnalyticsService):
        self.data = data
        self.analytics = analytics

    def recommendation(self, fund_id: str) -> dict:
        metrics = self.analytics.metrics(fund_id)
        latest = metrics["latest_price"]
        ma50 = metrics["moving_average_50d"]
        ma200 = metrics["moving_average_200d"]

        momentum = (
            0.25 * _clip((metrics["return_1m"] or 0) / 0.08, -1, 1)
            + 0.30 * _clip((metrics["return_3m"] or 0) / 0.15, -1, 1)
            + 0.25 * _clip((metrics["return_6m"] or 0) / 0.25, -1, 1)
            + 0.20 * _clip((metrics["return_1y"] or 0) / 0.40, -1, 1)
        )
        trend50 = 1 if ma50 and latest > ma50 else -1
        trend200 = 1 if ma200 and latest > ma200 else -1
        volatility_penalty = _clip((metrics["annualized_volatility_3m"] or 0) / 0.35, 0, 1)
        drawdown_penalty = _clip(abs(metrics["current_drawdown"] or 0) / 0.25, 0, 1)

        raw_score = 50 + 30 * momentum + 7 * trend50 + 9 * trend200 - 6 * volatility_penalty - 7 * drawdown_penalty
        score = round(max(0, min(100, raw_score)), 1)

        if score >= 72:
            outlook, action = "strong positive", "Consider a modest overweight only if it fits your long-term allocation plan."
        elif score >= 60:
            outlook, action = "positive", "Maintain or gradually add within your target allocation range."
        elif score >= 45:
            outlook, action = "neutral", "Hold the strategic allocation and wait for a clearer trend."
        elif score >= 32:
            outlook, action = "cautious", "Review exposure and avoid increasing it until conditions improve."
        else:
            outlook, action = "defensive", "Consider reducing tactical exposure while preserving your long-term plan."

        drivers: list[str] = []
        risks: list[str] = []
        if metrics["return_3m"] is not None:
            direction = "gained" if metrics["return_3m"] >= 0 else "lost"
            drivers.append(f"The fund {direction} {abs(metrics['return_3m']) * 100:.1f}% over three months.")
        if ma200:
            relative = (latest / ma200 - 1) * 100
            target = drivers if relative >= 0 else risks
            target.append(f"Price is {abs(relative):.1f}% {'above' if relative >= 0 else 'below'} its 200-day average.")
        if metrics["annualized_volatility_3m"] is not None:
            vol = metrics["annualized_volatility_3m"] * 100
            target = risks if vol >= 18 else drivers
            target.append(f"Recent annualized volatility is {vol:.1f}%.")
        if metrics["current_drawdown"] is not None and metrics["current_drawdown"] < -0.03:
            risks.append(f"The fund is {abs(metrics['current_drawdown']) * 100:.1f}% below its historical high.")
        if not risks:
            risks.append("Trend signals can reverse and historical performance does not predict future returns.")

        signal_agreement = mean(
            [
                1 if (metrics["return_1m"] or 0) > 0 else 0,
                1 if (metrics["return_3m"] or 0) > 0 else 0,
                1 if (metrics["return_6m"] or 0) > 0 else 0,
                1 if trend50 > 0 else 0,
                1 if trend200 > 0 else 0,
            ]
        )
        confidence = round(0.45 + abs(signal_agreement - 0.5) * 0.8, 2)
        confidence = max(0.45, min(0.85, confidence))

        return {
            "fund_id": fund_id,
            "fund_name": self.data.fund_name(fund_id),
            "as_of": metrics["as_of"],
            "score": score,
            "outlook": outlook,
            "action": action,
            "confidence": confidence,
            "drivers": drivers[:3],
            "risks": risks[:3],
            "methodology": "Weighted momentum, 50/200-day trend, recent volatility, and drawdown. News is not yet included in this MVP score.",
        }

    def all_recommendations(self, core_only: bool = False) -> list[dict]:
        fund_ids = CORE_FUNDS if core_only else self.data.funds_ids()
        return sorted((self.recommendation(fund_id) for fund_id in fund_ids), key=lambda item: item["score"], reverse=True)

    def core_recommendations(self) -> list[dict]:
        return sorted((self.recommendation(fund_id) for fund_id in CORE_FUNDS), key=lambda item: item["score"], reverse=True)
