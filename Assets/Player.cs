using UnityEngine;
using UnityEngine.Networking;


public class Player : NetworkBehaviour {
    
    [SyncVar] public string Name;
    [SyncVar] public Color Color;
    [SyncVar] public int Score;
    [SyncVar] public PlayerStatus Status;

    static public Player Instance;

    void Start()
    {
        if (isLocalPlayer)
        {
            Instance = this;
            if (GameManager.Instance != null) CmdConnectToServer();
        }
    }

    void OnGUI()
    {
        if (isLocalPlayer)
        {
            var color = ColorUtility.ToHtmlStringRGBA(Color);
            string text = "Name: <color=#" + Color + ">" + Name + "</color>\n";
            text += "<color=white>Turn:</color><color=white> " + Status + " - "
                + GameManager.Instance.TurnPhase + " - "
                + GameManager.Instance.TurnStartPlayer + " - "
                + GameManager.Instance.ImageSelection?.Name + " - "
                + GameManager.Instance.ImageSelections.Count + " - "
                             + Prototype.NetworkLobby.LobbyManager.n_PLAYERS + "</color>";
            GUI.Label(new Rect(10, 10, 300, 40), text);
        }
    }

    /// <summary>
    /// Pide al servidor que le inicialice.
    /// </summary>
    [Command]
    void CmdConnectToServer()
    {
        GameManager.Instance.ServerInitializePlayer(gameObject);
    }

    /// <summary>
    /// Informa al servidor que el jugador ha terminado su turno.
    /// </summary>
    [Command]
    void CmdNextTurn()
    {
        GameManager.Instance.NextTurn(this);
    }

    public void NextTurn()
    {
        Status = PlayerStatus.Waiting;
        CmdNextTurn();
    }

    [Command]
    public void CmdServer()
    {
        GameManager.Instance.SendStackToPlayer(connectionToClient, Name);
    }

    [Command]
    public void CmdSendAnswer(string player, string image)
    {
        GameManager.Instance.HandleAnswer(player, image);
    }

    [Command]
    public void CmdSendConcept(string concept)
    {
        GameManager.Instance.Concept = concept;
    }

    /// <summary>
    /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:TheFiskDataModel.Networking.OnlinePlayer"/>.
    /// </summary>
    /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:TheFiskDataModel.Networking.OnlinePlayer"/>.</returns>
    public override string ToString()
    {
        return $"[Player: Name = {Name}, Color = {Color}, Score = {Score}]";
    }
}
