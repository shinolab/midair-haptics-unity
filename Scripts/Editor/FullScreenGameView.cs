using UnityEngine;
using UnityEditor;

// GameViewをフルスクリーンで表示するスクリプト(WindowsはF11、macOSはCommand+Shift+Fでトグル動作)
public class FullScreenGameView
{
    const string menuPath = "Window/Game (Full Screen)";

#if UNITY_EDITOR_WIN
    [MenuItem(menuPath + " _F11", false, 2001)]
#elif UNITY_EDITOR_OSX
    [MenuItem(menuPath + " %#f", false, 2001)]
#endif
    public static void Execute()
    {
        EditorWindow gameView = GetGameView();

        if (Menu.GetChecked(menuPath) == false)
        {
            gameView.Close();       // ドッキング中にサイズを変えるとEditorのサイズも変わってしまうため一旦閉じる

            float width = Screen.currentResolution.width;
            float height = Screen.currentResolution.height;
            float offset = 17.0f;   // GameViewのコントロールバーの高さ(Unity2017.1の場合) ※タブや枠は計算に入れない

            gameView = GetGameView();
            gameView.minSize = new Vector2(width, height + offset);
            gameView.position = new Rect(0, -offset, width, height + offset);

            Menu.SetChecked(menuPath, true);
        }
        else
        {
            // 位置パラメータをデフォルトに戻してからClose
            gameView.minSize = minSize;
            gameView.position = position;
            gameView.Close();

            Menu.SetChecked(menuPath, false);
        }
    }

    private static EditorWindow GetGameView()
    {
        // ウィンドウが存在しない場合は生成される
        return EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor"));
    }

    // デフォルト位置パラメータ(元の位置には戻せないので、扱いやすい位置＆サイズにしておく)
    private static Vector2 minSize = new Vector2(100.0f, 100.0f);
    private static Rect position = new Rect(0.0f, 0.0f, 640.0f, 480.0f);
}

