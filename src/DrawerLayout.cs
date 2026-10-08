using System;
namespace Quartermaster;
internal static class DrawerLayout
{
    internal const int Count=12,Columns=6,Rows=4;
    internal static string Key(int index)
    {if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));return "Quartermaster.drawer."+index;}
}
