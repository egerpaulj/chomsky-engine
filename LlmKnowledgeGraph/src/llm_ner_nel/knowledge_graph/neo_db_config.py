import logging
import os


class NeoDbConfig:
    uri: str
    username: str
    password: str

    def __init__(self, **kwargs):
        self.uri = kwargs.get('uri', os.getenv('NEO4J_URI', 'bolt://localhost:7687'))
        self.username = kwargs.get('username', os.getenv('NEO4J_USERNAME', 'neo4j'))
        self.password = kwargs.get('password', os.getenv('NEO4J_PASSWORD', 'password'))
        logging.info(self.uri)