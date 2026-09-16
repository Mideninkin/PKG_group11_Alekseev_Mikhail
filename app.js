import * as Model from './model.js';

let isUpdating = false;

const elements = {
    r: { slider: document.getElementById('sliderR'), num: document.getElementById('numR') },
    g: { slider: document.getElementById('sliderG'), num: document.getElementById('numG') },
    b: { slider: document.getElementById('sliderB'), num: document.getElementById('numB') },
    L: { slider: document.getElementById('sliderL'), num: document.getElementById('numL') },
    a: { slider: document.getElementById('sliderA'), num: document.getElementById('numA') },
    labB: { slider: document.getElementById('sliderLabB'), num: document.getElementById('numLabB') },
    c: { slider: document.getElementById('sliderC'), num: document.getElementById('numC') },
    m: { slider: document.getElementById('sliderM'), num: document.getElementById('numM') },
    y: { slider: document.getElementById('sliderY'), num: document.getElementById('numY') },
    k: { slider: document.getElementById('sliderK'), num: document.getElementById('numK') },
    nativePicker: document.getElementById('nativePicker'),
    colorDisplay: document.getElementById('colorDisplay'),
    illuminant: document.getElementById('illuminantSelect'),
    cmykMethod: document.getElementById('cmykMethodSelect'),
    gamut: document.getElementById('gamutSelect'),
    warning: document.getElementById('warningBanner'),
    canvas: document.getElementById('cieCanvas')
};

function updateAllViews(rgb, source = '') {
    const illum = elements.illuminant.value;
    const cmykMethod = elements.cmykMethod.value;

    if (source !== 'RGB') {
        elements.r.slider.value = elements.r.num.value = Math.round(rgb.r);
        elements.g.slider.value = elements.g.num.value = Math.round(rgb.g);
        elements.b.slider.value = elements.b.num.value = Math.round(rgb.b);
    }

    if (source !== 'LAB') {
        const lab = Model.rgbToLab(rgb.r, rgb.g, rgb.b, illum);
        elements.L.slider.value = elements.L.num.value = lab.L.toFixed(1);
        elements.a.slider.value = elements.a.num.value = lab.a.toFixed(1);
        elements.labB.slider.value = elements.labB.num.value = lab.b.toFixed(1);
    }

    if (source !== 'CMYK') {
        const cmyk = Model.rgbToCmyk(rgb.r, rgb.g, rgb.b, cmykMethod);
        elements.c.slider.value = elements.c.num.value = cmyk.c.toFixed(1);
        elements.m.slider.value = elements.m.num.value = cmyk.m.toFixed(1);
        elements.y.slider.value = elements.y.num.value = cmyk.y.toFixed(1);
        elements.k.slider.value = elements.k.num.value = cmyk.k.toFixed(1);
    }

    const rHex = Math.round(rgb.r).toString(16).padStart(2,'0');
    const gHex = Math.round(rgb.g).toString(16).padStart(2,'0');
    const bHex = Math.round(rgb.b).toString(16).padStart(2,'0');
    elements.colorDisplay.style.backgroundColor = `rgb(${rgb.r}, ${rgb.g}, ${rgb.b})`;
    elements.nativePicker.value = `#${rHex}${gHex}${bHex}`;

    updateGradients(rgb);
    drawCIE(rgb);
}

