"""Sincroniza a captura original com uma narração e transcrição com tempos de palavras.
A voz permanece na velocidade original. Cada cena do vídeo acompanha seu parágrafo.
"""
import argparse,difflib,json,re,shutil,subprocess,unicodedata,textwrap
from pathlib import Path
import imageio_ffmpeg
from PIL import Image,ImageDraw,ImageFont

def transcribe(audio, output):
    """Recognize locally; only the Whisper model is downloaded on first use."""
    import os
    import numpy as np
    import torch
    import whisper
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    raw = subprocess.check_output([ffmpeg, '-v', 'error', '-i', str(audio), '-f', 'f32le', '-ac', '1', '-ar', '16000', '-'])
    samples = np.frombuffer(raw, np.float32).copy()
    torch.set_num_threads(min(6, os.cpu_count() or 4))
    model = whisper.load_model('base')
    result = model.transcribe(samples, language='pt', word_timestamps=True, fp16=False, verbose=False,
        initial_prompt='EasyShop. Aplicativo Android. Cadastro de produtos e clientes. Categorias, atributos e e-mail.')
    output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    return result

def normalize(text):
    return re.sub('[^a-z0-9]','',unicodedata.normalize('NFD',text.lower()).encode('ascii','ignore').decode())
def stamp(value,ass=False):
    ms=round(value*(100 if ass else 1000));unit=100 if ass else 1000
    h,ms=divmod(ms,3600*unit);m,ms=divmod(ms,60*unit);s,frac=divmod(ms,unit)
    return f'{h}:{m:02}:{s:02}.{frac:02}' if ass else f'{h:02}:{m:02}:{s:02},{frac:03}'
