using UnityEngine;
namespace Quartermaster;
internal static class ModularCabinetPlacement
{
    internal const float Scale=2f;
    internal static Vector3 DrawerScale=>new Vector3(Scale,Scale*.75f,Scale*1.25f);
    internal static Vector3 DrawerPosition(int index)=>Vector3.Scale(new Vector3(-.326f+(index%4)*.2175f,.207f+(index/4)*.282f,.226f),DrawerScale);
    internal static void Configure(GameObject prefab,bool drawers=false)
    {
        var p=prefab.GetComponent<Piece>();
        p.m_groundOnly=false;p.m_groundPiece=false;p.m_cultivatedGroundOnly=false;p.m_notOnWood=false;
        p.m_notOnFloor=false;p.m_inCeilingOnly=false;p.m_notOnTiltingSurface=false;p.m_noClipping=false;p.m_clipEverything=false;
        var wear=prefab.GetComponent<WearNTear>();if(wear)wear.m_supports=true;
        var scale=drawers?DrawerScale:Vector3.one*Scale;
        var art=prefab.transform.Find("Quartermaster furniture");if(art)art.localScale=scale;
        foreach(var t in prefab.GetComponentsInChildren<Transform>(true))if(t.CompareTag("snappoint"))t.localPosition=Vector3.Scale(t.localPosition,scale);
    }
}
