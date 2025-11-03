from rabbitmq_knowledge_graph_builder import Consumer

import sys
import logging
import os

logging.basicConfig(
    level=logging.INFO,  # Set the minimum log level
    format='%(asctime)s - %(levelname)s - %(message)s',
)

if __name__ == '__main__':
    host = os.getenv('RABBITMQ_HOST', 'localhost')
    port = int(os.getenv('RABBITMQ_PORT', '5672'))
    vhost = os.getenv('RABBITMQ_VHOST', 'dev')
    queue = os.getenv('RABBITMQ_QUEUE', 'chomsky.info')
    username = os.getenv('RABBITMQ_USER', 'guest')
    password = os.getenv('RABBITMQ_PASSWORD', 'guest')
    ollama_host = os.getenv('OLLAMA_HOST', 'http://localhost:11434')
    ollama_model = os.getenv('OLLAMA_MODEL', 'llama3.2')
    mlflow_tracking_host = os.getenv('MLFLOW_TRACKING_HOST', 'http://localhost:5050')
    mlflow_system_prompt_id = os.getenv('MLFLOW_SYSTEM_PROMPT_ID', None)
    mlflow_user_prompt_id = os.getenv('MLFLOW_USER_PROMPT_ID', None)
        

    consumer = Consumer(
        host=host,
        port=port,
        virtual_host=vhost,
        queue_name=queue,
        username=username,
        password=password,
        ollama_host=ollama_host,
        ollama_model=ollama_model,
        mlflow_host=mlflow_tracking_host,
        mlflow_system_prompt_id=mlflow_system_prompt_id,
        mlflow_user_prompt_id=mlflow_user_prompt_id
    )
    try:
        consumer.connect()
        consumer.start_consuming()
    except Exception as e:
        logging.error(f"Fatal error: {e}")
        sys.exit(1)
