import os
import re
import datetime

ADR_DIR = "docs/adr"
TEMPLATE_FILE = "docs/adr/000-template.md"
INDEX_FILE = "docs/adr/README.md"

# Регулярка для парсинга YAML Front Matter
FRONT_MATTER_REGEX = re.compile(r"^---\n(.*?)\n---", re.DOTALL)

def parse_adr(file_path):
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()
    
    match = FRONT_MATTER_REGEX.match(content)
    if not match:
        return None
    
    yaml_block = match.group(1)
    metadata = {}
    
    # Простой парсер YAML (key: value)
    for line in yaml_block.split("\n"):
        if ":" in line:
            key, value = line.split(":", 1)
            metadata[key.strip()] = value.strip().strip("[]")
            
    metadata["filename"] = os.path.basename(file_path)
    return metadata

def generate_index():
    adrs = []
    if not os.path.exists(ADR_DIR):
        os.makedirs(ADR_DIR)
        
    for f in sorted(os.listdir(ADR_DIR)):
        if f.endswith(".md") and f != "README.md" and f != "000-template.md":
            data = parse_adr(os.path.join(ADR_DIR, f))
            if data:
                adrs.append(data)
    
    # Группируем иконочки по статусу
    status_icons = {
        "Accepted": "✅",
        "Proposed": "🚧",
        "Rejected": "❌",
        "Deprecated": "🗑️"
    }

    with open(INDEX_FILE, "w", encoding="utf-8") as f:
        f.write("# Architecture Decision Records (ADR)\n\n")
        f.write("| ID | Title | Status | Date | Tags |\n")
        f.write("|:--:|:------|:------:|:----:|:-----|\n")
        
        for adr in adrs:
            icon = status_icons.get(adr.get("status"), "❓")
            link = f"[{adr.get('title')}]({adr.get('filename')})"
            f.write(f"| {adr.get('id')} | {link} | {icon} {adr.get('status')} | {adr.get('date')} | {adr.get('tags')} |\n")
            
    print(f"✅ Index generated in {INDEX_FILE}")

def create_new(title):
    # Ищем последний ID
    max_id = 0
    for f in os.listdir(ADR_DIR):
        if re.match(r"^\d{3}-", f):
            curr_id = int(f.split("-")[0])
            if curr_id > max_id:
                max_id = curr_id
    
    new_id = max_id + 1
    id_str = f"{new_id:03d}"
    
    slug = title.lower().replace(" ", "-")
    filename = f"{id_str}-{slug}.md"
    path = os.path.join(ADR_DIR, filename)
    
    today = datetime.date.today().isoformat()
    
    content = f"""---
id: {id_str}
title: {title}
status: Proposed
date: {today}
authors: [ZenonEl]
tags: []
---

# Context
...

# Decision
...

# Consequences
...
"""
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)
        
    print(f"✅ Created new ADR: {path}")

# Пример использования:
# import sys
# if len(sys.argv) > 1 and sys.argv[1] == "new":
#     create_new(sys.argv[2])
# else:
#     generate_index()

if __name__ == "__main__":
    import sys
    if len(sys.argv) > 2 and sys.argv[1] == "new":
        # python adr_tools.py new "Use Seq Logging"
        create_new(sys.argv[2])
    else:
        # python adr_tools.py
        generate_index()