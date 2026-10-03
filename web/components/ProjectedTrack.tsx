import { ruleColor, ruleLabel, type CatalogTrack, type RallySpan, type TrackPoint } from "@/lib/catalog";
import { pointAt, zoneColor, zoneLabel, type CustomZone } from "@/lib/trackEditor";
import { DEFAULT_SPONSOR_TEXT, type SuperPower } from "@/lib/sessionBus";

type Props = {
  track: CatalogTrack;
  mode?: "card" | "projector";
  clockRatio?: number;
  finalStretch?: boolean;
  zones?: CustomZone[] | null;
  effect?: SuperPower;
  laneStroke?: number;
  centerLineStroke?: number;
  bridgeStroke?: number;
  zoneFactor?: number;
  showBanners?: boolean;
  sponsorText?: string;
  sponsorLogos?: string[];
  vip?: boolean;
};

function CheckeredStraight({ ax, az, bx, bz }: { ax: number; az: number; bx: number; bz: number }) {
  const dx = bx - ax;
  const dz = bz - az;
  const length = Math.hypot(dx, dz) || 1;
  const ux = dx / length;
  const uz = dz / length;
  const px = -uz;
  const pz = ux;
  const cells = 10;
  const size = Math.min(0.028, length / cells);
  const angle = (Math.atan2(-(bz - az), bx - ax) * 180) / Math.PI;
  return (
    <g>
      {Array.from({ length: cells }, (_, index) => {
        const along = (index + 0.5) / cells;
        const x = ax + ux * length * along + px * 0.05;
        const z = az + uz * length * along + pz * 0.05;
        return (
          <rect
            key={index}
            x={-size / 2}
            y={-size / 2}
            width={size}
            height={size}
            fill={index % 2 === 0 ? "#f8f8f8" : "#141414"}
            stroke="#f5d90a"
            strokeWidth="0.002"
            transform={`translate(${x.toFixed(4)} ${(-z).toFixed(4)}) rotate(${angle.toFixed(2)})`}
          >
            <animate attributeName="opacity" values="1;0.2;1" dur="0.7s" repeatCount="indefinite" begin={`${(index * 0.04).toFixed(2)}s`} />
          </rect>
        );
      })}
    </g>
  );
}

function strokeOf(points: TrackPoint[], close: boolean) {
  const commands = points.map((point, index) => `${index === 0 ? "M" : "L"} ${point.x.toFixed(4)} ${(-point.z).toFixed(4)}`).join(" ");
  return close ? `${commands} Z` : commands;
}

function bridgeStroke(points: TrackPoint[]) {
  const commands: string[] = [];
  for (let index = 0; index < points.length - 1; index += 1) {
    const current = points[index];
    const next = points[index + 1];
    if ((current.layer ?? 1) < 2 && (next.layer ?? 1) < 2) continue;
    commands.push(`M ${current.x.toFixed(4)} ${(-current.z).toFixed(4)} L ${next.x.toFixed(4)} ${(-next.z).toFixed(4)}`);
  }
  return commands.join(" ");
}

function outward(point: { x: number; z: number }, previous?: { x: number; z: number }, next?: { x: number; z: number }) {
  let outX = point.x;
  let outZ = point.z;
  const radial = Math.hypot(outX, outZ);
  if (radial > 0.08) {
    return { outX: outX / radial, outZ: outZ / radial };
  }
  outX = -((next?.z ?? point.z) - (previous?.z ?? point.z));
  outZ = (next?.x ?? point.x) - (previous?.x ?? point.x);
  const length = Math.hypot(outX, outZ) || 1;
  return { outX: outX / length, outZ: outZ / length };
}

function bannerLine(text: string) {
  const clean = text.trim();
  if (!clean) return "";
  return clean.includes("★") ? clean : `★ ${clean} ★`;
}

