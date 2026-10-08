using Quartermaster;
using System.Reflection;
int checks=0;void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
OwlLedger Book(string id){var l=new OwlLedger();l.View.Data.m_uid=id;typeof(OwlLedger).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(l,null);return l;}
ContainerRegistry.All.Add(new Container());var n=new BaseNetwork();var first=Book("9");
Check(OwlLedger.Cap(n,"Coal",200)==200,"unconfigured ledger preserves existing machine cap");
Check(first.SaveCap("Coal",50).StartsWith("Saved")&&OwlLedger.Cap(n,"Coal",200)==50,"ledger overrides base production cap");
first.SaveCap("Coal",0);Check(OwlLedger.Cap(n,"Coal",200)==0,"zero persists as stop-new-production cap");
first.SaveCap("Coal",-1);first.SaveCap("Coal",100001);Check(OwlLedger.Cap(n,"Coal",200)==0,"invalid cap values cannot overwrite valid settings");
var second=Book("10");Check(OwlLedger.Cap(n,"Coal",200)==0,"adding an empty book cannot hide existing limits even with lower lexical ID");
second.SaveCap("Coal",75);Check(OwlLedger.Cap(n,"Coal",200)==75&&second.Settings.Caps.Single().Amount==75,"edits persist on the book being used and become the current base limits");
PrivateArea.Access=false;second.SaveCap("Coal",5);Check(OwlLedger.Cap(n,"Coal",200)==75,"ward denial blocks writes");PrivateArea.Access=true;
second.View.Owner=false;second.View.CanClaim=false;second.SaveCap("Coal",5);Check(OwlLedger.Cap(n,"Coal",200)==75,"failed ownership does not mutate cached caps");second.View.CanClaim=true;
second.SaveCap("Coal",25);Check(second.View.Owner&&OwlLedger.Cap(n,"Coal",200)==25,"book claims only its own settings ZDO and persists edit");
Check(OwlLedger.Cap(new(){Group="Harbor"},"Coal",300)==300,"other base groups retain their own limits");
var older=Book("0");older.View.Data.Set("Quartermaster.ledger.v1",first.View.Data.GetString("Quartermaster.ledger.v1",""));
Check(OwlLedger.Cap(n,"Coal",200)==25,"loading an older lower-ID book cannot replace the newest saved limits");older.Destroyed=true;

second.View.Data.IgnoreWrites=true;
Check(!second.SaveCap("Coal",51).StartsWith("Saved")&&OwlLedger.Cap(n,"Coal",200)==25,"a dropped ZDO write cannot report success");
second.View.Data.IgnoreWrites=false;

second.View.Data.Set("Quartermaster.ledger.v1","{\"Group\":\"Home\",\"UpdatedTicks\":3155378975999999998,\"Caps\":[{\"Item\":\"Coal\",\"Amount\":33}]}");
Check(OwlLedger.Cap(n,"Coal",200)==33,"replicated ZDO change invalidates cached settings");
first.Destroyed=true;second.View.Data.Set("Quartermaster.ledger.v1","broken json");Check(OwlLedger.Cap(n,"Coal",200)==200,"malformed ledger data falls back safely");
first.Destroyed=false;Check(!first.Interact(Player.m_localPlayer,true,false)&&ChestUi.Opens==0,"hold interaction does not repeatedly open ledger");
Check(first.Interact(Player.m_localPlayer,false,false)&&ChestUi.Opens==1,"normal local interaction opens ledger");
first.Destroyed=false;first.View.Data.Set("Quartermaster.ledger.v1","{\"Group\":\"Home\",\"Caps\":[]}");
Check(first.SaveCap("Coal",500).StartsWith("Saved"),"new cap receives verified confirmation");
Check(first.SaveCap("Iron",120).StartsWith("Saved"),"saving another product preserves the first cap");
var persisted=first.View.Data.GetString("Quartermaster.ledger.v1","");first.Destroyed=true;second.Destroyed=true;
var reopened=Book("9");reopened.View.Data.Set("Quartermaster.ledger.v1",persisted);
Check(OwlLedger.Cap(n,"Coal",200)==500&&OwlLedger.Cap(n,"Iron",200)==120,"freshly loaded ledger reads both saved values without the original object or cache");
reopened.SaveGroup("Home");Check(OwlLedger.Cap(n,"Coal",200)==500,"saving the base group cannot discard production limits");
var culture=System.Globalization.CultureInfo.CurrentCulture;
try
{
    foreach(var name in new[]{"en-US","de-DE","fr-FR"})
    {
        System.Globalization.CultureInfo.CurrentCulture=new System.Globalization.CultureInfo(name);
        var legacy=LedgerCodec.Read("{\"Group\":\"Home\",\"UpdatedTicks\":639000000000000001,\"Caps\":[{\"Item\":\"Bronze\",\"Amount\":20},{\"Item\":\"Coal\",\"Amount\":0}]}");
        Check(legacy.UpdatedTicks==639000000000000001&&legacy.Caps[0].Amount==20,"legacy JSON reads exact timestamp and cap in "+name);
        Check(LedgerCodec.Equal(legacy,LedgerCodec.Read(LedgerCodec.Write(legacy))),"real game JSON library round trip in "+name);
        Check(reopened.SaveCap("Bronze",20).StartsWith("Saved")&&OwlLedger.Cap(n,"Bronze",200)==20,"reported Bronze save in "+name);
    }
}
finally{System.Globalization.CultureInfo.CurrentCulture=culture;}
var copy=LedgerCodec.Copy(reopened.Settings);copy.Caps[0].Amount=999;
Check(reopened.Settings.Caps[0].Amount!=999,"settings copy cannot change original caps before persistence");
Check(LedgerCodec.Read("{\"Group\":null,\"Caps\":[null]}").Caps.Count==0,"legacy null entries normalize safely");
Console.WriteLine($"PASS: {checks} ledger persistence, group, permission and shared-cap regressions.");
