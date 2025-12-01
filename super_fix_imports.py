import os
import re
import subprocess

PROJECT_ROOT = "."
IGNORED_DIRS = {".git", ".vs", "bin", "obj", "TestResults", "Secrets", "logs"}

# Регулярка ищет namespace (поддерживает и file-scoped, и block-scoped)
# Ищем слово namespace, пробел, потом имя.
NAMESPACE_REGEX = re.compile(r"namespace\s+([\w\.]+)")

# Регулярка ищет объявления типов.
# Ищем: public/internal class/interface Name
# Группы: 1 - тип (class), 2 - имя (MyClass)
TYPE_REGEX = re.compile(r"(?:class|interface|enum|record|struct)\s+(\w+)")

# Регулярка ошибки (универсальная)
ERROR_REGEX = re.compile(r"(.*\.cs)\(\d+,\d+\): error CS0246: .*?[\"'](\w+)[\"']")

def get_file_content(path):
    try:
        with open(path, "r", encoding="utf-8") as f:
            return f.read()
    except:
        return ""

def index_types():
    print("🔍 Indexing types...")
    type_map = {} # TypeName -> Namespace
    
    for root, dirs, files in os.walk(PROJECT_ROOT):
        dirs[:] = [d for d in dirs if d not in IGNORED_DIRS]
        for file in files:
            if not file.endswith(".cs"): continue
            
            path = os.path.join(root, file)
            content = get_file_content(path)
            
            # 1. Ищем namespace
            ns_match = NAMESPACE_REGEX.search(content)
            if not ns_match: continue
            namespace = ns_match.group(1)
            
            # 2. Ищем типы
            for match in TYPE_REGEX.finditer(content):
                type_name = match.group(1)
                # Простая эвристика: если имя типа начинается с большой буквы (Convention)
                if type_name[0].isupper():
                    if type_name not in type_map:
                        type_map[type_name] = namespace
                    
    print(f"✅ Indexed {len(type_map)} types.")
    return type_map

def fix_errors(type_map):
    print("🔨 Building and checking errors...")
    
    # Запускаем билд
    proc = subprocess.Popen(
        ["dotnet", "build", "/clp:ErrorsOnly"],
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True
    )
    out, err = proc.communicate()
    full_log = out + "\n" + err
    
    fixed_files = set()
    
    for line in full_log.splitlines():
        match = ERROR_REGEX.search(line)
        if match:
            file_path = match.group(1).strip()
            missing_type = match.group(2)
            
            if missing_type in type_map:
                target_ns = type_map[missing_type]
                if apply_fix(file_path, target_ns):
                    print(f"💉 Injected: using {target_ns}; for {missing_type}")
                    fixed_files.add(file_path)
            # else:
                # print(f"❓ Unknown type: {missing_type}")

    return len(fixed_files)

def apply_fix(path, namespace):
    if not os.path.exists(path): return False
    
    with open(path, "r", encoding="utf-8") as f:
        lines = f.readlines()
        
    using_line = f"using {namespace};\n"
    
    # Проверка на дубликаты
    if any(using_line.strip() == l.strip() for l in lines):
        return False
        
    # Ищем, куда вставить (после последнего using или перед namespace)
    insert_idx = 0
    for i, line in enumerate(lines):
        if line.strip().startswith("using "):
            insert_idx = i + 1
        elif line.strip().startswith("namespace "):
            if insert_idx == 0: insert_idx = i # Если юзингов не было, ставим перед namespace
            break
            
    lines.insert(insert_idx, using_line)
    
    with open(path, "w", encoding="utf-8") as f:
        f.writelines(lines)
        
    return True

if __name__ == "__main__":
    t_map = index_types()
    if len(t_map) == 0:
        print("❌ Error: No types indexed. Check your files.")
        exit()
        
    count = fix_errors(t_map)
    print(f"\n🏁 Finished. Modified {count} files.")
    print("Run 'dotnet format' to cleanup.")