function updateGradients(rgb) {
    elements.r.slider.style.background = `linear-gradient(to right, rgb(0,${rgb.g},${rgb.b}), rgb(255,${rgb.g},${rgb.b}))`;
    elements.g.slider.style.background = `linear-gradient(to right, rgb(${rgb.r},0,${rgb.b}), rgb(${rgb.r},255,${rgb.b}))`;
    elements.b.slider.style.background = `linear-gradient(to right, rgb(${rgb.r},${rgb.g},0), rgb(${rgb.r},${rgb.g},255))`;

    elements.L.slider.style.background = `linear-gradient(to right, #000, #fff)`;
    elements.a.slider.style.background = `linear-gradient(to right, #00ff00, #808080, #ff0000)`;
    elements.labB.slider.style.background = `linear-gradient(to right, #0000ff, #808080, #ffff00)`;

    elements.c.slider.style.background = `linear-gradient(to right, #fff, #00ffff)`;
    elements.m.slider.style.background = `linear-gradient(to right, #fff, #ff00ff)`;
    elements.y.slider.style.background = `linear-gradient(to right, #fff, #ffff00)`;
    elements.k.slider.style.background = `linear-gradient(to right, #fff, #000)`;
}

function handleRGBInput() {
    if (isUpdating) return; isUpdating = true;
    const rgb = { r: +elements.r.slider.value, g: +elements.g.slider.value, b: +elements.b.slider.value };
    elements.warning.classList.add('hidden');
    updateAllViews(rgb, 'RGB');
    isUpdating = false;
}

function handleLABInput() {
    if (isUpdating) return; isUpdating = true;
    const L = +elements.L.slider.value, a = +elements.a.slider.value, b = +elements.labB.slider.value;
    const rgb = Model.labToRgb(L, a, b, elements.illuminant.value, elements.gamut.value);
    
    if (rgb.outOfGamut) elements.warning.classList.remove('hidden');
    else elements.warning.classList.add('hidden');

    updateAllViews(rgb, 'LAB');
    isUpdating = false;
}

function handleCMYKInput() {
    if (isUpdating) return; isUpdating = true;
    const c = +elements.c.slider.value, m = +elements.m.slider.value, y = +elements.y.slider.value, k = +elements.k.slider.value;
    const rgb = Model.cmykToRgb(c, m, y, k);
    elements.warning.classList.add('hidden');
    updateAllViews(rgb, 'CMYK');
    isUpdating = false;
}

['r','g','b','L','a','labB','c','m','y','k'].forEach(key => {
    const el = elements[key];
    if (!el) return;
    el.slider.addEventListener('input', () => { el.num.value = el.slider.value; triggerEvent(key); });
    el.num.addEventListener('input', () => { el.slider.value = el.num.value; triggerEvent(key); });
});

function triggerEvent(key) {
    if (['r','g','b'].includes(key)) handleRGBInput();
    else if (['L','a','labB'].includes(key)) handleLABInput();
    else handleCMYKInput();
}

elements.illuminant.addEventListener('change', handleRGBInput);
elements.cmykMethod.addEventListener('change', handleRGBInput);
elements.gamut.addEventListener('change', handleLABInput);
elements.nativePicker.addEventListener('input', (e) => {
    const hex = e.target.value;
    elements.r.slider.value = parseInt(hex.slice(1,3), 16);
    elements.g.slider.value = parseInt(hex.slice(3,5), 16);
    elements.b.slider.value = parseInt(hex.slice(5,7), 16);
    handleRGBInput();
});

