using DiscordRPC;

namespace ADashboard
{
    public partial class DBoard
    {
        private static DiscordRpcClient discordClient;
        private static int discordPipe = -1;

        public static void InitializeDiscord()
        {
            discordClient = new DiscordRpcClient("1149097329782173727", pipe: discordPipe);
            discordClient.Initialize();
        }
    }
}