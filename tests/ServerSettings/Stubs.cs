namespace UnityEngine {
 public class Object {public static implicit operator bool(Object o)=>o!=null;}
 public class MonoBehaviour:Object {public bool enabled=true;}
 public static class Time {public static float unscaledTime;}
}
namespace BepInEx.Configuration {
 public record ConfigDefinition(string Section,string Key);
 public class ConfigEntryBase {
  public ConfigDefinition Definition;public object BoxedValue;
  public string GetSerializedValue()=>BoxedValue.ToString();
  public void SetSerializedValue(string v)=>BoxedValue=BoxedValue is bool?bool.Parse(v):int.Parse(v);
 }
 public class ConfigFile:Dictionary<ConfigDefinition,ConfigEntryBase> {
  public bool SaveOnConfigSet=true,FailSave;public int Saves;
  public Dictionary<string,string> Disk=new();
  public ConfigEntryBase this[string section,string key]=>this[new(section,key)];
  public void Add(string section,string key,object value){var d=new ConfigDefinition(section,key);Add(d,new(){Definition=d,BoxedValue=value});}
  public void Save(){if(FailSave)throw new IOException();Saves++;Disk=this.ToDictionary(p=>p.Key.Key,p=>p.Value.GetSerializedValue());}
 }
}
public class ZPackage {
 readonly Queue<object> q=new();public void Write(bool v)=>q.Enqueue(v);public void Write(int v)=>q.Enqueue(v);public void Write(string v)=>q.Enqueue(v);
 public bool ReadBool()=>(bool)q.Dequeue();public int ReadInt()=>(int)q.Dequeue();public string ReadString()=>(string)q.Dequeue();
}
public class Socket {public string Name;public string GetHostName()=>Name;}
public class ZNetPeer {public long m_uid;public Socket m_socket;public bool Ready=true;public bool IsReady()=>Ready;}
public class ZNet:UnityEngine.Object {
 public static ZNet instance;public bool Server,Admin;public HashSet<string> Admins=new();public ZNetPeer Peer;
 public bool IsServer()=>Server;public bool LocalPlayerIsAdminOrHost()=>Server||Admin;
 public bool IsAdmin(string id)=>Admins.Contains(id);public ZNetPeer GetServerPeer()=>Peer;
 public ZNetPeer GetPeer(long id)=>Peer?.m_uid==id?Peer:null;
}
public class ZRoutedRpc {
 public static ZRoutedRpc instance;public Dictionary<string,Delegate> Handlers=new();
 public List<(long peer,string name,object[] args)> Sent=new();
 public void Register(string name,Action<long> callback)=>Handlers[name]=callback;
 public void Register<T>(string name,Action<long,T> callback)=>Handlers[name]=callback;
 public void InvokeRoutedRPC(long id,string name,params object[] args)=>Sent.Add((id,name,args));
 public void Receive(string name,long id,params object[] args)=>Handlers[name].DynamicInvoke(new object[]{id}.Concat(args).ToArray());
}
public class ObjectDB:UnityEngine.Object {public static ObjectDB instance=new();}
namespace Quartermaster {
 internal static class Plugin {
  internal static Setting Enabled=new();internal class Setting {public bool Value=true;}
  internal static Logger Log=new();internal class Logger {public void LogWarning(string v){}}
 }
 internal static class ItemStacks {
  internal static bool Active;internal static int Limit,Applied;
  internal static void Restore()=>Active=false;
  internal static void Configure(bool on,int n){Active=on;Limit=n;}
  internal static void ApplyDatabase(ObjectDB db)=>Applied++;
 }
}
