FROM python:3.10-slim

WORKDIR /app

COPY . .

RUN pip install --upgrade pip
RUN pip install -r requirements.txt


ENV OLLAMA_HOST=http://localhost:11434
ENV OLLAMA_MODEL=default-model
ENV MONGODB_CONNECTION_STRING=mongodb://localhost:27017
ENV MLFLOW_TRACKING_HOST=http://localhost:5050
ENV MLFLOW_SYSTEM_PROMPT_ID=NER_System/@entity
ENV MLFLOW_USER_PROMPT_ID=NER_User/@entity



EXPOSE 7777

CMD ["python", "main.py "]
