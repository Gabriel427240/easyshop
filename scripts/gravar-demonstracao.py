"""Grava uma demonstração Android real com voz pt-BR, legendas e verificações.
Use a instalação isolada br.edu.easyshop.demo; não limpa dados nem publica vídeos.
Dependências: adb, Python, Pillow, imageio-ffmpeg e voz Microsoft Maria Desktop.
"""
import argparse, json, re, subprocess, time, wave, xml.etree.ElementTree as ET
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCENES = [
    ("Conheça o EasyShop", "Produtos e clientes organizados em um só lugar.\nUso local, sem depender da internet.",
     "Este é o EasyShop, um aplicativo para organizar produtos e clientes. A demonstração acontece em um emulador Android, com uma instalação separada e dados fictícios. Vamos acompanhar o preenchimento dos formulários, as orientações de uso e as mensagens de validação. Os cadastros ficam salvos no próprio dispositivo."),
    ("Cadastre um produto", "Nome e preço são obrigatórios.\nCategorias e atributos são opcionais.",
     "Para cadastrar um produto, começamos pelo nome e pelo preço. O campo de preço aceita vírgula ou ponto para os centavos, sem separador de milhar. As categorias são separadas por vírgula. Já os atributos usam uma informação por linha, no formato nome, dois pontos e valor. O teclado abre ao tocar no campo e a tela acompanha a digitação."),
    ("Confira o catálogo", "Preço formatado em reais.\nDetalhes organizados em cartões.",
     "Ao tocar em cadastrar produto, o aplicativo confirma a operação e atualiza a lista. O item aparece em um cartão, com preço em reais, categorias e atributos. Os campos ficam livres para um novo cadastro. O botão atualizar lista permite consultar novamente os dados que estão armazenados no dispositivo."),
    ("Corrija sem perder o trabalho", "Preço zero é rejeitado.\nA orientação aparece junto ao campo.",
     "Agora vamos testar uma entrada inválida. Um preço igual a zero não pode ser cadastrado. O formulário mostra uma explicação abaixo do preço e direciona o foco para o campo que precisa de correção. Os valores digitados permanecem no formulário. Assim, a pessoa pode corrigir o problema sem preencher tudo de novo."),
    ("Organize seus clientes", "Informe nome e sobrenome.\nCada e-mail deve ser único.",
     "Na aba clientes, informamos nome completo e e-mail. As instruções explicam os campos obrigatórios e o limite de preenchimento. Ao concluir, o cliente aparece na lista de contatos. O endereço de e-mail é normalizado para letras minúsculas, facilitando a leitura e a identificação de cadastros repetidos."),
    ("Valide os contatos", "E-mail inválido: corrigir o formato.\nE-mail repetido: cadastro impedido.",
     "Também vamos verificar os erros no cadastro de clientes. Um endereço fora do formato de e-mail recebe uma orientação no próprio campo. Se tentarmos cadastrar novamente um e-mail que já existe, o aplicativo informa a duplicidade. A mensagem explica o problema e mantém os dados digitados, para que a pessoa consiga corrigi-los."),
    ("Dados preservados", "Fechar e reabrir mantém os cadastros.\nProdutos e clientes continuam disponíveis.",
     "Para verificar a permanência dos dados, encerramos o aplicativo e abrimos novamente. O produto continua no catálogo e o cliente permanece na lista de contatos. Este teste não limpa o armazenamento e não depende de um servidor externo. Os rascunhos também são preservados ao alternar entre as abas durante o uso."),
    ("Pronto para usar", "Cadastre. Confira. Corrija quando necessário.\nDemonstração real em emulador Android.",
     "Esta foi a demonstração do EasyShop. Vimos cadastro e consulta de produtos e clientes, teclado em funcionamento, orientações de preenchimento, validações e dados preservados após reabrir o aplicativo. A automação registra o resultado de cada verificação. O vídeo mostra o aplicativo real em um emulador, sem simular resultados e sem publicar qualquer informação na internet.")
]

