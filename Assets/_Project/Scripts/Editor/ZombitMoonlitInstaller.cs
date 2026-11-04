#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class ZombitMoonlitInstaller
{
 const string Art="Assets/_Project/Resources/Art/MoonlitTitle/";
 [MenuItem("ZomBit/Interface/Instalar menu Moonlit")]
 static void Install()
 {
 if(Application.isPlaying){EditorUtility.DisplayDialog("Menu", "Pare o Play primeiro.","OK");return;}
 string file="Assets/_Project/Scripts/UI/MainMenuController.PcTitle.cs";
 if(!File.Exists(file)){Debug.LogError("Arquivo esperado ausente: "+file);return;}
 string code=File.ReadAllText(file);
 if(!code.Contains("Art/PCTitleUI/") && !code.Contains("Art/MoonlitTitle/")){Debug.LogError("Versao de PcTitle diferente. Nada alterado.");return;}
 Directory.CreateDirectory("Assets/_Project/MenuMoonlitBackup");
 string backup="Assets/_Project/MenuMoonlitBackup/MainMenuController.PcTitle.cs.txt";
 if(!File.Exists(backup)) File.Copy(file,backup);
 code=code.Replace("Art/PCTitleUI/","Art/MoonlitTitle/");
 code=code.Replace("39,278,305,68","126,289,371,77");
 code=code.Replace("39,354,305,70","126,385,371,69");
 code=code.Replace("39,431,305,70","126,464,371,69");
 code=code.Replace("39,507,305,71","126,543,371,69");
 code=code.Replace("39,584,305,70","126,622,371,69");
 code=code.Replace("39,660,305,72","126,702,371,69");

 foreach(var path in Directory.GetFiles(Art,"*.png"))
 {
 var importer=AssetImporter.GetAtPath(path) as TextureImporter;
 if(importer==null)continue;
 importer.textureType=TextureImporterType.Sprite;
 importer.spriteImportMode=SpriteImportMode.Single;
 importer.mipmapEnabled=false; importer.maxTextureSize=2048;
 importer.textureCompression=TextureImporterCompression.Uncompressed;
 importer.filterMode=FilterMode.Bilinear; importer.SaveAndReimport();
 }
 var host=new GameObject("Screen_main",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
 try{
 host.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
 var scaler=host.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
 scaler.referenceResolution=new Vector2(1672,941); scaler.matchWidthOrHeight=0.5f;
 var stage=Rect(host.transform,"PcTitleStage",0,0,1672,941);
 var fit=stage.gameObject.AddComponent<PcTitleFit>(); fit.viewport=(RectTransform)host.transform;fit.stage=stage;
 Picture(stage,"PcTitleArtwork","title_scene",0,0,1672,941);
 MakeButton(stage,"BtnNewGame","new_game",126,289,371,77);
 MakeButton(stage,"BtnContinue","continue",126,385,371,69);
 MakeButton(stage,"BtnSlots","load",126,464,371,69);
 MakeButton(stage,"BtnOptions","options",126,543,371,69);
 MakeButton(stage,"BtnCredits","credits",126,622,371,69);
 MakeButton(stage,"BtnQuit","quit",126,702,371,69);

 var nick=Rect(stage,"MainNicknameButton",0,0,0,0);nick.gameObject.AddComponent<Button>();nick.gameObject.SetActive(false);
 const string prefab="Assets/_Project/Resources/UI/Screens/Screen_main.prefab";
 if(File.Exists(prefab) && !File.Exists("Assets/_Project/MenuMoonlitBackup/Screen_main.prefab.txt")) File.Copy(prefab,"Assets/_Project/MenuMoonlitBackup/Screen_main.prefab.txt");
 Directory.CreateDirectory(Path.GetDirectoryName(prefab));
 PrefabUtility.SaveAsPrefabAsset(host,prefab);
 File.WriteAllText(file,code);
 }finally{Object.DestroyImmediate(host);}
 AssetDatabase.Refresh();
 Debug.Log("[Moonlit] Menu instalado. Aguarde recompilar e abra MainMenu. Backup em Assets/_Project/MenuMoonlitBackup.");
 }
 static RectTransform Rect(Transform p,string n,float x,float y,float w,float h){
 var r=new GameObject(n,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);
 r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
 }
 static Image Picture(Transform p,string n,string s,float x,float y,float w,float h){
 var r=Rect(p,n,x,y,w,h);var i=r.gameObject.AddComponent<Image>();i.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+s+".png");i.raycastTarget=false;return i;
 }
 static void MakeButton(Transform p,string n,string s,float x,float y,float w,float h){
 var i=Picture(p,n,s,x,y,w,h);i.raycastTarget=true;var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;
 var c=b.colors;c.normalColor=Color.white;c.disabledColor=Color.white;c.highlightedColor=new Color(1,0.95f,0.85f,1);c.pressedColor=new Color(0.8f,0.8f,0.8f,1);b.colors=c;
 var nav=b.navigation;nav.mode=Navigation.Mode.None;b.navigation=nav;
 }
}
#endif
