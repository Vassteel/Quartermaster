namespace Quartermaster;

internal static partial class ChestUi
{
    private static void InterfaceBody()
    {
        Text(body,"SORT INVENTORY",0,0,672,32,22,Gold);
        Toggle("Show Sort Inventory button",Plugin.ShowSortInventory.Value,44,()=>{
            Plugin.ShowSortInventory.Value=!Plugin.ShowSortInventory.Value;Build();
        });
        Text(body,"Adjust the button from its normal position.\nThese settings affect only your interface.",0,-99,672,62,19,Muted);
        Text(body,"Horizontal offset",0,-177,235,36,20,Gold);
        Text(body,"Vertical offset",0,-225,235,36,20,Gold);
        Input(body,Plugin.SortInventoryOffsetX.Value.ToString(),250,-177,135,value=>SaveSortOffset(value,true),"0",true);
        Input(body,Plugin.SortInventoryOffsetY.Value.ToString(),250,-225,135,value=>SaveSortOffset(value,false),"0",true);
        Text(body,"+ right / − left",405,-177,265,36,18,Muted);
        Text(body,"+ down / − up",405,-225,265,36,18,Muted);
        Button(body,"Reset position",0,-295,220,()=>{
            Plugin.SortInventoryOffsetX.Value=0;Plugin.SortInventoryOffsetY.Value=0;notice="Button position reset";Build();
        });
        Text(body,"Close the ledger to see the button.\nChanges save immediately.",0,-335,672,45,18,Muted);
    }
    private static void SaveSortOffset(string value,bool horizontal)
    {
        if(!int.TryParse(value,out int offset)||offset < -1000||offset > 1000)
            notice="Enter a whole number from −1000 to 1000";
        else
        {
            if(horizontal)Plugin.SortInventoryOffsetX.Value=offset;else Plugin.SortInventoryOffsetY.Value=offset;
            notice="Button position saved";
        }
        if(status)status.text=notice;
    }
}
