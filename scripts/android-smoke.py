"""Teste real via ADB no emulador. Requer app recém-instalado sem cadastros.
Não limpa dados. Instale em emulador/base exclusivos para demonstração.
"""
import json
import re
import subprocess
import time
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs/evidencias/android'
OUT.mkdir(parents=True, exist_ok=True)
PACKAGE = 'br.edu.easyshop'

def adb(*args):
    return subprocess.check_output(['adb', '-e', *args])

def tree():
    for attempt in range(8):
        try:
            adb('shell','uiautomator','dump','/sdcard/easyshop-audit.xml')
            return ET.fromstring(adb('shell','cat','/sdcard/easyshop-audit.xml'))
        except (subprocess.CalledProcessError, ET.ParseError):time.sleep(1)
    raise AssertionError('A hierarquia Android não ficou disponível.')

def find(desc, text=None):
    for node in tree().iter('node'):
        if (desc is None or node.get('content-desc') == desc) and (text is None or text in node.get('text','')):
            return node
    return None

def tap(desc):
    for attempt in range(6):
        node=find(desc)
        if node is not None:
            left,top,right,bottom=map(int,re.findall(r'\d+',node.get('bounds')))
            if bottom>top and right>left:
                adb('shell','input','tap',str((left+right)//2),str((top+bottom)//2));return
        adb('shell','input','swipe','500','1800','500','700','300')
    raise AssertionError('Controle não encontrado: '+desc)

def fill(desc,value):
    tap(desc)
    adb('shell','input','text',value.replace(' ','%s'))
    adb('shell','input','keyevent','4')

def expect(text):
    for attempt in range(6):
        if find(None,text) is not None:return
        if attempt == 1:top()
        if attempt == 3:adb('shell','input','swipe','500','1800','500','700','300')
        time.sleep(.5)
    raise AssertionError('Texto não encontrado: '+text)

def screenshot(name):
    (OUT / (name+'.png')).write_bytes(adb('exec-out','screencap','-p'))
    (OUT / (name+'.xml')).write_bytes(ET.tostring(tree(),encoding='utf-8'))

def top():adb('shell','input','swipe','500','550','500','1900','300')

adb('shell','am','force-stop',PACKAGE)
adb('shell','monkey','-p',PACKAGE,'1')
expect('Catálogo de produtos')
screenshot('01-inicio')
fill('name','Caderno Android')
fill('price','29.90')
fill('categories','Papelaria')
fill('attributes','Cor: Verde')
tap('save')
expect('Cadastro realizado com sucesso.')
expect('Caderno Android')
screenshot('02-produto')
top()
fill('name','Produto invalido')
fill('price','0')
tap('save')
expect('preço positivo')
screenshot('03-preco-invalido')
top();tap('customers-tab')
expect('Cadastro de clientes')
fill('name','Marina Android')
fill('email','MARINA@example.com')
tap('save')
expect('Cadastro realizado com sucesso.')
expect('marina@example.com')
screenshot('04-cliente')
fill('name','Outra Pessoa')
fill('email','marina@example.com')
tap('save')
expect('já está cadastrado')
screenshot('05-duplicidade')
adb('shell','am','force-stop',PACKAGE)
adb('shell','monkey','-p',PACKAGE,'1')
expect('Catálogo de produtos')
adb('shell','input','swipe','500','1800','500','700','300')
expect('Caderno Android')
screenshot('06-produto-reinicio')
top();tap('customers-tab')
expect('marina@example.com')
screenshot('07-cliente-reinicio')
files=adb('shell','run-as',PACKAGE,'ls','-l','files').decode()
assert 'products.db' in files and 'customers.sqlite' in files,files
result={'result':'PASS','device':adb('shell','getprop','ro.product.model').decode().strip(),'api':adb('shell','getprop','ro.build.version.sdk').decode().strip(),'abi':adb('shell','getprop','ro.product.cpu.abi').decode().strip(),'checks':['APK instalado e iniciado','produto LiteDB cadastrado e listado','preço zero rejeitado','cliente SQLite cadastrado e listado','email normalizado','email duplicado rejeitado','force-stop e reabertura mantêm os dois cadastros','dois arquivos de bancos no sandbox Android'],'databaseFiles':files,'physicalDevice':False}
(OUT/'results.json').write_text(json.dumps(result,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps(result,indent=2,ensure_ascii=False))
