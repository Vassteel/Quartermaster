using Quartermaster.Cosmetics;
using UnityEngine;
int checks=0;
void Check(bool ok,string message) { checks++; if(!ok) throw new Exception(message); }
var piece=new Shader {name="Custom/Piece"}; var standard=new Shader {name="Standard"};
Shader.Available.Add(piece.name,piece); Shader.Available.Add(standard.name,standard);
var original=new Material(new Shader {name="Custom/Creature"});
var skin=new Texture(); original.SetTexture("_MainTex",skin);
original.color=new Color(.9f,.8f,.7f);
original.SetTextureScale("_MainTex",new(2,3)); original.SetTextureOffset("_MainTex",new(.1f,.2f));
original.SetColor("_EmissionColor",new(4,2,1)); original.SetColor("_NoiseGlowColor",new(3,3,3));
original.EnableKeyword("NOISEGLOW"); original.EnableKeyword("_EMISSION"); original.EnableKeyword("UNRELATED_CREATURE_VARIANT");
var before=new Dictionary<string,object>(original.values);
var feathers=GullMaterials.Feathers(original);
Check(feathers.shader==original.shader,"uses the prefab material shader reference");
Check(feathers.GetTexture("_MainTex")==skin && feathers.color==original.color,"preserves vanilla skin and tint");
Check(feathers.GetTextureScale("_MainTex")==new Vector2(2,3) && feathers.GetTextureOffset("_MainTex")==new Vector2(.1f,.2f),"preserves UV mapping");
Check(before.All(p=>Equals(original.values[p.Key],p.Value)) && original.keywords.Count==3,"source asset remains unchanged");
void NonEmissive(Material material)
{
    foreach(var property in new[]{"_EmissionColor","_EmissiveColor","_NoiseGlowColor"}) Check(material.GetColor(property)==Color.black,property+" disabled");
    Check(!material.keywords.Contains("NOISEGLOW") && !material.keywords.Contains("_EMISSION"),"both glow variants disabled");
    Check(material.globalIlluminationFlags==MaterialGlobalIlluminationFlags.EmissiveIsBlack,"no GI emission");
    Check((float)material.values["_Glossiness"]==0 && (float)material.values["_AddRain"]==0,"no glossy or rain shine");
}
NonEmissive(feathers);
Check(feathers.keywords.Contains("UNRELATED_CREATURE_VARIANT"),"preserves native lighting variants while disabling glow");
var helmet=GullMaterials.Create(original,new Color(.3f,.3f,.3f),.65f); NonEmissive(helmet);
Check((float)helmet.values["_Metallic"]==.65f,"helmet keeps metal coloring");
// Reproduce the live failure: named Piece/Standard shaders unavailable or unsupported.
piece.isSupported=false;standard.isSupported=false;Shader.Available.Clear();
Check(GullMaterials.Feathers(original).shader==original.shader,"missing Shader.Find entries cannot hide the native gull");
Check(GullMaterials.Create(original,Color.white).shader==original.shader,"helmet also uses native material with no named shaders");
NonEmissive(GullMaterials.Feathers(original));
bool refused=false;try{GullMaterials.Create(null,Color.white);}catch(InvalidOperationException){refused=true;}
Check(refused,"missing native material fails explicitly rather than using an unlit shader");
Console.WriteLine($"{checks} gull material assertions passed (GPU appearance requires in-game testing).");
