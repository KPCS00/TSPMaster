# MVP status

## Completed

- Imported and validated the supplied TSP historical CSV
- Built fund-history and normalized-comparison APIs
- Added return, volatility, moving-average, and drawdown analytics
- Added a transparent recommendation engine
- Added a five-fund portfolio analyzer
- Built a responsive PWA dashboard optimized around a 390×844 iPhone viewport
- Added home-screen metadata, service worker, and touch-friendly navigation
- Added optional AI narrative and news-provider boundaries
- Added Docker packaging and automated tests

## Verified

- 5,783 source rows
- May 31, 2003 through July 29, 2026
- Zero duplicate dates
- Zero invalid dates
- Zero nonpositive prices
- Four automated tests passing
- Main API endpoints return HTTP 200
- Dashboard, fund page, and portfolio analysis exercised in a mobile browser test

## Not yet implemented

- Automated daily retrieval of new TSP prices
- Live news feed and official economic-event ingestion
- News-aware scoring
- Authentication and persisted user settings
- Cloud deployment
- Push notifications