def run(args, **kwargs):
    return subprocess.run(args, check=True, **kwargs)

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--serial', default='emulator-5554')
    parser.add_argument('--package', default='br.edu.easyshop.demo')
    parser.add_argument('--output', type=Path)
    args=parser.parse_args()
    if args.package != 'br.edu.easyshop.demo':
        parser.error('Use a instalação separada br.edu.easyshop.demo para esta demonstração.')
    out=(args.output or ROOT/'artifacts'/'video'/datetime.now().strftime('%Y%m%d-%H%M%S')).resolve()
    out.mkdir(parents=True, exist_ok=True)
    adb_base=['adb','-s',args.serial]
    def adb(*cmd):return subprocess.check_output(adb_base+list(cmd),stderr=subprocess.STDOUT)
    assert args.package in adb('shell','pm','list','packages',args.package).decode(), 'Instale o APK Demo primeiro.'
    existing=subprocess.run(adb_base+['shell','pidof','screenrecord'],capture_output=True).stdout.strip()
    if existing:raise RuntimeError('Já existe uma gravação no emulador. Encerre-a antes de continuar.')
    checks=[]
    def tree():
        for attempt in range(5):
            try:
                adb('shell','uiautomator','dump','/sdcard/easyshop-demo.xml')
                return ET.fromstring(adb('shell','cat','/sdcard/easyshop-demo.xml'))
            except (ET.ParseError,subprocess.CalledProcessError):time.sleep(.5)
        raise RuntimeError('Não foi possível ler a tela Android.')
    def node(desc=None, text=None):
        return next((n for n in tree().iter('node') if (desc is None or n.get('content-desc')==desc) and (text is None or text in n.get('text',''))),None)
    def swipe(up=True):adb('shell','input','swipe','980','2100' if up else '500','980','600' if up else '2100','450')
    def top():
        for _ in range(3):swipe(False)
    def tap(desc):
        for _ in range(8):
            n=node(desc)
            if n is not None:
                l,t,r,b=map(int,re.findall(r'\d+',n.get('bounds')))
                if b>t and r>l and b-t>20:
                    adb('shell','input','tap',str((l+r)//2),str((t+b)//2));return
            swipe()
        raise AssertionError('Controle não encontrado: '+desc)
    def fill(desc,value,replace=False):
        tap(desc)
        time.sleep(.5)
        if replace:
            n=node(desc)
            old=n.get('text','') if n is not None else ''
            adb('shell','input','keyevent','123')
            if old:adb('shell','input','keyevent',*(['67']*len(old)))
        for i,line in enumerate(value.split('\n')):
            if i:adb('shell','input','keyevent','66')
            adb('shell','input','text',line.replace(' ','%s'))
        time.sleep(.7)
        adb('shell','input','keyevent','4')
    def expect(text, label):
        for attempt in range(8):
            if node(text=text) is not None:
                checks.append(label);print('PASS:',label,flush=True);return
            swipe()
        raise AssertionError('Texto esperado não encontrado: '+text)
    def shot(name): (out/(name+'.png')).write_bytes(adb('exec-out','screencap','-p'))
    def open_app():
        adb('shell','monkey','-p',args.package,'1')
        for _ in range(10):
            if node('name') is not None:return
            time.sleep(.5)
        raise AssertionError('O aplicativo não abriu.')

    narration=[{'title':s[0],'summary':s[1],'text':s[2]} for s in SCENES]
    (out/'narracao.json').write_text(json.dumps(narration,ensure_ascii=False),encoding='utf-8')
    voice_script=ROOT/'scripts'/'narrar-demonstracao.ps1'
    run(['powershell','-NoProfile','-ExecutionPolicy','Bypass','-File',str(voice_script),'-Directory',str(out)])
    durations=[]
    for i in range(len(SCENES)):
        with wave.open(str(out/f'voz-{i:02}.wav')) as audio:durations.append(audio.getnframes()/audio.getframerate())
    adb('shell','am','force-stop',args.package)
    open_app()
    token=datetime.now().strftime('%d%H%M%S')
    product='Caderno demonstracao '+token
    email='marina.demo.'+token+'@example.com'
    remote='/sdcard/easyshop-demo-'+token+'.mp4'
    recording=subprocess.Popen(adb_base+['shell','screenrecord','--size','720x1600','--bit-rate','4M','--time-limit','0',remote],stdout=subprocess.DEVNULL,stderr=subprocess.PIPE)
    t0=time.monotonic();time.sleep(1)
    pid=adb('shell','pidof','screenrecord').decode().strip()
    timeline=[]
    def scene(index,action):
        start=time.monotonic()-t0
        print('CENA:',SCENES[index][0],flush=True)
        action()
        remaining=durations[index]+2-(time.monotonic()-t0-start)
        if remaining>0:time.sleep(remaining)
        timeline.append({'index':index,'start':start,'end':time.monotonic()-t0,**narration[index]})
        shot(f'cena-{index+1:02}')
    try:
        scene(0,lambda: expect('Novo produto','Aplicativo Demo aberto'))
        def product_form():
            fill('name',product);fill('price','29,90');fill('categories','Papelaria, Escritorio');fill('attributes','Cor: Verde\nTamanho: A5')
            tap('attributes');time.sleep(2);shot('teclado');adb('shell','input','keyevent','4')
            checks.append('Digitação e atributos com múltiplas linhas')
        scene(1,product_form)
        def product_save():
            tap('save');expect('Produto cadastrado!','Produto cadastrado com sucesso');expect(product,'Produto listado');expect('29,90','Preço em reais na lista')
        scene(2,product_save)
        def invalid_price():
            top();fill('name','Produto com preco invalido');fill('price','0');tap('save');expect('Informe de R$ 0,01','Preço zero rejeitado no formulário')
        scene(3,invalid_price)
        def customer_save():
            top();tap('customers-tab');fill('name','Marina Exemplo');fill('email',email.upper());tap('save')
            expect('Cliente cadastrado!','Cliente cadastrado com sucesso');expect(email,'E-mail normalizado na listagem')
        scene(4,customer_save)
        def invalid_customer():
            top();fill('name','Outra Pessoa');fill('email','email-invalido');tap('save');expect('Informe um e-mail válido','Formato inválido de e-mail rejeitado')
            fill('email',email,True);tap('save');expect('Este e-mail já está cadastrado.','E-mail duplicado rejeitado')
        scene(5,invalid_customer)
        def persistence():
            top();tap('products-tab');expect('Produto com preco invalido','Rascunho preservado ao trocar de aba')
            adb('shell','am','force-stop',args.package);open_app();expect(product,'Produto preservado após reabrir')
            top();tap('customers-tab');expect(email,'Cliente preservado após reabrir')
        scene(6,persistence)
        def ending():top();tap('products-tab');expect(product,'Catálogo disponível ao final')
        scene(7,ending)
    except Exception:
        shot('falha');raise
    finally:
        if pid:subprocess.run(adb_base+['shell','kill','-2',pid],capture_output=True)
        recording.wait(timeout=20)
        run(adb_base+['pull',remote,str(out/'tela-original.mp4')],stdout=subprocess.DEVNULL)
        (out/'timeline.json').write_text(json.dumps(timeline,ensure_ascii=False,indent=2),encoding='utf-8')
    (out/'resultado.json').write_text(json.dumps({'result':'PASS','package':args.package,'serial':args.serial,'checks':checks,'physicalDevice':False},ensure_ascii=False,indent=2),encoding='utf-8')
    render(out,timeline)
    print('VIDEO:',out/'easyshop-demonstracao.mp4',flush=True)


def render(out,timeline):
    import imageio_ffmpeg
    from PIL import Image,ImageDraw,ImageFont
    ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
    image=Image.new('RGB',(1920,1080),'#f2f6f0');draw=ImageDraw.Draw(image)
    draw.rounded_rectangle((48,36,532,1044),radius=40,fill='#173b32')
    draw.rounded_rectangle((586,36,1872,1044),radius=32,fill='#ffffff')
    draw.rounded_rectangle((630,78,855,120),radius=20,fill='#e6f3eb')
    font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',20)
    draw.text((660,85),'DEMONSTRAÇÃO',font=font,fill='#136b53')
    draw.text((630,968),'EasyShop  •  Android  •  Dados fictícios  •  Voz sintética',font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',22),fill='#526c60')
    draw.rounded_rectangle((625,670,1830,926),radius=20,fill='#173b32')
    image.save(out/'fundo.png')
    def timestamp(value,ass=False):
        h=int(value//3600);m=int(value//60)%60;s=value%60
        return f'{h}:{m:02}:{s:05.2f}' if ass else f'{h:02}:{m:02}:{int(s):02},{int((s%1)*1000):03}'
    def esc(text):return text.replace('\\','\\\\').replace('{','').replace('}','').replace('\n',r'\N')
    header='''[Script Info]
ScriptType: v4.00+
PlayResX: 1920
PlayResY: 1080
WrapStyle: 0
[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Title,Segoe UI,53,&H00323B17,&H000000FF,&H00FFFFFF,&H00000000,-1,0,0,0,100,100,0,0,1,0,0,7,630,100,190,1
Style: Summary,Segoe UI,36,&H00606C52,&H000000FF,&H00FFFFFF,&H00000000,0,0,0,0,100,100,0,0,1,0,0,7,630,140,350,1
Style: Captions,Segoe UI,35,&H00FFFFFF,&H000000FF,&H00323B17,&H00000000,0,0,0,0,100,100,0,0,1,0,0,5,670,130,0,1
[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
'''
    events=[];srt=[];counter=1
    for scene in timeline:
        start,end=scene['start'],scene['end']
        for style,text in [('Title',scene['title']),('Summary',scene['summary'])]:
            events.append(f'Dialogue: 0,{timestamp(start,True)},{timestamp(end,True)},{style},,0,0,0,,{esc(text)}')
        chunks=[];chunk=[]
        for word in scene['text'].split():
            chunk.append(word)
            if len(' '.join(chunk))>=100:chunks.append(' '.join(chunk));chunk=[]
        if chunk:chunks.append(' '.join(chunk))
        with wave.open(str(out/f"voz-{scene['index']:02}.wav")) as audio:duration=audio.getnframes()/audio.getframerate()
        offset=start
        words=sum(len(c.split()) for c in chunks)
        for caption in chunks:
            finish=offset+duration*len(caption.split())/words
            events.append(f'Dialogue: 1,{timestamp(offset,True)},{timestamp(finish,True)},Captions,,0,0,0,,'+r'{\pos(1225,795)}'+esc(caption))
            srt.append(f'{counter}\n{timestamp(offset)} --> {timestamp(finish)}\n{caption}\n');counter+=1;offset=finish
    (out/'legendas.ass').write_text(header+'\n'.join(events),encoding='utf-8-sig')
    (out/'legendas.srt').write_text('\n'.join(srt),encoding='utf-8-sig')
    command=[ffmpeg,'-hide_banner','-y','-i','tela-original.mp4','-loop','1','-i','fundo.png']
    for s in timeline:command+=['-i',f"voz-{s['index']:02}.wav"]
    filters=['[0:v]scale=432:960,setsar=1[phone]','[1:v][phone]overlay=74:60,ass=legendas.ass[v]']
    for i,s in enumerate(timeline):filters.append(f'[{i+2}:a]adelay={round(s["start"]*1000)}:all=1[a{i}]')
    filters.append(''.join(f'[a{i}]' for i in range(len(timeline)))+f'amix=inputs={len(timeline)}:normalize=0,apad,loudnorm=I=-16:TP=-1.5:LRA=11[a]')
    command+=['-filter_complex',';'.join(filters),'-map','[v]','-map','[a]','-t',str(timeline[-1]['end']),'-r','30','-c:v','libx264','-preset','fast','-crf','21','-pix_fmt','yuv420p','-c:a','aac','-b:a','160k','-movflags','+faststart','easyshop-demonstracao.mp4']
    with (out/'render.log').open('w',encoding='utf-8') as log:run(command,cwd=out,stdout=log,stderr=subprocess.STDOUT)

if __name__=='__main__':main()
