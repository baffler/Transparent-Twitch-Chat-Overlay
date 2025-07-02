namespace TransparentTwitchChatWPF.Twitch;
public interface ITwitchAuthService
{
    event EventHandler<string> AccessTokenReceived;
    Task ConnectAsync();
}