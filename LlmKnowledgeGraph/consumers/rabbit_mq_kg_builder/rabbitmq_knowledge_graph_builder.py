#!/usr/bin/env python
import pika
import os
import json
from llm_ner_nel.inference_api.relationship_inference import RelationshipInferenceProvider
from llm_ner_nel.knowledge_graph.graph import KnowledgeGraph 

import logging
from prometheus_client import Counter, Summary, Gauge, start_http_server
import psutil

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
)

MESSAGE_COUNT = Counter('messages_processed_total', 'Total number of messages processed')
MESSAGE_PROCESSING_TIME = Summary('message_processing_seconds', 'Time spent processing a message')
CPU_USAGE = Gauge('process_cpu_percent', 'Process CPU usage percent')
MEMORY_USAGE = Gauge('process_memory_mb', 'Process memory usage in MB')

class Consumer:
    def __init__(self, host, port, virtual_host, queue_name, username, password, 
                 ollama_host, ollama_model, 
                 mlflow_host, mlflow_system_prompt_id, mlflow_user_prompt_id):
        self.host = host
        self.port = port
        self.virtual_host = virtual_host
        self.queue_name = queue_name
        self.username = username
        self.password = password
        
        self.relationships_extractor = RelationshipInferenceProvider(
            model=ollama_model,  
            ollama_host=ollama_host, 
            mlflow_tracking_host=mlflow_host, 
            mlflow_system_prompt_id = mlflow_system_prompt_id, 
            mlflow_user_prompt_id = mlflow_user_prompt_id)
        
        self.graph=KnowledgeGraph()
        self.connection = None
        self.channel = None
        
        logging.info(ollama_host)

    def connect(self, max_retries=5, base_wait=2):
        attempt = 0
        while True:
            try:
                credentials = pika.PlainCredentials(self.username, self.password)
                self.connection = pika.BlockingConnection(
                    pika.ConnectionParameters(
                        host=self.host,
                        port=self.port,
                        virtual_host=self.virtual_host,
                        credentials=credentials,
                        heartbeat=600,
                        blocked_connection_timeout=300,
                        connection_attempts=3,
                        retry_delay=5,
                    )
                )
                self.channel = self.connection.channel()
                logging.info("Connected to RabbitMQ.")
                break
            except (pika.exceptions.AMQPConnectionError, pika.exceptions.StreamLostError, ConnectionResetError) as e:
                attempt += 1
                wait_time = base_wait * (2 ** (attempt - 1))
                logging.error(f"Connection error: {e}. Retrying in {wait_time} seconds (attempt {attempt}/{max_retries})...")
                if attempt >= max_retries:
                    logging.error("Max retries reached. Exiting.")
                    raise
                import time
                time.sleep(wait_time)

    def start_consuming(self):
        start_http_server(7777)
        logging.info("Prometheus metrics exposed on port 7777.")

        def callback(ch, method, properties, body):
            MESSAGE_COUNT.inc()
            with MESSAGE_PROCESSING_TIME.time():
                try:
                    message = json.loads(body)
                    src = message.get('src')
                    src_type = message.get('src_type')
                    text = message.get('text')
                    logging.info(f"[x] Received: src={src}, text={text}")
                    relationships = self.relationships_extractor.get_relationships(text=text)
                    
                    
                    self.graph.add_or_merge_relationships(relationships, src=src, src_type=src_type)
                    ch.basic_ack(delivery_tag=method.delivery_tag)
                except Exception as e:
                    logging.error(f"[!] Error processing message: {e}")
                    ch.basic_nack(delivery_tag=method.delivery_tag)

        # Monitor CPU and memory usage in the background
        import threading
        def monitor_resources():
            process = psutil.Process(os.getpid())
            while True:
                try:
                    cpu = process.cpu_percent(interval=1)
                    mem = process.memory_info().rss / (1024 * 1024)  # MB
                    CPU_USAGE.set(cpu)
                    MEMORY_USAGE.set(mem)
                except Exception as e:
                    logging.error(f"Error updating resource metrics: {e}")
                import time
                time.sleep(5)

        t = threading.Thread(target=monitor_resources, daemon=True)
        t.start()

        while True:
            try:
                if self.channel is None:
                    raise Exception("Channel is not initialized. Call connect() first.")
                self.channel.basic_qos(prefetch_count=1)
                self.channel.basic_consume(
                    queue=self.queue_name,
                    on_message_callback=callback,
                    auto_ack=False
                )
                logging.info(' [*] Waiting for messages. To exit press CTRL+C')
                self.channel.start_consuming()
            except (pika.exceptions.StreamLostError, pika.exceptions.AMQPConnectionError, ConnectionResetError) as e:
                logging.error(f"Connection lost during consuming: {e}. Reconnecting...")
                import time
                time.sleep(5)
                self.connect()
            except KeyboardInterrupt:
                logging.info('Interrupted')
                break