"""Verificação de leitura offline em segunda resolução. Restaura configurações ao sair."""
import json
import re
import subprocess
import time
import xml.etree.ElementTree as ET
from pathlib import Path

OUT=Path(__file__).resolve().parents[1]/'docs/evidencias/android'
def adb(*args):return subprocess.check_output(['adb','-e',*args])
def snapshot():
    adb('shell','uiautomator','dump','/sdcard/easyshop-compat.xml')
    return ET.fromstring(adb('shell','cat','/sdcard/easyshop-compat.xml'))
def tap(desc):
    for node in snapshot().iter('node'):
        if node.get('content-desc')==desc:
            x1,y1,x2,y2=map(int,re.findall(r'\d+',node.get('bounds')))
            adb('shell','input','tap',str((x1+x2)//2),str((y1+y2)//2));return
    raise AssertionError(desc)
size=adb('shell','wm','size').decode()
match=re.search(r'Override size: (\d+x\d+)',size)
airplane=adb('shell','settings','get','global','airplane_mode_on').decode().strip()
try:
    adb('shell','cmd','connectivity','airplane-mode','enable')
    adb('shell','wm','size','720x1280')
    adb('shell','am','force-stop','br.edu.easyshop')
    adb('shell','monkey','-p','br.edu.easyshop','1')
    time.sleep(3)
    tap('customers-tab')
    found=False
    for attempt in range(6):
        root=snapshot()
        if any('marina@example.com' in n.get('text','') for n in root.iter('node')):found=True;break
        adb('shell','input','swipe','360','1050','360','400','300')
    assert found,'Cliente persistido não encontrado na resolução compacta'
    (OUT/'08-offline-720x1280.png').write_bytes(adb('exec-out','screencap','-p'))
    (OUT/'08-offline-720x1280.xml').write_bytes(ET.tostring(root,encoding='utf-8'))
    result={'result':'PASS','resolution':'720x1280','originalResolution':size.strip(),'airplaneModeDuringTest':adb('shell','settings','get','global','airplane_mode_on').decode().strip(),'check':'App reiniciado e cliente persistido listado sem rede em resolução compacta','physicalDevice':False}
    (OUT/'compat-results.json').write_text(json.dumps(result,indent=2,ensure_ascii=False),encoding='utf-8')
    print(json.dumps(result,ensure_ascii=False))
finally:
    adb('shell','wm','size',match.group(1) if match else 'reset')
    adb('shell','cmd','connectivity','airplane-mode','enable' if airplane=='1' else 'disable')
