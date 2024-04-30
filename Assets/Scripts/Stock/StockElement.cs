using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class StockElement : VisualElement
{
    public new class UxmlFactory : UxmlFactory<StockElement, UxmlTraits> { }


    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        readonly UxmlIntAttributeDescription _waitTime =
            new()
            { name = "Start-Wait-Time", defaultValue = 1000 };
        readonly UxmlIntAttributeDescription _totalTime =
            new()
            { name = "Total-Time", defaultValue = 60000 };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var se = ve as StockElement;

            se.StartWaitTime = _waitTime.GetValueFromBag(bag, cc);
            se.TotalTime = _totalTime.GetValueFromBag(bag, cc);
        }
    }

    // Must expose your element class to a { get; set; } property that has the same name 
    // as the name you set in your UXML attribute description with the camel case format
    public int StartWaitTime { get; set; }
    public int TotalTime { get; set; }
    public StockManager.StockData Data { get; private set; }
    public int currentStock = 0;

    public StockElement()
    {
        Initialization();
        generateVisualContent += OnGenerateVisualContent;
    }

    public async void Initialization()
    {
        if (!Application.isPlaying) return;
        await SetData();
    }

    public async Task SetData()
    {
        Data = await StockManager.GetRandomStockData();
        if (Data == null) return;
        Debug.Log(Data.ToString());

        // Trigger a repaint
        MarkDirtyRepaint();
    }

    private void OnGenerateVisualContent(MeshGenerationContext _context)
    {
        float width = contentRect.width;
        float height = contentRect.height;

        var ctx = _context.painter2D;

        // Background
        int count = Data == null ? 22 : Data.Stocks.Count + 1;
        for (int i = 0; i < count; i++)
        {
            float x = i * width / count;

            ctx.fillColor = i % 2 == 0 ? new Color32(27, 29, 30, 255) : new Color32(24, 26, 27, 255);
            ctx.BeginPath();
            int rectWidth = Mathf.CeilToInt(width / count) + 1;
            ctx.Rect(new Rect(x, 0, rectWidth, height));
            ctx.Fill();
        }

        if (Data == null) return;
        // Draw the graph
        ctx.BeginPath();
        ctx.lineWidth = 4;
        for (int i = 0; i < Data.Stocks.Count; i++)
        {
            var stock = Data.Stocks[i];
            float x = (i + 1) * width / (Data.Stocks.Count + 1);
            float y = height - ((stock.Close - Data.MinLowest) / (Data.MaxHighest - Data.MinLowest) * height);
            Vector2 center = new(x, y);

            ctx.strokeColor = new Color32(70, 78, 86, 255);
            if (i == 0) ctx.MoveTo(center);
            else ctx.LineTo(center);
        }
        ctx.Stroke();

        ctx.lineWidth = 8;
        for (int i = 0; i < Data.Stocks.Count; i++)
        {
            var stock = Data.Stocks[i];
            float x = (i + 1) * width / (Data.Stocks.Count + 1);

            float barTop = height - ((stock.Highest - Data.MinLowest) / (Data.MaxHighest - Data.MinLowest) * height);
            float barBottom = height - ((stock.Lowest - Data.MinLowest) / (Data.MaxHighest - Data.MinLowest) * height);

            // Color based on comparison with previous day
            if (i > 0) ctx.strokeColor = stock.Close > Data.Stocks[i - 1].Close ? new Color32(7, 131, 76, 255) : new Color32(186, 44, 49, 255);
            else ctx.strokeColor = new Color32(186, 44, 49, 255);

            ctx.BeginPath();
            ctx.MoveTo(new Vector2(x, barBottom));
            ctx.LineTo(new Vector2(x, barTop));
            ctx.Stroke();
        }

        // Draw horizontal line at current stock close price
        if (currentStock < Data.Stocks.Count)
        {
            var stock = Data.Stocks[currentStock];
            float y = height - ((stock.Close - Data.MinLowest) / (Data.MaxHighest - Data.MinLowest) * height);
            ctx.strokeColor = new Color32(255, 255, 255, 255);
            ctx.lineWidth = 2;
            ctx.BeginPath();
            ctx.MoveTo(new Vector2(0, y));
            ctx.LineTo(new Vector2(width, y));
            ctx.Stroke();
        }
    }
}
