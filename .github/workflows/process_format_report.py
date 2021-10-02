import json
import os
import sys

with open("_format_report.json") as file:
    for doc in json.load(file):
        relative_path = os.path.relpath(doc["FilePath"])
        for change in doc["FileChanges"]:
            print(f"::error file={relative_path},line={change['LineNumber']},col={change['CharNumber']}::{change['FormatDescription']}")

print("Formatting issues have been detected. Please check the changed files to see them, or run dotnet format.")
sys.exit(1)  # if we report formatting issues, have the pipeline indicate failure
