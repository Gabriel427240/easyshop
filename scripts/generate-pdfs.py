"""Gerar os dois PDFs a partir dos Markdown e capturas reais. Requer reportlab."""
from pathlib import Path
from xml.sax.saxutils import escape
import re
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Image, Table, TableStyle, PageBreak, KeepTogether
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.utils import ImageReader

ROOT = Path(__file__).resolve().parents[1]
styles = getSampleStyleSheet()
styles['Normal'].fontName = 'Helvetica'
styles['Normal'].fontSize = 10
styles['Normal'].leading = 15
styles['Normal'].spaceAfter = 9
styles['Title'].textColor = colors.black
styles['Title'].fontSize = 24
styles['Title'].leading = 29
styles['Title'].alignment = TA_LEFT
styles['Title'].spaceAfter = 22
styles['Heading2'].textColor = colors.black
styles['Heading2'].spaceBefore = 16
styles['Heading2'].spaceAfter = 9
styles.add(ParagraphStyle(name='Cell',fontName='Helvetica',fontSize=8,leading=11,spaceAfter=0))

def inline(text):
    text = escape(text)
    text = re.sub(r'\*\*(.+?)\*\*', r'<b>\1</b>',text)
    return re.sub(r'`(.+?)`',r'<font name="Courier">\1</font>',text)

def footer(canvas, doc):
    canvas.setFont('Helvetica',8)
    canvas.setFillColor(colors.HexColor('#6c7e85'))
    canvas.drawString(44,28,'EasyShop | Documentação acadêmica | 11/11/2026')
    canvas.drawRightString(551,28,str(doc.page))

def generate(name):
    source=ROOT/'docs'/f'{name}.md'
    lines=source.read_text(encoding='utf-8').splitlines()
    story=[]; i=0
    while i<len(lines):
        line=lines[i].strip()
        if line.startswith('|'):
            rows=[]
            while i<len(lines) and lines[i].strip().startswith('|'):
                cells=[c.strip() for c in lines[i].strip().strip('|').split('|')]
                if not all(re.fullmatch(r'[-: ]+',c) for c in cells): rows.append([Paragraph(inline(c),styles['Cell']) for c in cells])
                i+=1
            table=Table(rows,colWidths=[99,130,148,130],repeatRows=1,hAlign='LEFT')
            table.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),colors.HexColor('#e7efec')),('GRID',(0,0),(-1,-1),.5,colors.HexColor('#d9d9d9')),('VALIGN',(0,0),(-1,-1),'MIDDLE'),('LEFTPADDING',(0,0),(-1,-1),7),('RIGHTPADDING',(0,0),(-1,-1),7),('TOPPADDING',(0,0),(-1,-1),8),('BOTTOMPADDING',(0,0),(-1,-1),8)]))
            story.extend([table,Spacer(1,12)]);continue
        if line.startswith('!['):
            match=re.match(r'!\[(.*?)\]\((.*?)\)',line)
            path=source.parent/match.group(2)
            w,h=ImageReader(str(path)).getSize();width=507;height=width*h/w
            if height>450:width*=450/height;height=450
            story.extend([Image(str(path),width=width,height=height),Spacer(1,8),Paragraph(escape(match.group(1)),styles['Normal'])])
        elif line.startswith('# '):story.append(Paragraph(inline(line[2:]),styles['Title']))
        elif line.startswith('## '):
            if name == 'manual-do-usuario' and line in ['## Clientes','## Erros e recuperação']: story.append(PageBreak())
            story.append(Paragraph(inline(line[3:]),styles['Heading2']))
        elif line:story.append(KeepTogether([Paragraph(inline(line),styles['Normal'])]))
        i+=1
    doc=SimpleDocTemplate(str(source.with_suffix('.pdf')),pagesize=(595.28,841.89),leftMargin=44,rightMargin=44,topMargin=44,bottomMargin=48,title=lines[0].lstrip('# '),author='EasyShop')
    doc.build(story,onFirstPage=footer,onLaterPages=footer)

for name in ['relatorio-executivo','manual-do-usuario']:generate(name)
