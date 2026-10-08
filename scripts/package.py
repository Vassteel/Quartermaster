from pathlib import Path
import hashlib,json,zipfile
root=Path(__file__).resolve().parents[1]
manifest=json.loads((root/'packaging/manifest.json').read_text())
version=manifest['version_number']; out=root/'dist';out.mkdir(exist_ok=True)
plugin=root/'bin/Release/net472/Quartermaster.dll'
if not plugin.is_file(): raise SystemExit('Build Quartermaster before packaging')
required=['manifest.json','README.md','icon.png','CHANGELOG.md']
path=out/f'Quartermaster-{version}.zip'
with zipfile.ZipFile(path,'w',zipfile.ZIP_DEFLATED)as z:
    for name in required:z.write(root/'packaging'/name,name)
    z.write(root/'LICENSE.txt','LICENSE.txt')
    z.write(plugin,'BepInEx/plugins/Quartermaster/Quartermaster.dll')
with zipfile.ZipFile(path)as z:
    assert z.testzip() is None
    assert z.namelist().count('BepInEx/plugins/Quartermaster/Quartermaster.dll')==1
    assert sum(n.endswith('.dll') for n in z.namelist())==1
    assert json.loads(z.read('manifest.json'))['version_number']==version
    assert z.read('BepInEx/plugins/Quartermaster/Quartermaster.dll')==plugin.read_bytes()
    for name in required:assert z.read(name)==(root/'packaging'/name).read_bytes()
    assert z.read('LICENSE.txt')==(root/'LICENSE.txt').read_bytes()
sha=hashlib.sha256(path.read_bytes()).hexdigest()
(out/(path.name+'.sha256')).write_text(f'{sha}  {path.name}\n')
print(f'Package: {path}\nSHA256: {sha}')
