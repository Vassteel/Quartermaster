using Quartermaster;
var home=new Container();var parcel=new ZDO();parcel.Set("QM.mail.originName","North base");parcel.Set("QM.mail.playerName","Vassteel");
int checks=0;void Check(bool ok){checks++;if(!ok)throw new Exception("Check "+checks);}
DeliveryHistory.Record(home,parcel,3);DeliveryHistory.Record(home,parcel,2);
var history=DeliveryHistory.Read(home).ToList();Check(history.Count==2&&history.Sum(e=>e.Count)==5);Check(history[0].Origin=="North base"&&history[0].Player=="Vassteel");
DeliveryHistory.Record(home,parcel,0);Check(DeliveryHistory.Read(home).Count()==2);
SharedOperations.Allow=false;DeliveryHistory.Record(home,parcel,9);Check(DeliveryHistory.Read(home).Count()==2);SharedOperations.Allow=true;
for(int i=0;i<120;i++)DeliveryHistory.Record(home,parcel,i+1);Check(DeliveryHistory.Read(home).Count()==100);Check(DeliveryHistory.Read(home).First().Count==120);
var reload=new Container();reload.View.Data=home.View.Data;Check(DeliveryHistory.Read(reload).Count()==100);
Check(DeliveryHistory.Decode("bad json").Count==0);Check(DeliveryHistory.Decode("[null,{\"Count\":1,\"UtcTicks\":-1}]").Count==0);
Console.WriteLine($"PASS: {checks} delivery history persistence, partial receipt, permission and retention checks.");
class ZDO {readonly Dictionary<string,string> data=new();public void Set(string k,string v)=>data[k]=v;public string GetString(string k,string d="")=>data.GetValueOrDefault(k,d);}
class Container {public View View=new();}
class View {public ZDO Data=new();public bool IsValid()=>true;public ZDO GetZDO()=>Data;public static implicit operator bool(View v)=>v!=null;}
namespace Quartermaster {static class ContainerRegistry {public static View GetView(Container c)=>c.View;}static class SharedOperations {public static bool Allow=true;public static bool CanWrite(View v)=>Allow;}}
