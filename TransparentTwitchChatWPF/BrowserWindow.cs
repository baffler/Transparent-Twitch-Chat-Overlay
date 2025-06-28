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
        void ToggleBorderVisibility();
        void ResetWindowState();
        void SetTopMost(bool topMost);
    }
}
