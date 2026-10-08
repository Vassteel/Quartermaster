using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Quartermaster;

// Borrow live vanilla UI artwork and font materials. Never copy native callbacks,
// input bindings, animators or gameplay components into Quartermaster controls.
internal static class NativeMenuTheme
{
    private static InventoryGui owner;
    private static Image board;
    private static TMP_Text bodyText,buttonText,titleText;
    private static TMP_InputField input;
    internal static void Capture(InventoryGui gui)
    {
        if(!gui){Clear();return;}
        if(owner==gui&&board&&bodyText&&buttonText&&titleText)return;
        owner=gui;
        board=gui.m_player?gui.m_player.Find("Bkg")?.GetComponent<Image>():null;
        if(!board&&gui.m_container)board=gui.m_container.Find("Bkg")?.GetComponent<Image>();
        buttonText=gui.m_stackAllButton?gui.m_stackAllButton.GetComponentInChildren<TMP_Text>(true):null;
        bodyText=gui.m_recipeDecription?gui.m_recipeDecription:buttonText;
        titleText=gui.m_containerName?gui.m_containerName:buttonText;
        input=gui.m_splitDialog?gui.m_splitDialog.GetComponentInChildren<TMP_InputField>(true):null;
        if(!input&&TextInput.instance)input=TextInput.instance.GetComponentInChildren<TMP_InputField>(true);
    }
    internal static void Clear(){owner=null;board=null;bodyText=buttonText=titleText=null;input=null;}
    internal static void Image(Image target,Image source)
    {
        if(!target||!source)return;
        target.sprite=source.sprite;target.type=source.type;target.material=source.material;
        target.color=source.color;target.pixelsPerUnitMultiplier=source.pixelsPerUnitMultiplier;
        target.fillCenter=source.fillCenter;target.preserveAspect=source.preserveAspect;
    }
    internal static void Panel(Image target)
    {
        if(board&&board.sprite){Image(target,board);target.preserveAspect=false;}
        else target.color=new Color(.19f,.13f,.08f,.99f);
    }
    internal static void Text(TMP_Text target,bool heading=false,bool button=false)
    {
        var source=heading?titleText:button?buttonText:bodyText;
        if(!source)return;
        target.font=source.font;target.fontSharedMaterial=source.fontSharedMaterial;target.fontStyle=source.fontStyle;
    }
    internal static void Control(Selectable target,Selectable source)
    {
        if(!source)return;
        Image(target.targetGraphic as Image,source.targetGraphic as Image);
        target.colors=source.colors;target.spriteState=source.spriteState;
        target.transition=source.transition==Selectable.Transition.Animation?Selectable.Transition.ColorTint:source.transition;
        target.navigation=new Navigation{mode=Navigation.Mode.None};
    }
    internal static void Button(Button target,bool tab=false)
    {
        if(!owner)return;
        Control(target,tab?owner.m_tabCraft:owner.m_stackAllButton);
        var text=target.GetComponentInChildren<TMP_Text>(true);if(!text)return;
        Text(text,false,true);text.color=buttonText?buttonText.color:new Color(1,.69f,.29f);
    }
    internal static void ActiveTab(Button target)
    {
        if(!owner||!owner.m_tabCraft)return;
        var states=owner.m_tabCraft.spriteState;
        var image=target.targetGraphic as Image;
        var active=states.selectedSprite?states.selectedSprite:states.disabledSprite?states.disabledSprite:states.highlightedSprite;
        if(image&&active)image.sprite=active;
        // Use active artwork as the idle state; keyboard focus remains a separate highlight.
        var colors=target.colors;colors.normalColor=new Color(1,.88f,.65f);target.colors=colors;
    }
    internal static void Input(TMP_InputField target)
    {
        if(input)
        {
            Control(target,input);
            target.caretColor=input.caretColor;target.selectionColor=input.selectionColor;
            target.customCaretColor=input.customCaretColor;
        }
        else if(owner)Control(target,owner.m_stackAllButton);
        target.navigation=new Navigation{mode=Navigation.Mode.None};
    }
    internal static bool Checkmark(Image background,Image marker)
    {
        if(!owner||!owner.m_pvp)return false;
        var box=owner.m_pvp.targetGraphic as Image;var tick=owner.m_pvp.graphic as Image;
        if(!box||!box.sprite||!tick||!tick.sprite)return false;
        Image(background,box);Image(marker,tick);background.raycastTarget=marker.raycastTarget=false;
        return true;
    }
}
