Em Assets/Plugins/WebGL/ZombitSupabase.jslib faca DUAS edicoes pequenas (o resto do arquivo fica igual):

1) Dentro de "$ZB: { ... }", antes de "dispatch:", adicione esta funcao:

    getToken: function (id) {
      ZB.sb.auth.getSession().then(function (r) {
        var s = r && r.data ? r.data.session : null;
        ZB.push(id, true, s ? s.access_token : '');
      }).catch(function (e) { ZB.fail(id, e); });
    },

2) Dentro do "switch (op)" de dispatch, adicione:

          case 'getToken': ZB.getToken(id); break;

As operacoes antigas (getProfile, setNick, putSave, getSaves) deixam de ser usadas; podem ficar no arquivo.
Nao e necessario mais nada no jslib: perfil e saves agora vao por UnityWebRequest.
