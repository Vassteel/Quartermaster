// Host-only Harmony contracts. Production hook signatures and called members are
// validated separately against the game's actual Harmony and Valheim assemblies.
using System.Reflection;
using System.Reflection.Emit;
namespace HarmonyLib;
[AttributeUsage(AttributeTargets.Class|AttributeTargets.Method,AllowMultiple=true)]
public class HarmonyPatch:Attribute {public HarmonyPatch(){} public HarmonyPatch(Type t,string name){}public HarmonyPatch(Type t,string name,Type[] args){}}
public class HarmonyPrefix:Attribute {}
public class HarmonyPostfix:Attribute {}
public class HarmonyFinalizer:Attribute {}
public class HarmonyTranspiler:Attribute {}
public static class AccessTools {
 public static MethodInfo Method(Type type,string name,Type[] args=null)=>args==null
  ?type.GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance)
  :type.GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance,null,args,null);
}
public enum ExceptionBlockType {BeginExceptionBlock}
public class ExceptionBlock {public ExceptionBlockType BlockType;public ExceptionBlock(ExceptionBlockType type){BlockType=type;}}
public class CodeInstruction {
 public OpCode opcode;public object operand;public List<Label> labels=new();public List<ExceptionBlock> blocks=new();
 public CodeInstruction(OpCode code,object value=null){opcode=code;operand=value;}
 public CodeInstruction(CodeInstruction original){opcode=original.opcode;operand=original.operand;labels=new(original.labels);blocks=new(original.blocks);}
 public bool Calls(MethodInfo method)=>(opcode==OpCodes.Call||opcode==OpCodes.Callvirt)&&Equals(operand,method);
}
