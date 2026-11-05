using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Novidea.Zombit.CloudV2
{
    // Transporte isolado. Nao autentica, nao renova tokens e nao altera SaveSystem.
    public sealed class SupabaseRpcClient
    {
        readonly string url;
        readonly string publicKey;
        string accessToken;
        public SupabaseRpcClient(string projectUrl, string publishableKey)
        {
            if (string.IsNullOrWhiteSpace(projectUrl) || !projectUrl.StartsWith("https://", StringComparison.Ordinal))
                throw new ArgumentException("HTTPS project URL required");
            if (string.IsNullOrWhiteSpace(publishableKey)) throw new ArgumentException("Public key required");
            if (publishableKey.StartsWith("sb_secret_",StringComparison.Ordinal)) throw new ArgumentException("Secret keys are forbidden in client");
            url = projectUrl.TrimEnd('/'); publicKey = publishableKey;
        }
        public void SetAccessToken(string token) { accessToken = token; }
        public void ClearSession() { accessToken = null; }
        [Serializable] sealed class SlotArgs { public int p_slot; }
        [Serializable] sealed class BuyArgs { public int p_slot; public string p_offer_id; public string p_request_id; }
        public IEnumerator OpenSlot(int slot, Action<string> success, Action<long,string> failure)
        {
            return Send("zb2_open_slot", JsonUtility.ToJson(new SlotArgs { p_slot=slot }), success, failure);
        }
        // Reutilizar o MESMO requestId em nova tentativa da mesma compra.
        public IEnumerator BuyOffer(int slot, string offerId, string requestId, Action<string> success, Action<long,string> failure)
        {
            Guid unused;
            if (!Guid.TryParse(offerId,out unused) || !Guid.TryParse(requestId,out unused)) throw new ArgumentException("UUID required");
            return Send("zb2_buy_offer",JsonUtility.ToJson(new BuyArgs { p_slot=slot,p_offer_id=offerId,p_request_id=requestId }),success,failure);
        }
        IEnumerator Send(string rpc,string payload,Action<string> success,Action<long,string> failure)
        {
            if(string.IsNullOrEmpty(accessToken)){failure?.Invoke(401,"Authentication required");yield break;}
            string requestToken=accessToken;
            using(var req=new UnityWebRequest(url+"/rest/v1/rpc/"+rpc,"POST"))
            {
                req.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                req.downloadHandler=new DownloadHandlerBuffer(); req.timeout=25;
                req.SetRequestHeader("Content-Type","application/json");
                req.SetRequestHeader("apikey",publicKey);
                req.SetRequestHeader("Authorization","Bearer "+requestToken);
                yield return req.SendWebRequest();
                if(requestToken!=accessToken){failure?.Invoke(409,"Session changed; discard response");yield break;}
                if(req.result!=UnityWebRequest.Result.Success){failure?.Invoke(req.responseCode,req.downloadHandler.text);yield break;}
                success?.Invoke(req.downloadHandler.text);
            }
        }
    }
}
