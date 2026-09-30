import * as Model from './model.js';

let isUpdating = false;

// DOM Элементы
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
    
    // Синхронизация RGB (значения в инпутах и ползунках округляются до целых)
    if (source !== 'RGB') {
        const rRounded = Math.round(rgb.r);
        const gRounded = Math.round(rgb.g);
        const bRounded = Math.round(rgb.b);

        elements.r.slider.value = elements.r.num.value = rRounded;
        elements.g.slider.value = elements.g.num.value = gRounded;
        elements.b.slider.value = elements.b.num.value = bRounded;
    }

    // Расчет LAB
    let lab = { L: 0, a: 0, b: 0 };
    if (source !== 'LAB') {
        lab = Model.rgbToLab(rgb.r, rgb.g, rgb.b, illum);
        elements.L.slider.value = elements.L.num.value = Math.round(lab.L);
        elements.a.slider.value = elements.a.num.value = Math.round(lab.a);
        elements.labB.slider.value = elements.labB.num.value = Math.round(lab.b);
    } else {
        lab = { L: +elements.L.slider.value, a: +elements.a.slider.value, b: +elements.labB.slider.value };
    }

    // Расчет CMYK
    let cmyk = { c: 0, m: 0, y: 0, k: 0 };
    if (source !== 'CMYK') {
        cmyk = Model.rgbToCmyk(rgb.r, rgb.g, rgb.b, cmykMethod);
        elements.c.slider.value = elements.c.num.value = Math.round(cmyk.c);
        elements.m.slider.value = elements.m.num.value = Math.round(cmyk.m);
        elements.y.slider.value = elements.y.num.value = Math.round(cmyk.y);
        elements.k.slider.value = elements.k.num.value = Math.round(cmyk.k);
    } else {
        cmyk = { c: +elements.c.slider.value, m: +elements.m.slider.value, y: +elements.y.slider.value, k: +elements.k.slider.value };
    }

    // Обновление предпросмотра
    const rHex = Math.round(rgb.r).toString(16).padStart(2, '0');
    const gHex = Math.round(rgb.g).toString(16).padStart(2, '0');
    const bHex = Math.round(rgb.b).toString(16).padStart(2, '0');
    elements.colorDisplay.style.backgroundColor = `rgb(${rgb.r}, ${rgb.g}, ${rgb.b})`;
    elements.nativePicker.value = `#${rHex}${gHex}${bHex}`;

    // Обновление цветных полос всех ползунков и графика CIE
    updateSliderGradients(rgb, lab, cmyk);
    drawCIE(rgb);
}

function updateSliderGradients(rgb, lab, cmyk) {
    // Градиенты RGB
    elements.r.slider.style.background = `linear-gradient(to right, rgb(0,${rgb.g},${rgb.b}), rgb(255,${rgb.g},${rgb.b}))`;
    elements.g.slider.style.background = `linear-gradient(to right, rgb(${rgb.r},0,${rgb.b}), rgb(${rgb.r},255,${rgb.b}))`;
    elements.b.slider.style.background = `linear-gradient(to right, rgb(${rgb.r},${rgb.g},0), rgb(${rgb.r},${rgb.g},255))`;

    // Градиенты LAB
    elements.L.slider.style.background = `linear-gradient(to right, #000, #fff)`;
    elements.a.slider.style.background = `linear-gradient(to right, #00ff00, #808080, #ff0000)`;
    elements.labB.slider.style.background = `linear-gradient(to right, #0000ff, #808080, #ffff00)`;

    // Градиенты CMYK
    elements.c.slider.style.background = `linear-gradient(to right, #fff, #00ffff)`;
    elements.m.slider.style.background = `linear-gradient(to right, #fff, #ff00ff)`;
    elements.y.slider.style.background = `linear-gradient(to right, #fff, #ffff00)`;
    elements.k.slider.style.background = `linear-gradient(to right, #fff, #000)`;
}

// ---------------- Обработчики ввода ----------------
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

// Двухстороннее связывание (Slider <-> Number) с гарантийным округлением при вводе
['r','g','b','L','a','labB','c','m','y','k'].forEach(key => {
    const el = elements[key];
    if (!el) return;
    
    el.slider.addEventListener('input', () => { 
        el.num.value = Math.round(el.slider.value); 
        triggerEvent(key); 
    });
    
    el.num.addEventListener('input', () => { 
        if (el.num.value !== '') {
            el.num.value = Math.round(el.num.value);
            el.slider.value = el.num.value; 
        }
        triggerEvent(key); 
    });
});

function triggerEvent(key) {
    if (['r','g','b'].includes(key)) handleRGBInput();
    else if (['L','a','labB'].includes(key)) handleLABInput();
    else handleCMYKInput();
}

// Селекты и палитра
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

// ---------------- Отрисовка цветного графика МКО ----------------
function drawCIE(rgb) {
    const ctx = elements.canvas.getContext('2d');
    const w = elements.canvas.width, h = elements.canvas.height;
    ctx.clearRect(0, 0, w, h);

    const margin = 30;
    const plotW = w - margin * 2;
    const plotH = h - margin * 2;

    // 1. Отрисовка фоновой сетки хроматичности
    const imgData = ctx.createImageData(plotW, plotH);
    for (let py = 0; py < plotH; py++) {
        for (let px = 0; px < plotW; px++) {
            const xVal = px / plotW;
            const yVal = 1 - (py / plotH);
            const index = (py * plotW + px) * 4;

            if (xVal + yVal <= 1 && xVal > 0 && yVal > 0) {
                const zVal = 1 - xVal - yVal;
                let r = Math.max(0, 3.2406 * xVal - 1.5372 * yVal - 0.4986 * zVal);
                let g = Math.max(0, -0.9689 * xVal + 1.8758 * yVal + 0.0415 * zVal);
                let b = Math.max(0, 0.0557 * xVal - 0.2040 * yVal + 1.0570 * zVal);

                const maxC = Math.max(r, g, b);
                if (maxC > 0) { r /= maxC; g /= maxC; b /= maxC; }

                imgData.data[index] = r * 220;
                imgData.data[index + 1] = g * 220;
                imgData.data[index + 2] = b * 220;
                imgData.data[index + 3] = 180;
            } else {
                imgData.data[index + 3] = 0;
            }
        }
    }
    ctx.putImageData(imgData, margin, margin);

    // 2. Рамка и оси
    ctx.strokeStyle = '#666';
    ctx.lineWidth = 1;
    ctx.strokeRect(margin, margin, plotW, plotH);

    // Подписи осей x и y
    ctx.fillStyle = '#333';
    ctx.font = '12px sans-serif';
    ctx.fillText('x', w - 15, h - 10);
    ctx.fillText('y', 10, 20);

    // 3. Вычисление координат текущего цвета
    const { x, y } = Model.getCieCoordinates(rgb.r, rgb.g, rgb.b, elements.illuminant.value);

    // Перевод координат (0..1) в пиксели canvas
    const cx = margin + x * plotW;
    const cy = margin + (1 - y) * plotH;

    // 4. Отрисовка маркера текущего цвета
    ctx.shadowColor = 'rgba(0,0,0,0.5)';
    ctx.shadowBlur = 4;
    ctx.fillStyle = `rgb(${rgb.r}, ${rgb.g}, ${rgb.b})`;
    ctx.beginPath();
    ctx.arc(cx, cy, 7, 0, Math.PI * 2);
    ctx.fill();
    ctx.lineWidth = 2;
    ctx.strokeStyle = '#ffffff';
    ctx.stroke();
    ctx.shadowBlur = 0;
}

// Инициализация при старте
handleRGBInput();