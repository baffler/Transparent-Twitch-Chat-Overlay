using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransparentTwitchChatWPF.Chats
{
    public abstract class Chat
    {
        public virtual string PushNewChatMessage(string message = "", string nick = "", string color = "")
        {
            return String.Empty;
        }

        public virtual string PushNewMessage(string message = "")
        {
            return String.Empty;
        }

        public virtual string SetupJavascript()
        {
            return String.Empty;
        }

        public virtual string SetupCustomCSS()
        {
            return String.Empty;
        }
    }
}
