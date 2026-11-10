"use strict";
// Scene behind the tiled terminals. The host (C#) sends the layout in physical pixels; we draw headers,
// corner marks, state, dividers and edges, and send user gestures back.

const host = window.chrome && window.chrome.webview;
dilUygula(); // fill the labels once; later states switch the language when it changes
const gonder = (m) => host && host.postMessage(m);
const ikinci = new URLSearchParams(location.search).has("ikinci");
if (ikinci) document.body.classList.add("ikinci");

const $karolar = document.getElementById("karolar");
const $aralar = document.getElementById("aralar");
const $ozet = document.getElementById("ozet");
const $ipucu = document.getElementById("ipucu");
const $gerial = document.getElementById("gerial");

let durum = null;            // last state from the host
let alindi = performance.now();
const ogeler = new Map();    // id → { el, ... }
const px = (v) => v / (window.devicePixelRatio || 1);

const IKON = {
  ayir: '<svg viewBox="0 0 16 16"><path d="M9.5 2.5h4v4M13.5 2.5 8 8M11.5 9.5v3a1 1 0 0 1-1 1h-7a1 1 0 0 1-1-1v-7a1 1 0 0 1 1-1h3"/></svg>',
  buyut: '<svg viewBox="0 0 16 16"><path d="M9.5 2.5h4v4M6.5 13.5h-4v-4M13.5 2.5l-4.5 4.5M2.5 13.5 7 9"/></svg>',
};

// ---------- time words ----------
function sure(sn) {
  if (sn < 45) return T("simdi");
  const dk = Math.round(sn / 60);
  if (dk < 60) return dk + " " + T("dk");
  const sa = Math.floor(dk / 60), kalan = dk % 60;
  return kalan ? `${sa} ${T("sa")} ${kalan} ${T("dk")}` : `${sa} ${T("sa")}`;
}
function metaYazi(k, gecen) {
  const sn = k.sn + gecen;
  switch (k.durum) {
    case "calisiyor": return T("metaCalisiyor", sure(sn));
    case "izin": return T("metaIzin");
    case "bitti": return T("metaBitti", sn < 45 ? T("simdi") : sure(sn) + " " + T("once"));
    default: return T("bos");
  }
}

// ---------- render ----------
function ciz(d) {
  const ilk = !durum;
  durum = d; alindi = performance.now();
  const r = document.documentElement.style;
  r.setProperty("--kenar", px(d.kenar) + "px");
  r.setProperty("--ust", px(d.ust) + "px");
  r.setProperty("--baslik", px(d.baslik) + "px");
  r.setProperty("--bosluk", px(d.bosluk) + "px");
  document.body.classList.toggle("izin-var", d.izin > 0);
  document.getElementById("ekranDugme").hidden = !(d.ekranSayisi > 1);
  // language, theme, accent, effects
  if (d.dil && d.dil !== window.dil) { window.dil = d.dil; dilUygula(); }
  document.documentElement.dataset.tema = d.tema || "charcoal";
  if (d.vurgu) r.setProperty("--vurgu", d.vurgu);
  document.body.classList.toggle("efektsiz", d.efekt === false);
  document.querySelectorAll(".dizilim").forEach((b) => b.classList.toggle("secili", b.dataset.dizilim === d.dizilim));
  ipuclari(d);
  if (d.anim && !ilk) {
    document.body.classList.add("kayar");
    clearTimeout(ciz.kayZ); ciz.kayZ = setTimeout(() => document.body.classList.remove("kayar"), 340);
  }

  ozetCiz(d);
  document.querySelectorAll(".alan").forEach((b) => b.classList.toggle("secili", b.dataset.alan === d.alan));
  $gerial.hidden = d.ayrik === 0;
  $gerial.textContent = T("ayrikGeri", d.ayrik);

  const gorulen = new Set();
  for (const k of d.karolar) {
    gorulen.add(k.id);
    let o = ogeler.get(k.id);
    if (!o) { o = karoKur(k, !ilk); ogeler.set(k.id, o); }
    karoGuncelle(o, k);
  }
  for (const [id, o] of ogeler) {
    if (gorulen.has(id)) continue;
    ogeler.delete(id);
    o.el.classList.add("gidiyor");
    setTimeout(() => o.el.remove(), 380);
  }
  aralariCiz(d);
  if (ilk) requestAnimationFrame(() => document.body.classList.add("hazir"));
}

