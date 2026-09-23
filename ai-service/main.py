from fastapi import FastAPI
from pydantic import BaseModel
from textblob import TextBlob
import random

app = FastAPI(title="Mental Health AI Service")

class TextRequest(BaseModel):
    text: str


ADVICE_BY_SENTIMENT = {
    "positive": [
        "C'est merveilleux de voir que vous vous sentez bien ! Continuez à cultiver ces moments de joie.",
        "Votre positivité est une force. Prenez un instant pour savourer ce bien-être.",
        "Je suis ravi de vous sentir en forme. Notez ce qui vous rend heureux aujourd'hui.",
    ],
    "neutral": [
        "Prenez quelques minutes pour respirer profondément et vous recentrer.",
        "Une petite marche ou un verre d'eau peuvent aider à clarifier les idées.",
        "Essayez la technique 4-7-8 : inspirez 4s, retenez 7s, expirez 8s.",
    ],
    "negative": [
        "Je comprends que ce moment est difficile. Vous n'êtes pas seul(e).",
        "Essayez de nommer trois petites choses positives de votre journée.",
        "La respiration profonde peut aider. Inspirez 4s, retenez 4s, expirez 6s.",
        "Parler à un professionnel peut faire une grande différence. N'hésitez pas à contacter un docteur.",
    ],
}

def analyze_sentiment(text: str) -> str:
    blob = TextBlob(text)
    polarity = blob.sentiment.polarity
    if polarity > 0.1:
        return "positive"
    elif polarity < -0.1:
        return "negative"
    return "neutral"

@app.get("/")
def root():
    return {"status": "ok", "service": "mental-health-ai"}

@app.get("/health")
def health():
    return {"status": "healthy"}

@app.post("/sentiment")
def sentiment(req: TextRequest):
    return {"sentiment": analyze_sentiment(req.text)}

@app.post("/chat")
def chat(req: TextRequest):
    sentiment = analyze_sentiment(req.text)
    advice = random.choice(ADVICE_BY_SENTIMENT[sentiment])
    return {
        "response": advice,
        "sentiment": sentiment
    }