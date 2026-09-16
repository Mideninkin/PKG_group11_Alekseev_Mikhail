export const ILLUMINANTS = {
    D65: { X: 0.95047, Y: 1.00000, Z: 1.08883 },
    D50: { X: 0.96422, Y: 1.00000, Z: 0.82521 },
    E:   { X: 1.00000, Y: 1.00000, Z: 1.00000 }
};

export function getRgbToXyzMatrix(illuminantKey) {
    const base = [
        [0.4124564, 0.3575761, 0.1804375],
        [0.2126729, 0.7151522, 0.0721750],
        [0.0193339, 0.1191920, 0.9503041]
    ];
    if (illuminantKey === 'D65') return base;

    const src = ILLUMINANTS.D65;
    const dst = ILLUMINANTS[illuminantKey];

    const M_B = [
        [ 0.8951,  0.2664, -0.1614],
        [-0.7502,  1.7135,  0.0367],
        [ 0.0389, -0.0685,  1.0296]
    ];
    const M_B_inv = [
        [ 0.9869929, -0.1470543,  0.1599627],
        [ 0.4323053,  0.5183603,  0.0495428],
        [-0.0085287,  0.0400428,  0.9684867]
    ];

    const rgbSrc = [
        M_B[0][0]*src.X + M_B[0][1]*src.Y + M_B[0][2]*src.Z,
        M_B[1][0]*src.X + M_B[1][1]*src.Y + M_B[1][2]*src.Z,
        M_B[2][0]*src.X + M_B[2][1]*src.Y + M_B[2][2]*src.Z
    ];
    const rgbDst = [
        M_B[0][0]*dst.X + M_B[0][1]*dst.Y + M_B[0][2]*dst.Z,
        M_B[1][0]*dst.X + M_B[1][1]*dst.Y + M_B[1][2]*dst.Z,
        M_B[2][0]*dst.X + M_B[2][1]*dst.Y + M_B[2][2]*dst.Z
    ];

    const S = [rgbDst[0]/rgbSrc[0], rgbDst[1]/rgbSrc[1], rgbDst[2]/rgbSrc[2]];
    const M_adapt = [
        [M_B_inv[0][0]*S[0]*M_B[0][0] + M_B_inv[0][1]*S[1]*M_B[1][0] + M_B_inv[0][2]*S[2]*M_B[2][0],
         M_B_inv[0][0]*S[0]*M_B[0][1] + M_B_inv[0][1]*S[1]*M_B[1][1] + M_B_inv[0][2]*S[2]*M_B[2][1],
         M_B_inv[0][0]*S[0]*M_B[0][2] + M_B_inv[0][1]*S[1]*M_B[1][2] + M_B_inv[0][2]*S[2]*M_B[2][2]],
        [M_B_inv[1][0]*S[0]*M_B[0][0] + M_B_inv[1][1]*S[1]*M_B[1][0] + M_B_inv[1][2]*S[2]*M_B[2][0],
         M_B_inv[1][0]*S[0]*M_B[0][1] + M_B_inv[1][1]*S[1]*M_B[1][1] + M_B_inv[1][2]*S[2]*M_B[2][1],
         M_B_inv[1][0]*S[0]*M_B[0][2] + M_B_inv[1][1]*S[1]*M_B[1][2] + M_B_inv[1][2]*S[2]*M_B[2][2]],
        [M_B_inv[2][0]*S[0]*M_B[0][0] + M_B_inv[2][1]*S[1]*M_B[1][0] + M_B_inv[2][2]*S[2]*M_B[2][0],
         M_B_inv[2][0]*S[0]*M_B[0][1] + M_B_inv[2][1]*S[1]*M_B[1][1] + M_B_inv[2][2]*S[2]*M_B[2][1],
         M_B_inv[2][0]*S[0]*M_B[0][2] + M_B_inv[2][1]*S[1]*M_B[1][2] + M_B_inv[2][2]*S[2]*M_B[2][2]]
    ];

    return [
        [M_adapt[0][0]*base[0][0] + M_adapt[0][1]*base[1][0] + M_adapt[0][2]*base[2][0],
         M_adapt[0][0]*base[0][1] + M_adapt[0][1]*base[1][1] + M_adapt[0][2]*base[2][1],
         M_adapt[0][0]*base[0][2] + M_adapt[0][1]*base[1][2] + M_adapt[0][2]*base[2][2]],
        [M_adapt[1][0]*base[0][0] + M_adapt[1][1]*base[1][0] + M_adapt[1][2]*base[2][0],
         M_adapt[1][0]*base[0][1] + M_adapt[1][1]*base[1][1] + M_adapt[1][2]*base[2][1],
         M_adapt[1][0]*base[0][2] + M_adapt[1][1]*base[1][2] + M_adapt[1][2]*base[2][2]],
        [M_adapt[2][0]*base[0][0] + M_adapt[2][1]*base[1][0] + M_adapt[2][2]*base[2][0],
         M_adapt[2][0]*base[0][1] + M_adapt[2][1]*base[1][1] + M_adapt[2][2]*base[2][1],
         M_adapt[2][0]*base[0][2] + M_adapt[2][1]*base[1][2] + M_adapt[2][2]*base[2][2]]
    ];
}

