using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]public class RegionalRecord{public string id;public int seals,wins;public bool blueprint,title,frame;}
[Serializable]public class RegionalRewardState
{
    public int version=1;public List<RegionalRecord> records=new List<RegionalRecord>();
    public List<ItemInstance> pending=new List<ItemInstance>();public string selectedTitle="",selectedFrame="";
}
[CreateAssetMenu(menuName="ZomBit/Rewards/Regional Rewards",fileName="RegionalRewardConfig")]
public sealed class RegionalRewardConfig : ScriptableObject
{
    [Serializable]public class Region
    {
        public string id,name,itemName,description,title,frame;
        public ItemSlot slot;public int sealsCost=12,goldCost,scrapCost;public float damage,health,defense;public Sprite icon;
        public Region(string i,string n,string item,ItemSlot s,string effect,string t,string f,int rank)
        {id=i;name=n;itemName=item;slot=s;description=effect;title=t;frame=f;goldCost=rank*100;scrapCost=rank*60;damage=rank*.5f;health=20+rank*12;defense=rank*.5f;}
    }
    public float easyDropChance=.02f,normalDropChance=.04f,hardDropChance=.06f,nightmareDropChance=.08f;
    public float bootsDelay=8,bootsDuration=5,bootsHealPerSecond=.01f,frontReduction=.08f,shoppingScrapBonus=.1f;
    public float emergencyThreshold=.3f,emergencyShield=.12f,emergencyDuration=5,emergencyCooldown=30;
    public int interventionHits=3;
    public float interventionReduction=.2f,swapFireRate=.1f,swapDuration=5,swapCooldown=15,eliteShotBonus=.1f,reloadReduction=.1f,reloadProtection=3;
    public Region[] regions={
        new Region("suburbio","Subúrbio","Botas do Sobrevivente",ItemSlot.Botas,"Após 8s sem dano, recupera 1% da vida/s por até 5s.","Sobrevivente do Subúrbio","Última Luz",1),
        new Region("residencial","Bairro Residencial","Colete Improvisado",ItemSlot.Colete,"Reduz 8% do dano direto quando está na Frente.","Protetor do Bairro","Portas Fechadas",2),
        new Region("shopping","Shopping","Mochila do Saqueador",ItemSlot.Mochila,"+10% sucata na vitória, uma vez por equipe.","Caçador de Suprimentos","Liquidação Final",3),
        new Region("hospital","Hospital","Colete de Emergência",ItemSlot.Colete,"Dano não letal abaixo de 30% vida: escudo12% por5s, recarga30s.","Alta Improvável","Código Vermelho",4),
        new Region("delegacia","Delegacia","Capacete de Intervenção",ItemSlot.Capacete,"Após três ataques diretos, reduz o próximo em20%.","Última Patrulha","Operação Encerrada",5),
        new Region("metro","Estação de Metrô","Calças do Batedor",ItemSlot.Calca,"Trocar posição: +10% cadência por5s, recarga15s.","Passageiro da Última Linha","Sem Retorno",6),
        new Region("centro","Centro da Cidade","Capacete do Caçador",ItemSlot.Capacete,"Tiros próprios: +10% dano contra elites/chefes.","Reconquistador","Coração da Cidade",7),
        new Region("base","Base Militar","Colete de Operações",ItemSlot.Colete,"Após recarregar: -10% dano direto por3s.","Operador de Elite","Protocolo Alfa",8),
        new Region("industrial","Docas","Mochila do Contrabandista",ItemSlot.Mochila,"Uma rolagem extra de conclusão se ninguém cair.","Dono do Porto","Fim da Travessia",9)
    };
}
