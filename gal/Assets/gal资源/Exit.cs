using UnityEngine;

public class PacmanExitButton : MonoBehaviour
{
    public void BackToVN()
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.UnloadMiniGame();
    }
}