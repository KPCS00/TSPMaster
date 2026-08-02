const API = "/api/v1";
const app = document.querySelector("#app");
const state = {
  funds: [],
  dashboard: null,
  fundId: "c",
  fundRange: "1y",
  compareIds: ["g", "f", "c", "s", "i"],
  compareRange: "1y",
  weights: { g: 20, f: 10, c: 45, s: 15, i: 10 },
  portfolio: null,
};
const coreNames = { g: "G Fund", f: "F Fund", c: "C Fund", s: "S Fund", i: "I Fund" };
const colors = ["#2563eb", "#059669", "#d97706", "#7c3aed", "#dc2626", "#0891b2", "#4f46e5", "#65a30d"];

const escapeHtml = value => String(value ?? "").replace(/[&<>'"]/g, char => ({"&":"&amp;","<":"&lt;",">":"&gt;","'":"&#39;",'"':"&quot;"}[char]));
const percent = (value, digits=1, sign=true) => value == null || Number.isNaN(Number(value)) ? "—" : `${sign && value >= 0 ? "+" : ""}${(Number(value)*100).toFixed(digits)}%`;
const money = value => value == null ? "—" : `$${Number(value).toFixed(4)}`;
const shortDate = value => new Intl.DateTimeFormat("en-US", {month:"short",day:"numeric",year:"numeric"}).format(new Date(`${value}T12:00:00`));

async function request(path, options={}) {
  const response = await fetch(`${API}${path}`, {headers:{"Content-Type":"application/json",...(options.headers||{})},...options});
  if (!response.ok) {
    let message = `Request failed (${response.status})`;
    try { message = (await response.json()).detail || message; } catch (_) {}
    throw new Error(message);
  }
  return response.json();
}

function loading(label="Loading analysis…") {
  app.innerHTML = `<div class="loading-card"><span class="spinner"></span>${escapeHtml(label)}</div>`;
}
function errorCard(error) {
  app.innerHTML = `<div class="status-banner error">The dashboard could not load: ${escapeHtml(error.message || error)}</div>`;
}
function statusBanner(text, type="info") { return `<div class="status-banner ${type}">${text}</div>`; }
function metricCard(label, value, detail="", tone="") {
  return `<article class="metric-card ${tone}"><span class="metric-label">${escapeHtml(label)}</span><strong class="metric-value">${escapeHtml(value)}</strong>${detail ? `<span class="metric-detail">${escapeHtml(detail)}</span>` : ""}</article>`;
}
function recommendationCard(rec, compact=false) {
  const cls = rec.score >= 60 ? "positive" : rec.score < 45 ? "negative" : "neutral";
  return `<article class="recommendation-card">
    <div class="recommendation-top"><div><span class="eyebrow">${escapeHtml(rec.fund_name)}</span><h3>${escapeHtml(rec.outlook)}</h3></div><div class="score-ring ${cls}"><strong>${rec.score}</strong><span>/100</span></div></div>
    <p class="action-copy">${escapeHtml(rec.action)}</p>
    ${compact ? "" : `<div class="reason-block"><strong>Why</strong><ul>${rec.drivers.map(x=>`<li>${escapeHtml(x)}</li>`).join("")}</ul></div><div class="reason-block risk"><strong>Watch</strong><ul>${rec.risks.map(x=>`<li>${escapeHtml(x)}</li>`).join("")}</ul></div>`}
    <div class="confidence-row"><span>Signal confidence</span><strong>${Math.round(rec.confidence*100)}%</strong></div>
  </article>`;
}

function chart(data, keys, {currency=false, height=280}={}) {
  if (!data?.length || !keys.length) return `<div class="empty-chart">No chart data is available.</div>`;
  const width=800, top=18, bottom=42, left=56, right=18, plotW=width-left-right, plotH=height-top-bottom;
  const values=[];
  data.forEach(row=>keys.forEach(key=>{ const v=Number(row[key]); if(Number.isFinite(v)) values.push(v); }));
  if (!values.length) return `<div class="empty-chart">No chart data is available.</div>`;
  let min=Math.min(...values), max=Math.max(...values); if(min===max){min-=1;max+=1;} const pad=(max-min)*.08; min-=pad; max+=pad;
  const x=i=>left+(data.length===1?0:i/(data.length-1))*plotW;
  const y=v=>top+(max-v)/(max-min)*plotH;
  const grid=[];
  for(let i=0;i<5;i++){const gy=top+i*plotH/4; const val=max-i*(max-min)/4; grid.push(`<line x1="${left}" y1="${gy}" x2="${width-right}" y2="${gy}" class="chart-grid"/><text x="${left-8}" y="${gy+4}" text-anchor="end" class="chart-axis">${currency?`$${val.toFixed(0)}`:val.toFixed(0)}</text>`);}
  const lines=keys.map((key,k)=>{
    const points=data.map((row,i)=>Number.isFinite(Number(row[key]))?`${x(i).toFixed(1)},${y(Number(row[key])).toFixed(1)}`:null).filter(Boolean).join(" ");
    return `<polyline points="${points}" fill="none" stroke="${colors[k%colors.length]}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>`;
  }).join("");
  const tickIndexes=[0,Math.floor((data.length-1)/2),data.length-1].filter((v,i,a)=>a.indexOf(v)===i);
  const xlabels=tickIndexes.map(i=>`<text x="${x(i)}" y="${height-12}" text-anchor="${i===0?'start':i===data.length-1?'end':'middle'}" class="chart-axis">${new Date(`${data[i].date}T12:00:00`).toLocaleDateString('en-US',{month:'short',year:'2-digit'})}</text>`).join("");
  const legend=keys.map((key,k)=>`<span><i style="background:${colors[k%colors.length]}"></i>${escapeHtml((coreNames[key]||key).toUpperCase())}</span>`).join("");
  return `<div class="svg-chart" style="height:${height}px"><svg viewBox="0 0 ${width} ${height}" preserveAspectRatio="none" role="img" aria-label="Performance chart">${grid.join("")}${lines}${xlabels}</svg></div><div class="chart-legend">${legend}</div>`;
}

function setActiveNav(path) {
  document.querySelectorAll(".nav-item").forEach(link=>link.classList.toggle("active", link.dataset.path===path));
}
function route() {
  const path=(location.hash.slice(1)||"/").split("?")[0];
  setActiveNav(path);
  ({"/":renderDashboard,"/funds":renderFunds,"/compare":renderCompare,"/portfolio":renderPortfolio,"/settings":renderSettings}[path]||renderDashboard)();
  window.scrollTo({top:0,behavior:"instant"});
}

async function ensureFunds(){ if(!state.funds.length) state.funds=await request("/funds"); return state.funds; }

async function renderDashboard() {
  loading();
  try {
    state.dashboard=await request("/dashboard"); const d=state.dashboard;
    app.innerHTML=`<div class="page-stack">
      <section class="hero-card"><div><span class="eyebrow">Daily outlook · ${shortDate(d.as_of)}</span><h1>${escapeHtml(d.market_regime)}</h1><p>The signal engine updates from the latest share prices in your historical dataset.</p></div><div class="hero-score"><span>Leading signal</span><strong>${escapeHtml(d.top_signal.fund_name)}</strong><em>${d.top_signal.score}/100</em></div></section>
      ${statusBanner("Informational research only. The app does not connect to your TSP account or submit investment changes.","warning")}
      <section class="metric-grid three">${metricCard("Market regime",d.market_regime,"Based on individual funds")}${metricCard("Above 200-day average",`${d.funds_above_200d_average} of 5`,"G, F, C, S and I Funds",d.funds_above_200d_average>=4?"positive":"")}${metricCard("Data records",Number(d.data_quality.rows).toLocaleString(),`Healthy through ${shortDate(d.as_of)}`)}</section>
      <section class="panel"><div class="section-heading"><div><span class="eyebrow">Trailing 12 months</span><h2>Core fund comparison</h2></div><span class="chart-note">Indexed to 100</span></div>${chart(d.comparison,["g","f","c","s","i"])}</section>
      <section><div class="section-heading"><div><span class="eyebrow">Ranked signals</span><h2>Current recommendations</h2></div><span class="chart-note">Price model only</span></div><div class="horizontal-cards">${d.recommendations.map(r=>recommendationCard(r,true)).join("")}</div></section>
      <section class="panel narrative-panel"><div class="section-heading"><div><span class="eyebrow">Daily briefing</span><h2>AI interpretation</h2></div></div><p>${escapeHtml(d.narrative.summary)}</p>${!d.news.enabled?`<div class="inline-note">News analysis is staged for the next build. Connect a provider to add event-driven context.</div>`:""}</section>
    </div>`;
  } catch(e){errorCard(e);}
}

async function renderFunds() {
  loading("Loading fund research…");
  try {
    const funds=await ensureFunds();
    const [metrics,history,recs]=await Promise.all([request(`/funds/${state.fundId}/metrics`),request(`/funds/${state.fundId}/history?range=${state.fundRange}`),request("/recommendations")]);
    const rec=recs.find(x=>x.fund_id===state.fundId);
    const individual=funds.filter(x=>x.category==="individual"), lifecycle=funds.filter(x=>x.category==="lifecycle");
    const options=group=>group.map(f=>`<option value="${f.id}" ${f.id===state.fundId?"selected":""}>${escapeHtml(f.name)}</option>`).join("");
    const points=history.points.map(p=>({date:p.date,[state.fundId]:p.value}));
    const ranges=["1m","3m","6m","1y","3y","5y","10y","all"];
    app.innerHTML=`<div class="page-stack">
      <section class="page-title-row"><div><span class="eyebrow">Fund research</span><h1>Explore a fund</h1><p>Review price history, risk, trend and the current model signal.</p></div></section>
      <section class="control-panel"><label class="field-label" for="fund-select">TSP fund</label><select id="fund-select" class="select-control"><optgroup label="Individual funds">${options(individual)}</optgroup><optgroup label="Lifecycle funds">${options(lifecycle)}</optgroup></select><div class="segmented-scroll">${ranges.map(r=>`<button class="segment ${r===state.fundRange?'active':''}" data-range="${r}">${r.toUpperCase()}</button>`).join("")}</div></section>
      <section class="fund-header-card"><div><span class="eyebrow">${escapeHtml(metrics.fund_name)}</span><h2>${money(metrics.latest_price)}</h2><p>As of ${shortDate(metrics.as_of)}</p></div><div class="change-pill ${(metrics.daily_return||0)<0?'negative':'positive'}">${percent(metrics.daily_return)} today</div></section>
      <section class="panel"><div class="section-heading"><div><span class="eyebrow">Share price</span><h2>${state.fundRange.toUpperCase()} history</h2></div><span class="chart-note">${points.length} observations</span></div>${chart(points,[state.fundId],{currency:true})}</section>
      <section class="metric-grid">${metricCard("1 month",percent(metrics.return_1m),"",(metrics.return_1m||0)>=0?"positive":"negative")}${metricCard("3 months",percent(metrics.return_3m),"",(metrics.return_3m||0)>=0?"positive":"negative")}${metricCard("1 year",percent(metrics.return_1y),"",(metrics.return_1y||0)>=0?"positive":"negative")}${metricCard("3-year annual return",percent(metrics.annualized_return_3y))}${metricCard("1-year volatility",percent(metrics.annualized_volatility_1y,1,false))}${metricCard("Current drawdown",percent(metrics.current_drawdown),"",(metrics.current_drawdown||0)<-.05?"negative":"")}</section>
      <section class="panel detail-list"><div class="section-heading"><div><span class="eyebrow">Technical context</span><h2>${escapeHtml(metrics.trend)}</h2></div></div><div><span>50-day average</span><strong>${money(metrics.moving_average_50d)}</strong></div><div><span>200-day average</span><strong>${money(metrics.moving_average_200d)}</strong></div><div><span>Worst 1-year drawdown</span><strong>${percent(metrics.max_drawdown_1y)}</strong></div><div><span>Worst drawdown in dataset</span><strong>${percent(metrics.max_drawdown_all)}</strong></div></section>
      ${rec?recommendationCard(rec):""}
    </div>`;
    document.querySelector("#fund-select").addEventListener("change",e=>{state.fundId=e.target.value;renderFunds();});
    document.querySelectorAll("[data-range]").forEach(btn=>btn.addEventListener("click",()=>{state.fundRange=btn.dataset.range;renderFunds();}));
  } catch(e){errorCard(e);}
}

async function renderCompare() {
  loading("Building comparison…");
  try {
    const funds=await ensureFunds();
    const result=state.compareIds.length?await request(`/compare?funds=${state.compareIds.join(',')}&range=${state.compareRange}`):{points:[]};
    const ranges=["3m","6m","1y","3y","5y","10y","all"];
    app.innerHTML=`<div class="page-stack">
      <section class="page-title-row"><div><span class="eyebrow">Relative performance</span><h1>Compare funds</h1><p>Every selected series starts at 100 so performance can be compared directly.</p></div></section>
      <section class="control-panel"><div class="section-heading compact"><div><span class="field-label">Select up to five</span></div><span class="chart-note">${state.compareIds.length}/5 selected</span></div><div class="fund-toggle-grid">${funds.map(f=>`<button class="fund-toggle ${state.compareIds.includes(f.id)?'active':''}" data-fund="${f.id}" ${!state.compareIds.includes(f.id)&&state.compareIds.length>=5?'disabled':''}><strong>${escapeHtml(f.name)}</strong><span>${f.category==='lifecycle'?'Lifecycle':'Individual'}</span></button>`).join("")}</div><div class="segmented-scroll">${ranges.map(r=>`<button class="segment ${r===state.compareRange?'active':''}" data-compare-range="${r}">${r.toUpperCase()}</button>`).join("")}</div></section>
      ${!state.compareIds.length?statusBanner("Select at least one fund.","warning"):`<section class="panel"><div class="section-heading"><div><span class="eyebrow">Normalized growth</span><h2>${state.compareRange.toUpperCase()} comparison</h2></div><span class="chart-note">Starting value: 100</span></div>${chart(result.points,state.compareIds,{height:360})}</section>`}
    </div>`;
    document.querySelectorAll("[data-fund]").forEach(btn=>btn.addEventListener("click",()=>{const id=btn.dataset.fund;state.compareIds=state.compareIds.includes(id)?state.compareIds.filter(x=>x!==id):state.compareIds.length<5?[...state.compareIds,id]:state.compareIds;renderCompare();}));
    document.querySelectorAll("[data-compare-range]").forEach(btn=>btn.addEventListener("click",()=>{state.compareRange=btn.dataset.compareRange;renderCompare();}));
  } catch(e){errorCard(e);}
}

function portfolioEditor() {
  const total=Object.values(state.weights).reduce((a,b)=>a+b,0);
  return `<section class="control-panel allocation-panel">${Object.entries(state.weights).map(([id,value])=>`<div class="allocation-row"><div><strong>${coreNames[id]}</strong><span>${id.toUpperCase()}</span></div><div class="stepper"><button data-step="-5" data-id="${id}" aria-label="Reduce ${coreNames[id]}">−</button><label><input data-weight="${id}" type="number" inputmode="decimal" min="0" max="100" value="${value}"><span>%</span></label><button data-step="5" data-id="${id}" aria-label="Increase ${coreNames[id]}">+</button></div></div>`).join("")}<div class="allocation-total ${Math.abs(total-100)<.01?'valid':'invalid'}"><span>Total allocation</span><strong>${total.toFixed(0)}%</strong></div><button id="analyze" class="primary-button" ${Math.abs(total-100)>=.01?'disabled':''}>Analyze allocation</button></section>`;
}
async function renderPortfolio() {
  app.innerHTML=`<div class="page-stack"><section class="page-title-row"><div><span class="eyebrow">Allocation laboratory</span><h1>Analyze a portfolio</h1><p>Test a five-fund allocation against the common overlapping historical period.</p></div></section>${portfolioEditor()}<div id="portfolio-results">${state.portfolio?portfolioResults(state.portfolio):""}</div></div>`;
  bindPortfolio();
}
function bindPortfolio(){
  document.querySelectorAll("[data-step]").forEach(btn=>btn.addEventListener("click",()=>{const id=btn.dataset.id;state.weights[id]=Math.max(0,Math.min(100,state.weights[id]+Number(btn.dataset.step)));renderPortfolio();}));
  document.querySelectorAll("[data-weight]").forEach(input=>input.addEventListener("change",()=>{state.weights[input.dataset.weight]=Math.max(0,Math.min(100,Number(input.value)||0));renderPortfolio();}));
  document.querySelector("#analyze")?.addEventListener("click",analyzePortfolio);
}
async function analyzePortfolio(){
  const results=document.querySelector("#portfolio-results"); results.innerHTML=`<div class="loading-card"><span class="spinner"></span>Calculating historical portfolio results…</div>`;
  try { state.portfolio=await request("/portfolio/analyze",{method:"POST",body:JSON.stringify({holdings:Object.entries(state.weights).map(([fund_id,weight])=>({fund_id,weight}))})}); results.innerHTML=portfolioResults(state.portfolio); }
  catch(e){results.innerHTML=statusBanner(escapeHtml(e.message),"error");}
}
function portfolioResults(r){
  const history=r.history.map(p=>({date:p.date,portfolio:p.value}));
  return `<div class="result-stack">${statusBanner(`Historical results use data from ${shortDate(r.start_date)} through ${shortDate(r.as_of)}.`)}<section class="metric-grid">${metricCard("Annualized return",percent(r.annualized_return),"",r.annualized_return>=0?"positive":"negative")}${metricCard("Annualized volatility",percent(r.annualized_volatility,1,false))}${metricCard("Maximum drawdown",percent(r.max_drawdown),"","negative")}${metricCard("1-month return",percent(r.trailing_returns['1m']))}${metricCard("3-month return",percent(r.trailing_returns['3m']))}${metricCard("1-year return",percent(r.trailing_returns['1y']))}</section><section class="panel"><div class="section-heading"><div><span class="eyebrow">Last five years</span><h2>Growth of 100</h2></div></div>${chart(history,["portfolio"])}</section><section class="panel detail-list"><div class="section-heading"><div><span class="eyebrow">Return contribution</span><h2>What drove the portfolio</h2></div></div>${Object.entries(r.return_contribution).sort((a,b)=>b[1]-a[1]).map(([id,v])=>`<div><span>${coreNames[id]}</span><strong>${percent(v)}</strong></div>`).join("")}</section></div>`;
}

async function renderSettings() {
  loading("Checking the dataset…");
  try {
    const q=await request("/data-quality");
    app.innerHTML=`<div class="page-stack"><section class="page-title-row"><div><span class="eyebrow">Application status</span><h1>Settings and data</h1><p>Review the source file and home-screen installation steps.</p></div></section><section class="panel detail-list"><div class="section-heading"><div><span class="eyebrow">Dataset health</span><h2>${escapeHtml(q.status)}</h2></div></div><div><span>Rows</span><strong>${Number(q.rows).toLocaleString()}</strong></div><div><span>History begins</span><strong>${shortDate(q.start_date)}</strong></div><div><span>Latest price date</span><strong>${shortDate(q.end_date)}</strong></div><div><span>Duplicate dates</span><strong>${q.duplicate_dates}</strong></div><div><span>Invalid prices</span><strong>${q.nonpositive_prices}</strong></div></section><section class="panel"><div class="section-heading"><div><span class="eyebrow">iPhone web app</span><h2>Add to Home Screen</h2></div></div><ol class="instruction-list"><li>Open the deployed website in Safari.</li><li>Tap the Share button at the bottom of Safari.</li><li>Choose <strong>Add to Home Screen</strong>.</li><li>Open TSPMaster from its new app icon.</li></ol><div class="inline-note">The interface includes safe-area padding, large touch targets, bottom navigation and standalone PWA metadata.</div></section><section class="panel detail-list"><div class="section-heading"><div><span class="eyebrow">Connection</span><h2>Services</h2></div></div><div><span>API address</span><strong>Same-origin /api/v1</strong></div><div><span>News provider</span><strong>Staged for next build</strong></div><div><span>AI narrative</span><strong class="positive">Active · gemini-2.0-flash</strong></div></section>${statusBanner("Historical results and model signals are not guarantees of future performance or individualized financial advice.","warning")}</div>`;

  } catch(e){errorCard(e);}
}

window.addEventListener("hashchange",route);
if("serviceWorker" in navigator) window.addEventListener("load",()=>navigator.serviceWorker.register("/sw.js").catch(()=>{}));
route();
