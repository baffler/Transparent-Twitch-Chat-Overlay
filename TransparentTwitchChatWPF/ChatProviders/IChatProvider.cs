
using Microsoft.Web.WebView2.Core;

namespace TransparentTwitchChatWPF.ChatProviders;

public interface IChatProvider
{
    Uri GetNavigationUri();
    Task ConfigureAsync(CoreWebView2 coreWebView2) => Task.CompletedTask;
    string GetJavascriptToExecute() => string.Empty;
    string GetCssToInject() => string.Empty;
}
