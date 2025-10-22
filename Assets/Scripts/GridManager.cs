using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GridManager: creates a grid of cells, handles input (Input System),
/// manages the simulation.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid")]
    public int width;
    public int height;
    public float cellSize = 0.3f;  // step between cell centers
    public GameObject cellPrefab;

    public enum GameMode { Classic, PvP }
    [Header("Game Mode")]
    public GameMode gameMode = GameMode.PvP;

    [Header("Visual")]
    [Range(0.5f, 1f)]
    public float visualScale = 0.92f;   // for borders
    public Color borderColor = new Color(0.08f, 0.08f, 0.12f);

    [Header("Background")]
    public Sprite backgroundSprite;
    private GameObject backgroundGO;

    [Header("Camera Zoom")]
    public float minZoom = 2;
    public float maxZoom = 5f;
    public float zoomSpeed = 5f;
    private float currentZoom;

    [Header("Simulation")]
    public bool isRunning = false;
    public float delay = 1.0f;
    private float timer = 0f;

    [Header("PvP")]
    [Tooltip("1 = white, 2 = black")]
    public int currentPlayer = 1;
    public int scoreWhite = 0;
    public int scoreBlack = 0;

    [Header("UI (assign in Inspector)")]
    public TMP_Text scoreWhiteText;
    public TMP_Text scoreBlackText;
    public TMP_Text currentPlayerText;
    public TMP_Text statusText;
    public TMP_Text classicScoreText;
    [Header("Mode-specific UI Panels")]
    public GameObject pvpUIPanel;
    public GameObject classicUIPanel;
    [Header("End Game UI ")]
    public GameObject endGamePanel;
    public TMP_Text endGameTitleText;
    public TMP_Text endGameDetailsText;
    public Button playAgainButton;

    [Header("Patterns")]
    public GameObject patternPanel;
    public TMP_Text patternNameText;
    public int selectedPatternIndex = 0;

    [Header("UI Panels")]
    public Canvas mainUICanvas;
    public UnityEngine.UI.GraphicRaycaster mainUIRaycaster;

    private Cell[,] cells;
    private int[,] currentState; // 0 = dead, 1 = white, 2 = black
    private int[,] nextState;

    private Camera mainCam;
    private Vector3 origin;
    private bool initialized = false;

    // Drag-to-paint
    private bool isDragging = false;
    private int paintMode = 0; // 0 = toggle, 1 = paint, 2 = erase
    private int lastPaintedX = -1;
    private int lastPaintedY = -1;

    // detection
    public bool stopOnStable = true;
    public bool stopOnCycle = true;
    private Dictionary<string, int> seenStates = new Dictionary<string, int>();
    private int generationCount = 0;
    private string statusDetail = "";

    [Header("Limits (optional)")]
    public int maxGenerations = 10000;
    public int maxSeenStates = 10000;
    public float maxGenerationTime = 0.2f;
    public bool stopOnLongGeneration = true;

    #region Unity lifecycle
    void Awake()
    {
        if (cellPrefab == null)
        {
            Debug.LogError("GridManager: cellPrefab не назначен в инспекторе!");
        }

        if (width < 1) { Debug.LogWarning("GridManager: width < 1 — исправлено на 1"); width = 1; }
        if (height < 1) { Debug.LogWarning("GridManager: height < 1 — исправлено на 1"); height = 1; }
        if (cellSize <= 0f) { Debug.LogWarning("GridManager: cellSize <= 0 — исправлено на 0.1"); cellSize = 0.1f; }
        visualScale = Mathf.Clamp(visualScale, 0.01f, 1f);
    }

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.backgroundColor = borderColor;
        }

        GenerateGridCentered();

        // Init UI
        UpdateScoreUI();
        UpdateCurrentPlayerUI();
        UpdateStatusUI();
        UpdateUIForGameMode();
        UpdatePatternUI();

        if (endGamePanel != null) endGamePanel.SetActive(false);
        if (patternPanel != null) patternPanel.SetActive(false);
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveAllListeners();
            playAgainButton.onClick.AddListener(PlayAgain);
        }
    }

    void Update()
    {
        HandleInput();

        if (isRunning)
        {
            timer += Time.deltaTime;
            if (timer >= delay)
            {
                timer -= delay;  // better accuracy
                NextGeneration();
            }
        }
    }
    #endregion

    #region Grid generation
    /// <summary>
    /// Generate cells in a centered grid.
    /// </summary>
    void GenerateGridCentered()
    {
        if (cellPrefab == null)
        {
            Debug.LogError("GridManager: Не могу сгенерировать сетку — cellPrefab == null");
            return;
        }

        if (cells != null)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        cells = new Cell[width, height];
        currentState = new int[width, height];
        nextState = new int[width, height];

        float totalW = width * cellSize;
        float totalH = height * cellSize;

        origin = transform.position - new Vector3(totalW / 2f - cellSize / 2f, totalH / 2f - cellSize / 2f, 0f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = origin + new Vector3(x * cellSize, y * cellSize, 0f);
                GameObject go = Instantiate(cellPrefab, pos, Quaternion.identity, this.transform);
                go.name = $"Cell_{x}_{y}";

                go.transform.localScale = Vector3.one * (cellSize * visualScale);

                Cell c = go.GetComponent<Cell>();
                if (c == null) c = go.AddComponent<Cell>();
                c.x = x; c.y = y;

                currentState[x, y] = 0;
                c.SetVisualStateImmediate(0);

                cells[x, y] = c;
            }
        }

        if (backgroundSprite != null)
        {
            if (backgroundGO != null)
            {
                if (Application.isPlaying) Destroy(backgroundGO);
                else DestroyImmediate(backgroundGO);
            }

            backgroundGO = new GameObject("Background");
            var bgSr = backgroundGO.AddComponent<SpriteRenderer>();
            bgSr.sprite = backgroundSprite;
            bgSr.sortingOrder = -1; // за клетками
            backgroundGO.transform.parent = transform;


            if (mainCam == null) mainCam = Camera.main;
            if (mainCam != null && mainCam.orthographic)
            {
                Vector3 camPos = mainCam.transform.position;
                backgroundGO.transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
                var size = backgroundSprite.bounds.size;
                float vert = mainCam.orthographicSize * 2f;
                float hor = vert * mainCam.aspect;
                backgroundGO.transform.localScale = new Vector3(hor / size.x, vert / size.y, 1f);
            }
            else
            {
                var size = backgroundSprite.bounds.size;
                float scaleX = (width * cellSize) / size.x;
                float scaleY = (height * cellSize) / size.y;
                backgroundGO.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                backgroundGO.transform.localPosition = Vector3.zero;
            }
        }

        timer = 0f;
        seenStates.Clear();
        generationCount = 0;
        statusDetail = "";
        initialized = true;

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null && mainCam.orthographic)
        {
            if (minZoom >= maxZoom)
            {
                Debug.LogWarning("GridManager: minZoom должен быть меньше maxZoom! Применены значения по умолчанию.");
                minZoom = 1f;
                maxZoom = 50f;
            }

            currentZoom = mainCam.orthographicSize;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            mainCam.orthographicSize = currentZoom;
            Debug.Log($"GridManager: initialized camera zoom to {currentZoom:F2} (min={minZoom:F2}, max={maxZoom:F2})");
        }

        UpdateStatusUI();
    }
    #endregion

    #region Input handling (Input System)
    void HandleInput()
    {
        if (Mouse.current == null)
        {
            Debug.LogWarning("GridManager: нет доступной мыши.");
            return;
        }

        if (!initialized)
            return;

        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogWarning("GridManager: нет доступной камеры для преобразования координат клика.");
                return;
            }
        }

        // Drag
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 screenPos = Mouse.current.position.ReadValue();
            float planeZ = transform.position.z;
            float z = Mathf.Abs(mainCam.transform.position.z - planeZ);
            Vector3 worldPos = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));

            isDragging = true;
            lastPaintedX = -1;
            lastPaintedY = -1;
            HandleWorldClick(worldPos, true);
        }

        // Continue drag
        if (Mouse.current.leftButton.isPressed && isDragging)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 screenPos = Mouse.current.position.ReadValue();
            float planeZ = transform.position.z;
            float z = Mathf.Abs(mainCam.transform.position.z - planeZ);
            Vector3 worldPos = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));

            HandleWorldClick(worldPos, false);
        }

        // End drag
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
            lastPaintedX = -1;
            lastPaintedY = -1;
        }

        // Zoom
        if (mainCam != null && mainCam.orthographic)
        {
            if (minZoom >= maxZoom)
            {
                Debug.LogWarning("GridManager: minZoom должен быть меньше maxZoom!");
                minZoom = 1f;
                maxZoom = 50f;
            }

            float scrollDelta = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                currentZoom -= scrollDelta * zoomSpeed * 0.1f;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
                mainCam.orthographicSize = currentZoom;
                Debug.Log($"GridManager: camera zoom adjusted to {currentZoom:F2} (min={minZoom:F2}, max={maxZoom:F2})");
            }
        }

        // keyboard shortcuts (debug only!)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) { currentPlayer = 1; UpdateCurrentPlayerUI(); }
            if (Keyboard.current.digit2Key.wasPressedThisFrame) { currentPlayer = 2; UpdateCurrentPlayerUI(); }
            if (Keyboard.current.spaceKey.wasPressedThisFrame) { ToggleRun(); }
            if (Keyboard.current.rKey.wasPressedThisFrame) { ResumeIgnoreDetections(); }

            if (Keyboard.current.pKey.wasPressedThisFrame) { PlaceSelectedPatternAtCenter(); } // P
            if (Keyboard.current.leftBracketKey.wasPressedThisFrame) { SelectPreviousPattern(); } // [
            if (Keyboard.current.rightBracketKey.wasPressedThisFrame) { SelectNextPattern(); } // ]
        }
    }

    /// <summary>
    /// Click handle in world coordinates — convert to cell indices.
    /// </summary>
    void HandleWorldClick(Vector3 world, bool isFirstClick = false)
    {
        float localX = world.x - origin.x;
        float localY = world.y - origin.y;

        int ix = Mathf.RoundToInt(localX / cellSize);
        int iy = Mathf.RoundToInt(localY / cellSize);

        if (ix < 0 || ix >= width || iy < 0 || iy >= height) return;

        if (!isFirstClick && ix == lastPaintedX && iy == lastPaintedY)
            return;

        lastPaintedX = ix;
        lastPaintedY = iy;

        if (!isRunning)
        {
            if (isFirstClick)
            {
                if (currentState[ix, iy] == 0)
                {
                    paintMode = 1;
                    SetCellState(ix, iy, currentPlayer);
                }
                else
                {
                    paintMode = 2;
                    SetCellState(ix, iy, 0);
                }
            }
            else
            {
                if (paintMode == 1)
                {
                    SetCellState(ix, iy, currentPlayer);
                }
                else if (paintMode == 2)
                {
                    SetCellState(ix, iy, 0);
                }
            }
        }
    }
    #endregion

    #region State management / simulation
    public void SetCellState(int x, int y, int state)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        currentState[x, y] = state;

        if (cells[x, y] != null)
            cells[x, y].SetVisualState(state);
    }

    /// <summary>
    /// Generate the next generation based on currentState and update visuals.
    /// </summary>
    void NextGeneration()
    {
        string curHash = HashCurrentState();
        if (maxGenerations > 0 && generationCount >= maxGenerations)
        {
            isRunning = false;
            statusDetail = $"Stopped: reached max generations ({maxGenerations})";
            UpdateStatusUI();
            Debug.Log(statusDetail);
            return;
        }
        // check if cycle
        if (stopOnCycle && seenStates.ContainsKey(curHash))
        {
            isRunning = false;
            statusDetail = $"Stopped: detected cycle (period {generationCount - seenStates[curHash]})";
            UpdateStatusUI();
            Debug.Log(statusDetail);
            ShowEndGamePopup("A repeating pattern was detected.\n" + statusDetail);
            return;
        }

        seenStates[curHash] = generationCount;
        if (maxSeenStates > 0 && seenStates.Count > maxSeenStates)
        {
            int removeCount = seenStates.Count - maxSeenStates;
            var ordered = new List<KeyValuePair<string, int>>(seenStates);
            ordered.Sort((a, b) => a.Value.CompareTo(b.Value));
            for (int r = 0; r < removeCount; r++) seenStates.Remove(ordered[r].Key);
        }
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                nextState[i, j] = 0;

    int addWhite = 0;
        int addBlack = 0;

    float genStart = Time.realtimeSinceStartup;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int whiteNeighbors = 0;
                int blackNeighbors = 0;
                int totalNeighbors = 0;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        int s = currentState[nx, ny];
                        if (s == 1) { whiteNeighbors++; totalNeighbors++; }
                        else if (s == 2) { blackNeighbors++; totalNeighbors++; }
                    }
                }

                int cur = currentState[x, y];

                if (gameMode == GameMode.Classic)
                {
                    if (cur != 0)
                    {
                        if (totalNeighbors == 2 || totalNeighbors == 3) nextState[x, y] = 1;
                        else nextState[x, y] = 0;
                    }
                    else
                    {
                        if (totalNeighbors == 3) nextState[x, y] = 1;
                        else nextState[x, y] = 0;
                    }
                }
                else // PvP
                {
                    if (cur != 0)
                    {
                        if (totalNeighbors == 2 || totalNeighbors == 3) nextState[x, y] = cur;
                        else nextState[x, y] = 0;
                    }
                    else
                    {
                        if (totalNeighbors == 3)
                        {
                            if (whiteNeighbors > blackNeighbors)
                            {
                                nextState[x, y] = 1;
                                addWhite++;
                            }
                            else if (blackNeighbors > whiteNeighbors)
                            {
                                nextState[x, y] = 2;
                                addBlack++;
                            }
                            else
                            {
                                int pick = Random.value < 0.5f ? 1 : 2;
                                nextState[x, y] = pick;
                                if (pick == 1) addWhite++; else addBlack++;
                            }
                        }
                        else nextState[x, y] = 0;
                    }
                }
            }
        }

        bool anyAlive = false;
        bool changed = false;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                int prev = currentState[i, j];
                currentState[i, j] = nextState[i, j];
                if (prev != currentState[i, j])
                {
                    changed = true;
                    if (cells[i, j] != null) cells[i, j].SetVisualState(currentState[i, j]);
                }
                if (currentState[i, j] != 0) anyAlive = true;
            }
        }

        generationCount++;

        float genTime = Time.realtimeSinceStartup - genStart;
        if (stopOnLongGeneration && maxGenerationTime > 0f && genTime > maxGenerationTime)
        {
            isRunning = false;
            statusDetail = $"Stopped: generation too slow ({genTime:F3}s)";
            UpdateStatusUI();
            Debug.Log(statusDetail);
            return;
        }

        if (!changed)
        {
            if (stopOnStable)
            {
                isRunning = false;
                statusDetail = "Stopped: stable (no changes)";
                UpdateStatusUI();
                Debug.Log(statusDetail);
                ShowEndGamePopup("No changes between generations.\nThe field is stable.");
            }
            else
            {
                statusDetail = "Stable (no changes)";
                UpdateStatusUI();
            }
        }
        if (gameMode == GameMode.PvP)
        {
            if (addWhite != 0 || addBlack != 0)
            {
                scoreWhite += addWhite;
                scoreBlack += addBlack;
                UpdateScoreUI();
            }
        }
        else // Classic
        {
            int totalAlive = 0;
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    if (currentState[i, j] != 0) totalAlive++;
            scoreWhite = totalAlive;
            UpdateScoreUI();
        }

        if (!anyAlive)
        {
            isRunning = false;
            UpdateStatusUI();
            if (gameMode == GameMode.PvP)
                ShowEndGamePopup($"All cells died.\nWhite: {scoreWhite}  Black: {scoreBlack}");
            else
                ShowEndGamePopup("All cells died.");
        }
    }
    #endregion

    #region UI methods (для кнопок/слайдера)
    public void ToggleRun()
    {
        isRunning = !isRunning;
        UpdateStatusUI();
    }

    public void ClearGrid()
    {
        isRunning = false;
        scoreWhite = 0;
        scoreBlack = 0;

        // clear detection memory
        seenStates.Clear(); generationCount = 0; statusDetail = "";
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                SetCellState(x, y, 0);

        UpdateScoreUI();
        UpdateStatusUI();
    }

    public void RandomizeGridDefault()
    {
        RandomizeGrid(0.12f);
    }

    /// <summary>
    /// Fill grid randomly
    /// </summary>
    public void RandomizeGrid(float fillProbability)
    {
        isRunning = false;
        scoreWhite = 0;
        scoreBlack = 0;

        seenStates.Clear(); generationCount = 0; statusDetail = "";

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float r = Random.value;

                if (gameMode == GameMode.Classic)
                {
                    if (r < fillProbability) SetCellState(x, y, 1);
                    else SetCellState(x, y, 0);
                }
                else // PvP
                {
                    if (r < fillProbability / 2f) SetCellState(x, y, 1);
                    else if (r < fillProbability) SetCellState(x, y, 2);

                    else SetCellState(x, y, 0);
                }
            }
        }

        UpdateScoreUI();
        UpdateStatusUI();
    }

    private float lastSliderValue = -1f; // debug

    /// <summary>
    /// set sliderValue ∈ [0..1].
    /// </summary>
    public void SetDelayFromSlider(float sliderValue)
    {
        UnityEngine.UI.Slider slider = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject?.GetComponent<UnityEngine.UI.Slider>();
        if (slider != null)
        {
            sliderValue = slider.value;
        }

        // ignore dups
        if (Mathf.Approximately(sliderValue, lastSliderValue))
            return;

        lastSliderValue = sliderValue;

        float minDelay = 0.1f;
        float maxDelay = 2.0f;
        float newDelay = Mathf.Lerp(maxDelay, minDelay, Mathf.Clamp01(sliderValue));

        if (timer > newDelay)
            timer = newDelay;

        delay = newDelay;
    }

    public void SetCurrentPlayerToWhite() { currentPlayer = 1; UpdateCurrentPlayerUI(); }
    public void SetCurrentPlayerToBlack() { currentPlayer = 2; UpdateCurrentPlayerUI(); }

    public void UpdateScoreUI()
    {
        if (gameMode == GameMode.PvP)
        {
            if (scoreWhiteText != null) scoreWhiteText.text = $"First: {scoreWhite}";
            if (scoreBlackText != null) scoreBlackText.text = $"Second: {scoreBlack}";
        }
        else // Classic
        {
            if (classicScoreText != null)
                classicScoreText.text = $"Alive cells: {scoreWhite}";
        }
    }

    public void UpdateCurrentPlayerUI()
    {
        if (currentPlayerText != null)
            currentPlayerText.text = currentPlayer == 1 ? "Current: First" : "Current: Second";
    }

    public void UpdateStatusUI()
    {
        if (statusText != null)
            statusText.text = isRunning ? ("Running" + (string.IsNullOrEmpty(statusDetail) ? "" : (" — " + statusDetail))) : ("Paused" + (string.IsNullOrEmpty(statusDetail) ? "" : (" — " + statusDetail)));
    }

    /// <summary>
    /// Mode toggle.
    /// </summary>
    public void ToggleGameMode()
    {
        gameMode = (gameMode == GameMode.PvP) ? GameMode.Classic : GameMode.PvP;
        UpdateUIForGameMode();
        ClearGrid();
    }

    public void UpdateUIForGameMode()
    {
        bool isPvP = (gameMode == GameMode.PvP);

        if (pvpUIPanel != null)
            pvpUIPanel.SetActive(isPvP);

        if (classicUIPanel != null)
            classicUIPanel.SetActive(!isPvP);

        UpdateScoreUI();
        UpdateCurrentPlayerUI();
    }
    #endregion

    void ShowEndGamePopup(string details)
    {
        if (endGamePanel == null) return;
        if (endGameTitleText != null)
        {
            if (gameMode == GameMode.PvP)
            {
                endGameTitleText.text = scoreWhite > scoreBlack ? "First wins!" : (scoreBlack > scoreWhite ? "Second wins!" : "It's a tie!");
            }
            else
            {
                endGameTitleText.text = "Game Over";
            }
            endGameTitleText.alignment = TMPro.TextAlignmentOptions.Center;
        }
        if (endGameDetailsText != null)
        {
            if (gameMode == GameMode.PvP)
            {
                endGameDetailsText.text = details + $"\n\nFinal score:\nFirst: {scoreWhite} — Second: {scoreBlack}";
            }
            else
            {
                endGameDetailsText.text = details + $"\n\nMax alive cells: {scoreWhite}";
            }
            endGameDetailsText.alignment = TMPro.TextAlignmentOptions.Center;
        }
        endGamePanel.SetActive(true);
    }

    public void PlayAgain()
    {
        if (endGamePanel != null) endGamePanel.SetActive(false);
        ClearGrid();
        stopOnStable = true;
        stopOnCycle = true;
        statusDetail = "";
        UpdateStatusUI();
    }

    #region State hashing and detection control
    string HashCurrentState()
    {
        // Формируем byte[] длины width*height, где значения 0/1/2 в байте.
        byte[] buf = new byte[width * height];
        int idx = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                buf[idx++] = (byte)currentState[x, y];
            }
        }
        using (var md5 = MD5.Create())
        {
            byte[] hash = md5.ComputeHash(buf);
            // преобразуем в строку hex для словаря
            StringBuilder sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    public void ResumeIgnoreDetections()
    {
        stopOnStable = false;
        stopOnCycle = false;
        statusDetail = "(detections ignored)";
        isRunning = true;
        UpdateStatusUI();
    }

    /// <summary>
    /// Go to next generation (step button).
    /// </summary>
    public void StepOnce()
    {
        if (isRunning) return;
        NextGeneration();
    }

    public void SetStopOnStable(bool v) { stopOnStable = v; }
    public void SetStopOnCycle(bool v) { stopOnCycle = v; }

    /// <summary>
    /// clear memory.
    /// </summary>
    public void ClearDetections()
    {
        seenStates.Clear();
        generationCount = 0;
        statusDetail = "";
        UpdateStatusUI();
    }
    #endregion

    #region Pattern management
    public void PlaceSelectedPatternAtCenter()
    {
        if (selectedPatternIndex < 0 || selectedPatternIndex >= Pattern.Library.All.Length)
        {
            Debug.LogWarning("Недопустимый индекс паттерна");
            return;
        }

        Pattern pattern = Pattern.Library.All[selectedPatternIndex];
        int centerX = width / 2 - pattern.width / 2;
        int centerY = height / 2 - pattern.height / 2;

        PlacePattern(pattern, centerX, centerY);
        TogglePatternPanel();
    }

    public void PlacePattern(Pattern pattern, int startX, int startY)
    {
        if (pattern == null || pattern.grid == null)
        {
            Debug.LogWarning("Паттерн пустой");
            return;
        }

        for (int py = 0; py < pattern.height; py++)
        {
            for (int px = 0; px < pattern.width; px++)
            {
                int cellX = startX + px;
                int cellY = startY + py;

                if (cellX < 0 || cellX >= width || cellY < 0 || cellY >= height)
                    continue;

                int patternValue = pattern.grid[py, px];
                if (patternValue != 0)
                {
                    int state = (gameMode == GameMode.Classic) ? 1 : currentPlayer;
                    SetCellState(cellX, cellY, state);
                }
            }
        }

        UpdateScoreUI();
        UpdateStatusUI();
    }

    public void SelectPreviousPattern()
    {
        if (Pattern.Library.All.Length == 0) return;

        selectedPatternIndex--;
        if (selectedPatternIndex < 0) selectedPatternIndex = Pattern.Library.All.Length - 1;

        UpdatePatternUI();
    }

    public void SelectNextPattern()
    {
        if (Pattern.Library.All.Length == 0) return;

        selectedPatternIndex++;
        if (selectedPatternIndex >= Pattern.Library.All.Length) selectedPatternIndex = 0;

        UpdatePatternUI();
    }

    void UpdatePatternUI()
    {
        if (patternNameText != null && selectedPatternIndex >= 0 && selectedPatternIndex < Pattern.Library.All.Length)
        {
            patternNameText.text = Pattern.Library.All[selectedPatternIndex].name;
        }
    }

    public void TogglePatternPanel()
    {
        if (patternPanel != null)
        {
            bool isActive = patternPanel.activeSelf;
            patternPanel.SetActive(!isActive);
            if (mainUIRaycaster != null)
            {
                mainUIRaycaster.enabled = isActive;
            }
            if (!isActive)
            {
                UpdatePatternUI();
            }
        }
    }
    #endregion
}
