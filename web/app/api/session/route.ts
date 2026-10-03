import { listenSession, readSession, writeSession, type LiveState } from "@/lib/sessionBus";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

export function GET() {
  const encoder = new TextEncoder();
  let remove = () => {};
  const stream = new ReadableStream({
    start(controller) {
      const send = (state: LiveState) => {
        controller.enqueue(encoder.encode(`data: ${JSON.stringify(state)}\n\n`));
      };
      send(readSession());
      remove = listenSession(send);
    },
    cancel() {
      remove();
    },
  });

  return new Response(stream, {
    headers: {
      "Content-Type": "text/event-stream",
      "Cache-Control": "no-cache, no-transform",
      Connection: "keep-alive",
    },
  });
}

export async function POST(request: Request) {
  const body = (await request.json()) as LiveState;
  return Response.json(writeSession(body));
}
