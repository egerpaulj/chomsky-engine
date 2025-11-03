"""
Evaluate entities extracted by the graph module.

CSV format (no header assumed by default):
- column 0: text to extract entities from
- column 1: ground-truth entities separated by '|' (pipe) by default

The script will:
- call the extraction API exposed by LlmKnowledgeGraph.graph (several common function/class names tried)
- compute TP, FP, FN, precision, recall, F1
- log metrics, params and a predictions CSV artifact to MLflow (experiment name == model name by default)
"""

from typing import Iterable, List, Set, Dict, Any, Optional

import os
import logging
import time

import pandas as pd
import mlflow
from llm_ner_nel.inference_api.entity_inference import EntityInferenceProvider, get_unique_entity_names


logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO)


def _normalize_entities(items: Iterable[Any]) -> Set[str]:
    """
    Normalize entity items to a set of lowercased stripped strings.
    Accepts strings, dicts (tries common keys), or iterables.
    """
    out = set()
    if items is None:
        return out
    # If it's a single string with separators, caller should have split; here we handle lists/iterables
    for it in items:
        if it is None:
            continue
        if isinstance(it, str):
            s = it.strip().lower()
            if s:
                out.add(s)
            continue
        # dict-like with common keys
        if isinstance(it, dict):
            for key in ("entity", "text", "label", "value", "name"):
                if key in it:
                    val = it[key]
                    if isinstance(val, str):
                        v = val.strip().lower()
                        if v:
                            out.add(v)
                        break
            continue
        # fallback to str()
        try:
            s = str(it).strip().lower()
            if s:
                out.add(s)
        except Exception:
            continue
    return out


def evaluate_dataset(
    csv_path: str,
    model_name: str,
    system_prompt_id: str,
    user_prompt_id: str,
    entities_sep: str = "|",
    text_col: int = 0,
    entities_col: int = 1,
    mlflow_experiment: Optional[str] = None,
    
) -> Dict[str, Any]:
    # MLflow logging
    mlflow.set_tracking_uri("http://localhost:5050")
    mlflow_exp = mlflow_experiment or model_name
    mlflow.set_experiment(mlflow_exp)
    with mlflow.start_run(run_name=f"GPU-ENTITY-ONLY-TEST-{model_name}-{user_prompt_id}-{system_prompt_id}"):

        
        entity_extractor = EntityInferenceProvider(
            model=model_name, 
            ollama_host="http://localhost:11434", 
            mlflow_tracking_host="http://localhost:5050", 
            mlflow_system_prompt_id = system_prompt_id, 
            mlflow_user_prompt_id = user_prompt_id)

        df = pd.read_csv(csv_path, dtype=str).fillna("")
        
        if df.shape[1] <= max(text_col, entities_col):
            raise ValueError("CSV does not contain enough columns for text/entities based on provided indices")

        texts = df.iloc[:, text_col].astype(str).tolist()
        ground_cols = df.iloc[:, entities_col].astype(str).tolist()

        total_tp = total_fp = total_fn = 0
        rows = []

        for idx, (text, ground_str) in enumerate(zip(texts, ground_cols)):
            # parse ground truth
            if ground_str.strip() == "":
                ground_set = set()
            else:
                ground_items = [s.strip() for s in ground_str.split(entities_sep) if s.strip() != ""]
                ground_set = _normalize_entities(ground_items)

            # inference
            try:
                start_time = time.time()
                entities = entity_extractor.get_entities(text= text)
                raw_pred = get_unique_entity_names(entities)
            except Exception as e:
                logger.exception("Predictor failed on text index %s: %s", idx, e)
                raw_pred = []

            latency_ms = (time.time() - start_time) * 1000
            mlflow.log_metric("latency_ms", latency_ms, step=idx)
            
            # predicted may be a single string or iterable; if string treat as single entity unless it contains sep
            pred_items: List[str]
            if isinstance(raw_pred, str):
                # if predictor returns one string with separators we should not split unless user expects; keep as single entity
                pred_items = [raw_pred]
            elif isinstance(raw_pred, (list, tuple, set)):
                pred_items = list(raw_pred)
            else:
                # try to coerce
                try:
                    pred_items = list(raw_pred)
                except Exception:
                    pred_items = [str(raw_pred)]

            pred_set = _normalize_entities(pred_items)

            tp = len(pred_set & ground_set)
            fp = len(pred_set - ground_set)
            fn = len(ground_set - pred_set)

            total_tp += tp
            total_fp += fp
            total_fn += fn

            rows.append({
                "index": idx,
                "text": text,
                "ground_truth": "|".join(sorted(ground_set)),
                "predicted": "|".join(sorted(pred_set)),
                "tp": tp,
                "fp": fp,
                "fn": fn,
            })

        precision = (total_tp / (total_tp + total_fp)) if (total_tp + total_fp) > 0 else 0.0
        recall = (total_tp / (total_tp + total_fn)) if (total_tp + total_fn) > 0 else 0.0
        f1 = (2 * precision * recall / (precision + recall)) if (precision + recall) > 0 else 0.0

        metrics = {
            "precision": precision,
            "recall": recall,
            "f1": f1,
            "tp": total_tp,
            "fp": total_fp,
            "fn": total_fn,
            "n_samples": len(rows),
        }

        # Save predictions to a temp CSV and log to mlflow
        preds_df = pd.DataFrame(rows)
    
        # log params
        mlflow.log_param("dataset", csv_path)
        mlflow.log_param("model_name", f"{model_name}_gpu")
        mlflow.log_param("entities_separator", entities_sep)
        mlflow.log_param("n_samples", len(rows))
        # log metrics
        mlflow.log_metric("precision", precision)
        mlflow.log_metric("recall", recall)
        mlflow.log_metric("f1", f1)
        mlflow.log_metric("tp", total_tp)
        mlflow.log_metric("fp", total_fp)
        mlflow.log_metric("fn", total_fn)

        # write predictions csv artifact
        filename = f"predictions_{int(time.time())}.csv"
        preds_df.to_csv(filename, index=False)
        mlflow.log_artifact(os.path.abspath(filename), artifact_path="artifacts")
        # tmp_path = os.path.abspath(filename)
        # try:
        #     os.remove(filename)
        # except Exception:
        #     logger.exception("Failed to delete local predictions file %s", tmp_path)
      

    logger.info("Evaluation complete. precision=%.4f recall=%.4f f1=%.4f", precision, recall, f1)
    logger.info("Predictions CSV written to %s and logged to MLflow experiment '%s'", filename, mlflow_exp)

    return {"metrics": metrics, "predictions_csv": filename}