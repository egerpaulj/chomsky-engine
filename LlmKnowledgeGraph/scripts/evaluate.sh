cd ../src/llm_ner_nel/evaluation
# can't deal with large texts - chunking?? -> python main.py --csv example_data.csv --model-name llama3.2 --mlflow-experiment ner --mlflow-system-prompt-id NER_System@entity --mlflow-user-prompt-id NER_User@entity;
python main.py --csv example_data.csv --model-name gemma3:4b --mlflow-experiment ner --mlflow-system-prompt-id NER_System@entity --mlflow-user-prompt-id NER_User@entity;
python main.py --csv example_data.csv --model-name gemma3:12b --mlflow-experiment ner --mlflow-system-prompt-id NER_System@entity --mlflow-user-prompt-id NER_User@entity;
python main.py --csv example_data.csv --model-name qwen3:14b --mlflow-experiment ner --mlflow-system-prompt-id NER_System@entity --mlflow-user-prompt-id NER_User@entity;
python main.py --csv example_data.csv --model-name qwen3:8b --mlflow-experiment ner --mlflow-system-prompt-id NER_System@entity --mlflow-user-prompt-id NER_User@entity;
python main.py --csv example_data.csv --model-name magistral:24b --mlflow-experiment ner --mlflow-system-prompt-id NER_System@entity --mlflow-user-prompt-id NER_User@entity;
# output has reasoning - can't parse json -> python main.py --csv example_data.csv --model-name deepseek-r1:8b --mlflow-experiment ner --mlflow-system-prompt-id NER_System@entity --mlflow-user-prompt-id NER_User@entity;
# output has reasoning - can't parse json -> python main.py --csv example_data.csv --model-name deepseek-r1:14b --mlflow-experiment ner --mlflow-system-prompt-id NER_System@entity --mlflow-user-prompt-id NER_User@entity;
python main.py --csv example_data.csv --model-name qwen3:4b --mlflow-experiment ner --mlflow-system-prompt-id NER_System@entity --mlflow-user-prompt-id NER_User@entity;