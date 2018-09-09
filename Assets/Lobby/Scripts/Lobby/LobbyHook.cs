using UnityEngine;
using UnityEngine.Networking;
using System.Collections;



namespace Prototype.NetworkLobby
{
    // Subclass this and redefine the function you want
    // then add it to the lobby prefab
    public abstract class LobbyHook : MonoBehaviour
    {
        public virtual void OnLobbyServerSceneLoadedForPlayer(NetworkManager manager, GameObject lobbyPlayer, GameObject gamePlayer)
        {
            //base.OnLobbyServerSceneLoadedForPlayer(lobbyPlayer, gamePlayer);

            var lobby = lobbyPlayer.GetComponent<LobbyPlayer>();
            var player = gamePlayer.GetComponent<Player>();

            // Copy Lobby Player Attributes to Game Player
            player.Name = lobby.playerName;
            player.Color = lobby.playerColor;

            NetworkServer.Destroy(lobbyPlayer.gameObject);
        }
    }

}
