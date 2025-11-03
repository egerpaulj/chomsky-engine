from llm_ner_nel.knowledge_graph.neo_db_config import NeoDbConfig
from llm_ner_nel.core.dto import Relationships
from py2neo import Graph, Node, Relationship
from typing import List


class KnowledgeGraph:
    neo_config: NeoDbConfig
    graph: Graph

    def __init__(self, **kwargs):
        self.neo_config = kwargs.get('neoDbConfig', NeoDbConfig())
        self.graph = Graph(self.neo_config.uri, auth=(self.neo_config.username, self.neo_config.password))


    def __get_node(self, nodes: List[Node], title: str) -> Node:

        for node in nodes:
            if node.get("title") == title:
                return node
            
        #print(f"Warning: node not found: {title}")
        return None
    
    def add_or_merge_relationships(self, result: Relationships, src: str, src_type: str):
        nodes_set = set()

        for rel in result.relationships:
            if rel.head_type == None or rel.head == '':
                continue
            if rel.tail_type == None or rel.tail == '':
                continue

            head_node = (rel.head_type.lower(), rel.head.lower())
            tail_node = (rel.tail_type.lower(), rel.tail.lower())

            nodes_set.add(head_node)
            nodes_set.add(tail_node)
        
        topic_node = Node(result.topic.lower(), identity="topic", title=result.topic.lower(), src_type=src_type, src=src)
        self.graph.merge(topic_node, result.topic.lower(), "title")
        nodes = [Node(title.lower(), identity=identity.lower(), title=title.lower(), src_type=src_type, src=src) for identity, title in nodes_set]

        for node in nodes:
            label = node.get("title")
            #print(f"Merging node in KG: {label}")
            self.graph.merge(node, label, "title")

        for rel in result.relationships:
            if rel.head_type == None or rel.head == '':
                continue
            if rel.tail_type == None or rel.tail == '':
                continue

            head_node = self.__get_node(nodes, rel.head.lower())
            tail_node = self.__get_node(nodes, rel.tail.lower())
            
            if(tail_node is None or head_node is None):
                print(f"Skipping relationship creation due to missing node: {rel.relation}")
                continue
            
            #print(f"Creating relationship in KG: {head_node} --[{rel.relation}]-> {tail_node}")

            self.graph.create(Relationship(
                    head_node,
                    rel.relation,
                    tail_node
                ))
            self.graph.create(Relationship(
                    topic_node,
                    "MENTIONS",
                    head_node
                ))
            self.graph.create(Relationship(
                    topic_node,
                    "MENTIONS",
                    tail_node
                ))    

    
