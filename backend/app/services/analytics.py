from __future__ import annotations

import math
from datetime import date, timedelta
from typing import Iterable

import numpy as np
import pandas as pd

from .tsp_data import TSPDataService


TRADING_PERIODS = {"1m": 21, "3m": 63, "6m": 126, "1y": 252, "3y": 756, "5y": 1260}
CALENDAR_RANGES = {"1m": 31, "3m": 93, "6m": 186, "1y": 366, "3y": 1096, "5y": 1827, "10y": 3653}


def _safe_float(value: float | np.floating | None) -> float | None:
    if value is None or pd.isna(value) or math.isinf(float(value)):
        return None
    return float(value)


def _period_return(series: pd.Series, periods: int) -> float | None:
    if len(series) <= periods:
        return None
    return _safe_float(series.iloc[-1] / series.iloc[-periods - 1] - 1)


def _annualized_return(series: pd.Series, periods: int) -> float | None:
    if len(series) <= periods:
        return None
    years = periods / 252
    value = (series.iloc[-1] / series.iloc[-periods - 1]) ** (1 / years) - 1
    return _safe_float(value)


def _annualized_volatility(series: pd.Series, periods: int) -> float | None:
    returns = series.pct_change().dropna().tail(periods)
    if len(returns) < min(20, periods):
        return None
    return _safe_float(returns.std(ddof=1) * np.sqrt(252))


def _max_drawdown(series: pd.Series) -> float | None:
    if len(series) < 2:
        return None
    drawdown = series / series.cummax() - 1
    return _safe_float(drawdown.min())


def _current_drawdown(series: pd.Series) -> float | None:
    if series.empty:
        return None
    return _safe_float(series.iloc[-1] / series.max() - 1)


class AnalyticsService:
    def __init__(self, data: TSPDataService):
        self.data = data

    def history(self, fund_id: str, range_name: str = "1y") -> list[dict]:
        series = self.data.series(fund_id)
        if range_name != "all":
            days = CALENDAR_RANGES.get(range_name)
            if days is None:
                raise ValueError(f"Unsupported range '{range_name}'")
            cutoff = series.index.max() - timedelta(days=days)
            series = series.loc[series.index >= cutoff]
        return [{"date": index.date(), "value": round(float(value), 4)} for index, value in series.items()]

    def normalized_history(self, fund_ids: Iterable[str], range_name: str = "1y") -> list[dict]:
        series_map: dict[str, pd.Series] = {}
        for fund_id in fund_ids:
            series = self.data.series(fund_id)
            if range_name != "all":
                days = CALENDAR_RANGES.get(range_name)
                if days is None:
                    raise ValueError(f"Unsupported range '{range_name}'")
                cutoff = series.index.max() - timedelta(days=days)
                series = series.loc[series.index >= cutoff]
            series_map[fund_id] = series
        combined = pd.concat(series_map, axis=1, join="inner").dropna()
        if combined.empty:
            return []
        normalized = combined / combined.iloc[0] * 100
        records = []
        for index, row in normalized.iterrows():
            record: dict[str, object] = {"date": index.date()}
            record.update({fund_id: round(float(value), 3) for fund_id, value in row.items()})
            records.append(record)
        return records

    def metrics(self, fund_id: str) -> dict:
        series = self.data.series(fund_id)
        latest = float(series.iloc[-1])
        ma50 = _safe_float(series.tail(50).mean()) if len(series) >= 50 else None
        ma200 = _safe_float(series.tail(200).mean()) if len(series) >= 200 else None
        if ma50 is None or ma200 is None:
            trend = "insufficient history"
        elif latest > ma50 > ma200:
            trend = "strong upward trend"
        elif latest > ma200:
            trend = "upward trend"
        elif latest < ma50 < ma200:
            trend = "strong downward trend"
        elif latest < ma200:
            trend = "downward trend"
        else:
            trend = "mixed trend"
        daily_return = _period_return(series, 1)
        return {
            "fund_id": fund_id,
            "fund_name": self.data.fund_name(fund_id),
            "as_of": series.index[-1].date(),
            "latest_price": round(latest, 4),
            "daily_return": daily_return,
            "return_1m": _period_return(series, 21),
            "return_3m": _period_return(series, 63),
            "return_6m": _period_return(series, 126),
            "return_1y": _period_return(series, 252),
            "annualized_return_3y": _annualized_return(series, 756),
            "annualized_volatility_3m": _annualized_volatility(series, 63),
            "annualized_volatility_1y": _annualized_volatility(series, 252),
            "moving_average_50d": ma50,
            "moving_average_200d": ma200,
            "current_drawdown": _current_drawdown(series),
            "max_drawdown_1y": _max_drawdown(series.tail(252)),
            "max_drawdown_all": _max_drawdown(series),
            "trend": trend,
        }

    def portfolio(self, holdings: list[dict]) -> dict:
        fund_ids = [holding["fund_id"] for holding in holdings]
        weights = np.array([holding["weight"] / 100 for holding in holdings], dtype=float)
        series = pd.concat({fund_id: self.data.series(fund_id) for fund_id in fund_ids}, axis=1).dropna()
        returns = series.pct_change().dropna()
        if returns.empty:
            raise ValueError("The selected funds do not have overlapping history")
        portfolio_returns = returns.to_numpy() @ weights
        portfolio_curve = pd.Series((1 + portfolio_returns).cumprod(), index=returns.index)
        annual_return = float((portfolio_curve.iloc[-1] ** (252 / len(portfolio_curve))) - 1)
        annual_volatility = float(np.std(portfolio_returns, ddof=1) * np.sqrt(252))
        max_drawdown = _max_drawdown(portfolio_curve)
        trailing = {}
        for label, periods in TRADING_PERIODS.items():
            if label in {"3y", "5y"}:
                continue
            curve = portfolio_curve.tail(periods + 1)
            trailing[label] = float(curve.iloc[-1] / curve.iloc[0] - 1) if len(curve) > periods else None
        contributions = {}
        annualized_fund_returns = returns.mean() * 252
        for index, fund_id in enumerate(fund_ids):
            contributions[fund_id] = float(weights[index] * annualized_fund_returns[fund_id])
        return {
            "start_date": returns.index.min().date(),
            "as_of": returns.index.max().date(),
            "annualized_return": annual_return,
            "annualized_volatility": annual_volatility,
            "max_drawdown": max_drawdown,
            "trailing_returns": trailing,
            "return_contribution": contributions,
            "history": [
                {"date": index.date(), "value": round(float(value * 100), 3)}
                for index, value in portfolio_curve.tail(1260).items()
            ],
        }
