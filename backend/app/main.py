from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from pathlib import Path

from .api.routes import create_router
from .config import get_settings
from .services.analytics import AnalyticsService
from .services.news import NewsService
from .services.narrative import NarrativeService
from .services.recommendations import RecommendationService
from .services.tsp_data import TSPDataService

settings = get_settings()
data_service = TSPDataService(settings.data_file)
analytics_service = AnalyticsService(data_service)
recommendation_service = RecommendationService(data_service, analytics_service)
news_service = NewsService(settings.news_provider)
narrative_service = NarrativeService(settings.gemini_api_key, settings.gemini_model)

app = FastAPI(title=settings.app_name, version="0.1.0")
app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.cors_origin_list,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)
app.include_router(
    create_router(data_service, analytics_service, recommendation_service, news_service, narrative_service),
    prefix=settings.api_prefix,
)


@app.get("/health")
def health():
    return {"status": "ok", "data_as_of": data_service.latest_date()}


web_dir = Path(__file__).resolve().parents[1] / "web"
if web_dir.exists():
    app.mount("/", StaticFiles(directory=web_dir, html=True), name="web")
