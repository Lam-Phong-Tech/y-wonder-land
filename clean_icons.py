import os, re
dir_path = r"d:\LamGameUnity\BaChuKhuRung3D\Assets\_Project\UI\Styles"
pattern = re.compile(r"/\* ============================================\s*AI INJECTED 3D ICONS.*", re.DOTALL)
count = 0
for root, dirs, files in os.walk(dir_path):
    for f in files:
        if f.endswith(".uss"):
            path = os.path.join(root, f)
            with open(path, "r", encoding="utf-8") as file:
                content = file.read()
            if pattern.search(content):
                new_content = pattern.sub("", content)
                with open(path, "w", encoding="utf-8") as file:
                    file.write(new_content.strip() + "\n")
                print("Cleaned " + f)
                count += 1
print(f"Done. Cleaned {count} files.")
