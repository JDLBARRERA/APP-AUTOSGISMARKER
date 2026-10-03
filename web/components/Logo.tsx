interface LogoProps {
  className?: string;
  width?: number | string;
  height?: number | string;
  decorative?: boolean;
}

/**
 * Lockup oficial de AUTOSGISMARKER. La imagen vive en public/logo-autosgismarker.jpg.
 */
export const Logo = ({ className = "h-auto w-full", width, height, decorative = false }: LogoProps) => {
  return (
    // eslint-disable-next-line @next/next/no-img-element
    <img
      src="/logo-autosgismarker.jpg?v=2"
      alt={decorative ? "" : "AUTOSGISMARKER"}
      width={typeof width === "number" ? width : undefined}
      height={typeof height === "number" ? height : undefined}
      aria-hidden={decorative || undefined}
      className={`mix-blend-screen ${className}`}
    />
  );
};
