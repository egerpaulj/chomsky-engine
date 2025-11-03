from pydantic import BaseModel, Field
from typing import List


    
class Ner(BaseModel):
    name: str = Field(
        description=(
            "extracted entity name like Microsoft, Apple, Barrack Obama. "
            "Must use human-readable unique identifier."
        )
    )
    type: str = Field(
        description="type of the extracted head entity like Person, Company, etc"
    )
    condifence: float = Field(
        description="confidence score between 0 and 1 for the head entity"
    )
    
class Entities(BaseModel):
    entities: List[Ner]
    


class NerNel(BaseModel):
    head: str = Field(
        description=(
            "extracted head entity like Microsoft, Apple, John. "
            "Must use human-readable unique identifier."
        )
    )
    head_confidence: float = Field(description="confidence score between 0 and 1 for the head entity")

    head_type: str = Field(
        description="type of the extracted head entity like Person, Company, etc"
    )
    relation: str = Field(description="relation between the head and the tail entities")
    relation_confidence: float = Field(description="confidence score of the relationship")
    tail: str = Field(
        description=(
            "extracted tail entity like Microsoft, Apple, John. "
            "Must use human-readable unique identifier."
        )
    )
    tail_confidence: float = Field(description="confidence score between 0 and 1 for the tail entity")
    tail_type: str = Field(
        description="type of the extracted tail entity like Person, Company, etc"
    )
    
class Relationships(BaseModel):
    topic: str
    relationships: List[NerNel]