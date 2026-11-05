#if UNITY_WEBGL && !UNITY_EDITOR
#define ZB_WEB
#elif UNITY_ANDROID && !UNITY_EDITOR
#define ZB_ANDROID
#else
#define ZB_DESKTOP
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
#if ZB_DESKTOP
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
#endif

public enum CloudState { Starting, SignedOut, SigningIn, LoadingProfile, NeedsNick, Syncing, Ready, Error }

/// <summary>
/// Conta online (Supabase Auth + Google) e saves 100% online (zb2_saves via funcoes RPC).
/// Os dados vivem so na memoria (SaveSystem); nenhum JSON de progresso e gravado em disco.
/// Login: WebGL = supabase-js (jslib); Android = navegador + deep link; Windows/Editor = navegador + loopback 127.0.0.1.
/// </summary>
public class ZombitCloud : MonoBehaviour
{
    public const string NickRule = "3 a 18 caracteres: letras, números, _ . -";
    const float FlushDelaySeconds = 2f;
    const string RefreshKey = "zbc_refresh";
    const string AndroidRedirect = "com.novidea.zombit://auth";   // esquema SEMPRE minusculo
    const int LoopbackPort = 53682;

    public static ZombitCloud I { get; private set; }
    public static CloudState State { get; private set; } = CloudState.Starting;
    public static string Uid { get; private set; }
    public static string Email { get; private set; }
    public static string DisplayName { get; private set; }
    public static string Nick { get; private set; }
    public static string LastError { get; private set; } = "";
    public static event Action StateChanged;
    /// <summary>O servidor recusou um save (limite de seguranca ou conflito). Argumento: mensagem para o jogador.</summary>
    public static event Action<string> SaveRejected;

    public static bool IsReady => State == CloudState.Ready;
    public static bool HasNick => !string.IsNullOrEmpty(Nick);
    public static bool IsLocalBackend => false;

    [Serializable] class Cfg { public string url; public string anonKey; }
    [Serializable] class JsResult { public int id; public bool ok; public string data; public string error; }
    [Serializable] class AuthInfo { public bool signedIn; public string uid; public string email; public string name; }
    [Serializable] class ProfileInfo { public string nick; public string nick_key; }
    [Serializable] class ProfileList { public List<ProfileInfo> items = new List<ProfileInfo>(); }
    [Serializable] class SaveRow { public int slot; public long revision; public string data; }
    [Serializable] class SaveList { public List<SaveRow> items = new List<SaveRow>(); }
    [Serializable] class Meta { public string full_name; public string name; }
    [Serializable] class UserResp { public string id; public string email; public Meta user_metadata; }
    [Serializable] class TokenResp { public string access_token; public string refresh_token; public long expires_in; public UserResp user; }
    class Pending { public string json; public bool deleted; }

#if ZB_WEB
    [DllImport("__Internal")] static extern void Zb_Call(int id, string op, string arg);
    [DllImport("__Internal")] static extern string Zb_Poll();
    static int nextId = 1;
    static readonly Dictionary<int, Action<JsResult>> callbacks = new Dictionary<int, Action<JsResult>>();
#endif

    Cfg cfg; string cfgJson; bool backendReady;
    string accessToken, refreshToken; long expiresAt; bool refreshing; UserResp lastUser;
    float signInDeadline = -1f;
    readonly Dictionary<int, Pending> pending = new Dictionary<int, Pending>();
    readonly HashSet<int> inflight = new HashSet<int>();
    float firstDirtyTime = -1f;

    // ---------- ciclo de vida ----------
    public static void Ensure()
    {
        if (I != null) return;
        var go = new GameObject("ZombitCloud");
        DontDestroyOnLoad(go);
        I = go.AddComponent<ZombitCloud>();
        I.StartBackend();
    }

    static bool LooksSecret(string key)
    {
        if (string.IsNullOrEmpty(key)) return true;
        if (key.StartsWith("sb_secret_", StringComparison.Ordinal)) return true;
        try
        {
            var parts = key.Split('.');
            if (parts.Length == 3)
            {
                string p = parts[1].Replace('-', '+').Replace('_', '/');
                while (p.Length % 4 != 0) p += "=";
                if (Encoding.UTF8.GetString(Convert.FromBase64String(p)).Contains("service_role")) return true;
            }
        }
        catch (Exception) { }
        return false;
    }

