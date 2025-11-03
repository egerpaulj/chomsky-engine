FROM python:3.10-slim

WORKDIR /app

COPY . .

RUN pip install --upgrade pip
RUN pip install -r requirements.txt


ENV RABBITMQ_HOST=localhost
ENV RABBITMQ_PORT=5672
ENV RABBITMQ_VHOST=/
ENV RABBITMQ_QUEUE=default
ENV RABBITMQ_USER=guest
ENV RABBITMQ_PASSWORD=guest
ENV OLLAMA_HOST=http://localhost:11434
ENV OLLAMA_MODEL=default-model
ENV NEO4J_URI=neo4j://localhost:7687
ENV NEO4J_USERNAME=neo4j
ENV NEO4J_PASSWORD=password
ENV MLFLOW_TRACKING_HOST=http://localhost:5050
ENV MLFLOW_SYSTEM_PROMPT_ID=NER_System/@relationship
ENV MLFLOW_USER_PROMPT_ID=NER_User/@relationship



EXPOSE 7777

CMD ["python", "main.py "]
