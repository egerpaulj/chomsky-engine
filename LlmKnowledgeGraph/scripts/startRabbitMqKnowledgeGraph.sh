docker run -e RABBITMQ_PORT=5672 \
           -e RABBITMQ_HOST=rabbitmq \
           -e RABBITMQ_VHOST=dev \
           -e RABBITMQ_QUEUE=DatapipelineCleanData \
           -e RABBITMQ_USER=guest \
           -e RABBITMQ_PASSWORD=guest \
           -e OLLAMA_MODEL=gemma3:12b \
           -e OLLAMA_HOST=http://ollama:11434 \
           -e NEO4J_URI=bolt://neo4j-apoc:7687 \
           -e NEO4J_USERNAME=neo4j \
           -e NEO4J_PASSWORD=password \
           -e MLFLOW_SYSTEM_PROMPT_ID=NER_System/@relationship \
           -e MLFLOW_USER_PROMPT_ID=NER_User/@relationship \
           -e MLFLOW_TRACKING_HOST=http://mlflow:5000 \
           --network development_network \
           --rm \
           --name knowledge_consumer \
           --hostname knowledge_consumer \
           rabbit-mq-graph-builder