"""
Usage (example):
python main.py --csv example_data.csv --model-name llama3.2 --mlflow-experiment ner --mlflow-system-prompt-id NER_System/1 --mlflow-user-prompt-id NER_User/1
"""
import argparse
import sys
import logging

from llm_ner_nel.evaluation.evaluate import evaluate_dataset

logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO)

def _parse_args():
    p = argparse.ArgumentParser(description="Evaluate entity extraction using LlmKnowledgeGraph.graph")
    p.add_argument("--csv", required=True, help="Path to CSV file (text, entities). No header expected by default.")
    p.add_argument("--model-name", required=True, help="Model name (also used as MLflow run name / experiment)")
    p.add_argument("--entities-sep", default="|", help="Separator used in ground-truth entities column (default='|')")
    p.add_argument("--text-col", type=int, default=0, help="Zero-based index for text column (default 0)")
    p.add_argument("--entities-col", type=int, default=1, help="Zero-based index for entities column (default 1)")
    p.add_argument("--mlflow-experiment", default=None, help="MLflow experiment name (defaults to model-name)")
    p.add_argument("--mlflow-system-prompt-id", default=None, help="MLflow System prompt id")
    p.add_argument("--mlflow-user-prompt-id", default=None, help="MLflow User prompt id")
    return p.parse_args()

if __name__ == "__main__":
    args = _parse_args()
    try:
        evaluate_dataset(
            csv_path=args.csv,
            model_name=args.model_name,
            entities_sep=args.entities_sep,
            text_col=args.text_col,
            entities_col=args.entities_col,
            mlflow_experiment=args.mlflow_experiment,
            system_prompt_id=args.mlflow_system_prompt_id,
            user_prompt_id=args.mlflow_user_prompt_id
        )
    except Exception as exc:
        logger.exception("Evaluation failed: %s", exc)
        sys.exit(2)
        
        