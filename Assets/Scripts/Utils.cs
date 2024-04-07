using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class Utils
{
    public static void ChangeScene(string _sceneName)
    {
        var currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadSceneAsync(_sceneName);
        SceneManager.UnloadSceneAsync(currentScene);
    }

    public static string Bold(this string str) => "<b>" + str + "</b>";

    public static string Italic(this string str) => "<i>" + str + "</i>";

    public static string Underline(this string str) => "<u>" + str + "</u>";

    public static string Color(this string str, string _color) => "<color=" + _color + ">" + str + "</color>";

    public static string Size(this string str, int _size) => "<size=" + _size + ">" + str + "</size>";

    public static string Indent(this string str, int _indent = 4)
    {
        string[] strings = str.Split('\n');
        for (int i = 0; i < strings.Length; i++)
            strings[i] = new string(' ', _indent) + strings[i];

        return string.Join("\n", strings);
    }

    public static void Rect(this Painter2D painter, Rect _rect)
    {
        painter.MoveTo(new Vector2(_rect.x, _rect.y));
        painter.LineTo(new Vector2(_rect.x + _rect.width, _rect.y));
        painter.LineTo(new Vector2(_rect.x + _rect.width, _rect.y + _rect.height));
        painter.LineTo(new Vector2(_rect.x, _rect.y + _rect.height));
        painter.LineTo(new Vector2(_rect.x, _rect.y));
    }
}

public static class UnityWebRequestAsyncOperationAwaiter
{
    public static TaskAwaiter<UnityWebRequest> GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
    {
        var tcs = new TaskCompletionSource<UnityWebRequest>();
        asyncOp.completed += obj => { tcs.SetResult(((UnityWebRequestAsyncOperation)obj).webRequest); };
        return tcs.Task.GetAwaiter();
    }
}
