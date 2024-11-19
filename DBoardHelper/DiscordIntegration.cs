using DiscordRPC;

namespace ADashboard.DBoardHelper
{
    public class DiscordIntegration
    {
        private DiscordRpcClient discordClient;
        private static int discordPipe = -1;

        public void InitializeDiscord()
        {
            discordClient = new DiscordRpcClient("1149097329782173727", pipe: discordPipe);
            discordClient.Initialize();
        }
    }
}