function ozetCiz(d) {
  const p = [];
  p.push(`<b>${d.toplam}</b> ${T("pencere")}`);
  if (d.calisan) p.push(`<b>${d.calisan}</b> ${T("calisiyor")}`);
  if (d.izin) p.push(`<b class="izin">${d.izin}</b> ${T("izinBekliyor")}`);
  if (d.bitti) p.push(`<b class="bitti">${d.bitti}</b> ${T("bitti")}`);
  if (ikinci) p.unshift(T("tasanlar"));
  $ozet.innerHTML = p.join('<span class="ara">/</span>');
}

function karoKur(k, yeni) {
  const el = document.createElement("section");
  el.className = "karo" + (yeni ? " yeni" : "");
  el.innerHTML = `
    <div class="bas">
      <i class="durum-nokta"></i>
      <span class="baslik-metin"></span>
      <button class="kapat-hap" data-e="kapat"><svg viewBox="0 0 10 10"><path d="M2 2l6 6M8 2 2 8"/></svg><span></span></button>
      <span class="meta"></span>
      <span class="eylem">
        <button data-e="ayir" title="${T("ayir")}">${IKON.ayir}</button>
        <button data-e="buyut" title="${T("buyut")}">${IKON.buyut}</button>
      </span>
    </div>
    <div class="govde">
      <div class="hale"></div>
      <svg class="kose"><path/><path/><path/><path/></svg>
    </div>`;
  $karolar.appendChild(el);
  const o = { el, bas: el.querySelector(".bas"), baslik: el.querySelector(".baslik-metin"), meta: el.querySelector(".meta"),
              govde: el.querySelector(".govde"), kose: el.querySelector(".kose"), k };
  baslikEtkilesim(o);
  if (yeni) setTimeout(() => el.classList.remove("yeni"), 1000);
  return o;
}

function karoGuncelle(o, k) {
  o.k = k;
  const s = o.el.style;
  const x = px(k.x), y = px(k.y), w = px(k.w), h = px(k.h), hb = px(durum.baslik);
  s.left = x + "px"; s.top = y + "px"; s.width = w + "px"; s.height = h + "px";
  o.govde.style.top = hb + "px";
  o.el.classList.remove("calisiyor", "izin", "bitti", "bos");
  o.el.classList.add(k.durum);
  o.el.classList.toggle("odak", !!k.odak);
  if (o.baslik.textContent !== k.baslik) o.baslik.textContent = k.baslik;
  o.meta.textContent = metaYazi(k, 0);
  // save-then-close pill
  const hap = o.el.querySelector(".kapat-hap"), yazi = hap.querySelector("span");
  hap.dataset.d = k.kapanis || "";
  yazi.textContent = k.kapanis === "kaydediliyor" ? T("kaydediliyor") : k.kapanis === "hazir" ? T("simdiKapat") : T("kapat");
  hap.title = k.kapanis ? "" : (durum.kapanisMetni ? T("kapatIpucuMetin") : T("kapatIpucu")) + (durum.kisayol && durum.kisayol.kapat ? "  (" + durum.kisayol.kapat + ")" : "");
  if (k.titre) { hap.classList.remove("titre"); void hap.offsetWidth; hap.classList.add("titre"); }
  // corner marks just outside the terminal
  // rounded like the window corner (Windows 11: 8 px) and concentric with it: the mark sits 6 px out
  const gw = w + 12, gh = h - hb + 12, R = 14, L = Math.min(30, gw / 4, gh / 4);
  if (o.boyut !== gw + "x" + gh) {
    o.boyut = gw + "x" + gh;
    o.kose.setAttribute("viewBox", `0 0 ${gw} ${gh}`);
    o.kose.setAttribute("width", gw); o.kose.setAttribute("height", gh);
    const a = 0.75, r = Math.min(R, L - 4), p = o.kose.querySelectorAll("path");
    p[0].setAttribute("d", `M${a} ${L}V${a + r}A${r} ${r} 0 0 1 ${a + r} ${a}H${L}`);
    p[1].setAttribute("d", `M${gw - L} ${a}H${gw - a - r}A${r} ${r} 0 0 1 ${gw - a} ${a + r}V${L}`);
    p[2].setAttribute("d", `M${gw - a} ${gh - L}V${gh - a - r}A${r} ${r} 0 0 1 ${gw - a - r} ${gh - a}H${gw - L}`);
    p[3].setAttribute("d", `M${L} ${gh - a}H${a + r}A${r} ${r} 0 0 1 ${a} ${gh - a - r}V${gh - L}`);
  }
}

