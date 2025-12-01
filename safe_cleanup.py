import os
import re

PROJECT_ROOT = "."
IGNORED_DIRS = {".git", ".vs", "bin", "obj", "TestResults", "Secrets", "logs"}

LICENSE_HEADER = """// Copyright (C) 2024-2025 ZenonEl
// Licensed under the GNU Affero General Public License v3.0 (AGPL-3.0).
// See LICENSE file in the project root for full license information."""

def is_valid_import(line):
    s = line.strip()
    # Это настоящий импорт, если:
    # 1. Начинается с using
    # 2. Заканчивается точкой с запятой
    # 3. НЕ содержит "var ", "new ", "=" (кроме алиасов, но алиасы редкость, пока пропустим для безопасности)
    # 4. НЕ содержит скобок "(" или ")"
    if not s.startswith("using "): return False
    if not s.endswith(";"): return False
    if " var " in s: return False
    if " new " in s: return False
    if "(" in s: return False
    if "=" in s and not " = " in s: # Грубая защита от алиасов
        return False
    return True

def process_file(path):
    with open(path, "r", encoding="utf-8") as f:
        lines = f.readlines()

    # 1. Отделяем "Верхушку" (импорты и комменты) от "Тела" (код и namespace)
    imports = set()
    body_start_index = 0
    
    # Пропускаем старые лицензии и пустые строки в начале
    for i, line in enumerate(lines):
        stripped = line.strip()
        if not stripped: continue
        if stripped.startswith("//"): continue
        
        # Если это импорт - сохраняем
        if is_valid_import(line):
            imports.add(stripped)
            continue
        
        # Как только встретили что-то другое (namespace, аттрибут, класс) - это начало тела
        body_start_index = i
        break
    
    # Если файл пустой или странный
    if body_start_index == 0 and not imports:
        return

    # 2. Формируем новый контент
    new_content = []
    
    # Шапка
    new_content.append(LICENSE_HEADER)
    new_content.append("")
    
    # Импорты (сортируем: System сначала)
    sorted_imports = sorted(list(imports), key=lambda x: (not x.startswith("using System"), x))
    if sorted_imports:
        new_content.extend(sorted_imports)
        new_content.append("")
    
    # Тело (все остальное без изменений)
    # Убираем пустые строки перед телом, если они были
    body_lines = lines[body_start_index:]
    
    # Убираем лишние пустые строки в начале тела
    while body_lines and not body_lines[0].strip():
        body_lines.pop(0)
        
    new_content.extend([line.rstrip() for line in body_lines])
    new_content.append("") # Пустая строка в конце

    # Записываем
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(new_content))
    
    print(f"✅ Cleaned: {path}")

def main():
    for root, dirs, files in os.walk(PROJECT_ROOT):
        dirs[:] = [d for d in dirs if d not in IGNORED_DIRS]
        for file in files:
            if file.endswith(".cs"):
                process_file(os.path.join(root, file))

if __name__ == "__main__":
    main()