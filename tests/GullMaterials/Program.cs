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
Check(feathers.shader==piece,"uses the world-lit game shader");
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
Check(!feathers.keywords.Contains("UNRELATED_CREATURE_VARIANT"),"does not inherit creature shader keywords");
var helmet=GullMaterials.Create(new Color(.3f,.3f,.3f),.65f); NonEmissive(helmet);
Check((float)helmet.values["_Metallic"]==.65f,"helmet keeps metal coloring");
piece.isSupported=false;
Check(GullMaterials.Feathers(original).shader==standard,"unsupported native shader falls back to lit Standard");
standard.isSupported=false;
Shader.Available.Add("Sprites/Default",new Shader {name="Sprites/Default"});
bool refused=false; try { GullMaterials.Create(Color.white); } catch(InvalidOperationException) { refused=true; }
Check(refused,"never falls back to an unlit sprite shader");
Console.WriteLine($"{checks} gull material assertions passed (GPU appearance requires in-game testing).");
