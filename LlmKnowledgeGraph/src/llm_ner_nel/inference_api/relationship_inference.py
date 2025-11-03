from llm_ner_nel.core.dto import Relationships
from llm_ner_nel.inference_api.prompts import default_entity_recognition_system_prompt, default_entity_recognition_user_prompt
from llm_ner_nel.inference_api.llm_config import LlmConfig
from ollama import Client
import logging
import mlflow


def display_relationships(relationships : Relationships, console_log: bool) -> None:
    logging.info(relationships.topic)
    for rel in relationships.relationships:
        message = f"{rel.head}:({rel.head_confidence}) ({rel.head_type}) --[{rel.relation}]-> {rel.tail}:({rel.tail_confidence}) ({rel.tail_type})"
        if(console_log):
            print(message)
        logging.info(message)
        

class RelationshipInferenceProvider:
    model: str
    ollama_host: str
    llm_config: LlmConfig
    mlflow_tracking_host: str
    mlflow_system_prompt_id: str
    mlflow_user_prompt_id: str
    
    def __init__(self, **kwargs):
        self.model = kwargs.get('model', "llama3.2")
        self.llm_config = LlmConfig(model=self.model,
                       temperature = 0.5,
                       top_p = 0.3,
                       typical_p = 0.9,
                       top_k = 50,
                       max_tokens = 256,
                       repeat_penalty = 1.2,
                       frequency_penalty = 0.1,
                       presence_penalty = 0.1,
                       num_thread = 16
                       )
        self.ollama_host =  kwargs.get('ollama_host', "http://localhost:11434")
        self.mlflow_tracking_host =  kwargs.get('mlflow_tracking_host', "http://localhost:5050")
        self.mlflow_system_prompt_id =  kwargs.get('mlflow_system_prompt_id', None)
        self.mlflow_user_prompt_id =  kwargs.get('mlflow_user_prompt_id', None)
        self.client=Client(host=self.ollama_host)
        mlflow.set_tracking_uri(self.mlflow_tracking_host)

    def inference(self, prompt: str, system: str) -> Relationships:
        options = {
            "top_k": self.llm_config.top_k,
            "top_p": self.llm_config.top_p,
            "max_tokens": self.llm_config.max_tokens,
            "temperature": self.llm_config.temperature,
            "repeat_penalty": self.llm_config.repeat_penalty,
            "frequency_penalty": self.llm_config.frequency_penalty,
            "typical_p": self.llm_config.typical_p,
            "num_thread": self.llm_config.num_thread,
        }
        logging.info(self.ollama_host)
        
        response = self.client.generate(
                                prompt=prompt,
                                system=system,
                                model=self.llm_config.model,
                                format=Relationships.model_json_schema(),
                                options = options)
        return Relationships.model_validate_json(response.response)

    def generate_prompt(self, text):
        if(self.mlflow_user_prompt_id is None):
            return default_entity_recognition_user_prompt(text=text)
                    
        return mlflow.genai.load_prompt(f"prompts:/{self.mlflow_user_prompt_id}").format(text=text)
    
    def generate_system_prompt(self):
        if(self.mlflow_user_prompt_id is None):
            return default_entity_recognition_system_prompt
        
        return mlflow.genai.load_prompt(f"prompts:/{self.mlflow_system_prompt_id}").format()

    def get_relationships(self, text: str) -> Relationships:
        cleaned = text
        prompt = self.generate_prompt(cleaned)
        system_prompt = self.generate_system_prompt();

        return self.inference(
            prompt=prompt,
            system=system_prompt)
        

    

    
