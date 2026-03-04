using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransparentTwitchChatWPF.ChatProviders;

public static class ChatProviderFactory
{
    public static IChatProvider Create(ChatTypes chatType)
    {
        switch (chatType)
        {
            case ChatTypes.KapChat:
                return new KapChatProvider();
            case ChatTypes.TwitchPopout:
                return new TwitchPopoutProvider();
            case ChatTypes.CustomURL:
                return new CustomProvider();
            case ChatTypes.NativeChat:
                return new NativeChatProvider();
            default:
                throw new NotSupportedException($"Chat type {chatType} is not supported.");
        }
    }
}