setInterval(() => {
  if (!durum) return;
  const gecen = Math.round((performance.now() - alindi) / 1000);
  for (const o of ogeler.values()) o.meta.textContent = metaYazi(o.k, gecen);
}, 15000);

// ---------- dividers in the gaps ----------
// Elements are kept and only moved: rebuilding them while you drag would drop the pointer capture
// (every drag step re-tiles the windows and sends a new layout).
const aralar = new Map(); // key -> element
function aralariCiz(d) {
  const lazim = new Map();
  if (!ikinci && d.karolar.length >= 2) {
    const g = px(d.bosluk);
    const sutunlu = d.yon === "c";
    // primary groups: rows (row layouts) or columns (column layouts, e.g. main + stack)
    const gruplar = new Map();
    for (const k of d.karolar) { const p = sutunlu ? k.c : k.r; if (!gruplar.has(p)) gruplar.set(p, []); gruplar.get(p).push(k); }
    const sira = [...gruplar.keys()].sort((a, b) => a - b);
    sira.forEach((p, pi) => {
      const grup = gruplar.get(p).sort((a, b) => (sutunlu ? a.r - b.r : a.c - b.c));
      for (let i = 0; i < grup.length - 1; i++) {
        const a = grup[i];
        lazim.set("s" + pi + "-" + i, sutunlu
          ? { tur: "sutun", satir: pi, i, dizi: grup, dikey: false,
              stil: { top: px(a.y + a.h) - 6 + "px", height: g + 12 + "px", left: px(a.x) + "px", width: px(a.w) + "px", right: "auto" } }
          : { tur: "sutun", satir: pi, i, dizi: grup, dikey: true,
              stil: { left: px(a.x + a.w) - 6 + "px", width: g + 12 + "px", top: px(a.y) + "px", height: px(a.h) + "px", right: "auto" } });
      }
    });
    for (let i = 0; i < sira.length - 1; i++) {
      const once = gruplar.get(sira[i])[0];
      const dizi = sira.map((p) => gruplar.get(p)[0]);
      lazim.set("r" + i, sutunlu
        ? { tur: "satir", satir: -1, i, dizi, dikey: true,
            stil: { left: px(once.x + once.w) - 6 + "px", width: g + 12 + "px", top: px(d.ust) + "px", height: px(d.h - d.ust - d.kenar) + "px", right: "auto" } }
        : { tur: "satir", satir: -1, i, dizi, dikey: false,
            stil: { top: px(once.y + once.h) - 6 + "px", height: g + 12 + "px", left: px(d.kenar) + "px", right: px(d.kenar) + "px", width: "auto" } });
    }
  }
  for (const [key, v] of lazim) {
    let el = aralar.get(key);
    if (!el) {
      el = document.createElement("div");
      araSurukle(el);
      $aralar.appendChild(el); aralar.set(key, el);
    }
    el.className = "ara-cizgi " + (v.dikey ? "dikey" : "yatay") + (el.classList.contains("aktif") ? " aktif" : "");
    el._veri = v;
    Object.assign(el.style, v.stil);
  }
  for (const [key, el] of aralar) {
    if (lazim.has(key) || el.classList.contains("aktif")) continue;
    el.remove(); aralar.delete(key);
  }
}

