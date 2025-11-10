using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalWatcher : MonoBehaviour
{
    [Tooltip("Extra frames/seconds to wait after blocks appear idle, to catch slide chains.")]
    public int settleFrames = 2;
    public float settleSeconds = 0.02f;

    public System.Action OnLevelComplete;

    private GridManager grid;
    private Coroutine settleRoutine;
    private bool completionFired = false;

    void Awake()
    {
        grid = GetComponent<GridManager>();
    }

    void OnEnable()
    {
        // optional: initial pass if the level starts solved
        StartSettleCheck();
    }

    // ------------------------------
    // These are invoked via BroadcastMessage from the provided scripts:
    // - Block.CheckMove() triggers "BlockMoved"
    // - GridManager.UpdateGrid() triggers "GridChanged"
    // We *do not* edit those scripts ¡ª we just listen.
    // ------------------------------
    void GridChanged() { StartSettleCheck(); }
    void BlockMoved(Vector2Int _) { StartSettleCheck(); }

    void StartSettleCheck()
    {
        if (completionFired) return; // avoid double win
        if (settleRoutine != null) StopCoroutine(settleRoutine);
        settleRoutine = StartCoroutine(CoWaitForSettleThenCheck());
    }

    IEnumerator CoWaitForSettleThenCheck()
    {
        // 1) Wait until all Blocks are idle
        yield return StartCoroutine(CoWaitUntilAllBlocksIdle());

        // 2) Small extra settle to catch Slidey re-triggers
        for (int i = 0; i < Mathf.Max(0, settleFrames); i++)
            yield return null;
        if (settleSeconds > 0f)
            yield return new WaitForSeconds(settleSeconds);

        // 3) Confirm still idle (defensive)
        if (!AllBlocksIdle()) yield break;

        // 4) Now do the actual goal test once
        if (AllGoalsSatisfied())
        {
            completionFired = true;
            OnLevelComplete?.Invoke();
        }
    }

    IEnumerator CoWaitUntilAllBlocksIdle()
    {
        // Keep yielding frames while any Block is not idle
        while (!AllBlocksIdle())
            yield return null;
    }

    bool AllBlocksIdle()
    {
        // Use the new API to avoid deprecation warnings
        var blocks = UnityEngine.Object.FindObjectsByType<Block>(FindObjectsSortMode.None);
        foreach (var b in blocks)
        {
            if (b == null) continue;

            // We rely on the provided Block's public state.
            // If your Block exposes enums like MoveStates.idle, attemptingMove, moving:
            if (b.State != Block.MoveStates.idle)
                return false;
        }
        return true;
    }

    bool AllGoalsSatisfied()
    {
        var goals = GameObject.FindGameObjectsWithTag("Goal");
        if (goals == null || goals.Length == 0) return false;

        foreach (var g in goals)
        {
            var cell = FindNearestCell(g.transform.position);
            if (cell == null) return false;

            if (!cell.CheckContainObj()) return false;

            // Only count blocks (per assignment)
            var go = cell.ContainObj;
            if (go == null || !go.CompareTag("block")) return false;

            // Optional extra safety: ensure the occupant is also idle
            var blk = go.GetComponent<Block>();
            if (blk != null && blk.State != Block.MoveStates.idle)
                return false;
        }
        return true;
    }

    Cell FindNearestCell(Vector3 worldPos)
    {
        if (grid == null || grid.gridList == null || grid.gridList.Count == 0) return null;

        List<List<GameObject>> list = grid.gridList;
        Cell best = null;
        float bestDist = float.MaxValue;

        for (int x = 0; x < list.Count; x++)
        {
            var col = list[x];
            for (int y = 0; y < col.Count; y++)
            {
                var cellGO = col[y];
                if (cellGO == null) continue;

                var c = cellGO.GetComponent<Cell>();
                if (c == null) continue;

                float d = (c.transform.position - worldPos).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = c; }
            }
        }
        return best;
    }
}
