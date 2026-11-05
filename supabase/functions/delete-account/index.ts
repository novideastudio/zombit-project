// Exclusao de conta (exigencia Google Play). Deploy: supabase functions deploy delete-account
// SUPABASE_URL, SUPABASE_ANON_KEY e SUPABASE_SERVICE_ROLE_KEY ja existem no runtime das Edge Functions.
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const cors = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};
const json = (b: unknown, s = 200) =>
  new Response(JSON.stringify(b), { status: s, headers: { ...cors, "Content-Type": "application/json" } });

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") return new Response("ok", { headers: cors });
  if (req.method !== "POST") return json({ error: "method_not_allowed" }, 405);

  const auth = req.headers.get("Authorization") ?? "";
  const url = Deno.env.get("SUPABASE_URL")!;
  const userClient = createClient(url, Deno.env.get("SUPABASE_ANON_KEY")!, {
    global: { headers: { Authorization: auth } },
  });
  const { data, error } = await userClient.auth.getUser();
  if (error || !data.user) return json({ error: "unauthorized" }, 401);
  const uid = data.user.id;

  const admin = createClient(url, Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!);
  for (const t of ["zb2_save_history", "zb2_saves"]) {
    const r = await admin.from(t).delete().eq("user_id", uid);
    if (r.error && !/does not exist|schema cache/i.test(r.error.message)) return json({ error: "cleanup_failed", step: t }, 500);
  }
  const p = await admin.from("profiles").delete().eq("uid", uid);
  if (p.error) return json({ error: "cleanup_failed", step: "profiles" }, 500);

  const d = await admin.auth.admin.deleteUser(uid);
  if (d.error) return json({ error: "delete_failed" }, 500);
  return json({ ok: true });
});
