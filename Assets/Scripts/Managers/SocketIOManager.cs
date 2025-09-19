using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;

public class SocketIOManager : MonoBehaviour
{
    [SerializeField] private GameObject RaycastBlocker;
    internal Player playerdata = null;
    internal GameData TempResultData;
    internal Root resultData;
    // internal Paylines paylines;
    internal UiData uiData;
    internal List<int> Bets;
    internal List<int> Level;
    internal bool isResultdone = false;
    private SocketManager manager;
    private Socket gameSocket;
    [SerializeField] internal JSFunctCalls JSManager;
    [SerializeField] private SlotController SlotManager;
    [SerializeField] private UIManager UIManager;
    protected string nameSpace = "playground";
    protected string SocketURI = null;
    protected string TestSocketURI = "http://localhost:5000";
    // protected string TestSocketURI = "https://d9sbd7tg-5000.inc1.devtunnels.ms/";

    [SerializeField] private string testToken;
    protected string gameID = "SL-CM";
    //protected string gameID = "";
    internal bool SetInit = false;
    private const int maxReconnectionAttempts = 6;
    private readonly TimeSpan reconnectionDelay = TimeSpan.FromSeconds(10);
    private bool isConnected = false; //Back2 Start
    private bool hasEverConnected = false;
    private const int MaxReconnectAttempts = 5;
    private const float ReconnectDelaySeconds = 2f;
    private float lastPongTime = 0f;
    private float pingInterval = 2f;
    private bool waitingForPong = false;
    private int missedPongs = 0;
    private const int MaxMissedPongs = 5;
    private Coroutine PingRoutine; //Back2 end
    private void Awake()
    {
        SetInit = false;
    }

    private void Start()
    {
        OpenSocket();
    }

    void ReceiveAuthToken(string jsonData)
    {
        Debug.Log("Received data: " + jsonData);

        // Parse the JSON data
        var data = JsonUtility.FromJson<AuthTokenData>(jsonData);
        SocketURI = data.socketURL;
        myAuth = data.cookie;
        nameSpace = data.nameSpace;
    }
    string myAuth = null;
    private void OpenSocket()
    {
        SocketOptions options = new SocketOptions(); //Back2 Start
        options.AutoConnect = false;
        options.Reconnection = false;
        options.Timeout = TimeSpan.FromSeconds(3); //Back2 end
        options.ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket;

#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("authToken");
        StartCoroutine(WaitForAuthToken(options));
#else
        Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
        {
            return new
            {
                token = testToken
            };
        };
        options.Auth = authFunction;
        SetupSocketManager(options);
#endif
    }

    private IEnumerator WaitForAuthToken(SocketOptions options)
    {
        // Wait until myAuth is not null
        while (myAuth == null)
        {
            Debug.Log("My Auth is null");
            yield return null;
        }
        while (SocketURI == null)
        {
            Debug.Log("My Socket is null");
            yield return null;
        }

        Debug.Log("My Auth is not null");
        // Once myAuth is set, configure the authFunction
        Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
        {
            return new
            {
                token = myAuth
            };
        };
        options.Auth = authFunction;

        Debug.Log("Auth function configured with token: " + myAuth);

        // Proceed with connecting to the server
        SetupSocketManager(options);
    }

    private void SetupSocketManager(SocketOptions options)
    {
        // Create and setup SocketManager
#if UNITY_EDITOR
        this.manager = new SocketManager(new Uri(TestSocketURI), options);
#else
    this.manager = new SocketManager(new Uri(SocketURI), options);
#endif
        // Set subscriptions
        if (string.IsNullOrEmpty(nameSpace))
        {  //BackendChanges Start
            gameSocket = this.manager.Socket;
        }
        else
        {
            print("nameSpace: " + nameSpace);
            gameSocket = this.manager.GetSocket("/" + nameSpace);
        }

        gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
        gameSocket.On(SocketIOEventTypes.Disconnect, OnDisconnected); //Back2 Start
        gameSocket.On<Error>(SocketIOEventTypes.Error, OnError);
        gameSocket.On<string>("game:init", OnListenEvent);
        gameSocket.On<string>("result", OnListenEvent);
        gameSocket.On<string>("pong", OnPongReceived); //Back2 Start
        manager.Open();
    }

    void OnConnected(ConnectResponse resp) //Back2 Start
    {
        Debug.Log("✅ Connected to server.");

        if (hasEverConnected)
        {
            UIManager.CheckAndClosePopups();
        }

        isConnected = true;
        hasEverConnected = true;
        waitingForPong = false;
        missedPongs = 0;
        lastPongTime = Time.time;
        SendPing();
    } //Back2 end

    private void OnDisconnected() //Back2 Start
    {
        Debug.LogWarning("⚠️ Disconnected from server.");
        isConnected = false;
        ResetPingRoutine();
        UIManager.EnableDisconect();
    } //Back2 end

    private void OnPongReceived(string data) //Back2 Start
    {
        Debug.Log("✅ Received pong from server.");
        waitingForPong = false;
        missedPongs = 0;
        lastPongTime = Time.time;
        Debug.Log($"⏱️ Updated last pong time: {lastPongTime}");
        Debug.Log($"📦 Pong payload: {data}");
    } //Back2 end

    private void OnError(Error err)
    {
        Debug.LogError("Socket Error Message: " + err);
#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("error");
#endif
    }

    private void OnListenEvent(string data)
    {
        ParseResponse(data);
    }

    private void SendPing() //Back2 Start
    {
        ResetPingRoutine();
        PingRoutine = StartCoroutine(PingCheck());
    }

