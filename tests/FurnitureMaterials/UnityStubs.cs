// Material-state doubles only; these do not simulate Unity lighting or GPU shaders.
namespace UnityEngine;
public class Object { public static void Destroy(Object value){} public string name; public static implicit operator bool(Object value) => value != null; }
public record struct Color(float r, float g, float b, float a = 1) { public static Color black => new(0,0,0); public static Color white => new(1,1,1); }
public record struct Vector2(float x, float y);
public class Texture : Object { }
public enum MaterialGlobalIlluminationFlags { None, EmissiveIsBlack }
public class Shader : Object
{
    public bool isSupported = true;
    public static readonly Dictionary<string, Shader> Available = new();
    public static Shader Find(string name) => Available.GetValueOrDefault(name);
}
public class Material : Object
{
    public Shader shader; public int renderQueue=2000; public bool enableInstancing; public Texture mainTexture;
    public Color color { get => GetColor("_Color"); set => SetColor("_Color",value); }
    public MaterialGlobalIlluminationFlags globalIlluminationFlags;
    public readonly Dictionary<string, object> values = new();
    public readonly HashSet<string> keywords = new();
    public Material(Shader shader) { this.shader=shader; }
    public Material(Material source) {shader=source.shader;renderQueue=source.renderQueue;mainTexture=source.mainTexture;enableInstancing=source.enableInstancing; foreach(var pair in source.values)values[pair.Key]=pair.Value;foreach(var keyword in source.keywords)keywords.Add(keyword);}
    public bool HasProperty(string property) => true;
    public Color GetColor(string name) => values.TryGetValue(name,out var value) ? (Color)value : Color.white;
    public void SetColor(string name, Color value) => values[name]=value;
    public void SetFloat(string name, float value) => values[name]=value;
    public Texture GetTexture(string name) => values.GetValueOrDefault(name) as Texture;
    public void SetTexture(string name, Texture value) => values[name]=value;
    public Vector2 GetTextureScale(string name) => values.TryGetValue(name+" scale",out var value) ? (Vector2)value : new(1,1);
    public Vector2 GetTextureOffset(string name) => values.TryGetValue(name+" offset",out var value) ? (Vector2)value : new(0,0);
    public void SetTextureScale(string name, Vector2 value) => values[name+" scale"]=value;
    public void SetTextureOffset(string name, Vector2 value) => values[name+" offset"]=value;
    public void DisableKeyword(string name) => keywords.Remove(name);
    public void EnableKeyword(string name) => keywords.Add(name);
}

public class GameObject:Object {
 public Renderer[] Renderers=Array.Empty<Renderer>();
 public T[] GetComponentsInChildren<T>(bool includeInactive)=>Renderers.OfType<T>().ToArray();
}
public class Renderer:Object {public Material[] sharedMaterials=Array.Empty<Material>();}
public class MeshRenderer:Renderer {}
public record struct Vector3(float x,float y,float z);
public record struct Rect(float x,float y,float width,float height);
public enum TextureFormat {RGBA32}
public enum FilterMode {Point,Bilinear}
public enum TextureWrapMode {Clamp}
public class Texture2D:Texture {
 public int width=2,height=2;public FilterMode filterMode;public TextureWrapMode wrapMode;
 public Texture2D(int w,int h,TextureFormat format,bool mipmaps){}
}
public class Sprite:Object {public static Sprite Create(Texture2D t,Rect r,Vector2 p)=>new();}
public static class ImageConversion {public static bool FailNext;public static bool LoadImage(Texture2D t,byte[] b){if(FailNext){FailNext=false;return false;}return b.Length>8;}}
public class Mesh:Object {public Vector3[] vertices;public Vector2[] uv;public int[] triangles;public void RecalculateNormals(){}public void RecalculateBounds(){}}