export function invert3x3(m) {
    const det = m[0][0]*(m[1][1]*m[2][2] - m[1][2]*m[2][1]) -
                m[0][1]*(m[1][0]*m[2][2] - m[1][2]*m[2][0]) +
                m[0][2]*(m[1][0]*m[2][1] - m[1][1]*m[2][0]);
    const invdet = 1 / det;
    return [
        [(m[1][1]*m[2][2] - m[1][2]*m[2][1])*invdet, (m[0][2]*m[2][1] - m[0][1]*m[2][2])*invdet, (m[0][1]*m[1][2] - m[0][2]*m[1][1])*invdet],
        [(m[1][2]*m[2][0] - m[1][0]*m[2][2])*invdet, (m[0][0]*m[2][2] - m[0][2]*m[2][0])*invdet, (m[0][2]*m[1][0] - m[0][0]*m[1][2])*invdet],
        [(m[1][0]*m[2][1] - m[1][1]*m[2][0])*invdet, (m[0][1]*m[2][0] - m[0][0]*m[2][1])*invdet, (m[0][0]*m[1][1] - m[0][1]*m[1][0])*invdet]
    ];
}

export function rgbToLab(r, g, b, illuminantKey = 'D65') {
    let rL = r / 255, gL = g / 255, bL = b / 255;
    rL = rL > 0.04045 ? Math.pow((rL + 0.055) / 1.055, 2.4) : rL / 12.92;
    gL = gL > 0.04045 ? Math.pow((gL + 0.055) / 1.055, 2.4) : gL / 12.92;
    bL = bL > 0.04045 ? Math.pow((bL + 0.055) / 1.055, 2.4) : bL / 12.92;

    const M = getRgbToXyzMatrix(illuminantKey);
    const X = M[0][0]*rL + M[0][1]*gL + M[0][2]*bL;
    const Y = M[1][0]*rL + M[1][1]*gL + M[1][2]*bL;
    const Z = M[2][0]*rL + M[2][1]*gL + M[2][2]*bL;

    const illum = ILLUMINANTS[illuminantKey];
    const xr = X / illum.X, yr = Y / illum.Y, zr = Z / illum.Z;
    const delta = 6 / 29;

    const f = t => t > Math.pow(delta, 3) ? Math.cbrt(t) : (t / (3 * delta * delta)) + (4 / 29);

    return {
        L: 116 * f(yr) - 16,
        a: 500 * (f(xr) - f(yr)),
        b: 200 * (f(yr) - f(zr))
    };
}

