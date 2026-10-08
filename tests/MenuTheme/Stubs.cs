namespace UnityEngine {
 public class Object {public bool destroyed;public static implicit operator bool(Object o)=>o!=null&&!o.destroyed;}
 public class Component:Object {public Dictionary<Type,Component> children=new();public T GetComponentInChildren<T>(bool inactive=false) where T:Component=>children.Values.OfType<T>().FirstOrDefault();public T GetComponent<T>() where T:Component=>GetComponentInChildren<T>();}
 public class Transform:Component {public Transform background;public Transform Find(string path)=>background;}
 public class Sprite:Object{} public class Material:Object{}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
}
namespace UnityEngine.UI {
 public class Graphic:UnityEngine.Component {public UnityEngine.Color color;public UnityEngine.Material material;public bool raycastTarget=true;}
 public class Image:Graphic {public enum Type{Simple,Sliced} public UnityEngine.Sprite sprite;public Type type;public float pixelsPerUnitMultiplier;public bool fillCenter,preserveAspect;}
 public struct Navigation {public enum Mode{Automatic,None}public Mode mode;}
 public struct ColorBlock {public UnityEngine.Color normalColor,selectedColor;}
 public struct SpriteState {public UnityEngine.Sprite selectedSprite,disabledSprite,highlightedSprite;}
 public class Selectable:UnityEngine.Component {public enum Transition{ColorTint,SpriteSwap,Animation}public Graphic targetGraphic;public ColorBlock colors;public SpriteState spriteState;public Transition transition;public Navigation navigation;public bool interactable=true;}
 public class Button:Selectable {public Action onClick;}
 public class Toggle:Selectable {public Graphic graphic;}
}
namespace TMPro {
 public class TMP_FontAsset:UnityEngine.Object{}
 public class TMP_Text:UnityEngine.Component {public TMP_FontAsset font;public UnityEngine.Material fontSharedMaterial;public int fontStyle;public UnityEngine.Color color;}
 public class TMP_InputField:UnityEngine.UI.Selectable {public UnityEngine.Color caretColor,selectionColor;public bool customCaretColor;public Action onSubmit;}
}
public class InventoryGui:UnityEngine.Component {
 public UnityEngine.Transform m_player,m_container;public UnityEngine.UI.Button m_stackAllButton,m_tabCraft;public TMPro.TMP_Text m_recipeDecription,m_containerName;public UnityEngine.Component m_splitDialog;public UnityEngine.UI.Toggle m_pvp;
}
public class TextInput:UnityEngine.Component {public static TextInput instance;}
