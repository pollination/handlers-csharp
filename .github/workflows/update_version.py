import os
import re
import sys


working_dir = os.getcwd()

new_version = sys.argv[1]
print(f'New version: \n{new_version}\n')


# checking files
def replaceVersion(file, new_text):
    with open(file, encoding='utf-8', mode='r') as csFile:
        s = csFile.read()
    with open(file, encoding='utf-8', mode='w') as f:
        regex = r"(?<=\<Version\>)\S+(?=\<\/Version\>)"
        s = re.sub(regex, new_text, s)
        print(f"Update version in {file} to: {new_text}")
        f.write(s)


config = os.path.join(working_dir, 'src', 'Pollination.RhinoHandlers', 'Pollination.RhinoHandlers.csproj')
replaceVersion(config, str(new_version))