    void StartBackend()
    {
        SetState(CloudState.Starting);
        var ta = Resources.Load<TextAsset>("Config/supabase_config");
        if (ta == null || ta.text.Contains("COLE_AQUI") || ta.text.Contains("SEU-PROJETO"))
        { Fail("Supabase ainda não configurado. Preencha Assets/_Project/Resources/Config/supabase_config.json."); return; }
        try { cfg = JsonUtility.FromJson<Cfg>(ta.text); } catch (Exception) { cfg = null; }
        if (cfg == null || string.IsNullOrEmpty(cfg.url) || !cfg.url.StartsWith("https://", StringComparison.Ordinal) || string.IsNullOrEmpty(cfg.anonKey))
        { Fail("supabase_config.json inválido."); return; }
        if (LooksSecret(cfg.anonKey))
        { Fail("Chave secreta/service_role detectada no cliente. Use somente a chave pública (publishable/anon)."); return; }
        cfgJson = ta.text;
#if ZB_WEB
        Call("init", cfgJson, r =>
        {
            if (!r.ok) { Fail(Friendly(r.error)); return; }
            backendReady = true;
        });
#else
        backendReady = true;
#if ZB_ANDROID
        Application.deepLinkActivated -= OnDeepLink;
        Application.deepLinkActivated += OnDeepLink;
        if (!string.IsNullOrEmpty(Application.absoluteURL)) OnDeepLink(Application.absoluteURL);
#endif
        refreshToken = PlayerPrefs.GetString(RefreshKey, "");
        if (string.IsNullOrEmpty(refreshToken)) { if (State != CloudState.SigningIn) SetState(CloudState.SignedOut); }
        else StartCoroutine(Refresh(ok =>
        {
            if (ok) FinishNativeLogin();
            else if (!string.IsNullOrEmpty(refreshToken)) Fail(Friendly("network"));
            else SetState(CloudState.SignedOut);
        }));
#endif
    }

    void Update()
    {
#if ZB_WEB
        for (int i = 0; i < 16; i++)
        {
            string raw = Zb_Poll();
            if (string.IsNullOrEmpty(raw)) break;
            Handle(raw);
        }
#endif
#if ZB_DESKTOP
        string frag;
        while (loopbackQueue.TryDequeue(out frag)) HandleAuthFragment(frag);
#endif
        if (signInDeadline > 0f && Time.unscaledTime > signInDeadline)
        {
            signInDeadline = -1f;
            if (State == CloudState.SigningIn) { LastError = "Login expirou. Tente novamente."; SetState(CloudState.SignedOut); StopLoopbackIfAny(); }
        }
        if (pending.Count > 0 && firstDirtyTime >= 0f && Time.unscaledTime - firstDirtyTime >= FlushDelaySeconds)
            FlushNow();
    }

    void OnApplicationFocus(bool focus) { if (!focus) FlushNow(); }
    void OnApplicationPause(bool paused) { if (paused) FlushNow(); }
    void OnApplicationQuit() { FlushNow(); StopLoopbackIfAny(); }

    // ---------- API publica ----------
    public static void SignInWithGoogle()
    {
        if (State != CloudState.SignedOut || I == null) return;
        LastError = "";
        SetState(CloudState.SigningIn);
        I.signInDeadline = Time.unscaledTime + 180f;
#if ZB_WEB
        Call("signIn", "", r =>
        {
            if (r.ok) return;
            LastError = Friendly(r.error);
            if (State == CloudState.SigningIn) SetState(CloudState.SignedOut);
        });
#else
        I.BeginNativeSignIn();
#endif
    }

    public static void SignOut()
    {
        GameSession.Reset();
        if (I == null) return;
        I.StartCoroutine(I.SignOutRoutine());
    }

    IEnumerator SignOutRoutine()
    {
        FlushNow();
        float t0 = Time.unscaledTime;
        while ((inflight.Count > 0 || pending.Count > 0) && Time.unscaledTime - t0 < 8f) yield return null;
#if ZB_WEB
        Call("signOut", "", r => { if (!r.ok) Debug.LogWarning("Zombit: falha ao sair da conta: " + r.error); });
#else
        string tok = accessToken;
        accessToken = null; refreshToken = null; expiresAt = 0;
        PlayerPrefs.DeleteKey(RefreshKey); PlayerPrefs.Save();
        if (!string.IsNullOrEmpty(tok))
        {
            using (var req = new UnityWebRequest(cfg.url.TrimEnd('/') + "/auth/v1/logout", "POST"))
            {
                req.downloadHandler = new DownloadHandlerBuffer(); req.timeout = 10;
                req.SetRequestHeader("apikey", cfg.anonKey);
                req.SetRequestHeader("Authorization", "Bearer " + tok);
                yield return req.SendWebRequest();
            }
        }
        OnAuth(new AuthInfo { signedIn = false });
#endif
    }

