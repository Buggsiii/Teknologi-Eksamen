using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Collections;

public class StockManager : MonoBehaviour
{
    public static StockManager Instance;
    private const string API = "aMmCh_fZV_wzh53J6Hw_MWqN04oaRwH9";
    private UIDocument ui;
    StockElement stockElement;
    private float currentCost;
    private int boughtStocks = 0;

    private float balance;
    public float Balance
    {
        get { return balance; }
        set
        {
            if (balance != value)
            {
                balance = value;
                ui.rootVisualElement.Q<Label>("balance").text = $"${balance:n0}";
            }
        }
    }


    private static readonly StockReference[] Stocks = new StockReference[]
    {
        new ("AAPL", "2023-01-01"),
        new ("TSLA", "2023-01-01"),
        new ("AMZN", "2023-01-01"),
        new ("GOOG", "2023-01-01"),
        new ("MSFT", "2023-01-01"),
        new ("NVDA", "2023-01-01"),
        new ("NFLX", "2023-01-01"),
        new ("AMD", "2023-01-01"),
    };

    private void Awake() => Instance = this;

    private void Start()
    {
        ui = FindObjectOfType<UIDocument>();
        Balance = 10000;
        stockElement = ui.rootVisualElement.Q<StockElement>();
        StartCoroutine(UpdateStockElement());

        ui.rootVisualElement.Q<Button>("invest").clicked += Invest;
        ui.rootVisualElement.Q<Button>("sell").clicked += Sell;
        SerialInput.InputEvents["right"] += Invest;
        SerialInput.InputEvents["left"] += Sell;
    }

    public static async Task<StockData> GetRandomStockData()
    {
        StockReference stock = Stocks[UnityEngine.Random.Range(0, Stocks.Length)];
        StockData data = await GetStockData(stock.Symbol, stock.From, stock.To);
        return data;
    }

    public IEnumerator UpdateStockElement()
    {
        yield return null;
        stockElement.RemoveFromClassList("offset");

        yield return new WaitForSeconds(stockElement.StartWaitTime / 1000f);

        while (stockElement.currentStock < stockElement.Data.Stocks.Count)
        {
            yield return new WaitForSeconds(stockElement.TotalTime / stockElement.Data.Stocks.Count / 1000f);
            currentCost = stockElement.Data.Stocks[stockElement.currentStock].Close;
            stockElement.currentStock++;
            stockElement.MarkDirtyRepaint();
        }
    }

    private void Invest()
    {
        if (currentCost == 0) return;
        if (Balance < currentCost) return;
        Balance -= currentCost;
        boughtStocks++;
    }

    private void Sell()
    {
        if (currentCost == 0) return;
        if (boughtStocks < 1) return;
        Balance += currentCost;
        boughtStocks--;
    }

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
        Debug.Log(url);
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

    private class StockReference
    {
        public string Symbol;
        public string From;
        public string To;

        public StockReference(string _symbol, string _from, string _to)
        {
            Symbol = _symbol;
            From = _from;
            To = _to;
        }

        public StockReference(string _symbol, string _from)
        {
            Symbol = _symbol;
            From = _from;

            // Add two months to the start date
            DateTime date = DateTime.Parse(_from);
            date = date.AddMonths(2);
            To = date.ToString("yyyy-MM-dd");

            // If the date is in the future, set it to today
            if (date > DateTime.Now) To = DateTime.Now.ToString("yyyy-MM-dd");
        }

        public override string ToString() => $"{Symbol} from {From} to {To}";
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

        public float MaxHighest => Stocks != null ? Stocks.Max(stock => stock.Highest) : 0;
        public float MinLowest => Stocks != null ? Stocks.Min(stock => stock.Lowest) : 0;
        public DateTime StartDate => Stocks != null ? Stocks.Min(stock => stock.Date) : DateTime.Now.AddMonths(-2);
        public DateTime EndDate => Stocks != null ? Stocks.Max(stock => stock.Date) : DateTime.Now;

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