docker run -e OLLAMA_MODEL=gemma3:12b \
           -e OLLAMA_HOST=http://ollama:11434 \
           -e MONGODB_CONNECTION_STRING=mongodb://mongodb:27017 \
           -e MLFLOW_SYSTEM_PROMPT_ID=NER_System/@entity \
           -e MLFLOW_USER_PROMPT_ID=NER_User/@entity \
           -e MLFLOW_TRACKING_HOST=http://mlflow:5000
           --network development_network \
           --rm \
           --name entity_extractor \
           --hostname entity_extractor \
           mongodb-entity-extractor