    public static void Retry()
    {
        if (I == null) { Ensure(); return; }
        LastError = "";
#if !ZB_WEB
        if (string.IsNullOrEmpty(Uid) && !string.IsNullOrEmpty(I.refreshToken)) { I.StartBackend(); return; }
#endif
        if (!I.backendReady) I.StartBackend();
        else if (!string.IsNullOrEmpty(Uid)) I.LoadProfile();
        else SetState(CloudState.SignedOut);
    }

    public static bool ValidNick(string nick)
    {
        if (string.IsNullOrEmpty(nick) || nick.Length < 3 || nick.Length > 18) return false;
        foreach (char c in nick)
        {
            bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '.' || c == '-';
            if (!ok) return false;
        }
        return true;
    }

    public static void RegisterNick(string nick, Action<bool, string> done)
    {
        nick = (nick ?? "").Trim();
        if (!ValidNick(nick)) { done?.Invoke(false, Friendly("nick-invalid")); return; }
        if (State != CloudState.Ready && State != CloudState.NeedsNick) { done?.Invoke(false, "Conta ainda não está pronta."); return; }
        I.StartCoroutine(I.NickRoutine(nick, done));
    }

    IEnumerator NickRoutine(string nick, Action<bool, string> done)
    {
        long code = 0; string text = null;
        string body = "{\"p_nick\":\"" + nick + "\"}";
        yield return Rest("POST", "/rest/v1/rpc/zb2_set_nick", body, null,
            (c, t) => { code = c; text = t; });
        if (code < 200 || code >= 300)
        {
            string msg = (text ?? "").Contains("nick_profanity") ? "Esse nick nao e permitido."
                : (text ?? "").Contains("nick_rate_limited") ? "Limite de trocas de nick. Tente mais tarde."
                : FriendlyHttp(code, text);
            done?.Invoke(false, msg); yield break;
        }
        bool first = State == CloudState.NeedsNick;
        Nick = nick;
        done?.Invoke(true, "");
        if (first) BeginSync();
    }

    // ---------- save na nuvem ----------
    public static void QueueSave(int slot, string json, long ticks)
    {
        if (I == null || !IsReady || string.IsNullOrEmpty(Uid)) return;
        I.pending[slot] = new Pending { json = json, deleted = false };
        if (I.firstDirtyTime < 0f) I.firstDirtyTime = Time.unscaledTime;
    }

    public static void QueueDelete(int slot)
    {
        if (I == null || !IsReady || string.IsNullOrEmpty(Uid)) return;
        I.pending[slot] = new Pending { json = "", deleted = true };
        if (I.firstDirtyTime < 0f) I.firstDirtyTime = Time.unscaledTime;
    }

    public void FlushNow()
    {
        firstDirtyTime = -1f;
        if (pending.Count == 0) return;
        foreach (var slot in new List<int>(pending.Keys))
            if (!inflight.Contains(slot)) StartCoroutine(UploadSlot(slot));
    }