function drawCIE(rgb) {
    const ctx = elements.canvas.getContext('2d');
    const w = elements.canvas.width, h = elements.canvas.height;
    ctx.clearRect(0, 0, w, h);

    const pad = 40;
    const plotW = w - pad * 2;
    const plotH = h - pad * 2;

    const imgData = ctx.createImageData(plotW, plotH);
    for (let py = 0; py < plotH; py++) {
        for (let px = 0; px < plotW; px++) {
            const xVal = px / plotW;
            const yVal = 1 - (py / plotH);
            const idx = (py * plotW + px) * 4;

            if (xVal + yVal <= 1 && xVal > 0.05 && yVal > 0.05) {
                const zVal = 1 - xVal - yVal;
                let r = Math.max(0, 3.2406 * xVal - 1.5372 * yVal - 0.4986 * zVal);
                let g = Math.max(0, -0.9689 * xVal + 1.8758 * yVal + 0.0415 * zVal);
                let b = Math.max(0, 0.0557 * xVal - 0.2040 * yVal + 1.0570 * zVal);

                const max = Math.max(r, g, b);
                if (max > 0) { r /= max; g /= max; b /= max; }

                imgData.data[idx] = r * 255;
                imgData.data[idx + 1] = g * 255;
                imgData.data[idx + 2] = b * 255;
                imgData.data[idx + 3] = 220;
            } else {
                imgData.data[idx + 3] = 0;
            }
        }
    }
    ctx.putImageData(imgData, pad, pad);

    ctx.strokeStyle = '#475569';
    ctx.lineWidth = 1;
    ctx.strokeRect(pad, pad, plotW, plotH);

    ctx.fillStyle = '#94a3b8';
    ctx.font = '10px monospace';
    ctx.fillText('x', w - 20, h - 15);
    ctx.fillText('y', 15, 25);

    const { x, y } = Model.getCieCoordinates(rgb.r, rgb.g, rgb.b, elements.illuminant.value);
    const cx = pad + x * plotW;
    const cy = pad + (1 - y) * plotH;

    ctx.fillStyle = `rgb(${rgb.r}, ${rgb.g}, ${rgb.b})`;
    ctx.beginPath();
    ctx.arc(cx, cy, 7, 0, Math.PI * 2);
    ctx.fill();
    ctx.lineWidth = 2;
    ctx.strokeStyle = '#ffffff';
    ctx.stroke();
}

handleRGBInput();

//Блок тестов
const runTestsBtn = document.getElementById('runTestsBtn');
const testCard = document.getElementById('testResultsCard');
const closeTestsBtn = document.getElementById('closeTestsBtn');
const testOutput = document.getElementById('testOutput');

runTestsBtn.addEventListener('click', () => {
    testCard.classList.remove('hidden');
    testOutput.innerHTML = '';

    let total = 0, passed = 0;

    function assert(cond, msg) {
        total++;
        const item = document.createElement('div');
        item.className = cond ? 'test-pass' : 'test-fail';
        item.textContent = `${cond ? '✓ [PASS]' : '✗ [FAIL]'} ${msg}`;
        testOutput.appendChild(item);
        if (cond) passed++;
    }

    function assertNear(val1, val2, tolerance, msg) {
        assert(Math.abs(val1 - val2) <= tolerance, `${msg} (Значение: ${val1.toFixed(2)}, Ожидалось: ${val2})`);
    }

    //RGB -> CMYK (GCR)
    const cmyk = Model.rgbToCmyk(255, 0, 0, 'GCR');
    assertNear(cmyk.c, 0, 0.1, 'RGB(255,0,0) -> CMYK C%');
    assertNear(cmyk.m, 100, 0.1, 'RGB(255,0,0) -> CMYK M%');

    //CMYK -> RGB
    const rgb = Model.cmykToRgb(0, 100, 100, 0);
    assertNear(rgb.r, 255, 0.1, 'CMYK(0,100,100,0) -> RGB R');

    //RGB -> LAB (D65)
    const lab = Model.rgbToLab(255, 255, 255, 'D65');
    assertNear(lab.L, 100, 1.0, 'Белый RGB(255,255,255) -> LAB L* = 100');

    //Проверка выхода за охват sRGB
    const out = Model.labToRgb(100, 100, 100, 'D65', 'clipping');
    assert(out.outOfGamut === true, 'Детекция выхода за охват sRGB');

    const summary = document.createElement('strong');
    summary.style.marginTop = '10px';
    summary.textContent = `Итог: пройдено ${passed} из ${total} тестов.`;
    testOutput.insertBefore(summary, testOutput.firstChild);
});

runTestsBtn.addEventListener('click', () => {
    testCard.classList.remove('hidden');
    testCard.style.display = 'block';
    
});

closeTestsBtn.addEventListener('click', (e) => {
    e.preventDefault();
    testCard.classList.add('hidden');
    testCard.style.display = 'none';
});