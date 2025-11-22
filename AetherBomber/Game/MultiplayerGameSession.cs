using System.Collections.Generic;

namespace AetherBomber.Game
{
    public class MultiplayerGameSession
    {
        public List<string> Players { get; } = new();
        public string Passphrase { get; }

        public MultiplayerGameSession(string passphrase)
        {
            Passphrase = passphrase;
        }

        public void AddPlayer(string playerName)
        {
            if (!Players.Contains(playerName))
            {
                Players.Add(playerName);
            }
        }
    }
}
