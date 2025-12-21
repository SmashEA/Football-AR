using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("AR References")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    [Header("Префабы")]
    public GameObject goalPrefab;
    public GameObject footballPrefab;

    [Header("Настройки игры")]
    public float maxForce = 20f; // Увеличенная сила!
    public float forceMultiplier = 3f;
    public float spawnHeightAbovePlane = 0.1f;

    [Header("Счет")]
    public int score = 0;
    public Text scoreText; // Изменено на обычный Text вместо TMP_Text!

    [Header("Настройки UI")]
    public GameObject scorePanel;

    [Header("Размещение ворот")]
    public GameObject placementIndicator;
    public Text placementHintText; // Изменено на обычный Text
    private bool isPlacingGoal = false;
    private Pose placementPose;

    // Приватные переменные
    private GameObject currentGoal;
    private GameObject currentFootball;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private Vector2 touchStartPosition;
    private bool isAiming = false;
    private float currentForce = 0f;
    private HashSet<GameObject> scoredBalls = new HashSet<GameObject>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // Убрал DontDestroyOnLoad чтобы избежать проблем с камерой
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Находим AR компоненты если не назначены
        if (raycastManager == null)
            raycastManager = FindObjectOfType<ARRaycastManager>();
        if (planeManager == null)
            planeManager = FindObjectOfType<ARPlaneManager>();

        // Проверяем и настраиваем камеру
        SetupCamera();

        // Создаем индикатор размещения если нет
        if (placementIndicator == null)
        {
            CreatePlacementIndicator();
        }
        else
        {
            placementIndicator.SetActive(false);
        }

        // Скрываем подсказку если есть
        if (placementHintText != null)
        {
            placementHintText.gameObject.SetActive(false);
        }

        // Инициализация UI счета
        InitializeScoreDisplay();

        // Подписываемся на событие смены сцены
        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log("GameManager готов. Нажмите 'Создать ворота'");
    }

    void SetupCamera()
    {
        // Убеждаемся что есть камера с тегом MainCamera
        if (Camera.main == null)
        {
            Debug.LogWarning("MainCamera не найдена. Ищем камеру...");

            // Ищем камеру в XROrigin
            GameObject xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin != null)
            {
                Camera xrCamera = xrOrigin.GetComponentInChildren<Camera>();
                if (xrCamera != null)
                {
                    xrCamera.tag = "MainCamera";
                    Debug.Log("Назначена камера XR как MainCamera: " + xrCamera.name);
                }
            }

            // Если все еще нет, ищем любую камеру
            if (Camera.main == null)
            {
                Camera anyCamera = FindObjectOfType<Camera>();
                if (anyCamera != null)
                {
                    anyCamera.tag = "MainCamera";
                    Debug.Log("Назначена камера как MainCamera: " + anyCamera.name);
                }
                else
                {
                    Debug.LogError("В сцене нет камер!");
                }
            }
        }
    }

    void Update()
    {
        // Если в режиме размещения ворот
        if (isPlacingGoal)
        {
            UpdatePlacement();

            // Обработка тапа для размещения
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                PlaceGoalOnPlane();
            }

#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                PlaceGoalOnPlane();
            }
