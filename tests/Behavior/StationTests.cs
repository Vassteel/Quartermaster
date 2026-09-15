using Quartermaster;
using UnityEngine;

internal static class StationTests
{
    internal static void Run(Action<bool,string> assert)
    {
        var hub=new Container(); hub.Settings.Deposit=true; ContainerRegistry.All.Add(hub);
        var bench=new CraftingStation { m_name="$piece_workbench" };
        bench.transform.position=new Vector3(-90,0,0);
        var view=new ZNetView(); bench.Components[typeof(ZNetView)]=view; bench.Components[typeof(Piece)]=new Piece();
        var stations=new List<CraftingStation>{bench};
        var point=new Vector3(90,0,0);
        CraftingStation Find()=>StationCoverage.Find("$piece_workbench",point,stations);
        assert(Find()==bench,"opposite sides of one storage area share a station beyond its native radius");
        assert(StationCoverage.Find("$piece_forge",point,stations)==null,"a workbench does not satisfy a forge requirement");
        point=new Vector3(100,0,0);assert(Find()==bench,"storage boundary included");
        point=new Vector3(100.1f,0,0);assert(Find()==null,"outside storage area cannot borrow a station");
        point=new Vector3(0,101,0);assert(Find()==null,"storage height is respected");
        StationCoverage.CaptureBuildPoint(point,out var originalPoint);
        point.y=0; CraftingStation result=null;
        StationCoverage.ExtendBuildStation("$piece_workbench",originalPoint,stations,ref result);
        assert(result==null,"vanilla height flattening cannot extend the storage sphere");
        point=new Vector3(90,0,0);
        bench.transform.position=new Vector3(-101,0,0);assert(Find()==null,"station outside hub radius does not extend coverage");
        bench.transform.position=new Vector3(0,101,0);assert(Find()==null,"station above storage sphere is excluded");
        bench.transform.position=new Vector3(-90,0,0);
        hub.Settings.Deposit=false;assert(Find()==null,"ordinary storage is not a coverage hub");hub.Settings.Deposit=true;
        hub.Accessible=false;assert(Find()==null,"inaccessible deposit chest cannot lend coverage");hub.Accessible=true;
        hub.Destroyed=true;assert(Find()==null,"destroyed deposit removes coverage immediately");hub.Destroyed=false;
        bench.Destroyed=true;assert(Find()==null,"destroyed station removes coverage immediately");bench.Destroyed=false;
        bench.isActiveAndEnabled=false;assert(Find()==null,"inactive station excluded");bench.isActiveAndEnabled=true;
        view.Valid=false;assert(Find()==null,"unloaded station excluded");view.Valid=true;
        bench.Components.Remove(typeof(ZNetView));assert(Find()==null,"prefab without network instance excluded");bench.Components[typeof(ZNetView)]=view;
        bench.Components.Remove(typeof(Piece));assert(Find()==null,"non-piece station excluded");bench.Components[typeof(Piece)]=new Piece();
        PrivateArea.Access=_=>false;assert(Find()==null,"ward-protected station cannot lend coverage");PrivateArea.Access=_=>true;
        Plugin.Enabled.Value=false;assert(Find()==null,"global disable respected");Plugin.Enabled.Value=true;
        Plugin.ExtendStationCoverage.Value=false;assert(Find()==null,"coverage switch respected");Plugin.ExtendStationCoverage.Value=true;
        Player.m_localPlayer=null;assert(Find()==null,"no coverage without a local player");Player.m_localPlayer=new Player();
        result=new CraftingStation();var vanilla=result;
        StationCoverage.ExtendBuildStation("$piece_workbench",point,stations,ref result);
        assert(result==vanilla,"existing native or other-mod station result preserved");
        var remoteHub=new Container(); remoteHub.Settings.Deposit=true;remoteHub.transform.position=new Vector3(1000,0,0);ContainerRegistry.All.Add(remoteHub);
        bench.transform.position=new Vector3(1000,0,0);assert(Find()==null,"same-group distant bases cannot share a station");
        point=new Vector3(1090,0,0);assert(Find()==bench,"second hub has its own valid station coverage");
        Plugin.Range.Value=50;assert(Find()==null,"range config changes apply immediately");Plugin.Range.Value=100;
        stations.Clear();assert(Find()==null,"no station grants no coverage");
        ContainerRegistry.All.Clear();
    }
}
