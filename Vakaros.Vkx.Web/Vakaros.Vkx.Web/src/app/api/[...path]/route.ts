import { type NextRequest, NextResponse } from "next/server";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5223";

// Headers that must never be forwarded in either direction.
const HOP_BY_HOP = new Set([
  "connection",
  "keep-alive",
  "proxy-authenticate",
  "proxy-authorization",
  "te",
  "trailers",
  "transfer-encoding",
  "upgrade",
]);

async function proxy(req: NextRequest, path: string[]): Promise<NextResponse> {
  const target = `${API_BASE}/api/${path.join("/")}${req.nextUrl.search}`;

  // Build forwarded request headers.
  const reqHeaders = new Headers();
  req.headers.forEach((value, key) => {
    const k = key.toLowerCase();
    if (k === "host") return;
    if (HOP_BY_HOP.has(k)) return;
    // Tell the upstream NOT to compress. Node fetch auto-decompresses, so
    // forwarding content-encoding back to the browser would be double-encoded.
    if (k === "accept-encoding") return;
    reqHeaders.set(key, value);
  });

  const hasBody = req.method !== "GET" && req.method !== "HEAD";

  const upstream = await fetch(target, {
    method: req.method,
    headers: reqHeaders,
    body: hasBody ? req.body : undefined,
    // @ts-expect-error
    duplex: "half",
  });

  // Build forwarded response headers.
  const resHeaders = new Headers();
  upstream.headers.forEach((value, key) => {
    const k = key.toLowerCase();
    if (HOP_BY_HOP.has(k)) return;
    // Node fetch already decoded the body — strip these so the browser does
    // not try to decode an already-decoded stream (ERR_CONTENT_DECODING_FAILED).
    if (k === "content-encoding") return;
    if (k === "content-length") return;
    resHeaders.set(key, value);
  });

  return new NextResponse(upstream.body, {
    status: upstream.status,
    headers: resHeaders,
  });
}

type RouteCtx = { params: Promise<{ path: string[] }> };

export async function GET(req: NextRequest, ctx: RouteCtx) {
  return proxy(req, (await ctx.params).path);
}
export async function POST(req: NextRequest, ctx: RouteCtx) {
  return proxy(req, (await ctx.params).path);
}
export async function PUT(req: NextRequest, ctx: RouteCtx) {
  return proxy(req, (await ctx.params).path);
}
export async function PATCH(req: NextRequest, ctx: RouteCtx) {
  return proxy(req, (await ctx.params).path);
}
export async function DELETE(req: NextRequest, ctx: RouteCtx) {
  return proxy(req, (await ctx.params).path);
}