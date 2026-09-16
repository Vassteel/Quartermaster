using Quartermaster;

internal static class ItemMessageTests
{
    internal static void Run(Action<bool,string> assert)
    {
        const string before="Item description\n";
        const string after="\n$item_crafter: <color=orange>Vassteel</color>";
        string Block(string text)=>"\n<color=#808080><i>"+text+"</i></color>";
        Plugin.Enabled.Value=true;Plugin.HideCheatItemMessages.Value=true;
        foreach(var message in new[]{"This item was summoned through cheating means.","Cet objet a été invoqué par des moyens de triche.",ItemTagMessages.MessageKey})
        {
            Localization.instance.Message=message;
            string tooltip=before+Block(message)+after;
            ItemTagMessages.Tooltip(ref tooltip);
            assert(tooltip==before+after,"localized item notice removed without changing adjacent tooltip content");
            ItemTagMessages.Tooltip(ref tooltip);assert(tooltip==before+after,"tooltip suppression is idempotent");
            assert(ItemTagMessages.RemoveNotice(before+message+after,message)==before+message+after,"matching words outside the warning block remain intact");
            assert(ItemTagMessages.RemoveNotice(Block(message)+before+Block(message),message)==before,"repeated notice blocks removed");
        }
        assert(ItemTagMessages.RemoveNotice(null,"message")==null,"null tooltip preserved");
        assert(ItemTagMessages.RemoveNotice(before,"")==before,"missing localization preserves tooltip");
        string original=before+Block(Localization.instance.Message)+after;
        Plugin.HideCheatItemMessages.Value=false;string disabled=original;
        ItemTagMessages.Tooltip(ref disabled);assert(disabled==original,"message config off preserves notices");
        bool changed=true,popup=true;ItemTagMessages.InventoryMessages(ref changed,ref popup);
        assert(changed&&popup,"config off preserves inventory notification flags");
        Plugin.HideCheatItemMessages.Value=true;Plugin.Enabled.Value=false;
        ItemTagMessages.Tooltip(ref disabled);ItemTagMessages.InventoryMessages(ref changed,ref popup);
        assert(disabled==original&&changed&&popup,"global disable preserves both kinds of notice");
        Plugin.Enabled.Value=true;ItemTagMessages.InventoryMessages(ref changed,ref popup);
        assert(!changed&&!popup,"both pickup and removal messages suppressed");
        var localization=Localization.instance;Localization.instance=null;
        ItemTagMessages.Tooltip(ref disabled);assert(disabled==original,"missing localization service is safe");Localization.instance=localization;
        Plugin.ClearCheatItemTagsOnLoad.Value=false;
        changed=popup=true;ItemTagMessages.InventoryMessages(ref changed,ref popup);
        assert(!changed&&!popup,"message setting works independently of startup scan");
        Plugin.ClearCheatItemTagsOnLoad.Value=true;
    }
}

public sealed class Localization
{
    public static Localization instance=new();
    public string Message="This item was summoned through cheating means.";
    public string Localize(string key)=>Message;
}
