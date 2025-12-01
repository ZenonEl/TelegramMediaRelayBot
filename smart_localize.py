import os
import re
import xml.etree.ElementTree as ET

# ==========================================
# КОНФИГУРАЦИЯ
# ==========================================
PROJECT_ROOT = "."
RESOURCES_DIR = "./Resources"

# Карта соответствия: Имя файла .resx -> Данные сервиса
# Key: Имя файла без расширения (и без языка)
# Value: (InterfaceName, FieldName, VariableName)
SERVICE_MAP = {
    "UI":         ("IUiResourceService",        "_uiResources",       "uiResources"),
    "Errors":     ("IErrorsResourceService",    "_errorsResources",   "errorsResources"),
    "Formatting": ("IFormattingResourceService","_formattingResources","formattingResources"),
    "Help":       ("IHelpResourceService",      "_helpResources",     "helpResources"),
    "Inbox":      ("IInboxResourceService",     "_inboxResources",    "inboxResources"),
    "Settings":   ("ISettingsResourceService",  "_settingsResources", "settingsResources"),
    "States":     ("IStatesResourceService",    "_statesResources",   "statesResources"),
    "Status":     ("IStatusResourceService",    "_statusResources",   "statusResources"),
    "texts":      ("IResourceService",          "_resourceService",   "resourceService") # Legacy
}

# ==========================================
# ЛОГИКА
# ==========================================

class TranslationMap:
    def __init__(self):
        # OldKey -> { 'new_key': str, 'service_info': tuple }
        self.mapping = {}

    def load_from_resx(self):
        print("📖 Reading .resx files...")
        for filename in os.listdir(RESOURCES_DIR):
            if not filename.endswith(".resx") or "ru-RU" in filename: 
                # Читаем базовые файлы или ru-RU, где есть комменты. 
                # Предполагаем, что комменты есть в файлах вида "Name.ru-RU.resx" как в твоем примере
                pass
            
            # Определяем категорию (Formatting, UI...)
            # Formatting.ru-RU.resx -> Formatting
            category = filename.split('.')[0]
            
            if category not in SERVICE_MAP:
                continue

            service_info = SERVICE_MAP[category]
            tree = ET.parse(os.path.join(RESOURCES_DIR, filename))
            root = tree.getroot()

            for data in root.findall('data'):
                new_key = data.get('name')
                
                # Ищем комментарий внутри <data> или рядом
                # В XML комменты - это ноды, но ElementTree их плохо видит.
                # В твоем примере коммент внутри <data> но после <value>.
                # ElementTree в Python < 3.9 не парсит комменты легко.
                # Попробуем прочитать файл как текст для надежности поиска комментов.
                pass
        
        # Альтернативный парсинг текстом (надежнее для комментов)
        for filename in os.listdir(RESOURCES_DIR):
            if not filename.endswith("ru-RU.resx"): continue # Берем русские, там комменты
            
            category = filename.split('.')[0]
            if category not in SERVICE_MAP: continue
            
            service_info = SERVICE_MAP[category]
            
            with open(os.path.join(RESOURCES_DIR, filename), "r", encoding="utf-8") as f:
                content = f.read()
                
            # Ищем паттерн: <data name="NEW_KEY" ...> ... <!-- Старое имя: OLD_KEY -->
            # Regex multiline
            pattern = re.compile(r'<data name="([^"]+)"[^>]*>.*?<!--\s*Старое имя:\s*([A-Za-z0-9_]+)\s*-->', re.DOTALL)
            
            matches = pattern.findall(content)
            for new_key, old_key in matches:
                self.mapping[old_key] = {
                    'new_key': new_key,
                    'service_info': service_info
                }
                # print(f"   Found mapping: {old_key} -> {new_key} ({category})")
                
        print(f"✅ Loaded {len(self.mapping)} translation mappings.")

