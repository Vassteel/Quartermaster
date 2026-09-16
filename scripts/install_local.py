#!/usr/bin/env python3
"""Install the inspected local package; default is a read-only dry run."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile
import time
import zipfile
import yaml

PROJECT = Path(__file__).resolve().parents[1]
GAME = Path('/home/deck/.local/share/Steam/steamapps/common/Valheim')
MANAGER = Path('/home/deck/.var/app/io.github.ebkr.r2modman/config/r2modmanPlus-local/Valheim')
PROFILE = MANAGER / 'profiles/Mods'
DISABLE = {
    'MaddCatter365-Hearthkeeper': 'Hearthkeeper.dll',
    'TastyChickenLegs-AutomaticFermenters': 'AutomaticFermenters.dll',
    'TastyChickenLegs-TimedTorchesStayLit': 'TimedTorchesStayLit.dll',
    'TastyChickenLegs-CandlesForever': 'CandlesForever.dll',
}
OPTIONAL_DISABLE = {'NoTaintTooltip': 'NoTaintTooltip.dll'}

def digest(data):
    return hashlib.sha256(data).hexdigest() if data is not None else None

def closed():
    for name in ('valheim.exe', 'valheim.x86_64', 'r2modman'):
        result = subprocess.run(['pgrep', '-xi', name], capture_output=True, text=True)
        if result.returncode == 0:
            raise RuntimeError(f'{name} is running; close it before installation or restore.')
        if result.returncode != 1:
            raise RuntimeError(f'Process check failed for {name}.')

def replace(path, data):
    if data is None:
        path.unlink(missing_ok=True)
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    fd, tmp = tempfile.mkstemp(prefix='.quartermaster-', dir=path.parent)
    try:
        with os.fdopen(fd, 'wb') as f:
            f.write(data)
            f.flush()
            os.fsync(f.fileno())
        os.chmod(tmp, 0o644)
        os.replace(tmp, path)
    finally:
        if os.path.exists(tmp):
            os.unlink(tmp)

def plan():
    version = json.loads((PROJECT / 'packaging/manifest.json').read_text())['version_number']
    package = PROJECT / 'dist' / f'Quartermaster-{version}.zip'
    expected = package.with_suffix('.zip.sha256').read_text().split()[0]
    assert digest(package.read_bytes()) == expected, 'Package checksum mismatch'
    with zipfile.ZipFile(package) as z:
        assert z.testzip() is None
        files = {Path(n).name: z.read(n) for n in z.namelist() if not n.endswith('/')}
    assert files['Quartermaster.dll'] == (PROJECT / 'bin/Release/net472/Quartermaster.dll').read_bytes()
    manifest = json.loads(files['manifest.json'])
    manifest['author'] = 'local'
    files['manifest.json'] = (json.dumps(manifest, indent=2) + '\n').encode()
    version = manifest['version_number']
    changes = {}
    for root in (GAME / 'BepInEx/plugins/Quartermaster', PROFILE / 'BepInEx/plugins/local-Quartermaster', MANAGER / 'cache/local-Quartermaster' / version):
        for name, data in files.items():
            changes[root / name] = data
    for root in (GAME, PROFILE):
        for folder, filename in {**DISABLE, **OPTIONAL_DISABLE}.items():
            source = root / 'BepInEx/plugins' / folder / filename
            target = source.with_name(source.name + '.old')
            if source.exists():
                if target.exists() and source.read_bytes() != target.read_bytes():
                    raise RuntimeError(f'Different disabled copy already exists: {target}')
                changes[target] = source.read_bytes()
                changes[source] = None
            elif folder in DISABLE:
                assert target.exists(), f'Missing installed mod: {source}'
        cfg = root / 'BepInEx/config/r4v9n1.lightmyfire.cfg'
        text = cfg.read_bytes().decode() if cfg.exists() else '[General]\nEnabled = true\n'
        text, count = re.subn(r'(?m)^(Enabled[ \t]*=[ \t]*)(?:true|false)\b', r'\g<1>false', text)
        assert count == 1, f'Unexpected LightMyFire configuration: {cfg}'
        changes[cfg] = text.encode()
        cfg = root / 'BepInEx/config/local.valheim.quartermaster.cfg'
        text = cfg.read_bytes().decode() if cfg.exists() else '[General]\n'
        if not re.search(r'(?m)^HideCheatItemMessages[ \t]*=', text):
            newline = '\r\n' if '\r\n' in text else '\n'
            setting = newline.join(['', '## Hide item cheat notices in tooltips and pickup/removal messages.', '# Setting type: Boolean', '# Default value: true', 'HideCheatItemMessages = true', ''])
            if '[General]' + newline in text:
                text = text.replace('[General]' + newline, '[General]' + newline + setting, 1)
            else:
                text += newline + '[General]' + newline + setting
        changes[cfg] = text.encode()
    mods_path = PROFILE / 'mods.yml'
    mods = yaml.safe_load(mods_path.read_text())
    for m in mods:
        if m.get('enabled') and m['name'] not in DISABLE:
            assert not any(any(d.startswith(n + '-') for n in DISABLE) for d in m.get('dependencies', [])), f"Dependent mod: {m['name']}"
        if m['name'] in DISABLE or m['name'].split('-')[-1] in OPTIONAL_DISABLE:
            m['enabled'] = False
    mods = [m for m in mods if m['name'] != 'local-Quartermaster']
    major, minor, patch = map(int, version.split('.'))
    mods.append(dict(manifestVersion=1, name='local-Quartermaster', authorName='local', websiteUrl=manifest['website_url'], displayName='Quartermaster', description=manifest['description'], gameVersion='0', networkMode='both', packageType='other', installMode='managed', installedAtTime=int(time.time()*1000), loaders=[], dependencies=manifest['dependencies'], incompatibilities=[], optionalDependencies=[], versionNumber=dict(major=major, minor=minor, patch=patch), enabled=True, onlineSource=False))
    changes[mods_path] = yaml.safe_dump(mods, sort_keys=False, allow_unicode=True).encode()
    return {p: data for p, data in changes.items() if (p.read_bytes() if p.exists() else None) != data}

def restore(backup, apply):
    entries = json.loads((backup / 'manifest.json').read_text())['files']
    for entry in entries:
        p = Path(entry['path'])
        actual = digest(p.read_bytes()) if p.exists() else None
        assert actual == entry['installed_sha256'], f'Changed since installation; refusing to overwrite: {p}'
    if apply:
        closed()
        for entry in reversed(entries):
            data = (backup / entry['backup']).read_bytes() if entry['backup'] else None
            replace(Path(entry['path']), data)
    print(('Restored' if apply else 'Would restore') + f' {len(entries)} files from {backup}')

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true')
    parser.add_argument('--restore', type=Path)
    args = parser.parse_args()
    if args.restore:
        restore(args.restore, args.apply)
        return
    closed()
    changes = plan()
    for p, data in changes.items():
        print(('REMOVE ' if data is None else 'WRITE  ') + str(p))
    if not args.apply:
        print(f'Dry run: {len(changes)} file changes. No installed files modified.')
        return
    version = json.loads((PROJECT / 'packaging/manifest.json').read_text())['version_number']
    backup = PROJECT / 'backups' / time.strftime(f'install-{version}-%Y%m%d-%H%M%S')
    backup.mkdir(parents=True, exist_ok=False)
    entries = []
    before = {}
    for i, (p, data) in enumerate(changes.items()):
        old = p.read_bytes() if p.exists() else None
        before[p] = old
        filename = f'{i:03d}.bin' if old is not None else None
        if filename:
            (backup / filename).write_bytes(old)
        entries.append(dict(path=str(p), backup=filename, original_sha256=digest(old), installed_sha256=digest(data)))
    (backup / 'manifest.json').write_text(json.dumps(dict(files=entries), indent=2) + '\n')
    closed()
    touched = []
    try:
        for p, data in changes.items():
            assert (p.read_bytes() if p.exists() else None) == before[p], f'Changed during install: {p}'
            touched.append(p)
            replace(p, data)
        for p, data in changes.items():
            assert (p.read_bytes() if p.exists() else None) == data, f'Verification failed: {p}'
    except BaseException:
        for p in reversed(touched):
            replace(p, before[p])
        raise
    (backup / 'SUCCESS').write_text('All installed files verified byte-for-byte.\n')
    print(f'Installed and verified {len(changes)} file changes. Backup: {backup}')

if __name__ == '__main__':
    main()
