using Quartermaster;
using BepInEx.Configuration;
using System.Reflection;
int checks=0;void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}
ConfigFile Config(){var f=new ConfigFile();f.Add("Inventory","EnableStackSizes",true);f.Add("Inventory","MaximumStackSize",1000);return f;}
void Tick(ServerSettings s)=>typeof(ServerSettings).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(s,null);
ZPackage Change(bool on,int n){var p=new ZPackage();p.Write(on);p.Write(n);return p;}
ZPackage Settings(bool on,int n){var p=new ZPackage();p.Write(2);p.Write("Inventory");p.Write("EnableStackSizes");p.Write(on.ToString());p.Write("Inventory");p.Write("MaximumStackSize");p.Write(n.ToString());return p;}
var cfg=Config();var settings=new ServerSettings();settings.Initialize(cfg);
ZNet.instance=new(){Server=true,Peer=new(){m_uid=42,m_socket=new(){Name="peer-account"}}};
ZRoutedRpc.instance=new();Tick(settings);
Check(settings.SetStacks(false,1000).Contains("applied")&&!ItemStacks.Active&&cfg.Saves==1,"host applies normal limits and saves config immediately");
Check(settings.SetStacks(true,250).Contains("applied")&&ItemStacks.Active&&ItemStacks.Limit==250,"host applies custom maximum immediately");
int saves=cfg.Saves;settings.SetStacks(true,1);settings.SetStacks(true,100001);
Check(cfg.Saves==saves&&settings.StackMaximum==250,"invalid host inputs leave settings unchanged");
var rpc=ZRoutedRpc.instance;rpc.Receive("QuartermasterStackChange",42L,Change(true,750));
Check(settings.StackMaximum==250&&cfg.Saves==saves&&(int)rpc.Sent.Last().args[0]==0,"non-admin cannot change server settings");
ZNet.instance.Admins.Add("peer-account");rpc.Receive("QuartermasterStackChange",42L,Change(true,750));
Check(settings.StackMaximum==750&&cfg.Saves==saves+1,"authenticated admin changes persisted server config");
Check(rpc.Sent[^2].name=="QuartermasterSettingsReply"&&(int)rpc.Sent.Last().args[0]==1,"server sync precedes successful acknowledgment");
saves=cfg.Saves;rpc.Receive("QuartermasterStackChange",99L,Change(true,900));
Check(cfg.Saves==saves,"unknown peer ignored");
rpc.Receive("QuartermasterStackChange",42L,Change(true,-10));rpc.Receive("QuartermasterStackChange",42L,new ZPackage());
Check(cfg.Saves==saves&&settings.StackMaximum==750,"malformed and out of range requests do not mutate config");
cfg.FailSave=true;settings.SetStacks(false,20);
Check(settings.StackMaximum==750&&settings.CustomStacks&&ItemStacks.Limit==750&&cfg.SaveOnConfigSet,"failed host save restores previous live settings and persistence flag");
cfg.FailSave=false;
settings.Shutdown();
cfg=Config();settings=new();settings.Initialize(cfg);
ZNet.instance=new(){Server=false,Admin=false,Peer=new(){m_uid=77,m_socket=new(){Name="server"}}};
rpc=ZRoutedRpc.instance=new();Tick(settings);
settings.SetStacks(true,600);Check(!rpc.Sent.Any(s=>s.name=="QuartermasterStackChange"),"non-admin client cannot submit edits");
ZNet.instance.Admin=true;settings.SetStacks(true,600);
Check(rpc.Sent.Last().name=="QuartermasterStackChange"&&cfg.Saves==0&&settings.StackMaximum==1000,"admin client requests rather than saving locally");
rpc.Receive("QuartermasterSettingsReply",99L,Settings(false,200));
Check(settings.StackMaximum==1000,"forged settings sender rejected");
rpc.Receive("QuartermasterSettingsReply",77L,Settings(true,600));
Check(settings.StackMaximum==600&&ItemStacks.Limit==600&&cfg.Saves==0&&cfg.SaveOnConfigSet,"server sync applies live without overwriting client file");
rpc.Receive("QuartermasterStackResult",99L,1);
Check(settings.StackStatus.Contains("Requesting"),"forged success acknowledgment ignored");
rpc.Receive("QuartermasterStackResult",77L,1);
Check(settings.StackStatus.Contains("applied"),"server acknowledgment reaches book status");
rpc.Receive("QuartermasterSettingsReply",77L,Settings(false,600));
Check(!ItemStacks.Active,"server normal-limit switch applies live on client");
settings.Shutdown();
Check(settings.StackMaximum==1000&&settings.CustomStacks&&cfg.Saves==0,"disconnect restores client preferences without saving server values");
Console.WriteLine($"PASS: {checks} live stack settings, persistence and authenticated RPC regressions.");
