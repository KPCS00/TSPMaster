from __future__ import annotations

from dataclasses import dataclass
from datetime import date
from pathlib import Path
from threading import RLock

import pandas as pd


FUND_MAP: dict[str, str] = {
    "g": "G Fund",
    "f": "F Fund",
    "c": "C Fund",
    "s": "S Fund",
    "i": "I Fund",
    "l-income": "L Income",
    "l-2030": "L 2030",
    "l-2035": "L 2035",
    "l-2040": "L 2040",
    "l-2045": "L 2045",
    "l-2050": "L 2050",
    "l-2055": "L 2055",
    "l-2060": "L 2060",
    "l-2065": "L 2065",
    "l-2070": "L 2070",
    "l-2075": "L 2075",
}

CORE_FUNDS = ["g", "f", "c", "s", "i"]


@dataclass(frozen=True)
class ValidationReport:
    rows: int
    start_date: date
    end_date: date
    duplicate_dates: int
    invalid_dates: int
    nonpositive_prices: int
    missing_values: dict[str, int]
    status: str


class TSPDataService:
    def __init__(self, path: Path):
        self.path = Path(path)
        self._lock = RLock()
        self._frame: pd.DataFrame | None = None
        self._modified_ns: int | None = None

    def _load(self) -> pd.DataFrame:
        if not self.path.exists():
            raise FileNotFoundError(f"TSP data file not found: {self.path}")
        modified_ns = self.path.stat().st_mtime_ns
        with self._lock:
            if self._frame is None or self._modified_ns != modified_ns:
                frame = pd.read_csv(self.path)
                if "Date" not in frame.columns:
                    raise ValueError("CSV must contain a Date column")
                missing_columns = [name for name in FUND_MAP.values() if name not in frame.columns]
                if missing_columns:
                    raise ValueError(f"CSV is missing expected fund columns: {missing_columns}")
                frame["Date"] = pd.to_datetime(frame["Date"], errors="coerce")
                numeric_columns = list(FUND_MAP.values())
                frame[numeric_columns] = frame[numeric_columns].apply(pd.to_numeric, errors="coerce")
                frame = frame.sort_values("Date").drop_duplicates("Date", keep="last").reset_index(drop=True)
                self._frame = frame
                self._modified_ns = modified_ns
            return self._frame.copy()

    def frame(self) -> pd.DataFrame:
        return self._load()

    def series(self, fund_id: str) -> pd.Series:
        column = self.fund_name(fund_id)
        frame = self._load().set_index("Date")
        return frame[column].dropna().astype(float)

    def fund_name(self, fund_id: str) -> str:
        try:
            return FUND_MAP[fund_id.lower()]
        except KeyError as exc:
            raise KeyError(f"Unknown fund '{fund_id}'") from exc

    def fund_id(self, fund_name: str) -> str:
        for fund_id, name in FUND_MAP.items():
            if name == fund_name:
                return fund_id
        raise KeyError(f"Unknown fund name '{fund_name}'")

    def funds_ids(self) -> list[str]:
        return list(FUND_MAP.keys())

    def funds(self) -> list[dict]:
        result = []
        for fund_id, name in FUND_MAP.items():
            series = self.series(fund_id)
            result.append(
                {
                    "id": fund_id,
                    "name": name,
                    "category": "lifecycle" if fund_id.startswith("l-") else "individual",
                    "start_date": series.index.min().date(),
                    "latest_date": series.index.max().date(),
                    "latest_price": round(float(series.iloc[-1]), 4),
                }
            )
        return result

    def validation_report(self) -> ValidationReport:
        raw = pd.read_csv(self.path)
        parsed_dates = pd.to_datetime(raw.get("Date"), errors="coerce")
        numeric = raw[[column for column in FUND_MAP.values() if column in raw.columns]].apply(
            pd.to_numeric, errors="coerce"
        )
        duplicate_dates = int(parsed_dates.dropna().duplicated().sum())
        invalid_dates = int(parsed_dates.isna().sum())
        nonpositive = int((numeric <= 0).sum().sum())
        status = "healthy" if duplicate_dates == 0 and invalid_dates == 0 and nonpositive == 0 else "warning"
        return ValidationReport(
            rows=len(raw),
            start_date=parsed_dates.min().date(),
            end_date=parsed_dates.max().date(),
            duplicate_dates=duplicate_dates,
            invalid_dates=invalid_dates,
            nonpositive_prices=nonpositive,
            missing_values={column: int(raw[column].isna().sum()) for column in FUND_MAP.values()},
            status=status,
        )

    def latest_date(self) -> date:
        return self._load()["Date"].max().date()
