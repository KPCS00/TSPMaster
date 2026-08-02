from __future__ import annotations

from fastapi import APIRouter, HTTPException, Query

from ..schemas import PortfolioRequest
from ..services.analytics import AnalyticsService
from ..services.news import NewsService
from ..services.narrative import NarrativeService
from ..services.recommendations import RecommendationService
from ..services.tsp_data import CORE_FUNDS, TSPDataService


def create_router(
    data: TSPDataService,
    analytics: AnalyticsService,
    recommendations: RecommendationService,
    news: NewsService,
    narrative: NarrativeService,
) -> APIRouter:
    router = APIRouter()

    @router.get("/funds")
    def list_funds():
        return data.funds()

    @router.get("/funds/{fund_id}/history")
    def fund_history(fund_id: str, range: str = Query("1y")):
        try:
            return {"fund_id": fund_id, "range": range, "points": analytics.history(fund_id, range)}
        except (KeyError, ValueError) as exc:
            raise HTTPException(status_code=404 if isinstance(exc, KeyError) else 400, detail=str(exc)) from exc

    @router.get("/funds/{fund_id}/metrics")
    def fund_metrics(fund_id: str):
        try:
            return analytics.metrics(fund_id)
        except KeyError as exc:
            raise HTTPException(status_code=404, detail=str(exc)) from exc

    @router.get("/compare")
    def compare(funds: str = Query("g,f,c,s,i"), range: str = Query("1y")):
        fund_ids = [fund.strip().lower() for fund in funds.split(",") if fund.strip()]
        if not 1 <= len(fund_ids) <= 8:
            raise HTTPException(status_code=400, detail="Choose between one and eight funds")
        try:
            return {"funds": fund_ids, "range": range, "points": analytics.normalized_history(fund_ids, range)}
        except (KeyError, ValueError) as exc:
            raise HTTPException(status_code=400, detail=str(exc)) from exc

    @router.get("/recommendations")
    def get_recommendations(core_only: bool = True):
        if core_only:
            return recommendations.core_recommendations()
        return recommendations.all_recommendations()

    @router.get("/recommendations/{fund_id}")
    def get_recommendation(fund_id: str):
        try:
            return recommendations.recommendation(fund_id)
        except KeyError as exc:
            raise HTTPException(status_code=404, detail=str(exc)) from exc

    @router.get("/dashboard")
    def dashboard():
        recs = recommendations.core_recommendations()
        metrics = {fund_id: analytics.metrics(fund_id) for fund_id in CORE_FUNDS}
        above_200 = sum(
            1
            for metric in metrics.values()
            if metric["moving_average_200d"] and metric["latest_price"] > metric["moving_average_200d"]
        )
        if above_200 >= 4:
            regime = "broad positive trend"
        elif above_200 >= 2:
            regime = "mixed market trend"
        else:
            regime = "broad defensive trend"
        payload = {
            "as_of": data.latest_date(),
            "market_regime": regime,
            "funds_above_200d_average": above_200,
            "top_signal": recs[0],
            "recommendations": recs,
            "metrics": metrics,
            "comparison": analytics.normalized_history(CORE_FUNDS, "1y"),
            "data_quality": data.validation_report().__dict__,
            "news": news.latest(),
        }
        payload["narrative"] = narrative.generate(
            {
                "as_of": payload["as_of"],
                "market_regime": regime,
                "recommendations": recs,
                "news": payload["news"]["events"],
            }
        )
        return payload

    @router.post("/portfolio/analyze")
    def analyze_portfolio(request: PortfolioRequest):
        try:
            return analytics.portfolio([holding.model_dump() for holding in request.holdings])
        except (KeyError, ValueError) as exc:
            raise HTTPException(status_code=400, detail=str(exc)) from exc

    @router.get("/data-quality")
    def data_quality():
        return data.validation_report().__dict__

    @router.get("/news")
    def latest_news():
        return news.latest()

    return router
