using Quartermaster;
int checks=0;
void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
var c=new Container();c.Live.Count=99;c.View.Data.Bytes=new byte[]{20};
var first=StorageObservation.Read(c);
Check(first.Count==20&&c.Live.Count==99,"remote snapshot reads synchronized stock without altering live inventory");
Check(first!=c.Live&&c.View.Claims==0&&c.Saves==0,"observation has no write or ownership effects");
Check(ReferenceEquals(first,StorageObservation.Read(c)),"unchanged revision reuses snapshot");
c.View.Data.Revision++;c.View.Data.Bytes=new byte[]{7};
Check(StorageObservation.Read(c).Count==7,"remote update visible without opening chest");
c.View.Data.Revision++;c.View.Data.Bytes=null;
Check(StorageObservation.Read(c).Count==0,"empty synchronized contents do not retain stale items");
c.Live.Width=6;c.Live.Height=4;
Check(StorageObservation.Read(c).GetWidth()==6&&StorageObservation.Read(c).GetHeight()==4,"resized drawer snapshot matches dimensions");
c.View.Data=new ZDO{Bytes=new byte[]{4}};
Check(StorageObservation.Read(c).Count==4,"replacement network object invalidates cache");
c.View.Owner=true;
Check(ReferenceEquals(StorageObservation.Read(c),c.Live)&&c.Live.Count==4&&c.Loads==1,"ownership handoff refreshes native inventory before reuse");
c.View.Owner=false;c.View.Data.Bytes=new byte[]{8};
Check(StorageObservation.Read(c).Count==8,"return to remote ownership builds fresh snapshot");
c.View.Data.Revision++;c.View.Data.Bytes=new byte[]{255};
try{StorageObservation.Read(c);throw new Exception("expected corrupt packet");}catch(FormatException){}
c.View.Data.Bytes=new byte[]{6};
Check(StorageObservation.Read(c).Count==6,"failed decode does not publish a partial snapshot");
StorageObservation.Forget(c);Check(!ReferenceEquals(first,StorageObservation.Read(c)),"unregister discards snapshot");
Check(c.View.Claims==0&&c.Saves==0,"no requests or saves during complete read sequence");
var courtesy=new AutomationCourtesy<int>();
Check(!courtesy.Yield(1,false,0),"idle chest immediately available to automation");
Check(courtesy.Yield(1,true,0)&&courtesy.Yield(1,false,.4f),"active player use and brief grace delay automation");
Check(!courtesy.Yield(2,false,.4f),"one busy chest does not pause surplus chests");
Check(!courtesy.Yield(1,false,.5f),"automation resumes after bounded grace");
courtesy.Yield(1,true,1);courtesy.Clear();
Check(!courtesy.Yield(1,false,0),"world change clears scheduling hints");
Console.WriteLine($"PASS: {checks} synchronized inventory observation checks");
namespace UnityEngine {public class Object{public static implicit operator bool(Object o)=>o!=null;}}
public class ZDO{public uint Revision;public uint DataRevision=>Revision;public byte[] Bytes;public byte[] GetByteArray(int key)=>Bytes;}
public static class ZDOVars{public const int s_items=1;}
public class ZPackage{public byte[] Bytes;public ZPackage(byte[] bytes){Bytes=bytes;}}
public class Inventory{public int Count,Width=4,Height=2;public Inventory(string name,object bg,int w,int h){Width=w;Height=h;}public int GetWidth()=>Width;public int GetHeight()=>Height;public void Load(ZPackage p){if(p.Bytes[0]==255)throw new FormatException();Count=p.Bytes[0];}}
public class ZNetView:UnityEngine.Object{public bool Owner;public int Claims;public ZDO Data=new();public bool IsValid()=>true;public bool IsOwner()=>Owner;public ZDO GetZDO()=>Data;}
public class Container{public string name="chest";public Inventory Live=new("chest",null,4,2);public ZNetView View=new();public int Saves,Loads;}
namespace Quartermaster{static class ContainerRegistry{public static Inventory SafeInventory(Container c)=>c.Live;public static ZNetView GetView(Container c)=>c.View;public static void Refresh(Container c){c.Loads++;c.Live.Count=c.View.Data.Bytes?[0]??0;}}}