    IEnumerator UploadSlot(int slot)
    {
        inflight.Add(slot);
        string owner = Uid;
        try
        {
            Pending p;
            while (State == CloudState.Ready && owner == Uid && pending.TryGetValue(slot, out p))
            {
                pending.Remove(slot);
                long expected = SaveSystem.RevisionOf(slot);
                string head = "{\"p_slot\":" + slot + ",\"p_expected_revision\":" + expected;
                string body = p.deleted ? head + "}" : head + ",\"p_data\":" + p.json + "}";
                int attempt = 0;
                while (State == CloudState.Ready && owner == Uid)
                {
                    long code = 0; string text = null;
                    yield return Rest("POST", "/rest/v1/rpc/" + (p.deleted ? "zb2_delete_save" : "zb2_put_save"),
                        body, null, (c, t) => { code = c; text = t; });
                    if (owner != Uid || code == -2 || State != CloudState.Ready) yield break;
                    if (code >= 200 && code < 300)
                    {
                        long rev;
                        if (p.deleted && ((text ?? "").Trim() == "true" || (text ?? "").Trim() == "false"))
                        { SaveSystem.SetRevision(slot, 0); break; }
                        if (!p.deleted && long.TryParse((text ?? "").Trim(), out rev) && rev > 0)
                        { SaveSystem.SetRevision(slot, rev); break; }
                        ZbRejectAndRecover("Resposta invalida ao salvar. Recarregando do servidor.");
                        yield break;
                    }
                    bool limited = code == 429 || (text ?? "").Contains("rate_limited");
                    if (limited || code == 0 || code == 408 || code >= 500)
                    {
                        // Keep the exact request until acknowledged; newer saves wait.
                        attempt++;
                        float delay = limited ? 1.2f : Mathf.Min(20f, Mathf.Pow(2f, Mathf.Min(attempt, 4)));
                        yield return new WaitForSecondsRealtime(delay + UnityEngine.Random.Range(0f, 0.25f));
                        continue;
                    }
                    ZbRejectAndRecover(code == -1 || code == 401
                        ? "Sessao indisponivel. Entre novamente se a recuperacao falhar."
                        : "O servidor recusou o save. " + FriendlyHttp(code, text));
                    yield break;
                }
            }
        }
        finally { inflight.Remove(slot); }
    }

    int zbFailureGeneration;
    bool zbRecovering;

    void ZbRejectAndRecover(string message)
    {
        if (zbRecovering) return;
        zbRecovering = true;
        zbFailureGeneration++;
        pending.Clear();
        LastError = message;
        SetState(CloudState.Syncing);
        try { SaveRejected?.Invoke(message); } catch (Exception e) { Debug.LogException(e); }
        StartCoroutine(ZbRecoverSaves(Uid));
    }

    IEnumerator ZbRecoverSaves(string owner)
    {
        while (inflight.Count > 0) yield return null;
        if (Uid != owner || string.IsNullOrEmpty(owner)) { zbRecovering = false; yield break; }
        yield return SyncRoutine();
        zbRecovering = false;
        if (Uid == owner && State == CloudState.Ready)
        {
            GameSession.Reset();
            UnityEngine.SceneManagement.SceneManager.LoadScene(GameSession.MenuSceneIndex);
        }
    }

    public static IEnumerator FlushAndWait(Action<bool> done, float timeoutSeconds = 30f)
    {
        var cloud = I;
        if (cloud == null || !IsReady) { done?.Invoke(false); yield break; }
        int generation = cloud.zbFailureGeneration;
        string owner = Uid;
        cloud.FlushNow();
        float start = Time.realtimeSinceStartup;
        while (I == cloud && IsReady && owner == Uid && generation == cloud.zbFailureGeneration
            && (cloud.pending.Count > 0 || cloud.inflight.Count > 0)
            && Time.realtimeSinceStartup - start < timeoutSeconds)
            yield return null;
        done?.Invoke(I == cloud && IsReady && owner == Uid && generation == cloud.zbFailureGeneration
            && cloud.pending.Count == 0 && cloud.inflight.Count == 0);
    }

    // ---------- fluxo da conta ----------
    void OnAuth(AuthInfo a)
    {
        if (a == null || !a.signedIn)
        {
            if (State == CloudState.SigningIn && string.IsNullOrEmpty(Uid)) return;
            Uid = Email = DisplayName = Nick = null;
            pending.Clear();
            SaveSystem.ClearAll();
            SetState(CloudState.SignedOut);
            return;
        }
        bool same = Uid == a.uid;
        bool busyOrDone = State == CloudState.LoadingProfile || State == CloudState.Syncing
                          || State == CloudState.NeedsNick || State == CloudState.Ready;
        if (same && busyOrDone) return;
        if (!same) { pending.Clear(); SaveSystem.ClearAll(); }
        Uid = a.uid; Email = a.email; DisplayName = a.name;
        signInDeadline = -1f;
        LoadProfile();
    }

    void LoadProfile()
    {
        SetState(CloudState.LoadingProfile);
        StartCoroutine(ProfileRoutine());
    }

