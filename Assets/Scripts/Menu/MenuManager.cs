using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MenuManager : MonoBehaviour
{
    private VisualElement ui;

    private void Start()
    {
        ui = FindObjectOfType<UIDocument>().rootVisualElement;

        Button stock = ui.Q<Button>("stock");
        Button budget = ui.Q<Button>("budget");
        Button gambling = ui.Q<Button>("gambling");
        Button betting = ui.Q<Button>("betting");

        stock.clicked += () => SceneManager.LoadScene("StockScene");
        budget.clicked += () => { Debug.Log("Budget button clicked"); };
        gambling.clicked += () => { Debug.Log("Gambling button clicked"); };
        betting.clicked += () => { Debug.Log("Betting button clicked"); };

        SerialInput.Init();
        SerialInput.InputEvents["left"] += () => SwicthScene("StockScene");
    }

    private void SwicthScene(string _sceneName)
    {
        SceneManager.LoadScene(_sceneName);

        // Clear left the input events
        SerialInput.InputEvents["left"] = () => { };
    }
}
