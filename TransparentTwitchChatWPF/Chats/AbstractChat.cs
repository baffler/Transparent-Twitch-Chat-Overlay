using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransparentTwitchChatWPF.Chats
{
    public abstract class Chat
    {
        private ChatTypes _chatType = ChatTypes.CustomURL;
        public ChatTypes ChatType 
        {
            get { return _chatType; }
            private set { _chatType = value; }
        }

        public Chat(ChatTypes chatType)
        {
            _chatType = chatType;
        }

        public virtual string PushNewChatMessage(string message = "", string nick = "", string color = "")
        {
            return string.Empty;
        }

        public virtual string PushNewMessage(string message = "")
        {
            return string.Empty;
        }

        public virtual string SetupJavascript()
        {
            return string.Empty;
        }

        public virtual string SetupCustomCSS()
        {
            return string.Empty;
        }
    }
}