/** Franja LED fija, horizontal, fuera del cuadrado de la pista. */
function PerimeterBanner({ y, text }: { y: number; text: string }) {
  const width = 1.32;
  const height = 0.07;
  const label = bannerLine(text);
  return (
    <g>
      <rect
        x={(-width / 2).toFixed(4)}
        y={y.toFixed(4)}
        width={width.toFixed(4)}
        height={height.toFixed(4)}
        rx="0.008"
        fill="#07060d"
        fillOpacity="0.82"
        stroke="#00f3ff"
        strokeWidth="0.004"
      >
        <animate attributeName="stroke" values="#00f3ff;#ff2bd6;#ffd91a;#00f3ff" dur="2.4s" repeatCount="indefinite" />
      </rect>
      <text
        x="0"
        y={(y + 0.046).toFixed(4)}
        textAnchor="middle"
        fill="#f4fbff"
        fontSize="0.032"
        fontWeight="800"
      >
        {label}
      </text>
    </g>
  );
}

function roleLabel(point: TrackPoint) {
  if (point.role === "start") return "P0";
  if (point.role === "finish") return "PN";
  if (point.role === "checkpoint") return "CP";
  return "";
}

export function ProjectedTrack({
  track,
  mode = "card",
  clockRatio,
  finalStretch = false,
  zones,
  effect = "",
  laneStroke,
  centerLineStroke,
  bridgeStroke: elevatedStroke,
  zoneFactor = 1,
  showBanners = false,
  sponsorText = DEFAULT_SPONSOR_TEXT,
  sponsorLogos = [],
  vip = false,
}: Props) {
  const projector = mode === "projector";
  const road = projector ? laneStroke ?? 0.07 : 0.05;
  const center = projector ? centerLineStroke ?? road * (0.03 / 0.07) : 0.022;
  const bridgeShadow = projector ? elevatedStroke ?? road * (0.09 / 0.07) : 0.06;
  const bridgeDeck = projector ? bridgeShadow * (0.05 / 0.09) : 0.034;
  const bridgeLine = projector ? bridgeShadow * (0.018 / 0.09) : 0.012;
  const zoneGrow = projector ? zoneFactor : 1;
  const custom = zones != null;
  const open = track.open === true;
  const spans: RallySpan[] = track.spans?.length ? track.spans : [{ id: "spine", kind: "spine", points: track.points }];
  const markers = spans.flatMap((span) => span.points);
  const seen = new Set<string>();
  const unique = markers.filter((point) => {
    const key = `${point.x.toFixed(3)}:${point.z.toFixed(3)}:${point.role ?? ""}:${point.rule}:${point.power ?? ""}`;
    if (seen.has(key)) return false;
    seen.add(key);
    return true;
  });
  const finishPoint = unique.find((point) => point.role === "finish" || point.rule === "finish") ?? track.points[open ? track.points.length - 1 : 0];
  const finishSpot = custom
    ? pointAt(track.points, zones.find((zone) => zone.kind === "finish")?.t ?? (open ? 1 : 0), !open)
    : finishPoint;
  const fork = unique.find((point) => point.role === "fork");
  const forked = spans.some((span) => span.kind === "routeA" || span.kind === "routeB");
  const routeC = spans.some((span) => span.kind === "routeC");
  const beforeFinish = (() => {
    for (const span of spans) {
      const index = span.points.findIndex((point) => point.role === "finish" || point.rule === "finish");
      if (index > 0) return span.points[index - 1];
    }
    return track.points[Math.max(0, track.points.length - 2)];
  })();
  const scale = track.zoneScale ?? 1;

  return (
    <svg
      viewBox={projector ? "-0.72 -0.72 1.44 1.44" : "-0.52 -0.52 1.04 1.04"}
      className={projector ? "h-full w-full" : "h-28 w-full"}
      role="img"
      aria-label={track.name}
    >
      <rect x="-0.68" y="-0.68" width="1.36" height="1.36" fill={projector ? "transparent" : "#05070b"} />
      <rect x="-0.47" y="-0.47" width="0.94" height="0.94" fill="none" stroke="#1c2430" strokeWidth="0.008" />
      {spans.map((span) => {
        const path = strokeOf(span.points, !open && span.kind === "spine" && spans.length === 1);
        const deck = bridgeStroke(span.points);
        return (
          <g key={span.id}>
            <path d={path} fill="none" stroke={finalStretch ? "#f5d90a" : "#083848"} strokeWidth={road.toFixed(4)} strokeLinejoin="miter" strokeLinecap="square" className={finalStretch ? "animate-pulse" : undefined} />
            <path d={path} fill="none" stroke={finalStretch ? "#ff2244" : vip ? "#ffd91a" : span.kind === "routeA" ? "#ff4d6d" : span.kind === "routeC" ? "#ff7a18" : "#39f3ff"} strokeWidth={center.toFixed(4)} strokeLinejoin="miter" strokeLinecap="square" className={finalStretch || vip ? "animate-pulse" : undefined} />
            {vip ? <path d={path} fill="none" stroke="#ffd91a" strokeWidth={(road * 1.35).toFixed(4)} strokeLinejoin="miter" opacity="0.28" /> : null}
            {deck ? (
              <g>
                <path d={deck} fill="none" stroke="#020308" strokeWidth={bridgeShadow.toFixed(4)} strokeLinejoin="miter" transform="translate(0.02 0.028)" opacity="0.9" />
                <path d={deck} fill="none" stroke="#f4ffff" strokeWidth={bridgeDeck.toFixed(4)} strokeLinejoin="miter" />
                <path d={deck} fill="none" stroke="#39f3ff" strokeWidth={bridgeLine.toFixed(4)} strokeLinejoin="miter" />
              </g>
            ) : null}
          </g>
        );
      })}
      {effect === "shield" ? <rect x="-0.5" y="-0.4" width="1" height="0.8" fill="none" stroke="#39ff14" strokeWidth="0.016" className="animate-pulse" /> : null}
      {effect === "turbo3x" && track.points.length > 1 ? (
        <path d={strokeOf(track.points.slice(0, Math.min(4, track.points.length)), false)} fill="none" stroke="#39ff14" strokeWidth="0.02" className="animate-pulse" />
      ) : null}
      {effect === "rainbomb" && fork ? <ellipse cx={fork.x.toFixed(4)} cy={(-fork.z).toFixed(4)} rx="0.12" ry="0.07" fill="#1f52d9" opacity="0.62" /> : null}
      {effect === "freeze" && fork ? <circle cx={fork.x.toFixed(4)} cy={(-fork.z).toFixed(4)} r="0.07" fill="#8ce6ff" opacity="0.8" /> : null}
      {projector && showBanners && !vip ? (
        <g>
          <PerimeterBanner y={-0.69} text={sponsorText.trim() || DEFAULT_SPONSOR_TEXT} />
          <PerimeterBanner y={0.62} text={sponsorLogos.length > 0 ? sponsorLogos.join("  ·  ") : "GISMARKER RACING"} />
        </g>
      ) : null}
      {projector && finalStretch && finishPoint && beforeFinish ? (
        <CheckeredStraight ax={beforeFinish.x} az={beforeFinish.z} bx={finishPoint.x} bz={finishPoint.z} />
      ) : null}
      {unique.map((point, index) => {
        const onSpine = track.points.some((spot) => spot.x.toFixed(3) === point.x.toFixed(3) && spot.z.toFixed(3) === point.z.toFixed(3));
        const marked = (point.rule !== "path" || point.power === "turbo") && (!custom || !onSpine);
        const color = point.power === "turbo" ? "#39ff14" : ruleColor(point.rule);
        const label = point.power === "turbo" ? "TURBO" : ruleLabel(point);
        const place = roleLabel(point);
        const neighbors = outward(point, unique[index - 1], unique[index + 1]);
        const anchor = neighbors.outX > 0.45 ? "start" : neighbors.outX < -0.45 ? "end" : "middle";
        const radius = (marked ? 0.026 : 0.014) * (marked ? scale : 1) * (marked ? zoneGrow : 1);
        return (
          <g key={`${track.id}-p-${index}`} transform={`translate(${point.x.toFixed(4)} ${(-point.z).toFixed(4)})`}>
            <circle
              r={radius.toFixed(4)}
              fill={point.role === "start" ? "#39ff14" : point.role === "finish" ? "#ff260d" : marked ? color : "#071016"}
              stroke={point.role === "start" ? "#39ff14" : point.role === "finish" ? "#ff260d" : marked ? color : "#e8fbff"}
              strokeWidth="0.008"
            />
            {projector && place ? (
              <text y={place === "P0" ? -0.05 : 0.05} textAnchor="middle" fill={place === "P0" ? "#39ff14" : place === "PN" ? "#ff260d" : "#f4fff8"} fontSize="0.04" fontWeight="800">
                {place}
              </text>
            ) : null}
            {projector && label && marked && point.role !== "start" && point.role !== "finish" ? (
              <text x={(neighbors.outX * 0.12).toFixed(4)} y={(-neighbors.outZ * 0.12).toFixed(4)} textAnchor={anchor} fill={color} fontSize="0.028" fontWeight="700">
                {label}
              </text>
            ) : null}
          </g>
        );
      })}
      {custom
        ? zones.map((zone) => {
            const spot = pointAt(track.points, zone.t, !open);
            const color = zoneColor(zone.kind);
            const radius = (projector ? 0.03 : 0.02) * zone.scale * scale * zoneGrow;
            const neighbors = outward(spot);
            const anchor = neighbors.outX > 0.45 ? "start" : neighbors.outX < -0.45 ? "end" : "middle";
            const caption = zone.scale === 1 ? zoneLabel(zone.kind) : `${zoneLabel(zone.kind)} ${zone.scale}x`;
            return (
              <g key={zone.id} transform={`translate(${spot.x.toFixed(4)} ${(-spot.z).toFixed(4)})`}>
                <circle r={(radius + 0.012).toFixed(4)} fill={color} opacity="0.28" />
                <circle r={radius.toFixed(4)} fill={color} stroke="#ffffff" strokeWidth="0.006" />
                {projector ? (
                  <text x={(neighbors.outX * (0.12 + radius)).toFixed(4)} y={(-neighbors.outZ * (0.12 + radius)).toFixed(4)} textAnchor={anchor} fill={color} fontSize="0.028" fontWeight="700">
                    {caption}
                  </text>
                ) : null}
              </g>
            );
          })
        : null}
      {projector && forked && fork ? (
        <g>
          <text x={fork.x.toFixed(4)} y={(-fork.z - 0.1).toFixed(4)} textAnchor="middle" fill="#ff4d8d" fontSize="0.03" fontWeight="800">
            ⬅️ RUTA A: CORTO / EXTREMO
          </text>
          <text x={fork.x.toFixed(4)} y={(-fork.z + 0.12).toFixed(4)} textAnchor="middle" fill="#7dffe0" fontSize="0.03" fontWeight="800">
            ➡️ RUTA B: LARGO / SEGURO
          </text>
          {routeC ? (
            <text x={fork.x.toFixed(4)} y={(-fork.z + 0.18).toFixed(4)} textAnchor="middle" fill="#ffb020" fontSize="0.026" fontWeight="800">
              ↯ RUTA C: DESPLOME
            </text>
          ) : null}
        </g>
      ) : null}
      {projector && clockRatio !== undefined && finishSpot ? (
        <g transform={`translate(${finishSpot.x.toFixed(4)} ${(-finishSpot.z).toFixed(4)})`}>
          <circle r="0.055" fill="none" stroke="#102018" strokeWidth="0.01" />
          <circle
            r="0.055"
            fill="none"
            stroke={clockRatio > 0.5 ? "#39FF14" : clockRatio > 0.2 ? "#F5D90A" : "#FF3355"}
            strokeWidth="0.014"
            strokeLinecap="round"
            strokeDasharray={`${(2 * Math.PI * 0.055 * Math.max(0, Math.min(1, clockRatio))).toFixed(4)} ${(2 * Math.PI * 0.055).toFixed(4)}`}
            transform="rotate(-90)"
          />
        </g>
      ) : null}
    </svg>
  );
}
