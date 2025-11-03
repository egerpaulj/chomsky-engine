from llm_ner_nel.core.dto import Entities
from llm_ner_nel.inference_api.prompts import default_entity_recognition_system_prompt, default_entity_recognition_user_prompt
from llm_ner_nel.inference_api.llm_config import LlmConfig
from ollama import Client
import logging
from typing import List
import mlflow

def display_entities(entities : Entities, console_log: bool) -> None:
    for entity in entities.entities:
        message = f"{entity.name}-[{entity.type}]. ({entity.condifence})"
        logging.info(message)
        
        if(console_log):
            print(message)
        
        
def get_unique_entity_names(entities: Entities) -> List[str]:
    unique_nodes = set()
    for entity in entities.entities:
        unique_nodes.add(entity.name)
        
    return list(unique_nodes)


class EntityInferenceProvider:
    model: str
    ollama_host: str
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



    def inference(self, prompt: str, system: str) -> Entities:
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
                                format=Entities.model_json_schema(),
                                options = options)
        return Entities.model_validate_json(response.response)

    def generate_prompt(self, text):
        if(self.mlflow_user_prompt_id is None):
            return default_entity_recognition_user_prompt(text=text)
                    
        return mlflow.genai.load_prompt(f"prompts:/{self.mlflow_user_prompt_id}").format(text=text)
    
    def generate_system_prompt(self):
        if(self.mlflow_user_prompt_id is None):
            return default_entity_recognition_system_prompt
        
        return mlflow.genai.load_prompt(f"prompts:/{self.mlflow_system_prompt_id}").format()

    def get_entities(self, text: str) -> Entities:
        cleaned = text
        prompt = self.generate_prompt(cleaned)
        system_prompt = self.generate_system_prompt();

        return self.inference(
            prompt=prompt,
            system=system_prompt)
        

    

    
