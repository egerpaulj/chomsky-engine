default_entity_recognition_system_prompt = """
You are an expert in named entity recognition. Your task is to perform  named entity recognition on news articles and identify entities.

# Rules

## Entity Name
- Always use the full name of the entity. Attempt to deduce the full name based on the context.  For example, use "Barack Obama" instead of just "Obama" or "Barack".
- Do not use the sentence as the entity name. Instead map the sentence to a relevant entity.
- Limit the number of words in the entity to 5. It should not be a sentence nor phrase.
- If the name is obvious, then use the full name of the individual.
  Original Text: "John Jones bought some crickets. His bought items were amazing. John was happy. Obama was happy with John"
  Entity: "John Jones", "Barrack Obama"
  """
  
default_knowledge_graph_system_prompt = """
You are an expert in knowledge graphs. Your task is to perform  named entity recognition on news articles and build a knowledge graph.
 
Define a short topic for the text which will be used as the parent node.

# Rules

## Entity Name
- Always use the full name of the entity. Attempt to deduce the full name based on the context.  For example, use "Barack Obama" instead of just "Obama" or "Barack".
- Do not use the sentence as the entity name. Instead map the sentence to a relevant entity.
- Limit the number of words in the entity to 5. It should not be a sentence nor phrase.
- Entities are also known as Head and Tail\n.
- If the name is obvious, then use the full name of the individual.
  Original Text: "John Jones bought some crickets. His bought items were amazing. John was happy. Obama was happy with John"
  Entity: "John Jones", "Barrack Obama"

## Relationship
- Identify relationships between entities.
- Define simple relationships that are easy to understand.
- The entity should not be part of the relationship text.
- See examples below:\n

# Validation
- After generation verify the results and calculate a confidence score between 0 and 1.
- Ensure that the extracted entities and relationships are accurate and relevant to the text.
  """
  
def default_entity_recognition_user_prompt(text: str) -> str:
    return f"""
Based on the following example, extract entities from the provided text.
Your task is to extract entities from text. 

Below are a number of examples of text and their extracted entities.  Entites should have the full name. Focus on extracting entities.

# Examples: 

"example": (
        Text:   "Authorities in Israel and Gaza are preparing for the release of Israeli hostages and Palestinian prisoners ahead of a Monday deadline for the swap stipulated in the ceasefire deal that could end the two-year war in Gaza."
            "Hamas is meant to release all living hostages from Gaza within 72 hours of the signing of the deal – a deadline that ends at noon local time (10am UK time). The militant group holds 48 hostages, 20 of whom are believed to be alive."
        )
        [
    Ner(
        name="Israel",
        type="Country",
        condifence=1.0,
    ),
    Ner(
        name="Hamas",
        type="Organization",
        condifence=1.0,
    ),
    Ner(
        name="Palastine",
        type="Country",
        condifence=1.0,
    )
]   

Extract this: {text}"""


def default_knowledge_graph_user_prompt(text: str) -> str:
    return f"""
Based on the following example, extract entities and relations from the provided text.
Your task is to extract relationships from text. 
The relationships can only appear between specific node types are presented in the schema format like: 
(Entity1Type, RELATIONSHIP_TYPE, Entity2Type)

Below are a number of examples of text and their extracted entities and relationships. 
Entites should have the full name.

# Examples: 

"example": (
        Text:   "Authorities in Israel and Gaza are preparing for the release of Israeli hostages and Palestinian prisoners ahead of a Monday deadline for the swap stipulated in the ceasefire deal that could end the two-year war in Gaza."
            "Hamas is meant to release all living hostages from Gaza within 72 hours of the signing of the deal – a deadline that ends at noon local time (10am UK time). The militant group holds 48 hostages, 20 of whom are believed to be alive."
        )
        [
    NerNel(
        head="Authorities in Israel and Gaza",
        head_type="Organization",
        head_condifence=0.92,
        relation="preparing for",
        relation_confidence=0.95,
        tail="Release of Hostages and Prisoners",
        tail_type="Event",
        tail_condifence=0.93
    ),
    NerNel(
        head="Release of Hostages and Prisoners",
        head_type="Event",
        head_condifence=0.93,
        relation="includes",
        relation_confidence=0.95,
        tail="Israeli Hostages",
        tail_type="Group",
        tail_condifence=0.94
    ),
    NerNel(
        head="Release of Hostages and Prisoners",
        head_type="Event",
        head_condifence=0.93,
        relation="includes",
        relation_confidence=0.95,
        tail="Palestinian Prisoners",
        tail_type="Group",
        tail_condifence=0.94
    ),
    NerNel(
        head="Release of Hostages and Prisoners",
        head_type="Event",
        head_condifence=0.93,
        relation="stipulated in",
        relation_confidence=0.96,
        tail="Ceasefire Deal",
        tail_type="Agreement",
        tail_condifence=0.95
    ),
    NerNel(
        head="Ceasefire Deal",
        head_type="Agreement",
        head_condifence=0.95,
        relation="aims to end",
        relation_confidence=0.97,
        tail="Two-year War in Gaza",
        tail_type="Conflict",
        tail_condifence=0.94
    ),
    NerNel(
        head="Ceasefire Deal",
        head_type="Agreement",
        head_condifence=0.95,
        relation="includes clause",
        relation_confidence=0.90,
        tail="Swap",
        tail_type="Event",
        tail_condifence=0.92
    ),
    NerNel(
        head="Swap",
        head_type="Event",
        head_condifence=0.92,
        relation="requires Hamas to release",
        relation_confidence=0.96,
        tail="48 Hostages",
        tail_type="Group",
        tail_condifence=0.93
    ),
    NerNel(
        head="48 Hostages",
        head_type="Group",
        head_condifence=0.93,
        relation="held by",
        relation_confidence=0.98,
        tail="Hamas",
        tail_type="Organization",
        tail_condifence=0.96
    ),
    NerNel(
        head="48 Hostages",
        head_type="Group",
        head_condifence=0.93,
        relation="subset believed to be",
        relation_confidence=0.92,
        tail="20 Alive Hostages",
        tail_type="Group",
        tail_condifence=0.91
    ),
    NerNel(
        head="Swap",
        head_type="Event",
        head_condifence=0.92,
        relation="requires Israel to release",
        relation_confidence=0.95,
        tail="Palestinian Prisoners",
        tail_type="Group",
        tail_condifence=0.94
    ),
    NerNel(
        head="48 Hostages",
        head_type="Group",
        head_condifence=0.93,
        relation="release deadline",
        relation_confidence=0.94,
        tail="72 hours after deal signing",
        tail_type="Time",
        tail_condifence=0.90
    ),
    NerNel(
        head="72 hours after deal signing",
        head_type="Time",
        head_condifence=0.90,
        relation="ends at",
        relation_confidence=0.91,
        tail="Noon local time / 10am UK time",
        tail_type="Time",
        tail_condifence=0.89
    )
        ]

Extract this: {text}"""