    IEnumerator ProfileRoutine()
    {
        long code = 0; string text = null;
        yield return Rest("GET", "/rest/v1/profiles?select=nick,nick_key&uid=eq." + Uid, null, null, (c, t) => { code = c; text = t; });
        if (code < 200 || code >= 300) { Fail(FriendlyHttp(code, text)); yield break; }
        ProfileList list = null;
        try { list = JsonUtility.FromJson<ProfileList>("{\"items\":" + text + "}"); } catch (Exception) { }
        if (list == null || list.items.Count == 0 || string.IsNullOrEmpty(list.items[0].nick)) { Nick = null; SetState(CloudState.NeedsNick); yield break; }
        Nick = list.items[0].nick;
        BeginSync();
    }

    void BeginSync()
    {
        SetState(CloudState.Syncing);
        StartCoroutine(SyncRoutine());
    }

    IEnumerator SyncRoutine()
    {
        long code = 0; string text = null;
        yield return Rest("POST", "/rest/v1/rpc/zb2_get_saves", "{}", null, (c, t) => { code = c; text = t; });
        if (code < 200 || code >= 300) { Fail(FriendlyHttp(code, text)); yield break; }
        SaveList list = null;
        try { list = JsonUtility.FromJson<SaveList>("{\"items\":" + text + "}"); }
        catch (Exception e) { Debug.LogWarning("Zombit: resposta de saves inválida: " + e.Message); }
        if (list == null) { Fail("Resposta inválida do servidor."); yield break; }
        SaveSystem.ClearAll();
        foreach (var row in list.items)
            if (row.slot >= 1 && row.slot <= SaveSystem.SlotCount && !string.IsNullOrEmpty(row.data))
                SaveSystem.ImportFromCloud(row.slot, row.data, row.revision);
        SaveSystem.PurgeLegacyLocalFiles();
        SetState(CloudState.Ready);
    }

    static void SetState(CloudState s)
    {
        State = s;
        try { StateChanged?.Invoke(); }
        catch (Exception e) { Debug.LogException(e); }
    }

    static void Fail(string message)
    {
        LastError = message;
        SetState(CloudState.Error);
    }

    // ---------- REST (todas as plataformas) ----------
    IEnumerator GetToken(Action<string> cb)
    {
#if ZB_WEB
        bool got = false; string tok = null;
        Call("getToken", "", r => { got = true; tok = r.ok ? r.data : null; });
        float t0 = Time.unscaledTime;
        while (!got && Time.unscaledTime - t0 < 10f) yield return null;
        cb(tok);
#else
        if (string.IsNullOrEmpty(accessToken)) { cb(null); yield break; }
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expiresAt - 60)
        {
            bool ok = false;
            yield return StartCoroutine(Refresh(r => ok = r));
            if (!ok) { cb(null); yield break; }
        }
        cb(accessToken);
#endif
    }

    /// <summary>done(code, texto). code: 0 = rede, -1 = sem sessao, -2 = sessao mudou.</summary>
    IEnumerator Rest(string method, string path, string body, string prefer, Action<long, string> done)
    {
        string token = null;
        yield return StartCoroutine(GetToken(t => token = t));
        if (string.IsNullOrEmpty(token)) { done(-1, "not-signed-in"); yield break; }
        string uidAtStart = Uid;
        using (var req = new UnityWebRequest(cfg.url.TrimEnd('/') + path, method))
        {
            if (body != null) req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = 25;
            req.SetRequestHeader("apikey", cfg.anonKey);
            req.SetRequestHeader("Authorization", "Bearer " + token);
            req.SetRequestHeader("Content-Type", "application/json");
            if (prefer != null) req.SetRequestHeader("Prefer", prefer);
            yield return req.SendWebRequest();
            if (uidAtStart != Uid) { done(-2, "session-changed"); yield break; }
            bool netErr = req.result == UnityWebRequest.Result.ConnectionError;
            done(netErr ? 0 : req.responseCode, req.downloadHandler.text);
        }
    }

#if ZB_WEB
    static void Call(string op, string argJson, Action<JsResult> cb)
    {
        int id = nextId++;
        if (cb != null) callbacks[id] = cb;
        Zb_Call(id, op, argJson ?? "");
    }

    void Handle(string raw)
    {
        try
        {
            var res = JsonUtility.FromJson<JsResult>(raw);
            if (res == null) return;
            if (res.id == 0) { OnAuth(JsonUtility.FromJson<AuthInfo>(res.data)); return; }
            Action<JsResult> cb;
            if (callbacks.TryGetValue(res.id, out cb)) { callbacks.Remove(res.id); cb(res); }
        }
        catch (Exception e) { Debug.LogException(e); }
    }
