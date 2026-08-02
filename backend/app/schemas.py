from datetime import date
from typing import Literal

from pydantic import BaseModel, Field, field_validator


class FundInfo(BaseModel):
    id: str
    name: str
    category: Literal["individual", "lifecycle"]
    start_date: date
    latest_date: date
    latest_price: float


class PricePoint(BaseModel):
    date: date
    value: float


class MetricSet(BaseModel):
    fund_id: str
    fund_name: str
    as_of: date
    latest_price: float
    daily_return: float | None
    return_1m: float | None
    return_3m: float | None
    return_6m: float | None
    return_1y: float | None
    annualized_return_3y: float | None
    annualized_volatility_3m: float | None
    annualized_volatility_1y: float | None
    moving_average_50d: float | None
    moving_average_200d: float | None
    current_drawdown: float | None
    max_drawdown_1y: float | None
    max_drawdown_all: float | None
    trend: str


class Recommendation(BaseModel):
    fund_id: str
    fund_name: str
    as_of: date
    score: float = Field(ge=0, le=100)
    outlook: str
    action: str
    confidence: float = Field(ge=0, le=1)
    drivers: list[str]
    risks: list[str]
    methodology: str


class PortfolioHolding(BaseModel):
    fund_id: str
    weight: float = Field(ge=0, le=100)


class PortfolioRequest(BaseModel):
    holdings: list[PortfolioHolding]

    @field_validator("holdings")
    @classmethod
    def validate_holdings(cls, holdings: list[PortfolioHolding]) -> list[PortfolioHolding]:
        if not holdings:
            raise ValueError("At least one holding is required")
        ids = [holding.fund_id for holding in holdings]
        if len(ids) != len(set(ids)):
            raise ValueError("Each fund may only appear once")
        total = sum(holding.weight for holding in holdings)
        if abs(total - 100) > 0.01:
            raise ValueError(f"Portfolio weights must total 100; received {total:.2f}")
        return holdings
