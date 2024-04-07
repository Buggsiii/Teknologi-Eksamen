using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Linq;

public class StockManager : MonoBehaviour
{
    private const string API = "aMmCh_fZV_wzh53J6Hw_MWqN04oaRwH9";

    /// <summary>
    /// Get stock data from Polygon API.
    /// </summary>
    /// <param name="_symbol">
    /// The stock's symbol.
    /// </param>
    /// <param name="_from">
    /// Start date in the format "yyyy-MM-dd".
    /// </param>
    /// <param name="_to">
    /// End date in the format "yyyy-MM-dd".
    /// </param>
    /// <returns>
    /// The stock data of the given symbol.
    /// </returns>
    public static async Task<StockData> GetStockData(string _symbol, string _from, string _to)
    {
        string url = $"https://api.polygon.io/v2/aggs/ticker/{_symbol}/range/1/day/{_from}/{_to}?adjusted=true&sort=asc&limit=120&apiKey={API}";
        UnityWebRequest req = UnityWebRequest.Get(url);
        await req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + req.error);
            await Task.Delay(60100);
            return await GetStockData(_symbol, _from, _to);
        }

        string json = req.downloadHandler.text;
        return StockData.CreateFromJSON(json);
    }

    public class StockData
    {
        [JsonProperty("ticker")]
        public string Symbol;
        [JsonProperty("queryCount")]
        public int QueryCount;
        [JsonProperty("resultsCount")]
        public int ResultsCount;
        [JsonProperty("adjusted")]
        public bool Adjusted;
        [JsonProperty("results")]
        public List<Stock> Stocks;

        public float MaxHighest => Stocks.Max(stock => stock.Highest);
        public float MinLowest => Stocks.Min(stock => stock.Lowest);
        public DateTime StartDate => Stocks.Min(stock => stock.Date);
        public DateTime EndDate => Stocks.Max(stock => stock.Date);

        public static StockData CreateFromJSON(string _json)
            => JsonConvert.DeserializeObject<StockData>(_json);

        public override string ToString()
        {
            if (this == null) return "No data found.".Bold().Color("red");
            string str = "Symbol: " + Symbol + "\n";
            str += "QueryCount: " + QueryCount + "\n";
            str += "ResultsCount: " + ResultsCount + "\n";
            str += "Adjusted: " + Adjusted + "\n\n";

            str += "MaxHighest: " + MaxHighest + "\n";
            str += "MinLowest: " + MinLowest + "\n";
            str += "StartDate: " + StartDate + "\n";
            str += "EndDate: " + EndDate + "\n\n";

            if (Stocks == null) return str += "No stocks found.".Bold().Color("red");

            foreach (var stock in Stocks)
            {
                str += (stock.Date.ToString("yyyy-MM-dd HH:mm:ss") + ":").Bold() + "\n";
                str += stock.ToString().Indent() + "\n";
            }
            return str;
        }
    }

    public class Stock
    {
        [JsonProperty("c")]
        public float Close;
        [JsonProperty("h")]
        public float Highest;
        [JsonProperty("l")]
        public float Lowest;
        [JsonProperty("n")]
        public int NumberOfTrades;
        [JsonProperty("o")]
        public float Open;
        [JsonProperty("t")]
        public string DateString;
        [JsonProperty("v")]
        public float Volume;
        [JsonProperty("vw")]
        public float VolumeWeightedAverage;

        public DateTime Date
        {
            get
            {
                long time = long.Parse(DateString);
                DateTime dt = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return dt.AddMilliseconds(time);
            }
        }


        public override string ToString()
        {
            string str = "Close: " + Close + "\n";
            str += "Highest: " + Highest + "\n";
            str += "Lowest: " + Lowest + "\n";
            str += "NumberOfTrades: " + NumberOfTrades + "\n";
            str += "Open: " + Open + "\n";
            str += "Time: " + Date + "\n";
            str += "Volume: " + Volume + "\n";
            str += "VolumeWeightedAverage: " + VolumeWeightedAverage + "\n";
            return str;
        }
    }
}