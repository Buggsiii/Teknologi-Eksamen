using Unity.Mathematics;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

public class StockElement : VisualElement
{
    public new class UxmlFactory : UxmlFactory<StockElement, UxmlTraits> { }

    private StockManager.StockData data;

    public StockElement()
    {
        Initialize();
        generateVisualContent += OnGenerateVisualContent;
    }

    public async void Initialize()
    {
        data = await StockManager.GetStockData("NVDA", "2024-01-01", "2024-02-01");
        if (data == null) return;
        Debug.Log(data.ToString());

        // Trigger a repaint
        MarkDirtyRepaint();
    }

    private void OnGenerateVisualContent(MeshGenerationContext _context)
    {
        if (data == null) return;

        float width = contentRect.width;
        float height = contentRect.height;

        var ctx = _context.painter2D;

        // Background
        for (int i = 0; i < data.Stocks.Count + 1; i++)
        {
            float x = i * width / (data.Stocks.Count + 1);

            ctx.fillColor = i % 2 == 0 ? new Color32(27, 29, 30, 255) : new Color32(24, 26, 27, 255);
            ctx.BeginPath();
            int rectWidth = Mathf.CeilToInt(width / (data.Stocks.Count + 1)) + 1;
            ctx.Rect(new Rect(x, 0, rectWidth, height));
            ctx.Fill();
        }

        // Draw the graph
        ctx.BeginPath();
        ctx.lineWidth = 4;
        for (int i = 0; i < data.Stocks.Count; i++)
        {
            var stock = data.Stocks[i];
            float x = (i + 1) * width / (data.Stocks.Count + 1);
            float y = height - ((stock.Close - data.MinLowest) / (data.MaxHighest - data.MinLowest) * height);
            Vector2 center = new(x, y);

            ctx.strokeColor = new Color32(70, 78, 86, 255);
            if (i == 0) ctx.MoveTo(center);
            else ctx.LineTo(center);
        }
        ctx.Stroke();

        ctx.lineWidth = 8;
        for (int i = 0; i < data.Stocks.Count; i++)
        {
            var stock = data.Stocks[i];
            float x = (i + 1) * width / (data.Stocks.Count + 1);
            float y = height - ((stock.Close - data.MinLowest) / (data.MaxHighest - data.MinLowest) * height);
            Vector2 center = new(x, y);

            float barTop = height - ((stock.Highest - data.MinLowest) / (data.MaxHighest - data.MinLowest) * height);
            float barBottom = height - ((stock.Lowest - data.MinLowest) / (data.MaxHighest - data.MinLowest) * height);

            // Color based on comparison with previous day
            if (i > 0) ctx.strokeColor = stock.Close > data.Stocks[i - 1].Close ? new Color32(7, 131, 76, 255) : new Color32(186, 44, 49, 255);
            else ctx.strokeColor = new Color32(186, 44, 49, 255);

            ctx.BeginPath();
            ctx.MoveTo(new Vector2(x, barBottom));
            ctx.LineTo(new Vector2(x, barTop));
            ctx.Stroke();
        }

        ctx.strokeColor = new Color32(70, 78, 86, 255);
        ctx.lineWidth = 4;
        ctx.BeginPath();
        ctx.Rect(new Rect(0, 0, width, height));
        ctx.ClosePath();
        ctx.Stroke();
    }
}
