"""Rebuild the approved mailbox from its exported geometry, without reverting to the old stump.
Edit assets/postal/mailbox/mailbox.blend and run scripts/export_mailbox.py to change it.
"""
import json
from pathlib import Path
from types import SimpleNamespace

def build(low=False):
 data=json.loads((Path(__file__).resolve().parents[1]/'assets/postal/mailbox/model.json').read_text())
 parts=data['levels'][1 if low else 0]
 return SimpleNamespace(parts={i:p for i,p in enumerate(parts)}),data['pivots']
