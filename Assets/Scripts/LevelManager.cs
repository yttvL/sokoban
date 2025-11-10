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

    [Header("Optional Parent for spawned rooms")]
    public Transform roomParent; // create an empty GameObject in the scene and assign here

    private int currentIndex = -1;
    private GameObject currentRoom;
    private GoalWatcher currentWatcher;

    private bool isLoading = false;   // <¡ª prevents double loads (duplication)
    private bool gameEnded = false;   // <¡ª avoids extra loads after end

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

        // clamp / finish
        if (index >= roomPrefabs.Count)
        {
            gameEnded = true;

            // destroy old room cleanly
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

        // 1) destroy old
        if (currentRoom != null)
        {
            Destroy(currentRoom);
            currentRoom = null;
            currentWatcher = null;
            yield return null; // let Unity actually remove it this frame
        }

        // safety: also clear any leftover children under roomParent
        if (roomParent != null)
        {
            for (int i = roomParent.childCount - 1; i >= 0; i--)
                Destroy(roomParent.GetChild(i).gameObject);
            yield return null;
        }

        // 2) instantiate new
        currentIndex = index;
        var prefab = roomPrefabs[currentIndex];
        currentRoom = (roomParent == null)
            ? Instantiate(prefab)
            : Instantiate(prefab, roomParent);

        // 3) wire goal watcher
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

        // 4) UI cleanup
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
