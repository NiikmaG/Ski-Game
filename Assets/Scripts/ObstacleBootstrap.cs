using UnityEngine;

public static class ObstacleBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Setup()
    {
        Initialize();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => Initialize();
    }

    static void Initialize()
    {
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (GameObject obstacle in obstacles)
        {
            if (obstacle.name.StartsWith("Snowman")) continue;

            if (obstacle.GetComponentInChildren<ObstacleDetector>() == null)
                obstacle.AddComponent<ObstacleDetector>();
        }
    }
}
