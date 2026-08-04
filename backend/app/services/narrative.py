from __future__ import annotations

import json


class NarrativeService:
    def __init__(self, api_key: str | None, model: str):
        self.api_key = api_key
        self.model = model

    def generate(self, payload: dict) -> dict:
        if not self.api_key:
            return {
                "enabled": False,
                "summary": "AI narrative is disabled. Add GEMINI_API_KEY to generate a cited daily interpretation after a news provider is connected.",
            }
        try:
            from google import genai
            from google.genai import types

            client = genai.Client(api_key=self.api_key)
            response = client.models.generate_content(
                model=self.model,
                contents=json.dumps(payload, default=str),
                config=types.GenerateContentConfig(
                    system_instruction=(
                        "You are a cautious retirement-plan research assistant analyzing TSP fund metrics. "
                        "Explain only the supplied validated metrics. Never claim certainty, never instruct the user to trade, "
                        "and explicitly state that the analysis is informational decision-support only.\n\n"
                        "Format your response using structured Markdown for optimal readability:\n"
                        "- Use short, clear paragraphs separated by blank lines.\n"
                        "- Use section headers like `### Market Regime Overview`, `### Key Observations`, and `### Risk & Strategy Considerations`.\n"
                        "- Use bullet points (`- `) for key metrics and fund highlights.\n"
                        "- Bold important terms like fund names or key values (e.g. **C Fund**, **+12.4%**)."
                    ),
                ),
            )
            return {"enabled": True, "summary": response.text}
        except Exception as exc:  # The deterministic dashboard must remain available if AI fails.
            return {"enabled": True, "error": str(exc), "summary": "AI narrative was unavailable for this run."}
