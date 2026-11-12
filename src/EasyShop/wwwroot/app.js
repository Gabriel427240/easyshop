const $ = s => document.querySelector(s);
const money = new Intl.NumberFormat('pt-BR', {style:'currency',currency:'BRL'});
function element(tag, text, cls) { const node=document.createElement(tag); node.textContent=text; if(cls)node.className=cls; return node; }
function notice(text,error=false) { const box=$('#notice'); box.textContent=text; box.className=error?'error':''; box.hidden=false; }
async function api(path,body) {
  const response=await fetch('/api/'+path,body?{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)}:{});
  if(!response.ok){let data;try{data=await response.json();}catch{} throw new Error(data?.error||'Não foi possível concluir a operação. Tente novamente.');}
  return response.json();
}
async function load(kind) {
  const list=$(kind==='products'?'#product-list':'#customer-list');
  list.replaceChildren(element('p','Carregando…','empty'));
  try {
    const items=await api(kind); $(kind==='products'?'#product-count':'#customer-count').textContent=items.length;
    list.replaceChildren();
    if(!items.length) {const empty=element('div','','empty');empty.append(element('b',kind==='products'?'Seu catálogo começa aqui':'Seus contatos começam aqui'),element('p','Use o formulário ao lado para fazer o primeiro cadastro.'));list.append(empty);}
    for(const item of items){
      const card=element('article','','item');const head=element('div','','item-head');head.append(element('h3',kind==='products'?item.name:item.fullName));
      if(kind==='products'){
        head.append(element('span',money.format(item.price),'price'));card.append(head);
        const tags=element('div','','tags');for(const c of item.categories)tags.append(element('span',c,'tag'));card.append(tags);
        if(Object.keys(item.attributes).length)card.append(element('p',Object.entries(item.attributes).map(([k,v])=>`${k}: ${v}`).join(' · '),'attributes'));
      }else{card.append(head,element('p',item.email,'email'));}
      list.append(card);
    }
    return true;
  } catch(error){list.replaceChildren(element('p',error.message,'empty'));notice(error.message,true);return false;}
}
document.querySelectorAll('[data-view]').forEach(button=>button.addEventListener('click',()=>{
 const products=button.dataset.view==='products';$('#products').hidden=!products;$('#customers').hidden=products;
 document.querySelectorAll('[data-view]').forEach(b=>{b.classList.toggle('active',b===button);b.setAttribute('aria-current',b===button?'page':'false');});
 $('#page-title').textContent=products?'Catálogo de produtos':'Cadastro de clientes';$('#subtitle').textContent=products?'Cadastre os detalhes que fazem cada produto ser único.':'Organize os contatos de quem faz parte do seu negócio.';$('#notice').hidden=true;load(button.dataset.view);
}));
document.querySelectorAll('[data-reload]').forEach(b=>b.addEventListener('click',()=>load(b.dataset.reload)));
async function submit(event,kind,makeBody){
 event.preventDefault();const form=event.target, button=form.querySelector('button');button.disabled=true;const original=button.textContent;button.textContent='Salvando…';
 try{await api(kind,makeBody(new FormData(form)));form.reset();const refreshed=await load(kind);notice(refreshed?'Cadastro realizado com sucesso.':'Cadastro salvo, mas a lista não pôde ser atualizada. Clique em Atualizar.',!refreshed);}catch(error){notice(error.message,true);}finally{button.disabled=false;button.textContent=original;}
}
$('#product-form').addEventListener('submit',event=>submit(event,'products',data=>{
 const raw=data.get('price').trim();if(!/^\d+(?:[,.]\d{1,2})?$/.test(raw))throw new Error('Use um preço como 29,90, sem separador de milhar.');
 const attributes=Object.create(null);for(const line of data.get('attributes').split('\n').filter(l=>l.trim())){const split=line.indexOf(':');if(split<1||!line.slice(split+1).trim())throw new Error('Preencha cada atributo no formato nome: valor.');const key=line.slice(0,split).trim();if(Object.hasOwn(attributes,key))throw new Error('Não repita o nome de um atributo.');attributes[key]=line.slice(split+1).trim();}
 return {name:data.get('name'),price:Number(raw.replace(',','.')),categories:data.get('categories').split(',').map(c=>c.trim()).filter(Boolean),attributes};
}));
$('#customer-form').addEventListener('submit',event=>submit(event,'customers',data=>({fullName:data.get('fullName'),email:data.get('email')})));
load('products');