function araSurukle(el) {
  el.addEventListener("dblclick", () => gonder({ t: "esitle" }));
  el.addEventListener("pointerdown", (e) => {
    if (e.button !== 0) return;
    e.preventDefault(); el.setPointerCapture(e.pointerId); el.classList.add("aktif");
    document.body.classList.add("ara-suruk");
    const { tur, satir, i, dizi, dikey } = el._veri;
    const dpr = window.devicePixelRatio || 1;
    const boy = dizi.map((k) => (dikey ? k.w : k.h));
    const bas = dizi.map((k) => (dikey ? k.x : k.y));
    const enAz = (dikey ? 360 : 220) * dpr;
    const toplam = boy[i] + boy[i + 1];
    let bekleyen = null, son = 0, bitti = false;
    const hareket = (ev) => {
      const p = (dikey ? ev.clientX : ev.clientY) * dpr;
      const sol = Math.max(enAz, Math.min(toplam - enAz, p - bas[i]));
      const w = boy.slice(); w[i] = sol; w[i + 1] = toplam - sol;
      bekleyen = w;
    };
    const dongu = (t) => {
      if (bitti) return;
      if (bekleyen && t - son > 40) { gonder({ t: tur, r: satir, w: bekleyen }); bekleyen = null; son = t; }
      requestAnimationFrame(dongu);
    };
    requestAnimationFrame(dongu);
    const bit = () => {
      if (bitti) return; bitti = true;
      el.classList.remove("aktif"); document.body.classList.remove("ara-suruk");
      gonder({ t: tur, r: satir, w: bekleyen || boy, son: true });
      el.removeEventListener("pointermove", hareket); el.removeEventListener("pointerup", bit); el.removeEventListener("lostpointercapture", bit);
    };
    el.addEventListener("pointermove", hareket);
    el.addEventListener("pointerup", bit);
    el.addEventListener("lostpointercapture", bit);
  });
}

// ---------- headers: click = focus, double = maximise, drag = swap / pull out ----------
function baslikEtkilesim(o) {
  o.bas.addEventListener("click", (e) => {
    const b = e.target.closest("button");
    if (b) { gonder({ t: b.dataset.e, id: o.k.id }); return; }
    if (!o.suruklendi) gonder({ t: "odak", id: o.k.id });
  });
  o.bas.addEventListener("dblclick", (e) => { if (!e.target.closest("button")) gonder({ t: "buyut", id: o.k.id }); });
  o.bas.addEventListener("pointerdown", (e) => {
    if (e.button !== 0 || e.target.closest("button")) return;
    const x0 = e.clientX, y0 = e.clientY;
    let hayalet = null, hedef = null;
    o.suruklendi = false;
    o.bas.setPointerCapture(e.pointerId);
    const hareket = (ev) => {
      if (!o.suruklendi && Math.hypot(ev.clientX - x0, ev.clientY - y0) < 7) return;
      if (!o.suruklendi) {
        o.suruklendi = true; o.el.classList.add("surukleniyor");
        hayalet = document.createElement("div"); hayalet.className = "hayalet"; hayalet.textContent = o.k.baslik;
        document.body.appendChild(hayalet);
      }
      hayalet.style.left = ev.clientX + "px"; hayalet.style.top = ev.clientY + "px";
      const disari = ev.clientX < 0 || ev.clientY < 0 || ev.clientX > innerWidth || ev.clientY > innerHeight;
      hayalet.classList.toggle("disari", disari);
      hayalet.textContent = disari ? T("ayirDisari", o.k.baslik) : o.k.baslik;
      const yeniHedef = disari ? null : karoAltinda(ev.clientX, ev.clientY, o);
      if (yeniHedef !== hedef) { if (hedef) hedef.el.classList.remove("hedef"); hedef = yeniHedef; if (hedef) hedef.el.classList.add("hedef"); }
    };
    const bit = (ev) => {
      o.bas.removeEventListener("pointermove", hareket); o.bas.removeEventListener("pointerup", bit);
      if (!o.suruklendi) return;
      const disari = ev.clientX < 0 || ev.clientY < 0 || ev.clientX > innerWidth || ev.clientY > innerHeight;
      if (hedef) { gonder({ t: "takas", a: o.k.id, b: hedef.k.id }); hedef.el.classList.remove("hedef"); }
      else if (disari) gonder({ t: "ayir", id: o.k.id });
      hayalet && hayalet.remove();
      o.el.classList.remove("surukleniyor");
      setTimeout(() => (o.suruklendi = false), 0);
    };
    o.bas.addEventListener("pointermove", hareket);
    o.bas.addEventListener("pointerup", bit);
  });
}

