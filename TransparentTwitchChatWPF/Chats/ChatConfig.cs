using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransparentTwitchChatWPF.Chats;

public class WebMessage
{
    public string Type { get; set; }
    public object Payload { get; set; }
}

public class Credentials
{
    public string Token { get; set; }
    public string ClientId { get; set; }
}