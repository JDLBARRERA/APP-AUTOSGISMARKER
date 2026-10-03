/** Homografía de las cuatro esquinas del proyector a una cadena CSS matrix3d. */

export type Corner = { x: number; y: number };

export interface CornerPoints {
  topLeft: Corner;
  topRight: Corner;
  bottomRight: Corner;
  bottomLeft: Corner;
}

export const IDENTITY_CORNERS: Corner[] = [
  { x: 0, y: 0 },
  { x: 1, y: 0 },
  { x: 1, y: 1 },
  { x: 0, y: 1 },
];

export function toCornerPoints(corners: Corner[]): CornerPoints {
  return {
    topLeft: corners[0] ?? IDENTITY_CORNERS[0],
    topRight: corners[1] ?? IDENTITY_CORNERS[1],
    bottomRight: corners[2] ?? IDENTITY_CORNERS[2],
    bottomLeft: corners[3] ?? IDENTITY_CORNERS[3],
  };
}

export function toCornerList(points: CornerPoints): Corner[] {
  return [points.topLeft, points.topRight, points.bottomRight, points.bottomLeft];
}

export const KEYSTONE_KEY = "autosgismarker-keystone";

/** Devuelve el trapecio de la pared al cuadrado original. */
export function resetKeystone() {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(KEYSTONE_KEY);
}

function solve(matrix: number[][], vector: number[]) {
  const size = vector.length;
  const rows = matrix.map((row, index) => [...row, vector[index]]);
  for (let column = 0; column < size; column += 1) {
    let pivot = column;
    for (let row = column + 1; row < size; row += 1) {
      if (Math.abs(rows[row][column]) > Math.abs(rows[pivot][column])) pivot = row;
    }
    if (Math.abs(rows[pivot][column]) < 1e-8) return null;
    const held = rows[column];
    rows[column] = rows[pivot];
    rows[pivot] = held;
    const divisor = rows[column][column];
    for (let cell = column; cell <= size; cell += 1) rows[column][cell] /= divisor;
    for (let row = 0; row < size; row += 1) {
      if (row === column) continue;
      const factor = rows[row][column];
      for (let cell = column; cell <= size; cell += 1) rows[row][cell] -= factor * rows[column][cell];
    }
  }
  return rows.map((row) => row[size]);
}

/**
 * matrix3d que lleva el cuadrado del escenario a las cuatro esquinas normalizadas.
 * El origen de la transformación debe ser la esquina superior izquierda.
 */
export function keystoneMatrix(corners: Corner[], width: number, height: number) {
  if (width < 2 || height < 2) return "none";
  const source = [
    [0, 0],
    [width, 0],
    [width, height],
    [0, height],
  ];
  const target = corners.map((corner) => [corner.x * width, corner.y * height]);
  const equations: number[][] = [];
  const values: number[] = [];
  for (let index = 0; index < 4; index += 1) {
    const [x, y] = source[index];
    const [u, v] = target[index];
    equations.push([x, y, 1, 0, 0, 0, -u * x, -u * y]);
    values.push(u);
    equations.push([0, 0, 0, x, y, 1, -v * x, -v * y]);
    values.push(v);
  }
  const homography = solve(equations, values);
  if (!homography) return "none";
  const [h0, h1, h2, h3, h4, h5, h6, h7] = homography;
  const cells = [h0, h3, 0, h6, h1, h4, 0, h7, 0, 0, 1, 0, h2, h5, 0, 1];
  return `matrix3d(${cells.map((cell) => cell.toFixed(6)).join(",")})`;
}

function finiteCorner(corner: Corner | undefined): Corner | null {
  if (!corner || !Number.isFinite(corner.x) || !Number.isFinite(corner.y)) return null;
  return { x: corner.x, y: corner.y };
}

export function readCorners(raw: string | null): Corner[] {
  if (!raw) return IDENTITY_CORNERS;
  try {
    const parsed = JSON.parse(raw) as { corners?: Corner[]; points?: Partial<CornerPoints> };
    const named = parsed.points;
    if (named) {
      const list = [named.topLeft, named.topRight, named.bottomRight, named.bottomLeft].map((corner) => finiteCorner(corner));
      if (list.every((corner): corner is Corner => corner !== null)) return list;
    }
    const corners = parsed.corners;
    if (!Array.isArray(corners) || corners.length !== 4) return IDENTITY_CORNERS;
    const list = corners.map((corner) => finiteCorner(corner));
    if (list.every((corner): corner is Corner => corner !== null)) return list;
    return IDENTITY_CORNERS;
  } catch {
    return IDENTITY_CORNERS;
  }
}