#endif
        }
        else // Если НЕ в режиме размещения, то обрабатываем управление мячом
        {
            // Если нет мяча - не обрабатываем ввод для удара  
            if (currentFootball == null) return;

            HandleTouchInput();
        }
    }

    // ===== СОЗДАНИЕ ИНДИКАТОРА РАЗМЕЩЕНИЯ =====

    void CreatePlacementIndicator()
    {
        placementIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        placementIndicator.name = "PlacementIndicator";
        Destroy(placementIndicator.GetComponent<Collider>());

        placementIndicator.transform.localScale = new Vector3(0.3f, 0.01f, 0.3f);
        MeshRenderer renderer = placementIndicator.GetComponent<MeshRenderer>();

        // Создаем простой материал
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0, 1, 0, 0.5f);
        renderer.material = mat;

        placementIndicator.SetActive(false);
    }

    // ===== ОБНОВЛЕНИЕ ПОЗИЦИИ ИНДИКАТОРА НА ПЛОСКОСТИ =====

    void UpdatePlacement()
    {
        if (placementIndicator == null || !isPlacingGoal) return;

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        // Центр экрана
        Vector3 screenCenter = mainCam.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));

        if (raycastManager != null)
        {
            hits.Clear();

            // Ищем пересечение с AR плоскостями
            if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
            {
                placementPose = hits[0].pose;

                // Проверяем что плоскость достаточно большая
                var plane = hits[0].trackable as ARPlane;
                if (plane != null && plane.size.x * plane.size.y > 0.25f)
                {
                    // Позиционируем индикатор
                    placementIndicator.transform.position = placementPose.position;

                    // Поворачиваем к камере
                    Vector3 cameraForward = mainCam.transform.forward;
                    Vector3 cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
                    if (cameraBearing.magnitude > 0.1f)
                    {
                        placementIndicator.transform.rotation = Quaternion.LookRotation(cameraBearing);
                    }

                    placementIndicator.SetActive(true);

                    // Обновляем подсказку
                    if (placementHintText != null)
                    {
                        placementHintText.text = "Тапните для размещения ворот";
                        placementHintText.color = Color.green;
                    }

                    return;
                }
            }
        }

        // Если не нашли подходящую плоскость
        placementIndicator.SetActive(false);

        if (placementHintText != null)
        {
            placementHintText.text = "Двигайте камеру, чтобы найти поверхность";
            placementHintText.color = Color.yellow;
        }
    }

    // ===== ОСНОВНЫЕ МЕТОДЫ =====

    public void SpawnGoal()
    {
        Debug.Log("=== НАЧАЛО РАЗМЕЩЕНИЯ ВОРОТ ===");

        // Удаляем старые ворота
        if (currentGoal != null)
        {
            Destroy(currentGoal);
            currentGoal = null;
        }

        // Включаем режим размещения
        isPlacingGoal = true;

        // Показываем подсказку
        if (placementHintText != null)
        {
            placementHintText.gameObject.SetActive(true);
            placementHintText.text = "Двигайте камеру, чтобы найти поверхность";
        }

        Debug.Log("Режим размещения активирован. Наведите на поверхность и тапните.");
    }

    void PlaceGoalOnPlane()
    {
        if (!placementIndicator.activeSelf || !isPlacingGoal)
        {
            Debug.LogWarning("Не найдена подходящая поверхность для размещения!");
            return;
        }

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("Камера не найдена!");
            return;
        }

        // Создаем ворота на позиции индикатора
        Vector3 spawnPosition = placementPose.position;
        spawnPosition.y += spawnHeightAbovePlane; // Немного выше плоскости

        // Создаем ворота
        currentGoal = Instantiate(goalPrefab, spawnPosition, Quaternion.identity);

        // Поворачиваем ворота к камере
        Vector3 cameraForward = mainCam.transform.forward;
        Vector3 cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
        if (cameraBearing.magnitude > 0.1f)
        {
            currentGoal.transform.rotation = Quaternion.LookRotation(cameraBearing);
        }

        // Настраиваем размер
        float scaleFactor = 0.005f;
        currentGoal.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

        Debug.Log($"Ворота созданы на плоскости. Позиция: {spawnPosition}");

        // Выключаем режим размещения
        isPlacingGoal = false;
        placementIndicator.SetActive(false);

        // Скрываем подсказку
        if (placementHintText != null)
        {
            placementHintText.gameObject.SetActive(false);
        }

        // Создаем мяч
        SpawnFootball();
    }

    void SpawnFootball()
    {
        Debug.Log("=== СОЗДАНИЕ МЯЧА ===");

        // Удаляем старый мяч
        if (currentFootball != null)
        {
            Destroy(currentFootball);
            currentFootball = null;
        }

        // Получаем позицию камеры
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("Камера не найдена!");
            return;
        }

        Transform cam = mainCam.transform;

        // Позиция мяча: перед камерой
        Vector3 ballPosition = cam.position + cam.forward * 1.0f + Vector3.down * 0.5f;

        // Если есть ворота - размещаем мяч ближе к ним
        if (currentGoal != null)
        {
            Vector3 goalPosition = currentGoal.transform.position;
            ballPosition = Vector3.Lerp(cam.position, goalPosition, 0.3f);
            ballPosition.y = cam.position.y - 0.5f;
        }

        // Создаем мяч
        currentFootball = Instantiate(footballPrefab, ballPosition, Quaternion.identity);

        // Настраиваем мяч
        Rigidbody rb = currentFootball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            Debug.LogError("У мяча нет Rigidbody!");
        }

        Debug.Log($"Мяч создан. Позиция: {ballPosition}");

        // Очищаем список забитых мячей
        scoredBalls.Clear();
    }

    // ===== УПРАВЛЕНИЕ МЯЧОМ =====

    void HandleTouchInput()
    {
        // Для мобильных устройств
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began && !isAiming)
            {
                // Проверяем, тапнули ли по мячу
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("Football"))
                {
                    touchStartPosition = touch.position;
                    isAiming = true;
                    currentForce = 0f;
                    Debug.Log("Начало прицеливания...");
                }
            }
            else if (touch.phase == TouchPhase.Moved && isAiming)
            {
                float dragDistance = Vector2.Distance(touchStartPosition, touch.position);
                currentForce = Mathf.Clamp(dragDistance / 200f, 0f, 1f); // Уменьшил делитель для большей чувствительности
                Debug.Log($"Сила: {Mathf.RoundToInt(currentForce * 100)}%");
            }
            else if (touch.phase == TouchPhase.Ended && isAiming)
            {
                PerformKick(touch.position);
                isAiming = false;
            }
        }

        // Для теста в редакторе (левая кнопка мыши)
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0) && !isAiming)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("Football"))
            {
                touchStartPosition = Input.mousePosition;
                isAiming = true;
                currentForce = 0f;
            }
        }

        if (isAiming && Input.GetMouseButton(0))
        {
            float dragDistance = Vector2.Distance(touchStartPosition, Input.mousePosition);
            currentForce = Mathf.Clamp(dragDistance / 200f, 0f, 1f);
        }

        if (isAiming && Input.GetMouseButtonUp(0))
        {
            PerformKick(Input.mousePosition);
            isAiming = false;
        }
