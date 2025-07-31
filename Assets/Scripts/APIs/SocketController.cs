using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;
using Newtonsoft.Json.Linq;

public class SocketController : MonoBehaviour
{
  private SocketManager manager;

  protected string SocketURI = null;

  // protected string TestSocketURI = "https://game-crm-rtp-backend.onrender.com/";
  // protected string TestSocketURI = "https://7p68wzhv-5000.inc1.devtunnels.ms/";
  protected string TestSocketURI = "http://localhost:5000/";

  private Socket gameSocket;
  protected string nameSpace = "playground";

  // protected string gameID = "";
  protected string gameID = "SL-TM";

  private const int maxReconnectionAttempts = 6;
  private readonly TimeSpan reconnectionDelay = TimeSpan.FromSeconds(10);
  [SerializeField] internal bool isResultdone = false;
  [SerializeField] private string TestToken;
  [SerializeField] internal JSFunctCalls JSManager;
  [SerializeField] private UIManager uiManager;
  [SerializeField] private GameObject RaycastBlocker;
  string myAuth = null;
  internal SocketModel socketModel = new();
  internal Action OnInit;
  internal Action ShowDisconnectionPopup;
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

  void CloseGame()
  {
    Debug.Log("Unity: Closing Game");
    StartCoroutine(CloseSocket());
  }

  void ReceiveAuthToken(string jsonData)
  {
    Debug.Log("Received Auth Data: " + jsonData);

    // Parse the JSON data
    var data = JsonUtility.FromJson<AuthTokenData>(jsonData);
    SocketURI = data.socketURL;
    myAuth = data.cookie;
    nameSpace = data.nameSpace;
  }

  internal void OpenSocket()
  {
    // Create and setup SocketOptions
    SocketOptions options = new SocketOptions();
    options.AutoConnect = false;
    options.Reconnection = false;
    options.Timeout = TimeSpan.FromSeconds(3);
    options.ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket;

#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("authToken");
        StartCoroutine(WaitForAuthToken(options));
#else
    object authFunction(SocketManager manager, Socket socket)
    {
      return new
      {
        token = TestToken
      };
    }
    options.Auth = authFunction;
    // Proceed with connecting to the server
    SetupSocketManager(options);
#endif
  }

  private IEnumerator WaitForAuthToken(SocketOptions options)
  {
    // Wait until myAuth is not null
    while (myAuth == null)
    {
      yield return null;
    }

    // Once myAuth is set, configure the authFunction
    object authFunction(SocketManager manager, Socket socket)
    {
      return new
      {
        token = myAuth
      };
    }
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
    if (string.IsNullOrEmpty(nameSpace) | string.IsNullOrWhiteSpace(nameSpace))
    {
      gameSocket = this.manager.Socket;
    }
    else
    {
      Debug.Log("Namespace used :" + nameSpace);
      gameSocket = this.manager.GetSocket("/" + nameSpace);
    }
    // Set subscriptions
    gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
    gameSocket.On(SocketIOEventTypes.Disconnect, OnDisconnected); //Back2 Start
    gameSocket.On<Error>(SocketIOEventTypes.Error, OnError);
    gameSocket.On<string>("game:init", OnListenEvent);
    gameSocket.On<string>("result", OnListenEvent);
    gameSocket.On<string>("pong", OnPongReceived); //Back2 Start
    gameSocket.On<bool>("socketState", OnSocketState);
    gameSocket.On<string>("internalError", OnSocketError);
    gameSocket.On<string>("alert", OnSocketAlert);
    gameSocket.On<string>("AnotherDevice", OnSocketOtherDevice);

    manager.Open();
  }

  private void OnSocketState(bool state)
  {
    Debug.Log("my state is " + state);
  }

  private void OnSocketError(string data)
  {
    Debug.Log("Received error with data: " + data);
  }

  private void OnSocketAlert(string data)
  {
    Debug.Log("Received alert with data: " + data);
  }

  private void OnSocketOtherDevice(string data)
  {
    Debug.Log("Received Device Error with data: " + data);
  }

  void OnConnected(ConnectResponse resp) //Back2 Start
  {
    Debug.Log("✅ Connected to server.");

    if (hasEverConnected)
    {
      uiManager.CheckAndClosePopups();
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
        uiManager.CheckAndClosePopups();
      }

      // If waiting for pong, and timeout passed
      if (waitingForPong)
      {
        if (missedPongs == 2)
        {
          uiManager.ReconnectionPopup();
        }
        missedPongs++;
        Debug.LogWarning($"⚠️ Pong missed #{missedPongs}/{MaxMissedPongs}");

        if (missedPongs >= MaxMissedPongs)
        {
          Debug.LogError("❌ Unable to connect to server — 5 consecutive pongs missed.");
          isConnected = false;
          uiManager.DisconnectionPopup();
          yield break;
        }
      }

      // Send next ping
      waitingForPong = true;
      lastPongTime = Time.time;
      Debug.Log("📤 Sending ping...");
      SendData("ping");
      yield return new WaitForSeconds(pingInterval);
    }
  } //Back2 end

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
    JObject resp = JObject.Parse(jsonObject);

    string id = resp["id"].ToString();
    if (resp["player"] != null)
      socketModel.playerData = resp["player"].ToObject<PlayerData>();

    switch (id)
    {
      case "initData":
        {
          socketModel.uIData.symbols = resp["uiData"]["paylines"]["symbols"].ToObject<List<Symbol>>();
          socketModel.initGameData.Bets = resp["gameData"]["bets"].ToObject<List<double>>();
          // socketModel.initGameData.freeSpinCount = resp["message"]["GameData"]["FreespinBonusCount"].ToObject<int>();
          OnInit?.Invoke();
          RaycastBlocker.SetActive(false);
          break;
        }
      case "ResultData":
        {
          socketModel.resultGameData = new();
          socketModel.resultGameData = resp.ToObject<ResultGameData>();
          if (socketModel.resultGameData.payload.wins.Count > 0)
          {
            socketModel.resultGameData.symbolsToEmit = GetSymbolsToEmit(socketModel.resultGameData.payload.wins);
          }
          isResultdone = true;
          break;
        }
      case "ExitUser":
        {
          gameSocket?.Disconnect();
          manager?.Close();
#if UNITY_WEBGL && !UNITY_EDITOR
          JSManager.SendCustomMessage("OnExit");
#endif
          break;
        }
    }

  }

  internal void SendData(string eventName, object message = null)
  {
    if (gameSocket == null || !gameSocket.IsOpen)
    {
      Debug.LogWarning("Socket is not connected.");
      return;
    }
    if (message == null)
    {
      gameSocket.Emit(eventName);
      return;
    }
    isResultdone = false;
    string json = JsonConvert.SerializeObject(message);
    gameSocket.Emit(eventName, json);
    Debug.Log("JSON data sent: " + json);
  }

  List<string> GetSymbolsToEmit(List<Win> wins)
  {
    List<string> pos = new();
    foreach (Win win in wins)
    {
      foreach (List<int> posi in win.positions)
      {
        string s = posi[1].ToString() + "," + posi[0].ToString();
        pos.Add(s);
      }
    }

    return Helper.RemoveDuplicates(pos);
  }
}
