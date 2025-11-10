using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelFlowManager : MonoBehaviour
{
    [Header("Rooms (prefabs) in order")]
    public List<GameObject> roomPrefabs;

    [Header("UI")]
    public GameObject startScreen;
    public GameObject endScreen;
    public Button startButton;
    public Button restartButton;

    [Header("Audio")]
    public AudioSource sfx;
    public AudioClip levelWinClip;
    public AudioClip gameWinClip;


    private int currentIndex = -1;
    private GameObject currentRoom;
    private GoalWatcher currentWatcher;

    private bool isLoading = false;
    private bool gameEnded = false;

    void Start()
    {
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);

        // hide end, show start
        if (endScreen) endScreen.SetActive(false);
        if (startScreen) startScreen.SetActive(true);
    }

    void Update()
    {
        if (!isLoading && !gameEnded && currentRoom != null && Input.GetKeyDown(KeyCode.R))
        {
            ReloadCurrentLevel();
        }
    }

    public void StartGame()
    {
        if (isLoading) return;
        gameEnded = false;
        if (startScreen) startScreen.SetActive(false);
        LoadLevelAtIndex(0);
    }

    public void RestartGame()
    {
        if (isLoading) return;
        gameEnded = false;
        if (endScreen) endScreen.SetActive(false);
        LoadLevelAtIndex(0);
    }

    void LoadNextLevel()
    {
        LoadLevelAtIndex(currentIndex + 1);
    }

    void ReloadCurrentLevel()
    {
        LoadLevelAtIndex(currentIndex);
    }

    void LoadLevelAtIndex(int index)
    {
        if (isLoading) return;
        StartCoroutine(LoadLevelRoutine(index));
    }

    IEnumerator LoadLevelRoutine(int index)
    {
        isLoading = true;

        if (index >= roomPrefabs.Count)
        {
            gameEnded = true;

            if (currentRoom != null)
            {
                Destroy(currentRoom);
                currentRoom = null;
                yield return null;
            }

            if (sfx && gameWinClip) sfx.PlayOneShot(gameWinClip);
            if (endScreen) endScreen.SetActive(true);
            isLoading = false;
            yield break;
        }

        if (index < 0) index = 0;

        if (currentRoom != null)
        {
            Destroy(currentRoom);
            currentRoom = null;
            currentWatcher = null;
            yield return null;
        }



        currentIndex = index;
        var prefab = roomPrefabs[currentIndex];
        currentRoom = Instantiate(prefab);


        var gm = currentRoom.GetComponentInChildren<GridManager>();
        if (gm == null)
        {
            Debug.LogError("[LevelFlow] No GridManager found in room prefab!");
        }
        else
        {
            currentWatcher = gm.GetComponent<GoalWatcher>();
            if (currentWatcher == null) currentWatcher = gm.gameObject.AddComponent<GoalWatcher>();
            currentWatcher.OnLevelComplete = OnLevelComplete;
        }


        if (endScreen) endScreen.SetActive(false);

        isLoading = false;
        yield break;
    }

    void OnLevelComplete()
    {
        if (isLoading || gameEnded) return;
        if (sfx && levelWinClip) sfx.PlayOneShot(levelWinClip);
        StartCoroutine(NextAfterDelay(0.35f));
    }

    IEnumerator NextAfterDelay(float t)
    {
        isLoading = true;
        yield return new WaitForSeconds(t);
        isLoading = false;
        LoadNextLevel();
    }
}
