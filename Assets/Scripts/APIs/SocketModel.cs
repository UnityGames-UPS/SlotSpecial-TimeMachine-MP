using System.Collections.Generic;
using System;

public class SocketModel
{
  public PlayerData playerData;
  public UIData uIData;
  public InitGameData initGameData;
  public ResultGameData resultGameData;
  internal SocketModel()
  {
    playerData = new PlayerData();
    uIData = new UIData();
    initGameData = new InitGameData();
    resultGameData = new ResultGameData();
  }
}

[Serializable]
public class Features
{
  public FreeSpin freeSpin { get; set; }
}

[Serializable]
public class FreeSpin
{
  public int count { get; set; }
  public int level { get; set; }
  public bool isFreeSpin { get; set; }
  public bool isLevelUp { get; set; }
}

[Serializable]
public class Payload
{
  public double winAmount { get; set; }
  public List<Win> wins { get; set; }
}

[Serializable]
public class Win
{
  public List<List<int>> positions { get; set; }
}

[Serializable]
public class ResultGameData
{
  public List<List<int>> matrix { get; set; }
  public Payload payload { get; set; }
  public Features features { get; set; }
  public List<string> symbolsToEmit = new();
}

[Serializable]
public class InitGameData
{
  public List<double> Bets { get; set; }
}

[Serializable]
public class UIData
{
  public List<Symbol> symbols { get; set; }
}

[Serializable]
public class MessageData
{
  public string type = "";
  public Data payload = new();
}

[Serializable]
public class Data
{
  public int betIndex = 0;
}

[Serializable]
public class Symbol
{
  public List<double> multiplier { get; set; }
  public string description { get; set; }
}

[Serializable]
public class PlayerData
{
  public double balance { get; set; }
}

[Serializable]
public class AuthTokenData
{
  public string cookie;
  public string socketURL;
  public string nameSpace = "";
}
