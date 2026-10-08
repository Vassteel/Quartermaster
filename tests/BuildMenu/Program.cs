using System.Reflection;
using Quartermaster;
int checks=0;
void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
object Invoke(Type type,string method,object[] args)=>type.GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,args);
var flags=new[]{Piece.UsageTagFlags.Building,Piece.UsageTagFlags.Furniture,(Piece.UsageTagFlags)0};
var names=new[]{"Building","Furniture","$helmsman_build_category"};
object[] ctor={flags,names};Invoke(typeof(AddQuartermasterUsageCategory),"Postfix",ctor);
var updatedFlags=(Piece.UsageTagFlags[])ctor[0];var updatedNames=(string[])ctor[1];int id=updatedNames.Length-1;
Check(updatedNames.SequenceEqual(names.Concat(new[]{BuildMenuCategory.Token})),"preserve existing category order, including Helmsman");
Check(updatedFlags.Take(flags.Length).SequenceEqual(flags)&&updatedFlags[id]==0,"do not allocate or change usage flags");
Invoke(typeof(AddQuartermasterUsageCategory),"Postfix",ctor);Check(((string[])ctor[1]).Length==updatedNames.Length,"category cannot duplicate");
object[] label={BuildMenuCategory.Token};Invoke(typeof(QuartermasterUsageLabel),"Postfix",label);Check((string)label[0]=="Quartermaster","readable category label");
object[] otherLabel={"Helmsman"};Invoke(typeof(QuartermasterUsageLabel),"Postfix",otherLabel);Check((string)otherLabel[0]=="Helmsman","other labels unchanged");
var chest=new Piece{gameObject=ChestDefaults.DepositPrefab+"(Clone)"};var ledger=new Piece{gameObject=OwlLedger.PrefabName};var other=new Piece{gameObject="wood_floor"};var repair=new Piece{gameObject="repair",m_repairPiece=true};var remove=new Piece{gameObject="remove",m_removePiece=true};
var table=new PieceTable();table.m_availablePieces.UnionWith(new[]{chest,ledger,other,repair,remove});
var tags=new List<int>{0,1,2,id};Invoke(typeof(HideUnrelatedQuartermasterCategory),"Postfix",new object[]{table,updatedNames,tags});Check(tags.Contains(id),"show category when recipes available");
var results=new List<Piece>();var runNative=(bool)Invoke(typeof(FilterQuartermasterUsageCategory),"Prefix",new object[]{id,table,results,updatedNames});
Check(!runNative&&results.ToHashSet().SetEquals(new[]{ledger,repair,remove}),"ledger and native tool actions; redundant chest excluded");
foreach(int otherId in new[]{-1,0,2,99}) {results.Clear();runNative=(bool)Invoke(typeof(FilterQuartermasterUsageCategory),"Prefix",new object[]{otherId,table,results,updatedNames});Check(runNative&&results.Count==0,"leave native/other mod filters alone");}
table.m_availablePieces.Remove(chest);table.m_availablePieces.Remove(ledger);Invoke(typeof(HideUnrelatedQuartermasterCategory),"Postfix",new object[]{table,updatedNames,tags});Check(tags.SequenceEqual(new[]{0,1,2}),"hide from unrelated tools without changing other categories");
Check(!BuildMenuCategory.Contains(null)&&!BuildMenuCategory.Contains(other)&&!BuildMenuCategory.Contains(chest),"reject unrelated pieces and retired chest recipe");
BuildMenuCategory.Register("Quartermaster_ClayCabinet");BuildMenuCategory.Register("Quartermaster_PotteryKiln");
var cabinet=new Piece{gameObject="Quartermaster_ClayCabinet(Clone)"};var kiln=new Piece{gameObject="Quartermaster_PotteryKiln"};
table.m_availablePieces.UnionWith(new[]{cabinet,kiln});results.Clear();
Invoke(typeof(FilterQuartermasterUsageCategory),"Prefix",new object[]{id,table,results,updatedNames});
Check(results.Contains(cabinet)&&results.Contains(kiln)&&!results.Contains(other),"new registered furniture and kiln use the current vanilla usage filter");
Console.WriteLine($"Build menu: {checks} checks passed.");