def escape(text):return text.replace('{','').replace('}','').replace('\n',r'\N')

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--audio',required=True,type=Path)
    parser.add_argument('--transcription',type=Path,help='JSON Whisper existente; quando omitido, transcreve localmente.')
    parser.add_argument('--source',type=Path,default=Path('artifacts/video/demonstracao'))
    parser.add_argument('--output',type=Path,default=Path('artifacts/video/elevenlabs'))
    args=parser.parse_args();source=args.source.resolve();out=args.output.resolve();out.mkdir(parents=True,exist_ok=True)
    timeline=json.loads((source/'timeline.json').read_text(encoding='utf-8'))
    transcript=(json.loads(args.transcription.read_text(encoding='utf-8')) if args.transcription
        else transcribe(args.audio, out/'transcricao.json'))
    words=[w for s in transcript['segments'] for w in s['words']]
    if not words:raise RuntimeError('A transcrição não contém palavras com tempos.')
    canonical=[];offsets=[]
    for scene in timeline:
        offsets.append(len(canonical));canonical.extend(scene['text'].split())
    # Preserve the extra spoken closing credits, absent from the original script.
    credits=next((i for i,w in enumerate(words) if normalize(w['word'])=='desenvolvido'),len(words))
    canonical.extend(w['word'].strip() for w in words[credits:])
    matcher=difflib.SequenceMatcher(None,list(map(normalize,canonical)),[normalize(w['word']) for w in words],autojunk=False)
    if matcher.ratio()<.85:raise RuntimeError('A narração diverge do roteiro; revise antes de sincronizar.')
    aligned=[None]*len(canonical)
    for op,a,b,c,d in matcher.get_opcodes():
        if op=='equal':
            for i,j in zip(range(a,b),range(c,d)):aligned[i]={'text':canonical[i],'start':words[j]['start'],'end':words[j]['end']}
        elif op in ('replace','delete'):
            start=words[c]['start'] if c<len(words) else words[-1]['end']
            end=words[d-1]['end'] if d>c else start+.01
            for i in range(a,b):
                aligned[i]={'text':canonical[i],'start':start+(end-start)*(i-a)/(b-a),'end':start+(end-start)*(i-a+1)/(b-a)}
    assert all(aligned)
    ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
    audio=out/'narracao-elevenlabs.mp3'
    if args.audio.resolve()!=audio.resolve():shutil.copyfile(args.audio,audio)
    raw=subprocess.check_output([ffmpeg,'-v','error','-i',str(audio),'-f','f32le','-ac','1','-ar','16000','-'])
    audio_duration=len(raw)/4/16000
    total=audio_duration+1
    boundaries=[0]+[aligned[i]['start'] for i in offsets[1:]]+[total]
    if any(b<=a for a,b in zip(boundaries,boundaries[1:])):
        raise RuntimeError('Não foi possível identificar a ordem das cenas na narração.')
    old_boundaries=[0]+[s['start'] for s in timeline[1:]]+[timeline[-1]['end']]
    scenes=[]
    for i,s in enumerate(timeline):
        scenes.append({**s,'start':boundaries[i],'end':boundaries[i+1],'originalStart':old_boundaries[i],'originalEnd':old_boundaries[i+1]})
    print('Alinhamento:',round(matcher.ratio()*100,1),'%',flush=True)
    for scene in scenes:print(scene['title'],round(scene['start'],2),round(scene['end'],2),flush=True)
    captions=[];chunk=[]
    for word in aligned:
        if chunk and (len(' '.join(w['text'] for w in chunk))+len(word['text'])>86 or word['end']-chunk[0]['start']>5):
            captions.append(chunk);chunk=[]
        chunk.append(word)
        if word['text'].endswith(('.', '?', '!')) and len(' '.join(w['text'] for w in chunk))>=28:
            captions.append(chunk);chunk=[]
    if chunk:captions.append(chunk)
    srt=[];events=[]
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
    credit_start=words[credits]['start'] if credits<len(words) else total
    for scene in scenes:
        end=min(scene['end'],credit_start) if scene['index']==7 else scene['end']
        for style,text in [('Title',scene['title']),('Summary',scene['summary'])]:
            events.append(f"Dialogue: 0,{stamp(scene['start'],True)},{stamp(end,True)},{style},,0,0,0,,{escape(text)}")
    if credits<len(words):
        names=' '.join(w['word'].strip() for w in words[credits+2:]).rstrip('.')
        events.append(f'Dialogue: 0,{stamp(credit_start,True)},{stamp(total,True)},Title,,0,0,0,,Desenvolvido por')
        events.append(f'Dialogue: 0,{stamp(credit_start,True)},{stamp(total,True)},Summary,,0,0,0,,{escape(names)}')
    for i,chunk in enumerate(captions):
        start=chunk[0]['start'];end=chunk[-1]['end']+.08
        if i+1<len(captions):end=min(end,captions[i+1][0]['start'])
        end=max(start+.01,end);text='\n'.join(textwrap.wrap(' '.join(w['text'] for w in chunk),width=62,break_long_words=False,break_on_hyphens=False))
        srt.append(f'{i+1}\n{stamp(start)} --> {stamp(end)}\n{text}\n')
        events.append(f'Dialogue: 1,{stamp(start,True)},{stamp(end,True)},Captions,,0,0,0,,'+r'{\pos(1225,795)}'+escape(text))
    (out/'legendas-elevenlabs.srt').write_text('\n'.join(srt),encoding='utf-8-sig')
    (out/'legendas-elevenlabs.ass').write_text(header+'\n'.join(events),encoding='utf-8-sig')
    (out/'alinhamento.json').write_text(json.dumps({'match':matcher.ratio(),'audioDuration':audio_duration,'duration':total,'scenes':scenes,'words':aligned},ensure_ascii=False,indent=2),encoding='utf-8')
    background=Image.open(source/'fundo.png').convert('RGB');draw=ImageDraw.Draw(background)
    draw.rectangle((620,954,1840,1015),fill='white')
    draw.text((630,968),'EasyShop  •  Android  •  Dados fictícios  •  Narração: ElevenLabs',font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',22),fill='#526c60')
    background.save(out/'fundo-elevenlabs.png')
    # Split, retime and concatenate only video. Original narration is never used.
    filters=['[0:v]scale=432:960,setsar=1,split=8'+''.join(f'[s{i}]' for i in range(8))]
    for i,scene in enumerate(scenes):
        ratio=(scene['end']-scene['start'])/(scene['originalEnd']-scene['originalStart'])
        filters.append(f"[s{i}]trim=start={scene['originalStart']}:end={scene['originalEnd']},setpts=(PTS-STARTPTS)*{ratio},fps=30[v{i}]")
    filters.append(''.join(f'[v{i}]' for i in range(8))+'concat=n=8:v=1:a=0[phone]')
    filters+=['[1:v][phone]overlay=74:60,ass=legendas-elevenlabs.ass[video]','[2:a]loudnorm=I=-16:TP=-1.5:LRA=11,apad[audio]']
    command=[ffmpeg,'-hide_banner','-y','-i',str(source/'tela-original.mp4'),'-loop','1','-i','fundo-elevenlabs.png','-i','narracao-elevenlabs.mp3','-filter_complex',';'.join(filters),'-map','[video]','-map','[audio]','-t',str(total),'-r','30','-c:v','libx264','-preset','fast','-crf','20','-pix_fmt','yuv420p','-c:a','aac','-ar','48000','-b:a','192k','-movflags','+faststart','EasyShop - Video Completo - ElevenLabs.mp4']
    with (out/'render-elevenlabs.log').open('w',encoding='utf-8') as log:subprocess.run(command,cwd=out,stdout=log,stderr=subprocess.STDOUT,check=True)
    print(out/'EasyShop - Video Completo - ElevenLabs.mp4',flush=True)

if __name__=='__main__':main()
