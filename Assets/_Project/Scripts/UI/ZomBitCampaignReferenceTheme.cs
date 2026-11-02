using UnityEngine;

[CreateAssetMenu(menuName="ZomBit/Campaign Reference Theme",fileName="CampaignReferenceTheme")]
public sealed class ZomBitCampaignReferenceTheme : ScriptableObject
{
    public bool enabledTheme=true;
    [Header("Substitua as artes aqui; campos vazios usam as artes atuais do jogo")]
    public Sprite normalFrame,goldFrame,selectedFrame,mapIcon,rulesIcon,formationIcon,startIcon,coinIcon,gearIcon;
    public Sprite mapBackground;
    public Color textColor=Color.white;
    public Color accent=new Color(1f,.73f,.16f);
    [Header("Retangulos x / y a partir do topo / largura / altura — quadro 1920x1080")]
    public Vector4 mapRect=new Vector4(0,104,1290,976);
    public Vector4 detailsRect=new Vector4(1298,104,602,950);
    public Vector4 rulesRect=new Vector4(24,982,330,76);
    public Vector4 formationRect=new Vector4(372,982,280,76);
    [Header("Nos do mapa (coordenadas relativas ao mapRect)")]
    public Entry[] locations=new Entry[]{
        new Entry("suburbio",74,102,320,116),new Entry("residencial",422,194,280,108),
        new Entry("shopping",490,405,294,110),new Entry("hospital",90,488,286,100),
        new Entry("delegacia",454,644,318,108),new Entry("metro",858,250,282,108),
        new Entry("centro",938,470,316,108),new Entry("base",906,74,308,108),
        new Entry("industrial",920,686,320,108)};
    [System.Serializable]public sealed class Entry
    {
        public string id;public Vector4 rect;
        public Entry(string key,float x,float y,float w,float h){id=key;rect=new Vector4(x,y,w,h);}
    }
}
