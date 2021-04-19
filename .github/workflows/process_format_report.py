import json
import os

with open("_format_report.json") as file:
    for doc in json.load(file):
        relative_path = os.path.relpath(doc["FilePath"])
        for change in doc["FileChanges"]:
            print(f"::error file={relative_path},line={change['LineNumber']},col={change['CharNumber']}::{change['FormatDescription']}")
