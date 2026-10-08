using System.Reflection;
using Quartermaster;
using UnityEngine;
using Jotunn.Managers;
class Program {
 static int checks;
 static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static void Main(){
  var nativeShader=new Shader{name="Custom/Piece"};
  var native=new Material(nativeShader){name="wood",color=new(.2f,.3f,.4f),mainTexture=new Texture()};native.EnableKeyword("NOISEGLOW");
  var chest=new GameObject{name="piece_chest_wood",Renderers=new Renderer[]{new MeshRenderer{sharedMaterials=new[]{native}}}};
  PrefabManager.Instance.Prefabs["piece_chest_wood"]=chest;
  var flint=new GameObject{name="Flint",Renderers=new Renderer[]{new MeshRenderer{sharedMaterials=new[]{new Material(new Shader{name="Lit/Item"})}}}};
  Check(Shader.Find("Standard")==null,"Fixture reproduces stripped Standard shader");
  var atlas=new Texture();var clay=StorageMaterials.Opaque(flint,"clay",atlas);
  Check(clay.shader==nativeShader&&clay.mainTexture==atlas,"Clay falls back from Flint to known native building shader");
  Check(native.keywords.Contains("NOISEGLOW")&&!clay.keywords.Contains("NOISEGLOW")&&native.mainTexture!=atlas,"Native material remains untouched");
  var glassShader=new Shader{name="Custom/Crystal"};var crystal=new Material(glassShader){name="crystal glass",renderQueue=3000,color=new(.6f,.7f,.8f,.3f),mainTexture=new Texture()};crystal.EnableKeyword("NATIVE_TRANSPARENCY");crystal.SetFloat("NativeRefraction",.15f);
  PrefabManager.Instance.Prefabs["crystal_wall_1x1"]=new GameObject{Renderers=new Renderer[]{new MeshRenderer{sharedMaterials=new[]{native,crystal}}}};
  var glass=StorageMaterials.Crystal(clay);
  Check(glass.shader==glassShader&&glass.renderQueue==3000&&glass.keywords.Contains("NATIVE_TRANSPARENCY")&&glass.mainTexture==crystal.mainTexture,"Copy native crystal transparency and textures without Standard");
  glass.color=Color.white;Check(crystal.color!=Color.white,"Flask material does not mutate native crystal");
  ApothecaryArt.Initialize(chest);
  var fields=typeof(ApothecaryArt).GetFields(BindingFlags.Static|BindingFlags.NonPublic);
  var models=(System.Collections.IDictionary)fields.Single(f=>f.Name=="models").GetValue(null);
  Check(models.Count==131,"Every authored model loads with Standard unavailable");
  var count=models.Count;ApothecaryArt.Initialize(chest);Check(models.Count==count,"Repeated initialization is idempotent");
  var metal=(Material)fields.Single(f=>f.Name=="metal").GetValue(null);
  Check(metal.shader==nativeShader,"Metal also uses a shader available in the game");
  ApothecaryArt.Release();Check(models.Count==0,"Teardown clears complete resource state");
  PrefabManager.Instance.Prefabs.Remove("crystal_wall_1x1");ApothecaryArt.Initialize(chest);
  Check(models.Count==131&&Plugin.Log.Warnings==1,"Missing optional glass material cannot block the furniture catalog");
  var fallback=(Material)fields.Single(f=>f.Name=="glass").GetValue(null);
  Check(fallback.shader==nativeShader&&fallback.color.a==1,"Glass fallback remains renderable and opaque");
  ApothecaryArt.Release();ImageConversion.FailNext=true;
  try{ApothecaryArt.Initialize(chest);throw new Exception("Expected resource failure");}catch(System.IO.InvalidDataException){}
  Check(models.Count==0&&fields.Single(f=>f.Name=="wood").GetValue(null)==null,"Failed initialization leaves no false-ready material cache");
  ApothecaryArt.Initialize(chest);Check(models.Count==131,"Retry after resource failure loads all models");
  ApothecaryArt.Release();
  Console.WriteLine($"PASS: {checks} stripped-shader, native-material and art initialization regressions.");
 }
}
namespace Jotunn.Managers {public class PrefabManager {public static PrefabManager Instance=new();public Dictionary<string,GameObject> Prefabs=new();public GameObject GetPrefab(string name)=>Prefabs.GetValueOrDefault(name);}}
namespace Quartermaster {internal class Plugin {internal static Logger Log=new();internal class Logger {internal int Warnings;internal void LogWarning(string message){Warnings++;}}}}
