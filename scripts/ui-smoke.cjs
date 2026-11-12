// Execute com Playwright instalado: npm install --prefix <pasta-temporaria> playwright
// NODE_PATH=<pasta-temporaria>/node_modules node scripts/ui-smoke.cjs
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async()=>{
 const browser=await chromium.launch({headless:true});
 const page=await browser.newPage({viewport:{width:1440,height:1100}});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 const output='docs/evidencias'; fs.mkdirSync(output,{recursive:true});
 await page.goto('http://127.0.0.1:5080');
 await page.getByText('Seu catálogo começa aqui').waitFor();
 await page.screenshot({path:output+'/produtos-vazio.png',fullPage:true});
 await page.getByLabel('Nome do produto').fill('Caderno pontilhado');
 await page.getByLabel('Preço (R$)').fill('29,90');
 await page.getByLabel('Categorias').fill('Papelaria, Escritório');
 await page.getByLabel('Atributos').fill('Cor: Verde\nTamanho: A5');
 await page.getByRole('button',{name:'Cadastrar produto',exact:false}).click();
 await page.getByRole('status').filter({hasText:'Cadastro realizado com sucesso.'}).waitFor();
 await page.getByRole('heading',{name:'Caderno pontilhado',exact:true}).waitFor();
 await page.screenshot({path:output+'/produtos.png',fullPage:true});
 await page.getByLabel('Nome do produto').fill('Inválido');await page.getByLabel('Preço (R$)').fill('0');
 await page.getByRole('button',{name:'Cadastrar produto',exact:false}).click();
 await page.getByRole('status').filter({hasText:'preço positivo'}).waitFor();
 await page.getByRole('button',{name:'Clientes',exact:false}).click();
 await page.getByText('Seus contatos começam aqui').waitFor();
 await page.getByLabel('Nome completo').fill('Marina Exemplo');await page.getByLabel('E-mail').fill('MARINA@example.com');
 await page.getByRole('button',{name:'Cadastrar cliente',exact:false}).click();
 await page.getByRole('heading',{name:'Marina Exemplo',exact:true}).waitFor();
 await page.screenshot({path:output+'/clientes.png',fullPage:true});
 await page.getByLabel('Nome completo').fill('Outra Pessoa');await page.getByLabel('E-mail').fill('marina@example.com');
 await page.getByRole('button',{name:'Cadastrar cliente',exact:false}).click();
 await page.getByRole('status').filter({hasText:'já está cadastrado'}).waitFor();
 await page.screenshot({path:output+'/email-duplicado.png',fullPage:true});
 await page.setViewportSize({width:390,height:844});
 await page.screenshot({path:output+'/mobile.png',fullPage:true});
 assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false,'Sem overflow horizontal');
 assert.deepEqual(errors,[]);
 fs.writeFileSync(output+'/ui-results.json',JSON.stringify({result:'PASS',checks:['estado vazio','cadastro produto 29,90','listagem produto','preço zero rejeitado','cadastro cliente','email normalizado','duplicidade rejeitada','viewport 390 sem overflow','sem erros JavaScript'],screenshots:'capturadas da aplicação em execução'},null,2));
 await browser.close();console.log('UI smoke: PASS');
})().catch(e=>{console.error(e);process.exit(1);});

