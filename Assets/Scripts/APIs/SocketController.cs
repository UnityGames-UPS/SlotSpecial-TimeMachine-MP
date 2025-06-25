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
  internal SocketModel socketModel = new SocketModel();
  [SerializeField] internal bool isResultdone = false;
  private SocketManager manager;
  protected string SocketURI = null;
  // protected string TestSocketURI = "https://game-crm-rtp-backend.onrender.com/";
  // protected string TestSocketURI = "https://7p68wzhv-5000.inc1.devtunnels.ms/";
  protected string TestSocketURI = "https://sl3l5zz3-5000.inc1.devtunnels.ms/";
  //protected string SocketURI = "http://localhost:5000";
  [SerializeField] private string TestToken;
  [SerializeField] internal JSFunctCalls JSManager;
  private Socket gameSocket;
  protected string nameSpace = "playground";
  protected string gameID = "SL-TM";
  // protected string gameID = "";
  internal bool isLoading;
  internal bool SetInit = false;
  private const int maxReconnectionAttempts = 6;
  private readonly TimeSpan reconnectionDelay = TimeSpan.FromSeconds(10);
  internal Action OnInit;
  internal Action ShowDisconnectionPopup;

  private void Awake()
  {
    isLoading = true;
    SetInit = false;
    // Debug.unityLogger.logEnabled = false;
  }

  private void Start()
  {
    //OpenWebsocket();
    // OpenSocket();
  }

  void ReceiveAuthToken(string jsonData)
  {
    Debug.Log("Received data: " + jsonData);

    // Parse the JSON data
    var data = JsonUtility.FromJson<AuthTokenData>(jsonData);
    SocketURI = data.socketURL;
    myAuth = data.cookie;
    nameSpace = data.nameSpace;
    // Proceed with connecting to the server using myAuth and socketURL
  }

  string myAuth = null;

  internal void OpenSocket()
  {
    // Create and setup SocketOptions
    SocketOptions options = new SocketOptions();
    options.ReconnectionAttempts = maxReconnectionAttempts;
    options.ReconnectionDelay = reconnectionDelay;
    options.Reconnection = true;
    options.ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket;

    // #if UNITY_WEBGL && !UNITY_EDITOR
    //     JSManager.SendCustomMessage("authToken");
    //     StartCoroutine(WaitForAuthToken(options));
    // #else
    //     Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    //     {
    //       return new
    //       {
    //         token = TestToken
    //       };
    //     };
    //     options.Auth = authFunction;
    //     // Proceed with connecting to the server
    //     SetupSocketManager(options);
    // #endif
#if UNITY_WEBGL && !UNITY_EDITOR
    string url = Application.absoluteURL;
    Debug.Log("Unity URL : " + url);
    ExtractUrlAndToken(url);

    Func<SocketManager, Socket, object> webAuthFunction = (manager, socket) =>
    {
      return new
      {
        token = TestToken,
      };
    };
    options.Auth = webAuthFunction;
#else
    Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    {
      return new
      {
        token = TestToken,
      };
    };
    options.Auth = authFunction;
#endif
    // Proceed with connecting to the server
    SetupSocketManager(options);
  }

  private IEnumerator WaitForAuthToken(SocketOptions options)
  {
    // Wait until myAuth is not null
    while (myAuth == null)
    {
      yield return null;
    }

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
  private void OnSocketState(bool state)
  {
    if (state)
    {
      Debug.Log("my state is " + state);
      InitRequest("AUTH");
    }
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

  private void AliveRequest()
  {
    SendData("YES I AM ALIVE");
  }

  void OnConnected(ConnectResponse resp)
  {
    Debug.Log("Connected!");
    SendPing();
  }

  private void SendPing()
  {
    InvokeRepeating("AliveRequest", 0f, 3f);
  }

  private void OnDisconnected(string response)
  {
    Debug.Log("Disconnected from the server");
    StopAllCoroutines();
    ShowDisconnectionPopup?.Invoke();
  }

  private void OnError(string response)
  {
    Debug.LogError("Error: " + response);
  }

  private void OnListenEvent(string data)
  {
    ParseResponse(data);
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
    gameSocket.On<string>(SocketIOEventTypes.Disconnect, OnDisconnected);
    gameSocket.On<string>(SocketIOEventTypes.Error, OnError);
    gameSocket.On<string>("game:init", OnListenEvent);
    gameSocket.On<string>("spin:result", OnListenEvent);
    gameSocket.On<bool>("socketState", OnSocketState);
    gameSocket.On<string>("internalError", OnSocketError);
    gameSocket.On<string>("alert", OnSocketAlert);
    gameSocket.On<string>("AnotherDevice", OnSocketOtherDevice);
  }

  // Connected event handler implementation

  private void InitRequest(string eventName)
  {
    var initmessage = new { Data = new { GameID = gameID }, id = "Auth" };
    SendData(eventName, initmessage);
  }

  internal void CloseSocket()
  {
    SendData("game:exit");
    gameSocket.Disconnect();
    if (this.manager != null)
    {
      this.manager.Close();
    }
#if UNITY_WEBGL && !UNITY_EDITOR
      JSManager.SendCustomMessage("OnExit");
#endif
  }

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
          gameSocket.Disconnect();
          if (this.manager != null)
          {
            this.manager.Close();
          }
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

  public void ExtractUrlAndToken(string fullUrl)
  {
    Uri uri = new Uri(fullUrl);
    string query = uri.Query; // Gets the query part, e.g., "?url=http://localhost:5000&token=e5ffa84216be4972a85fff1d266d36d0"

    Dictionary<string, string> queryParams = new Dictionary<string, string>();
    string[] pairs = query.TrimStart('?').Split('&');

    foreach (string pair in pairs)
    {
      string[] kv = pair.Split('=');
      if (kv.Length == 2)
      {
        queryParams[kv[0]] = Uri.UnescapeDataString(kv[1]);
      }
    }

    if (queryParams.TryGetValue("url", out string extractedUrl) &&
        queryParams.TryGetValue("token", out string token))
    {
      Debug.Log("Extracted URL: " + extractedUrl);
      Debug.Log("Extracted Token: " + token);
      TestToken = token;
      SocketURI = extractedUrl;
    }
    else
    {
      Debug.LogError("URL or token not found in query parameters.");
    }
  }
}



