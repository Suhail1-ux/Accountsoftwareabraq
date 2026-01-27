import os

def replace_in_folder(folder_path, old_text, new_text):
    for root, dirs, files in os.walk(folder_path):
        for file in files:
            if file.endswith(".cs"):
                file_path = os.path.join(root, file)
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                new_content = content.replace(old_text, new_text)
                
                if new_content != content:
                    with open(file_path, 'w', encoding='utf-8') as f:
                        f.write(new_content)
                    print(f"Updated {file_path}")

if __name__ == "__main__":
    import sys
    if len(sys.argv) == 4:
        replace_in_folder(sys.argv[1], sys.argv[2], sys.argv[3])
    else:
        print("Usage: python replace_script.py <folder> <old> <new>")