def inject_dependency(content, interface_name, field_name, var_name):
    """Внедряет зависимость в класс C#."""
    
    # 1. Проверяем, есть ли уже поле
    if f"{interface_name} {field_name}" in content:
        return content # Уже есть

    lines = content.splitlines()
    new_lines = []
    
    class_found = False
    constructor_found = False
    fields_inserted = False
    
    # Регулярка для поиска конструктора: public ClassName(
    # Находим имя класса сначала
    class_regex = re.compile(r"public class (\w+)")
    class_name = ""
    
    for i, line in enumerate(lines):
        # Ищем класс
        if not class_found:
            m = class_regex.search(line)
            if m:
                class_name = m.group(1)
                class_found = True
                new_lines.append(line)
                # Вставляем поле сразу после объявления класса (или после {)
                if "{" in line:
                    new_lines.append(f"    private readonly {interface_name} {field_name};")
                    fields_inserted = True
                continue
                
        if class_found and not fields_inserted and "{" in line:
             new_lines.append(line)
             new_lines.append(f"    private readonly {interface_name} {field_name};")
             fields_inserted = True
             continue

        # Ищем конструктор
        # public MyClass(
        if class_found and not constructor_found and f"public {class_name}(" in line:
            constructor_found = True
            
            # Проверяем, есть ли аргументы
            if ")" in line: # Конструктор в одну строку или закрывается тут же
                # public MyClass()
                # -> public MyClass(IService s)
                if "()" in line:
                    replaced = line.replace("()", f"({interface_name} {var_name})")
                else:
                    replaced = line.replace(")", f", {interface_name} {var_name})")
                
                new_lines.append(replaced)
                
                # Ищем где вставить присваивание
                # Если тело начинается тут же {
                if "{" in line:
                    new_lines.append(f"        {field_name} = {var_name};")
            else:
                # Многострочный конструктор
                new_lines.append(line)
                new_lines.append(f"        {interface_name} {var_name},")
            continue
            
        # Если мы внутри конструктора и ищем тело
        if constructor_found:
            # Ищем первую открывающую скобку тела конструктора
            if line.strip() == "{":
                new_lines.append(line)
                new_lines.append(f"        {field_name} = {var_name};")
                constructor_found = False # Done injecting assignment
                continue
            elif "{" in line and not "=>" in line: # public C() {
                 # Если мы не вставили присваивание ранее
                 if f"{field_name} =" not in "\n".join(new_lines[-5:]):
                     parts = line.split("{")
                     new_lines.append(parts[0] + "{")
                     new_lines.append(f"        {field_name} = {var_name};" + parts[1])
                     constructor_found = False
                     continue

        new_lines.append(line)

    # Добавляем using в начало
    final_content = "\n".join(new_lines)
    if "using TelegramMediaRelayBot.Config.Services;" not in final_content:
        final_content = "using TelegramMediaRelayBot.Config.Services;\n" + final_content
        
    return final_content

def process_files(mapper):
    print("🚀 Starting code refactoring...")
    
    for root, dirs, files in os.walk(PROJECT_ROOT):
        if "bin" in dirs: dirs.remove("bin")
        if "obj" in dirs: dirs.remove("obj")
        
        for file in files:
            if not file.endswith(".cs"): continue
            
            path = os.path.join(root, file)
            with open(path, "r", encoding="utf-8") as f:
                content = f.read()
            
            # Ищем старые вызовы: .GetResourceString("OldKey")
            # Regex: (GetResourceString\s*\(\s*")(\w+)("\s*\))
            matches = list(re.finditer(r'GetResourceString\s*\(\s*"([^"]+)"\s*\)', content))
            
            if not matches:
                continue
            
            print(f"🔧 Processing {file}...")
            
            original_content = content
            services_to_inject = set() # (Interface, Field, Var)
            
            # Замена вызовов (с конца, чтобы не сбить индексы)
            for m in reversed(matches):
                old_key = m.group(1)
                
                if old_key in mapper.mapping:
                    data = mapper.mapping[old_key]
                    new_key = data['new_key']
                    svc_interface, svc_field, svc_var = data['service_info']
                    
                    # 1. Заменяем строку вызова
                    # Было: _resource.GetResourceString("Old")
                    # Стало: _uiResources.GetString("UI.New")
                    
                    start, end = m.span()
                    # Находим переменную перед точкой (например _resourceService.)
                    # Ищем назад от start
                    call_prefix = content.rfind(".", 0, start)
                    variable_start = content.rfind(" ", 0, call_prefix) + 1
                    # Заменяем весь кусок `_oldService.GetResourceString("Old")`
                    # На `_newField.GetString("New")`
                    
                    # Упрощение: мы меняем только `GetResourceString("Old")` на `GetString("New")`
                    # А переменную меняем отдельным проходом или надеемся на context?
                    # Нет, надо менять и переменную.
                    
                    # Но regex нашел только метод.
                    # Ладно, делаем проще. Заменяем `_resourceService.GetResourceString("Old")`
                    # Если переменная называется по-другому, скрипт может пропустить.
                    # Но у тебя везде `_resourceService` судя по коду.
                    
                    # Паттерн для замены полного вызова
                    full_pattern = re.compile(rf'(\w+)\.GetResourceString\s*\(\s*"{old_key}"\s*\)')
                    
                    def replacer(match_obj):
                        services_to_inject.add(data['service_info'])
                        return f"{svc_field}.GetString(\"{new_key}\")"
                        
                    content = full_pattern.sub(replacer, content)
                else:
                    # print(f"   ⚠️ Unknown key: {old_key}")
                    pass

            if content != original_content:
                # Если были замены, нужно внедрить зависимости
                for svc in services_to_inject:
                    content = inject_dependency(content, svc[0], svc[1], svc[2])
                
                with open(path, "w", encoding="utf-8") as f:
                    f.write(content)
                print(f"   ✅ Modified {file}")

if __name__ == "__main__":
    mapper = TranslationMap()
    mapper.load_from_resx()
    process_files(mapper)
    print("🏁 Done. Please run 'dotnet format' and check for errors.")