#else
    static void Call(string op, string argJson, Action<JsResult> cb) { }
#endif

#if !ZB_WEB
    // ---------- login nativo (Android / Windows / Editor) ----------
    void BeginNativeSignIn()
    {
        string redirect;
#if ZB_ANDROID
        redirect = AndroidRedirect;
#else
        redirect = "http://127.0.0.1:" + LoopbackPort + "/";
        if (!StartLoopback()) { LastError = "Não foi possível abrir a porta local " + LoopbackPort + " para o login."; SetState(CloudState.SignedOut); return; }
#endif
        string url = cfg.url.TrimEnd('/') + "/auth/v1/authorize?provider=google&redirect_to=" +
                     UnityWebRequest.EscapeURL(redirect) + "&prompt=select_account";
        Application.OpenURL(url);
    }

    static Dictionary<string, string> ParseKv(string s)
    {
        var d = new Dictionary<string, string>();
        foreach (var part in (s ?? "").Split('&'))
        {
            int i = part.IndexOf('=');
            if (i <= 0) continue;
            d[UnityWebRequest.UnEscapeURL(part.Substring(0, i))] = UnityWebRequest.UnEscapeURL(part.Substring(i + 1));
        }
        return d;
    }

    void OnDeepLink(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        int h = url.IndexOf('#');
        if (h < 0) h = url.IndexOf('?');
        if (h < 0) return;
        HandleAuthFragment(url.Substring(h + 1));
    }

    void HandleAuthFragment(string frag)
    {
        var kv = ParseKv(frag);
        string err;
        if (kv.TryGetValue("error_description", out err) || kv.TryGetValue("error", out err))
        {
            LastError = err;
            if (State == CloudState.SigningIn) SetState(CloudState.SignedOut);
            StopLoopbackIfAny();
            return;
        }
        if (!kv.ContainsKey("access_token") || !kv.ContainsKey("refresh_token")) return;
        accessToken = kv["access_token"]; refreshToken = kv["refresh_token"];
        long exp = 3600; if (kv.ContainsKey("expires_in")) long.TryParse(kv["expires_in"], out exp);
        expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + exp;
        PlayerPrefs.SetString(RefreshKey, refreshToken); PlayerPrefs.Save();
        StopLoopbackIfAny();
        StartCoroutine(FetchUser());
    }

    IEnumerator FetchUser()
    {
        using (var req = UnityWebRequest.Get(cfg.url.TrimEnd('/') + "/auth/v1/user"))
        {
            req.timeout = 20;
            req.SetRequestHeader("apikey", cfg.anonKey);
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            { LastError = Friendly("network"); SetState(CloudState.SignedOut); yield break; }
            try { lastUser = JsonUtility.FromJson<UserResp>(req.downloadHandler.text); } catch (Exception) { lastUser = null; }
        }
        if (lastUser == null || string.IsNullOrEmpty(lastUser.id)) { SetState(CloudState.SignedOut); yield break; }
        FinishNativeLogin();
    }

    void FinishNativeLogin()
    {
        if (lastUser == null || string.IsNullOrEmpty(lastUser.id)) { SetState(CloudState.SignedOut); return; }
        string name = lastUser.user_metadata != null ? (lastUser.user_metadata.full_name ?? lastUser.user_metadata.name) : "";
        OnAuth(new AuthInfo { signedIn = true, uid = lastUser.id, email = lastUser.email, name = name });
    }

    IEnumerator Refresh(Action<bool> done)
    {
        if (refreshing)
        {
            while (refreshing) yield return null;
            done(!string.IsNullOrEmpty(accessToken) && DateTimeOffset.UtcNow.ToUnixTimeSeconds() < expiresAt);
            yield break;
        }
        refreshing = true;
        bool ok = false;
        string body = "{\"refresh_token\":\"" + (refreshToken ?? "").Replace("\\", "").Replace("\"", "") + "\"}";
        using (var req = new UnityWebRequest(cfg.url.TrimEnd('/') + "/auth/v1/token?grant_type=refresh_token", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer(); req.timeout = 20;
            req.SetRequestHeader("apikey", cfg.anonKey);
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                TokenResp t = null;
                try { t = JsonUtility.FromJson<TokenResp>(req.downloadHandler.text); } catch (Exception) { }
                if (t != null && !string.IsNullOrEmpty(t.access_token) && !string.IsNullOrEmpty(t.refresh_token))
                {
                    accessToken = t.access_token; refreshToken = t.refresh_token;
                    expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (t.expires_in > 0 ? t.expires_in : 3600);
                    if (t.user != null && !string.IsNullOrEmpty(t.user.id)) lastUser = t.user;
                    PlayerPrefs.SetString(RefreshKey, refreshToken); PlayerPrefs.Save();
                    ok = true;
                }
            }
            else if (req.responseCode == 400 || req.responseCode == 401)
            {
                accessToken = null; refreshToken = null; PlayerPrefs.DeleteKey(RefreshKey); PlayerPrefs.Save();
            }
        }
        refreshing = false;
        done(ok);
    }
