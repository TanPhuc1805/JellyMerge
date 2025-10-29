using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Script helper để xử lý việc tải và tải lại scene.
/// Gắn script này vào _UIManager.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    /// <summary>
    /// Tải lại scene hiện tại
    /// </summary>
    public void ReloadScene()
    {
        // Lấy build index của scene hiện tại và tải lại nó
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    /// <summary>
    /// Tải màn chơi tiếp theo (dựa trên Build Index)
    /// </summary>
    public void LoadNextLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        // Kiểm tra xem có màn tiếp theo không
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            // Nếu không còn màn, quay về màn 0 (Menu)
            Debug.Log("Đã hoàn thành tất cả các màn! Quay về Menu...");
            SceneManager.LoadScene(0); 
        }
    }

    // (Tùy chọn) Hàm này hữu ích nếu bạn muốn tải 1 scene cụ thể
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}