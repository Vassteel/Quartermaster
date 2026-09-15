from pathlib import Path
import hashlib,json,zipfile
root=Path(__file__).resolve().parents[1]
manifest=json.loads((root/'packaging/manifest.json').read_text())
version=manifest['version_number']; out=root/'dist';out.mkdir(exist_ok=True)
plugin=root/'bin/Release/net472/Quartermaster.dll'
if not plugin.is_file(): raise SystemExit('Build Quartermaster before packaging')
required=['manifest.json','README.md','CHANGELOG.md','icon.png','LICENSE.txt']
path=out/f'Quartermaster-{version}.zip'
with zipfile.ZipFile(path,'w',zipfile.ZIP_DEFLATED)as z:
    for name in required:z.write(root/'packaging'/name,name)
    z.write(plugin,'BepInEx/plugins/Quartermaster/Quartermaster.dll')
    for name in ['VALIDATION.md']:
        if (root/name).is_file():z.write(root/name,name)
with zipfile.ZipFile(path)as z:
    assert z.testzip() is None
    assert z.namelist().count('BepInEx/plugins/Quartermaster/Quartermaster.dll')==1
    assert sum(n.endswith('.dll') for n in z.namelist())==1
    assert json.loads(z.read('manifest.json'))['version_number']==version
sha=hashlib.sha256(path.read_bytes()).hexdigest()
(out/(path.name+'.sha256')).write_text(f'{sha}  {path.name}\n')
print(f'Package: {path}\nSHA256: {sha}')