function karoAltinda(x, y, haric) {
  for (const o of ogeler.values()) {
    if (o === haric) continue;
    const r = o.el.getBoundingClientRect();
    if (x >= r.left && x <= r.right && y >= r.top && y <= r.bottom) return o;
  }
  return null;
}

// ---------- work-area edges (all four) ----------
document.querySelectorAll(".kenar").forEach((el) => {
  el.addEventListener("dblclick", () => gonder({ t: "alan", ad: "tam" }));
  el.addEventListener("pointerdown", (e) => {
    if (e.button !== 0) return;
    e.preventDefault(); el.setPointerCapture(e.pointerId); el.classList.add("aktif");
    const dikeyKenar = el.dataset.e === "ust" || el.dataset.e === "alt";
    const dpr = window.devicePixelRatio || 1;
    let deger = null, son = 0, bitti = false;
    const hareket = (ev) => {
      deger = (dikeyKenar ? ev.screenY : ev.screenX) * dpr;
      $ipucu.style.left = ev.clientX + "px"; $ipucu.style.top = ev.clientY + "px";
    };
    const dongu = (t) => {
      if (bitti) return;
      if (deger !== null && t - son > 40) { gonder({ t: "kenar", e: el.dataset.e, x: deger }); deger = null; son = t; }
      requestAnimationFrame(dongu);
    };
    requestAnimationFrame(dongu);
    $ipucu.dataset.yon = dikeyKenar ? "h" : "w";
    $ipucu.classList.add("gorunur"); ipucuYaz();
    const bit = () => {
      if (bitti) return; bitti = true;
      el.classList.remove("aktif"); $ipucu.classList.remove("gorunur");
      if (deger !== null) gonder({ t: "kenar", e: el.dataset.e, x: deger });
      gonder({ t: "kenarBitti" });
      el.removeEventListener("pointermove", hareket); el.removeEventListener("pointerup", bit); el.removeEventListener("lostpointercapture", bit);
    };
    el.addEventListener("pointermove", hareket);
    el.addEventListener("pointerup", bit);
    el.addEventListener("lostpointercapture", bit);
  });
});

function ipucuYaz() {
  if (!durum || !$ipucu.classList.contains("gorunur")) return;
  const dpr = window.devicePixelRatio || 1;
  const h = $ipucu.dataset.yon === "h";
  const oran = h ? durum.h / (durum.ekranH || screen.availHeight * dpr) : durum.w / (durum.ekranW || screen.availWidth * dpr);
  const yakin = (v) => Math.abs(oran - v) < .012;
  const ad = yakin(1) ? "100%" : yakin(.5) ? "½" : yakin(1 / 3) ? "⅓" : yakin(2 / 3) ? "⅔" : Math.round(oran * 100) + "%";
  $ipucu.innerHTML = ad + "<small>" + (h ? T("yukseklik") : T("genislik")) + "</small>";
}

