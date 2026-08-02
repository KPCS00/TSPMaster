from unittest.mock import MagicMock, patch

from app.services.narrative import NarrativeService


def test_narrative_disabled():
    service = NarrativeService(api_key=None, model="gemini-3.5-flash")
    result = service.generate({"metric": "value"})
    assert result["enabled"] is False
    assert "GEMINI_API_KEY" in result["summary"]


@patch("google.genai.Client")
def test_narrative_enabled_mocked(mock_client_cls):
    mock_client = MagicMock()
    mock_response = MagicMock()
    mock_response.text = "This is a test Gemini narrative interpretation."
    mock_client.models.generate_content.return_value = mock_response
    mock_client_cls.return_value = mock_client

    service = NarrativeService(api_key="test-api-key", model="gemini-3.5-flash")
    result = service.generate({"fund": "C", "score": 85})

    assert result["enabled"] is True
    assert result["summary"] == "This is a test Gemini narrative interpretation."
    mock_client.models.generate_content.assert_called_once()
