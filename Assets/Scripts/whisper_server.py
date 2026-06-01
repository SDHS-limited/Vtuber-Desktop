from flask import Flask, request, jsonify
import whisper
import os

app = Flask(__name__)
model = whisper.load_model("small")  # tiny / small / medium / large

@app.route("/transcribe", methods=["POST"])
def transcribe():
    if "file" not in request.files:
        return jsonify({"error": "no file"}), 400

    audio = request.files["file"]
    audio.save("temp.wav")

    result = model.transcribe("temp.wav", language="ko")
    text = result["text"].strip()

    print(f"인식됨: {text}")
    return jsonify({"text": text})

if __name__ == "__main__":
    print("Whisper 로컬 서버 시작 (포트 5000)")
    app.run(host="127.0.0.1", port=5000)