// ---------- top bar ----------
document.querySelectorAll(".alan").forEach((b) => b.addEventListener("click", () => gonder({ t: "alan", ad: b.dataset.alan })));
document.getElementById("yeni").addEventListener("click", () => gonder({ t: "yeni" }));
document.getElementById("yeniOk").addEventListener("click", (e) => {
  const r = e.currentTarget.getBoundingClientRect(), dpr = window.devicePixelRatio || 1;
  gonder({ t: "klasorMenu", x: Math.round((window.screenX + r.right) * dpr), y: Math.round((window.screenY + r.bottom + 6) * dpr) });
});
document.getElementById("ayarDugme").addEventListener("click", () => gonder({ t: "ayarlar" }));
document.querySelectorAll(".dizilim").forEach((b) => b.addEventListener("click", () => gonder({ t: "dizilim", ad: b.dataset.dizilim })));

// tooltips carry the shortcut so people learn them by hovering
function ipuclari(d) {
  const k = d.kisayol || {};
  const ek = (s) => (s ? "  (" + s + ")" : "");
  document.getElementById("yeni").title = T("yeniAc") + ek(k.yeni);
  document.getElementById("ekranDugme").title = T("oburEkranAc") + ek(k.ekran);
  document.getElementById("tuslarDugme").title = T("kisayollarAc") + ek(k.rehber);
}
$gerial.addEventListener("click", () => gonder({ t: "gerital" }));
document.getElementById("tuslarDugme").addEventListener("click", () => gonder({ t: "rehber" }));
document.getElementById("ekranDugme").addEventListener("click", () => gonder({ t: "ekran" }));

// ---------- host messages ----------
if (host) {
  host.addEventListener("message", (e) => { if (e.data && e.data.t === "durum") { ciz(e.data); ipucuYaz(); } });
  gonder({ t: "hazir" });
} else {
  // opened in a normal browser: demo state, so the look can be judged without the app
  const demo = (n) => {
    const W = innerWidth * devicePixelRatio, H = innerHeight * devicePixelRatio, k = 18, u = 52, g = 18;
    const durumlar = ["calisiyor", "izin", "bitti", "bos", "calisiyor", "bos", "calisiyor", "bitti"];
    const adlar = ["Refactor the billing service", "Landing page hero", "Fix flaky login test", "Write the release notes", "Migrate to Postgres 17", "Design tokens cleanup", "Profile the image worker", "Docs: getting started"];
    const satir = n <= 3 ? 1 : 2, karolar = [];
    let i = 0;
    for (let r = 0; r < satir; r++) {
      const c = Math.floor(n / satir) + (r < n % satir ? 1 : 0);
      const h = (H - u - k - g * (satir - 1)) / satir, w = (W - 2 * k - g * (c - 1)) / c;
      for (let j = 0; j < c; j++, i++)
        karolar.push({ id: "d" + i, x: k + j * (w + g), y: u + r * (h + g), w, h, r, c: j, cs: c, rs: satir,
          baslik: adlar[i], durum: durumlar[i], sn: 60 * (i * 3 + 2), odak: i === 0 });
    }
    const q = new URLSearchParams(location.search);
    return { t: "durum", w: W, h: H, bosluk: g, kenar: k, ust: u, baslik: 34, alan: "tam", dizilim: "auto", yon: "r", toplam: n, ayrik: 0,
      izin: 1, bitti: 2, calisan: 3, karolar, dil: q.get("dil") || "en", tema: q.get("tema") || "charcoal", vurgu: q.get("vurgu") || "#ff6a38", efekt: true,
      ekranSayisi: 2, kisayol: { yeni: "Ctrl+Win+Alt+N", ekran: "Ctrl+Win+Alt+M", rehber: "Ctrl+Win+Alt+K", kapat: "Ctrl+Win+Alt+Q" } };
  };
  window.addEventListener("resize", () => ciz(demo(+(new URLSearchParams(location.search).get("n") || 6))));
  ciz(demo(+(new URLSearchParams(location.search).get("n") || 6)));
  document.body.classList.add("demo");
}
