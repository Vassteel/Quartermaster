"""Exercise installer planning against temporary profiles; never apply to the user's game."""
from pathlib import Path
import hashlib,importlib.util,json,tempfile,unittest,zipfile
ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('quartermaster_installer',ROOT/'scripts/install_local.py');installer=importlib.util.module_from_spec(spec);spec.loader.exec_module(installer)
class InstallerPlan(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory();root=Path(self.tmp.name)
  self.saved={k:getattr(installer,k) for k in ('PROJECT','GAME','MANAGER','PROFILE')}
  installer.PROJECT=root/'project';installer.GAME=root/'game';installer.MANAGER=root/'manager';installer.PROFILE=root/'manager/profiles/Test'
  for p in [installer.PROJECT/'packaging',installer.PROJECT/'dist',installer.PROJECT/'bin/Release/net472',installer.PROFILE]:p.mkdir(parents=True)
  manifest={'version_number':'0.1.37','website_url':'https://example.invalid','description':'Test','dependencies':[]}
  (installer.PROJECT/'packaging/manifest.json').write_text(json.dumps(manifest));(installer.PROJECT/'bin/Release/net472/Quartermaster.dll').write_bytes(b'test dll')
  p=installer.PROJECT/'dist/Quartermaster-0.1.37.zip'
  with zipfile.ZipFile(p,'w') as z:
   z.writestr('manifest.json',json.dumps(manifest));z.writestr('BepInEx/plugins/Quartermaster/Quartermaster.dll',b'test dll')
  p.with_suffix('.zip.sha256').write_text(hashlib.sha256(p.read_bytes()).hexdigest());gale=installer.PROFILE/'BepInEx/plugins/Vassteel-Quartermaster/Quartermaster';gale.mkdir(parents=True);(gale/'Quartermaster.dll').write_bytes(b'old dll')
 def tearDown(self):
  for k,v in self.saved.items():setattr(installer,k,v)
  self.tmp.cleanup()
 def test_duplicate_gale_plugin_is_rejected(self):
  extra=installer.PROFILE/'BepInEx/plugins/duplicate/Quartermaster.dll';extra.parent.mkdir(parents=True);extra.write_bytes(b'duplicate')
  with self.assertRaisesRegex(RuntimeError,'Expected one Quartermaster'):installer.plan()
 def conflict(self):return installer.PROFILE/'BepInEx/plugins/TastyChickenLegs-AutomaticFermenters/AutomaticFermenters.dll'
 def test_missing_legacy_mods_do_not_block_upgrade(self):
  changes=installer.plan();self.assertIn(installer.PROFILE/'BepInEx/plugins/Vassteel-Quartermaster/Quartermaster/Quartermaster.dll',changes)
  self.assertTrue(all(p.is_relative_to(installer.PROFILE) for p in changes),'installs must target Gale only')
  self.assertFalse((installer.GAME/'BepInEx').exists(),'planning must not create installed files')
  self.assertEqual((installer.PROFILE/'BepInEx/plugins/Vassteel-Quartermaster/Quartermaster/Quartermaster.dll').read_bytes(),b'old dll')
  self.assertFalse(any('cache' in p.parts or p.name=='mods.yml' for p in changes))
 def test_existing_conflict_gets_reversible_disable(self):
  p=self.conflict();p.parent.mkdir(parents=True);p.write_bytes(b'legacy')
  changes=installer.plan();self.assertIsNone(changes[p]);self.assertEqual(changes[p.with_name(p.name+'.old')],b'legacy');self.assertEqual(p.read_bytes(),b'legacy')
 def test_different_disabled_backup_remains_protected(self):
  p=self.conflict();p.parent.mkdir(parents=True);p.write_bytes(b'legacy');p.with_name(p.name+'.old').write_bytes(b'older')
  with self.assertRaisesRegex(RuntimeError,'Different disabled copy'):installer.plan()
 def test_absent_active_with_backup_is_untouched(self):
  p=self.conflict();p.parent.mkdir(parents=True);backup=p.with_name(p.name+'.old');backup.write_bytes(b'older')
  changes=installer.plan();self.assertNotIn(backup,changes);self.assertNotIn(p,changes)
if __name__=='__main__':unittest.main()
