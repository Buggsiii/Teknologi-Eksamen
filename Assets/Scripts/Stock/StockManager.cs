using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Collections;
using UnityEngine.SceneManagement;

public class StockManager : MonoBehaviour
{
    public static StockManager Instance;
    private const string API = "aMmCh_fZV_wzh53J6Hw_MWqN04oaRwH9";
    private UIDocument ui;
    StockElement stockElement;
    VisualElement modal;
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
                ui.rootVisualElement.Q<Label>("balance").text = $"{balance:n0}DKK";
            }
        }
    }

    public StockReference[] stocks;

    private void Awake() => Instance = this;

    private void Start()
    {
        ui = FindObjectOfType<UIDocument>();
        stockElement = ui.rootVisualElement.Q<StockElement>();
        modal = ui.rootVisualElement.Q<VisualElement>("modal");

        Balance = 10000;
        StartRound();

        ui.rootVisualElement.Q<Button>("invest").clicked += Invest;
        ui.rootVisualElement.Q<Button>("sell").clicked += Sell;
    }

    public static async Task<StockData> GetRandomStockData()
    {
        Debug.Log("Getting random stock data");
        Debug.Log(Instance);
        StockReference[] enabledStocks = Instance.stocks.Where(stock => stock.enabled).ToArray();
        StockReference stock = enabledStocks[UnityEngine.Random.Range(0, enabledStocks.Length)];
        StockData data = await GetStockData(stock.Symbol, stock.From, stock.To);
        return data;
    }

    public IEnumerator StockElementAnimation()
    {
        float seconds = 60f;

        float startTime = Time.time;
        while (Time.time - startTime < seconds)
        {
            float t = (Time.time - startTime) / seconds;
            float width = stockElement.contentRect.width;
            stockElement.parent.style.left = Mathf.Lerp(0, -width * 1f, t);
            yield return null;
        }

        SellAll();
        float profit = Balance - 10000;
        Label profitLabel = modal.Q<Label>("profit");
        profitLabel.style.color = profit > 0 ? new Color(0.03f, 0.51f, 0.3f) : new Color(0.73f, 0.17f, 0.19f);
        profitLabel.text = $"{profit:n0}DKK";
        modal.AddToClassList("show");

        SerialInput.InputEvents["left"] -= Invest;
        SerialInput.InputEvents["right"] -= Sell;
        SerialInput.InputEvents["arm"] += StartRound;
        SerialInput.InputEvents["mid"] += ToMenu;
    }

    private async void StartRound()
    {
        SerialInput.InputEvents["arm"] -= StartRound;
        stockElement.currentStock = 0;
        await stockElement.SetData();
        modal.RemoveFromClassList("show");
        StartCoroutine(StockElementAnimation());
        StartCoroutine(UpdateStockElement());
        SerialInput.InputEvents["left"] += Invest;
        SerialInput.InputEvents["right"] += Sell;
    }

    private void ToMenu()
    {
        StopAllCoroutines();

        SerialInput.InputEvents["left"] -= Invest;
        SerialInput.InputEvents["right"] -= Sell;
        SerialInput.InputEvents["arm"] -= StartRound;
        SerialInput.InputEvents["mid"] -= ToMenu;

        SceneManager.LoadScene(0);
    }

    public IEnumerator UpdateStockElement()
    {
        VisualElement xAxis = ui.rootVisualElement.Q<VisualElement>("x-axis");
        xAxis.style.marginLeft = xAxis.contentRect.width / (stockElement.Data.Stocks.Count + 1) * .5f;
        xAxis.Clear();

        for (int i = 0; i < stockElement.Data.Stocks.Count; i++)
        {
            Stock stock = stockElement.Data.Stocks[i];
            Stock prevStock = i > 0 ? stockElement.Data.Stocks[i - 1] : stock;

            bool isUp = stock.Close >= prevStock.Close;
            Label label = new(stock.Close.ToString("n2"))
            {
                text = $"{stock.Date:dd/MM}\n{(isUp ? '▲' : '▼')}{stock.Close * 6.98f:n0}DKK",
                style = { color =
                    isUp ?
                        new Color(0.03f, 0.51f, 0.3f) :
                        new Color(0.73f, 0.17f, 0.19f)
                }
            };
            label.style.width = xAxis.contentRect.width / (stockElement.Data.Stocks.Count + 1);

            xAxis.Add(label);
        }

        yield return null;
        stockElement.RemoveFromClassList("offset");

        currentCost = stockElement.Data.Stocks[0].Close * 6.98f;

        yield return new WaitForSeconds(stockElement.StartWaitTime / 1000f);

        while (stockElement.currentStock < stockElement.Data.Stocks.Count)
        {
            yield return new WaitForSeconds(stockElement.TotalTime / stockElement.Data.Stocks.Count / 1000f);
            if (stockElement.currentStock < stockElement.Data.Stocks.Count - 1)
                currentCost = stockElement.Data.Stocks[stockElement.currentStock + 1].Close * 6.98f;
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

    private void SellAll()
    {
        if (currentCost == 0) return;
        Balance += currentCost * boughtStocks;
        boughtStocks = 0;
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

    [Serializable]
    public class StockReference
    {
        public string Symbol;
        public string From;
        public string To;
        public bool enabled = true;

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