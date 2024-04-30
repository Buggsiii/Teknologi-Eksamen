using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StockManager))]
public class StockManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        StockManager stockManager = (StockManager)target;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Enable All Stocks"))
            foreach (StockManager.StockReference stock in stockManager.stocks)
                stock.enabled = true;

        if (GUILayout.Button("Disable All Stocks"))
            foreach (StockManager.StockReference stock in stockManager.stocks)
                stock.enabled = false;
        GUILayout.EndHorizontal();
    }
}