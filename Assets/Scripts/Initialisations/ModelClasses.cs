// using System;
// using System.Collections.Generic;

// [Serializable]
// public class MessageData
// {
//   public string type;
//   public Data payload = new();

// }
// [Serializable]
// public class Data
// {
//   public int betIndex;
//   public string Event;
//   public int levelIndex;
//   public List<int> index;
//   public int option;
// }

// [Serializable]
// public class AuthTokenData
// {
//     public string cookie;
//     public string socketURL;
//     public string nameSpace;
// }

// public class GameData
// {
//     public List<int> Bets { get; set; }
//     public List<List<int>> resultSymbols { get; set; }
//     public bool hasReSpin { get; set; }
//     public bool hasRedSpin { get; set; }
// }

// public class Message
// {
//     public GameData GameData { get; set; }
//     public UIData UIData { get; set; }
//     public PlayerData PlayerData { get; set; }
// }

// public class Paylines
// {
//     public List<Symbol> symbols { get; set; }
// }

// public class PlayerData
// {
//     public double Balance { get; set; }
//     public double currentWining { get; set; }
//     public double totalbet { get; set; }
// }

// public class Root
// {
//     public string id { get; set; }
//     public Message message { get; set; }
//     public string username { get; set; }
// }

// public class Symbol
// {
//     public double ID { get; set; }
//     public string Name { get; set; }
//     public object payout { get; set; }
//     public object description { get; set; }
// }

// public class UIData
// {
//     public Paylines paylines { get; set; }
//     public List<object> spclSymbolTxt { get; set; }
//     public string ToULink { get; set; }
//     public string PopLink { get; set; }
// }
