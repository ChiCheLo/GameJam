using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Level");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 1. 回到主選單場景 (NewGame)
    public void GoToMainMenu()
    {
        SceneManager.LoadScene("NewGame");
    }

    // 2. 載入指定名稱的場景 (可自訂名稱)
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // 3. 關閉暫停選單以回到遊戲畫面 (隱藏 UI)
    public void ResumeGame(GameObject pauseMenu)
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }
    }
}
