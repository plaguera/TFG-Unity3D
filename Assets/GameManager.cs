using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour {
    
    public static GameManager Instance;
    public Socket ServerSocket = new Socket();
    public Socket ClientSocket = new Socket();
    public static int N_PLAYERS = 2;

    [SyncVar(hook = "OnChangeTurn")] public int Turn = 1;
    [SyncVar(hook = "OnChangeConcept")] public string Concept;
    [SyncVar(hook = "OnWaitingPlayersChange")] public string WaitingPlayers;
    [SyncVar] public TurnPhase TurnPhase = TurnPhase.Initial;
    [SyncVar] public string TurnStartPlayer;
    [SyncVar] long roomID;
    public SyncListString ImageSelections = new SyncListString();

    public Dictionary<string, string> PlayerSelections = new Dictionary<string, string>();
    Dictionary<string, List<string>> DealtHands = new Dictionary<string, List<string>>();
    Dictionary<string, int> Votes = new Dictionary<string, int>();
    List<Player> Players { get; set; } = new List<Player>();
    List<Player> Waiting { get; set; } = new List<Player>();

    public Text TextTurn, Debugtext;
    public Image ImageSelection;
    public InputField ConceptInput;
    public Button AcceptButton;
    public GameObject WaitingPanel, MainPanel, FinalPanel, EndGameScreen;
    public Transform UpperRow, LowerRow;
    public ToggleGroup ToggleGroupMain, ToggleGroupVote;
    public GameObject ImagePrefab;

    void Awake() { Instance = this; }

    public override void OnStartClient()
    {
        ClientSocket = new Socket();
        ClientSocket.Connect("127.0.0.1", 5555);
    }

    public override void OnStartServer()
    {
        foreach (Player player in FindObjectsOfType<Player>()) AddPlayer(player);
        ServerSocket = new Socket();
        ServerSocket.Connect("127.0.0.1", 5555);
    }

    [Server]
    public void ServerInitializePlayer(GameObject @object)
    {
        var player = @object.GetComponent<Player>();
        AddPlayer(player);
    }

    [Server]
    public void AddPlayer(Player player)
    {
        if (!Players.Contains(player))
        {
            Players.Add(player);
            if (Players.Count == 1) TurnStartPlayer = player.Name;
            else if (Players.Count == Prototype.NetworkLobby.LobbyManager.n_PLAYERS)
            {
                RpcChangePhase(TurnPhase, TurnStartPlayer);
                NotifyJavaServer();
            }
        }
    }

    [Server]
    void NotifyJavaServer()
    {
        roomID = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmssf"));

        string msg = "create#room#" + roomID;
        foreach (Player i in Players)
            msg += "#" + i.Name;
        ServerSocket.Send(msg);

        msg = ServerSocket.Receive();

        string[] tokens = msg.Split('#');
        foreach (string i in tokens)
        {
            string[] subtokens = i.Split(',');
            List<string> aux = new List<string>();
            for (int j = 1; j < subtokens.Length; j++)
                aux.Add(subtokens[j]);
            DealtHands.Add(subtokens[0], aux);
        }

        RpcStackInitRequest();
    }

    [Server]
    bool AllPlayersReadyForNextTurn()
    {
        return Waiting.Count == Players.Count;
    }

    [Server]
    public void NextTurn(Player player)
    {
        if (Waiting.Contains(player)) return;
        Waiting.Add(player);
        if (AllPlayersReadyForNextTurn())
        {
            Waiting.Clear();
            if (TurnPhase == TurnPhase.Initial) TurnPhase = TurnPhase.Middle;
            else if (TurnPhase == TurnPhase.Middle)
            {
                foreach (string image in ImageSelections)
                    Votes.Add(image, 0);
                TurnPhase = TurnPhase.Final;
            }
            else if (TurnPhase == TurnPhase.Final)
            {
                foreach (string image in ImageSelections)
                    Image.DestroyInstances(image);
                
                RpcDestroySelectedImages();

                for (int i = 0; i < Players.Count; i++)
                    if (Players[i].Name == TurnStartPlayer)
                    {
                        TurnStartPlayer = Players[(i + 1) % Players.Count].Name;
                        break;
                    }
                List<string> keys = Votes.Keys.ToList();
                foreach (string i in keys)
                    Votes[i] = ImageSelections.Count(obj => obj == i) - 1;

                if (Votes[PlayerSelections[TurnStartPlayer]] == Votes.Values.Sum())
                    foreach (Player i in Players)
                        if (i.Name == TurnStartPlayer)
                            i.Score += 1;
                else
                    foreach (Player j in Players)
                        j.Score += Votes[PlayerSelections[j.Name]];

                string msg = "result#" + Concept;
                foreach (KeyValuePair<string, int> i in Votes)
                    msg += "#" + i.Key + "," + i.Value;
                ServerSocket.Send(msg);

                if (Players.Any(obj => obj.Score >= 10))
                {
                    RpcGameWonBy(Players.Find(obj => obj.Score == Players.Max(obj1 => obj1.Score)).Name);
                    msg = "scores";
                    foreach (Player i in Players)
                        msg += "#" + i.Name + "," + i.Score;
                    ServerSocket.Send(msg);
                }

                PlayerSelections.Clear();
                ImageSelections.Clear();
                Votes.Clear();
                Turn++;
                TurnPhase = TurnPhase.Initial;
            }
            RpcChangePhase(TurnPhase, TurnStartPlayer);
        }
        WaitingPlayers = "( " + Waiting.Count + " / " + Players.Count + " )";
    }

    [ClientRpc]
    public void RpcChangePhase(TurnPhase turnPhase, string player)
    {
        TurnPhase = turnPhase;
        TurnStartPlayer = player;

        if (TurnPhase == TurnPhase.Initial)
        {
            ConceptInput.text = "";
            ConceptInput.interactable = AcceptButton.interactable = Player.Instance.Name == TurnStartPlayer;
            Player.Instance.Status = Player.Instance.Name == TurnStartPlayer ? PlayerStatus.Playing : PlayerStatus.Waiting;
            WaitingPanel.SetActive(Player.Instance.Status == PlayerStatus.Waiting);
            if (Player.Instance.Name != TurnStartPlayer) Player.Instance.NextTurn();

            FinalPanel.SetActive(false);
            MainPanel.SetActive(true);
        }

        else if (TurnPhase == TurnPhase.Middle)
        {
            ClearToggles();
            ConceptInput.interactable = false;

            AcceptButton.interactable = Player.Instance.Name != TurnStartPlayer;
            Player.Instance.Status = Player.Instance.Name != TurnStartPlayer ? PlayerStatus.Playing : PlayerStatus.Waiting;
            WaitingPanel.SetActive(Player.Instance.Status == PlayerStatus.Waiting);
            if (Player.Instance.Name == TurnStartPlayer) Player.Instance.NextTurn();

            FinalPanel.SetActive(false);
            MainPanel.SetActive(true);
        }

        else if (TurnPhase == TurnPhase.Final)
        {
            ClearToggles();
            ConceptInput.interactable = false;
            AcceptButton.interactable = Player.Instance.Name != TurnStartPlayer;
            Player.Instance.Status = Player.Instance.Name != TurnStartPlayer ? PlayerStatus.Playing : PlayerStatus.Waiting;
            WaitingPanel.SetActive(Player.Instance.Status == PlayerStatus.Waiting);
            if (Player.Instance.Name == TurnStartPlayer) Player.Instance.NextTurn();
            if (Player.Instance.Name != TurnStartPlayer)
            {
                MainPanel.SetActive(false);
                FinalPanel.SetActive(true);
                foreach (string image in ImageSelections) AddImage(ClientSocket.RequestImage(image));
            }
        }
    }

    public void OnClickAccept()
    {
        switch (TurnPhase)
        {
            case TurnPhase.Initial:
                if (Player.Instance.Name == TurnStartPlayer && ConceptInput.text.Trim() != "" && ImageSelection != null)
                {
                    Concept = ConceptInput.text;
                    Player.Instance.CmdSendAnswer(Player.Instance.Name, ImageSelection.Name);
                    Player.Instance.CmdSendConcept(ConceptInput.text.Trim());
                    Player.Instance.NextTurn();
                }
                break;
            case TurnPhase.Middle:
                if (Player.Instance.Name != TurnStartPlayer && ImageSelection != null)
                {
                    Player.Instance.CmdSendAnswer(Player.Instance.Name, ImageSelection.Name);
                    Player.Instance.NextTurn();
                }
                break;
            case TurnPhase.Final:
                if (Player.Instance.Name != TurnStartPlayer && ImageSelection != null)
                {
                    Player.Instance.CmdSendAnswer(Player.Instance.Name, ImageSelection.Name);
                    Player.Instance.NextTurn();
                }
                break;
        }
    }

    [ClientRpc]
    void RpcGameWonBy(string winner)
    {
        if (Player.Instance.Name == winner)
            ShowEndScreen("¡¡ VICTORIA !!");
        else
            ShowEndScreen("¡¡ DERROTA !!");
    }

    void ShowEndScreen(string text)
    {
        EndGameScreen.SetActive(true);
        EndGameScreen.transform.Find("Content/Upper/Result/Text").GetComponent<Text>().text = text;
        EndGameScreen.transform.Find("Content/Upper/Score/Text").GetComponent<Text>().text = "Puntos: " + Player.Instance.Score;
    }

    [Server]
    public void HandleAnswer(string player, string image)
    {
        if (TurnPhase == TurnPhase.Initial || TurnPhase == TurnPhase.Middle)
            PlayerSelections.Add(player, image);
        ImageSelections.Add(image);
    }

    void ClearToggles()
    {
        if (ImageSelection == null) return;
        ImageSelection.Toggle.isOn = false;
        ImageSelection = null;
    }

    [ClientRpc]
    void RpcStackInitRequest()
    {
        Player.Instance.CmdServer();
    }

    [Server]
    public void SendStackToPlayer(NetworkConnection connection, string player)
    {
        List<string> aux = DealtHands[player];
        foreach (string i in aux)
            TargetSendImage(connection, i);
    }

    [TargetRpc]
    public void TargetSendImage(NetworkConnection connection, string image)
    {
        AddImage(ClientSocket.RequestImage(image));
    }

    [ClientRpc]
    void RpcDestroySelectedImages()
    {
        foreach (string image in ImageSelections)
            Image.DestroyInstances(image);
    }

    void OnChangeTurn(int turn)
    {
        TextTurn.text = "Turno: " + turn;
    }

    void OnChangeConcept(string concept)
    {
        ConceptInput.text = concept;
    }

    void OnWaitingPlayersChange(string text)
    {
        WaitingPanel.transform.Find("Text").GetComponent<Text>().text = text;
    }

    public void AddImage(Image image)
    {
        GameObject @object;
        if (TurnPhase == TurnPhase.Initial || TurnPhase == TurnPhase.Middle)
        {
            if (UpperRow.childCount == LowerRow.childCount)
                @object = Instantiate(ImagePrefab, UpperRow);
            else
                @object = Instantiate(ImagePrefab, LowerRow);
        } else {
            @object = Instantiate(ImagePrefab, FinalPanel.transform);
        }

        image.SetGameObject(@object);

        if (TurnPhase == TurnPhase.Initial || TurnPhase == TurnPhase.Middle) image.Toggle.group = ToggleGroupMain;
        else image.Toggle.group = ToggleGroupVote;
    }

    public void OnClickReturnToMenu()
    {
        SceneManager.LoadScene(0);
        EndGameScreen.SetActive(false);
    }

}
