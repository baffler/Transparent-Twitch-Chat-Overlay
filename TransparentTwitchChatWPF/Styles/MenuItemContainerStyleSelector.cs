using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace TransparentTwitchChatWPF;

public class MenuItemContainerStyleSelector : StyleSelector
{
    // This property will hold the style for our regular MenuItems
    public Style MenuItemStyle { get; set; }

    // This property will hold the style for Separators
    public Style SeparatorStyle { get; set; }

    public override Style SelectStyle(object item, DependencyObject container)
    {
        // Check the type of the item
        if (item is MenuItem)
        {
            return MenuItemStyle;
        }
        if (item is Separator)
        {
            return SeparatorStyle;
        }

        return base.SelectStyle(item, container);
    }
}