    void ResetPingRoutine()
    {
        if (PingRoutine != null)
        {
            StopCoroutine(PingRoutine);
        }
        PingRoutine = null;
    }

    private IEnumerator PingCheck()
    {
        while (true)
        {
            Debug.Log($"🟡 PingCheck | waitingForPong: {waitingForPong}, missedPongs: {missedPongs}, timeSinceLastPong: {Time.time - lastPongTime}");

            if (missedPongs == 0)
            {
                UIManager.CheckAndClosePopups();
            }

            // If waiting for pong, and timeout passed
            if (waitingForPong)
            {
                if (missedPongs == 2)
                {
                    UIManager.ReconnectionPopup();
                }
                missedPongs++;
                Debug.LogWarning($"⚠️ Pong missed #{missedPongs}/{MaxMissedPongs}");

                if (missedPongs >= MaxMissedPongs)
                {
                    Debug.LogError("❌ Unable to connect to server — 5 consecutive pongs missed.");
                    isConnected = false;
                    UIManager.EnableDisconect();
                    yield break;
                }
            }

            // Send next ping
            waitingForPong = true;
            lastPongTime = Time.time;
            Debug.Log("📤 Sending ping...");
            SendDataWithNamespace("ping");
            yield return new WaitForSeconds(pingInterval);
        }
    } //Back2 end

    private void SendDataWithNamespace(string eventName, string json = null)
    {
        // Send the message
        if (gameSocket != null && gameSocket.IsOpen)
        {
            if (json != null)
            {
                gameSocket.Emit(eventName, json);
                Debug.Log("JSON data sent: " + json);
            }
            else
            {
                gameSocket.Emit(eventName);
            }
        }
        else
        {
            Debug.LogWarning("Socket is not connected.");
        }
    }

    void CloseGame()
    {
        Debug.Log("Unity: Closing Game");
        StartCoroutine(CloseSocket());
    }

    internal IEnumerator CloseSocket() //Back2 Start
    {
        RaycastBlocker.SetActive(true);
        ResetPingRoutine();

        Debug.Log("Closing Socket");

        manager?.Close();
        manager = null;

        Debug.Log("Waiting for socket to close");

        yield return new WaitForSeconds(0.5f);

        Debug.Log("Socket Closed");

#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("OnExit"); //Telling the react platform user wants to quit and go back to homepage
#endif
    } //Back2 end

    private void ParseResponse(string jsonObject)
    {
        Debug.Log(jsonObject);
        Root myData = JsonConvert.DeserializeObject<Root>(jsonObject);

        string id = myData.id;
        playerdata = myData.player;
        uiData = myData.uiData;
        switch (id)
        {
            case "initData":
                {
                    if (SlotManager) SlotManager.UpdateUI(myData.player.balance);
                    if (!SetInit)
                    {
                        // Application.ExternalCall("window.parent.postMessage", "OnEnter", "*");
#if UNITY_WEBGL && !UNITY_EDITOR
                            JSManager.SendCustomMessage("OnEnter");
#endif
                        Bets = myData.gameData.bets;
                        Level = myData.features.levels;
                        UIManager.SetupBets(Level);
                        UIManager.SetupDenoms(Bets);
                        SlotManager.PopulateSymbols(myData.uiData.paylines);
                        SetInit = true;
                    }
                    break;
                }
            case "ResultData":
                {
                    // Debug.Log(jsonObject);
                    resultData = myData;
                    playerdata = myData.player;
                    TempResultData = myData.gameData;
                    isResultdone = true;
                    break;
                }
        }
    }

    internal void AccumulateResult(int currBet, int level)
    {
        isResultdone = false;
        MessageData message = new MessageData();
        message.type = "SPIN";
        message.payload.betIndex = currBet;
        message.payload.levelIndex = level;
        // Serialize message data to JSON
        string json = JsonUtility.ToJson(message);
        SendDataWithNamespace("request", json);
    }
}

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
[Serializable]
public class Features
{
    public List<int> levels { get; set; }
}

[Serializable]
public class GameData
{
    public List<int> bets { get; set; }
}

[Serializable]
public class MessageData
{
    public string type;
    public Data payload = new();

}

[Serializable]
public class Data
{
    public int betIndex;
    public string Event;
    public int levelIndex;
    public List<int> index;
    public int option;
}

[Serializable]
public class AuthTokenData
{
    public string cookie;
    public string socketURL;
    public string nameSpace;
}

[Serializable]
public class Paylines
{
    public List<Symbol> symbols { get; set; }
}

[Serializable]
public class Player
{
    public double balance { get; set; }
}

[Serializable]
public class Root
{
    public string id { get; set; }
    public GameData gameData { get; set; }
    public Features features { get; set; }
    public UiData uiData { get; set; }
    public Player player { get; set; }
    public bool success { get; set; }
    public List<List<string>> matrix { get; set; }
    public Payload payload { get; set; }
}

[Serializable]
public class Symbol
{
    public int id { get; set; }
    public string name { get; set; }
    public List<object> multiplier { get; set; }
    public int? payout { get; set; }
    public string description { get; set; }
}

[Serializable]
public class FrozenIndex
{
    public List<int> position { get; set; }
    public string symbol { get; set; }
}


[Serializable]
public class Payload
{
    public int currentWinning { get; set; }
    public bool isRedRespin { get; set; }
    public bool isZeroRespin { get; set; }
    public List<FrozenIndex> frozenIndices { get; set; }
}

[Serializable]
public class UiData
{
    public Paylines paylines { get; set; }
}