#endif

#if ZB_DESKTOP
    readonly ConcurrentQueue<string> loopbackQueue = new ConcurrentQueue<string>();
    HttpListener listener;
    const string LoopbackPage = "<html><head><meta charset='utf-8'></head><body style='font-family:sans-serif;background:#111;color:#eee;text-align:center;padding-top:80px'>" +
        "<h2 id='m'>Entrando...</h2><script>var h=location.hash.substring(1);" +
        "if(h.indexOf('access_token')>=0){fetch('/cb?'+h).then(function(){document.getElementById('m').textContent='Login concluido. Volte ao jogo.';});}" +
        "else{document.getElementById('m').textContent='Falha no login. Volte ao jogo e tente novamente.';}</script></body></html>";

    bool StartLoopback()
    {
        StopLoopbackIfAny();
        try
        {
            var l = new HttpListener();
            l.Prefixes.Add("http://127.0.0.1:" + LoopbackPort + "/");
            l.Start();
            listener = l;
            var th = new Thread(() =>
            {
                try
                {
                    while (l.IsListening)
                    {
                        var ctx = l.GetContext();
                        string html;
                        if (ctx.Request.Url.AbsolutePath == "/cb") { loopbackQueue.Enqueue(ctx.Request.Url.Query.TrimStart('?')); html = "OK"; }
                        else html = LoopbackPage;
                        var bytes = Encoding.UTF8.GetBytes(html);
                        ctx.Response.ContentType = "text/html; charset=utf-8";
                        ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                        ctx.Response.Close();
                    }
                }
                catch (Exception) { }
            });
            th.IsBackground = true;
            th.Start();
            return true;
        }
        catch (Exception e) { Debug.LogWarning("Zombit: loopback falhou: " + e.Message); return false; }
    }

    void StopLoopbackIfAny()
    {
        try { if (listener != null) { listener.Close(); } } catch (Exception) { }
        listener = null;
    }
#else
    void StopLoopbackIfAny() { }
#endif

    // ---------- mensagens ----------
    static string Friendly(string code)
    {
        switch (code ?? "")
        {
            case "network": return "Sem conexão com a internet.";
            case "script-load-failed": return "Não foi possível carregar o Supabase (internet ou bloqueador de scripts).";
            case "nick-taken": return "Esse nick já está em uso. Escolha outro.";
            case "nick-invalid": return "Nick inválido. Use " + NickRule + ".";
            case "permission-denied": return "Sem permissão no banco.";
            default: return "Erro inesperado (" + code + ").";
        }
    }

    static string FriendlyHttp(long code, string text)
    {
        text = text ?? "";
        if (code == 0) return Friendly("network");
        if (code == -1) return "Sessão expirada. Entre novamente.";
        if (text.Contains("23505")) return Friendly("nick-taken");
        if (text.Contains("23514")) return Friendly("nick-invalid");
        if (text.Contains("PGRST202") || text.Contains("42883") || text.Contains("PGRST205") || text.Contains("42P01"))
            return "Banco não preparado. Rode supabase/001_saves_online.sql no Supabase.";
        if (text.Contains("limit_gold") || text.Contains("limit_scrap") || text.Contains("limit_level") || text.Contains("limit_new_save"))
            return "O servidor recusou este progresso (limite de segurança).";
        if (text.Contains("save_too_large")) return "Save grande demais para o servidor.";
        if (text.Contains("revision_conflict")) return "Save alterado em outro dispositivo.";
        if (code == 401 || code == 403) return Friendly("permission-denied");
        return "Erro do servidor (" + code + ").";
    }
}