export function labToRgb(L, a, b, illuminantKey = 'D65', gamutMode = 'clipping') {
    const illum = ILLUMINANTS[illuminantKey];
    const fy = (L + 16) / 116;
    const fx = fy + (a / 500);
    const fz = fy - (b / 200);

    const delta = 6 / 29;
    const fInv = t => t > delta ? Math.pow(t, 3) : 3 * delta * delta * (t - 4 / 29);

    const X = illum.X * fInv(fx);
    const Y = illum.Y * fInv(fy);
    const Z = illum.Z * fInv(fz);

    const M_inv = invert3x3(getRgbToXyzMatrix(illuminantKey));
    let rL = M_inv[0][0]*X + M_inv[0][1]*X + M_inv[0][2]*Z;
    let gL = M_inv[1][0]*X + M_inv[1][1]*Y + M_inv[1][2]*Z;
    let bL = M_inv[2][0]*X + M_inv[2][1]*Y + M_inv[2][2]*Z;

    let outOfGamut = rL < 0 || rL > 1 || gL < 0 || gL > 1 || bL < 0 || bL > 1;

    if (outOfGamut && gamutMode === 'scaling') {
        const maxVal = Math.max(rL, gL, bL, 1);
        rL = Math.max(0, rL / maxVal);
        gL = Math.max(0, gL / maxVal);
        bL = Math.max(0, bL / maxVal);
    }

    const gammaInv = C => C <= 0.0031308 ? 12.92 * C : 1.055 * Math.pow(Math.max(0, C), 1/2.4) - 0.055;

    let r = gammaInv(rL) * 255;
    let g = gammaInv(gL) * 255;
    let bVal = gammaInv(bL) * 255;

    if (gamutMode === 'clipping') {
        r = Math.min(255, Math.max(0, r));
        g = Math.min(255, Math.max(0, g));
        bVal = Math.min(255, Math.max(0, bVal));
    }

    return { r, g, b: bVal, outOfGamut };
}

export function rgbToCmyk(r, g, b, cmykMethod = 'GCR') {
    const rN = r / 255, gN = g / 255, bN = b / 255;
    let c = 1 - rN, m = 1 - gN, y = 1 - bN;
    let k = Math.min(c, m, y);

    if (cmykMethod === 'UCR') {
        const minCmy = Math.min(c, m, y);
        k = minCmy > 0.4 ? (minCmy - 0.4) * 0.5 : 0;
    }

    if (k >= 1) return { c: 0, m: 0, y: 0, k: 100 };

    c = ((c - k) / (1 - k)) * 100;
    m = ((m - k) / (1 - k)) * 100;
    y = ((y - k) / (1 - k)) * 100;
    k = k * 100;

    return { c, m, y, k };
}

export function cmykToRgb(c, m, y, k) {
    const cN = c / 100, mN = m / 100, yN = y / 100, kN = k / 100;
    return {
        r: 255 * (1 - cN) * (1 - kN),
        g: 255 * (1 - mN) * (1 - kN),
        b: 255 * (1 - yN) * (1 - kN)
    };
}

export function getCieCoordinates(r, g, b, illuminantKey = 'D65') {
    let rL = r / 255, gL = g / 255, bL = b / 255;
    rL = rL > 0.04045 ? Math.pow((rL + 0.055) / 1.055, 2.4) : rL / 12.92;
    gL = gL > 0.04045 ? Math.pow((gL + 0.055) / 1.055, 2.4) : gL / 12.92;
    bL = bL > 0.04045 ? Math.pow((bL + 0.055) / 1.055, 2.4) : bL / 12.92;

    const M = getRgbToXyzMatrix(illuminantKey);
    const X = M[0][0]*rL + M[0][1]*gL + M[0][2]*bL;
    const Y = M[1][0]*rL + M[1][1]*gL + M[1][2]*bL;
    const Z = M[2][0]*rL + M[2][1]*gL + M[2][2]*bL;
    const sum = X + Y + Z || 1;
    return { x: X / sum, y: Y / sum };
}