#endif
    }

    void PerformKick(Vector2 touchPosition)
    {
        if (currentFootball == null)
        {
            Debug.LogError("Мяч не существует!");
            return;
        }

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("Камера не найдена!");
            return;
        }

        // Рассчитываем направление удара
        Vector3 forceDirection = mainCam.transform.forward;
        forceDirection.y = 0.2f; // Небольшой подъем
        forceDirection.Normalize();

        // Увеличиваем силу!
        float actualForce = currentForce * maxForce * forceMultiplier;

        Debug.Log($"Удар! Сила: {actualForce:F1}, currentForce: {currentForce}");

        FootballController ballController = currentFootball.GetComponent<FootballController>();
        if (ballController != null)
        {
            Vector3 force = forceDirection * actualForce;
            ballController.Kick(force, currentFootball.transform.position);

            // Создаем новый мяч через 3 секунды
            Invoke("SpawnFootball", 3f);
        }
        else
        {
            Debug.LogError("FootballController не найден на мяче!");
        }
    }

    // ===== УПРАВЛЕНИЕ СЧЕТОМ =====

    void InitializeScoreDisplay()
    {
        if (scoreText == null)
        {
            GameObject scoreObj = GameObject.Find("ScoreText");
            if (scoreObj != null)
            {
                scoreText = scoreObj.GetComponent<Text>();
            }
        }

        if (scorePanel != null)
        {
            scorePanel.SetActive(true);
        }

        UpdateScoreDisplay();
    }

    void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Счет: {score}";
        }
    }

    public void ResetScore()
    {
        score = 0;
        UpdateScoreDisplay();
        Debug.Log("Счет сброшен");
    }

    // ===== ОСНОВНЫЕ МЕТОДЫ ИГРЫ =====

    public void ResetGame()
    {
        Debug.Log("=== ПЕРЕЗАПУСК ИГРЫ ===");

        // Отменяем все вызовы
        CancelInvoke();

        // Выходим из режима размещения
        isPlacingGoal = false;
        if (placementIndicator != null)
            placementIndicator.SetActive(false);
        if (placementHintText != null)
            placementHintText.gameObject.SetActive(false);

        // Удаляем ворота
        if (currentGoal != null)
        {
            Destroy(currentGoal);
            currentGoal = null;
            Debug.Log("Ворота удалены");
        }

        // Удаляем мяч
        if (currentFootball != null)
        {
            Destroy(currentFootball);
            currentFootball = null;
            Debug.Log("Мяч удален");
        }

        // Очищаем список
        scoredBalls.Clear();

        // Сбрасываем состояние
        isAiming = false;
        currentForce = 0f;

        // Сбрасываем счет!
        ResetScore();

        Debug.Log("Игра сброшена. Нажмите 'Создать ворота' для начала");
    }

    // Управление AR плоскостями
    void SetPlanesActive(bool active)
    {
        if (planeManager != null)
        {
            planeManager.enabled = active;

            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(active);
            }
        }
    }

    // Обработка гола
    public void GoalScored(GameObject ball)
    {
        if (scoredBalls.Contains(ball)) return;

        scoredBalls.Add(ball);

        // Увеличиваем счет
        score += 100;

        // Обновляем отображение
        UpdateScoreDisplay();

        Debug.Log($"ГОООЛ! Счет: {score}");

        FootballController ballController = ball.GetComponent<FootballController>();
        if (ballController != null)
        {
            ballController.DestroyAfterDelay(2f);
        }

        Invoke("SpawnFootball", 3f);
    }

    public bool IsGoalScored(GameObject ball)
    {
        return scoredBalls.Contains(ball);
    }

    // ===== УПРАВЛЕНИЕ СЦЕНАМИ =====

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Загружена сцена: {scene.name}");

        // Перенастраиваем камеру на новой сцене
        SetupCamera();

        // Если сцена меню - сбрасываем счет
        if (scene.name.Contains("Menu") || scene.name == "MainMenu")
        {
            ResetScore();
        }

        InitializeScoreDisplay();
    }



    public void CancelGoalPlacement()
    {
        isPlacingGoal = false;
        if (placementIndicator != null)
            placementIndicator.SetActive(false);
        if (placementHintText != null)
            placementHintText.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}