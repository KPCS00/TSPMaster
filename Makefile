.PHONY: install run test daily

install:
	python -m pip install -r backend/requirements.txt

run:
	cd backend && uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload

test:
	cd backend && pytest -q

daily:
	cd backend && PYTHONPATH=. python scripts/run_daily.py
