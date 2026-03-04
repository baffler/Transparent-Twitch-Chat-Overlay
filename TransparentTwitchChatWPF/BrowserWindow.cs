using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransparentTwitchChatWPF
{
    interface BrowserWindow
    {
        void hideBorders();
        void drawBorders();
        void SetInteractable(bool interactable);
        void ToggleBorderVisibility();
        void ResetWindowState();
        void SetTopMost(bool topMost);
    }

    public enum WindowDisplayMode
    {
        /// <summary>
        /// Borders and title bar are visible for moving/resizing.
        /// </summary>
        Setup,
        /// <summary>
        /// Borders are hidden and the window is click-through.
        /// </summary>
        Overlay
    }
}
