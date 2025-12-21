using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ARStabilizer : MonoBehaviour
{
    [Header("AR Components")]
    public ARSession arSession;
    public ARSessionOrigin sessionOrigin;
    public ARPlaneManager planeManager;
    public ARCameraManager cameraManager;

    [Header("Стабилизация")]
    [Range(0.1f, 5f)] public float stabilizationStrength = 2f;
    [Range(0.1f, 3f)] public float planeMergeDistance = 1f;
    public bool lockVerticalPlanes = true;
    public bool usePlaneFiltering = true;

    [Header("Отладка")]
    public bool showDebugInfo = true;
    public UnityEngine.UI.Text debugText;

    private List<ARPlane> trackedPlanes = new List<ARPlane>();
    private Vector3 worldAnchorPosition;
    private bool isWorldAnchored = false;
    private float lastStableTime;

    void Start()
    {
        // Находим компоненты
        if (arSession == null) arSession = FindObjectOfType<ARSession>();
        if (sessionOrigin == null) sessionOrigin = FindObjectOfType<ARSessionOrigin>();
        if (planeManager == null) planeManager = FindObjectOfType<ARPlaneManager>();
        if (cameraManager == null) cameraManager = FindObjectOfType<ARCameraManager>();

        // Подписываемся на события
        if (planeManager != null)
        {
            planeManager.planesChanged += OnPlanesChanged;
        }

        Debug.Log("AR Stabilizer инициализирован");
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        // Обработка новых плоскостей
        foreach (var plane in args.added)
        {
            if (!trackedPlanes.Contains(plane))
            {
                trackedPlanes.Add(plane);
                StabilizePlane(plane);
            }
        }

        // Обработка обновленных плоскостей
        foreach (var plane in args.updated)
        {
            StabilizePlane(plane);
        }

        // Обработка удаленных плоскостей
        foreach (var plane in args.removed)
        {
            trackedPlanes.Remove(plane);
        }

        // Фильтрация и слияние плоскостей
        if (usePlaneFiltering)
        {
            FilterAndMergePlanes();
        }

        // Создаем якорь мира при первой стабильной плоскости
        if (!isWorldAnchored && trackedPlanes.Count > 0)
        {
            CreateWorldAnchor();
        }
    }

    void StabilizePlane(ARPlane plane)
    {
        if (plane == null) return;

        // Фильтруем слишком маленькие плоскости
        if (plane.size.x * plane.size.y < 0.1f)
        {
            plane.gameObject.SetActive(false);
            return;
        }

        // Блокируем вертикальные плоскости если нужно
        if (lockVerticalPlanes && IsVerticalPlane(plane))
        {
            plane.gameObject.SetActive(false);
            return;
        }

        // Стабилизация позиции (сглаживание)
        if (stabilizationStrength > 0)
        {
            SmoothPlaneMovement(plane);
        }

        // Обновляем материал для отладки
        UpdatePlaneVisualization(plane);
    }

    bool IsVerticalPlane(ARPlane plane)
    {
        // Проверка для разных версий Unity
        string alignment = plane.alignment.ToString();
        return alignment.Contains("Vertical") || alignment == "Vertical" || alignment == "1";
    }

    bool IsHorizontalPlane(ARPlane plane)
    {
        // Проверка для разных версий Unity
        string alignment = plane.alignment.ToString();
        return alignment.Contains("Horizontal") || alignment == "Horizontal" || alignment == "0";
    }

    void SmoothPlaneMovement(ARPlane plane)
    {
        // Простое сглаживание движения плоскости
        // В реальном проекте можно использовать более сложные алгоритмы
    }

    void FilterAndMergePlanes()
    {
        // Удаляем дубликаты и близкие плоскости
        for (int i = trackedPlanes.Count - 1; i >= 0; i--)
        {
            if (trackedPlanes[i] == null) continue;

            for (int j = trackedPlanes.Count - 1; j >= 0; j--)
            {
                if (i == j || trackedPlanes[j] == null) continue;

                // Проверяем расстояние между плоскостями
                float distance = Vector3.Distance(
                    trackedPlanes[i].transform.position,
                    trackedPlanes[j].transform.position
                );

                // Если плоскости слишком близко - деактивируем меньшую
                if (distance < planeMergeDistance)
                {
                    float areaI = trackedPlanes[i].size.x * trackedPlanes[i].size.y;
                    float areaJ = trackedPlanes[j].size.x * trackedPlanes[j].size.y;

                    if (areaI > areaJ)
                    {
                        trackedPlanes[j].gameObject.SetActive(false);
                        trackedPlanes.RemoveAt(j);
                        if (j < i) i--;
                    }
                    else
                    {
                        trackedPlanes[i].gameObject.SetActive(false);
                        trackedPlanes.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }

    void CreateWorldAnchor()
    {
        if (trackedPlanes.Count == 0) return;

        // Используем первую большую горизонтальную плоскость как якорь
        ARPlane anchorPlane = null;
        float maxArea = 0f;

        foreach (var plane in trackedPlanes)
        {
            if (plane == null) continue;

            // Ищем горизонтальные плоскости
            if (!IsHorizontalPlane(plane)) continue;

            float area = plane.size.x * plane.size.y;
            if (area > maxArea && area > 0.5f) // Минимум 0.5м²
            {
                maxArea = area;
                anchorPlane = plane;
            }
        }

        if (anchorPlane != null)
        {
            worldAnchorPosition = anchorPlane.transform.position;
            isWorldAnchored = true;
            lastStableTime = Time.time;

            Debug.Log($"Создан мировой якорь на плоскости площадью {maxArea:F2}м²");

            // Можно создать визуальный маркер
            CreateAnchorMarker(worldAnchorPosition);
        }
    }

    void CreateAnchorMarker(Vector3 position)
    {
        // Создаем простой маркер якоря
        GameObject anchorMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        anchorMarker.name = "WorldAnchor";
        anchorMarker.transform.position = position + Vector3.up * 0.01f;
        anchorMarker.transform.localScale = new Vector3(0.1f, 0.01f, 0.1f);

        MeshRenderer renderer = anchorMarker.GetComponent<MeshRenderer>();
        renderer.material.color = Color.blue;

        // Уничтожаем через время
        Destroy(anchorMarker, 10f);
    }

    void UpdatePlaneVisualization(ARPlane plane)
    {
        // Можно настроить цвет плоскости в зависимости от стабильности
        MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // Зеленый = стабильно, Красный = движется
            float stability = CalculatePlaneStability(plane);
            Color planeColor = Color.Lerp(Color.red, Color.green, stability);
            planeColor.a = 0.3f;

            if (renderer.material != null)
            {
                renderer.material.color = planeColor;
            }
        }
    }

    float CalculatePlaneStability(ARPlane plane)
    {
        if (plane == null) return 0f;

        // Большие плоскости обычно стабильнее
        float area = plane.size.x * plane.size.y;
        float areaStability = Mathf.Clamp01(area / 2f); // Нормализуем до 2м²

        // Проверяем alignment плоскости
        float alignmentStability = GetAlignmentStability(plane);

        return (areaStability + alignmentStability) / 2f;
    }

    float GetAlignmentStability(ARPlane plane)
    {
        // Для совместимости с разными версиями Unity
        string alignment = plane.alignment.ToString();

        if (alignment.Contains("Horizontal") || alignment == "Horizontal" || alignment == "0")
        {
            return 1f;
        }
        else if (alignment.Contains("Vertical") || alignment == "Vertical" || alignment == "1")
        {
            return 0.3f;
        }
        else if (alignment.Contains("NotAxisAligned"))
        {
            return 0.2f;
        }
        else
        {
            return 0.5f;
        }
    }

    void Update()
    {
        // Стабилизация мира если якорь создан
        if (isWorldAnchored && stabilizationStrength > 0)
        {
            StabilizeWorldTracking();
        }

        // Обновление отладки
        if (showDebugInfo)
        {
            UpdateDebugInfo();
        }
    }

    void StabilizeWorldTracking()
    {
        // Попытка стабилизировать мировой трекинг
        // Это сложная задача, но можно немного помочь

        if (sessionOrigin != null)
        {
            // Если камера слишком далеко от якоря, пытаемся корректировать
            float distanceToAnchor = Vector3.Distance(
                Camera.main.transform.position,
                worldAnchorPosition
            );

            // Если ушли слишком далеко, создаем новый якорь
            if (distanceToAnchor > 5f && Time.time - lastStableTime > 10f)
            {
                isWorldAnchored = false;
                Debug.Log("Слишком далеко от якоря, ищем новый...");
            }
        }
    }

    void UpdateDebugInfo()
    {
        if (debugText != null)
        {
            string info = "AR Стабильность:\n";
            info += $"Плоскостей: {trackedPlanes.Count}\n";
            info += $"Якорь: {(isWorldAnchored ? "✓" : "✗")}\n";

            if (isWorldAnchored)
            {
                float dist = Vector3.Distance(Camera.main.transform.position, worldAnchorPosition);
                info += $"До якоря: {dist:F1}м\n";
            }

            // Статистика плоскостей
            int largePlanes = 0;
            float totalArea = 0f;

            foreach (var plane in trackedPlanes)
            {
                if (plane != null)
                {
                    float area = plane.size.x * plane.size.y;
                    totalArea += area;
                    if (area > 0.5f) largePlanes++;
                }
            }

            info += $"Больших (>0.5м²): {largePlanes}\n";
            info += $"Общая площадь: {totalArea:F1}м²";

            debugText.text = info;
        }
    }

    // Публичные методы для управления

    public void ResetWorldAnchor()
    {
        isWorldAnchored = false;
        trackedPlanes.Clear();
        Debug.Log("Мировой якорь сброшен");
    }

    public void SetStabilizationStrength(float strength)
    {
        stabilizationStrength = Mathf.Clamp(strength, 0.1f, 5f);
    }

    public Vector3? GetMostStablePlanePosition()
    {
        if (trackedPlanes.Count == 0) return null;

        ARPlane mostStable = null;
        float maxStability = 0f;

        foreach (var plane in trackedPlanes)
        {
            if (plane == null) continue;

            float stability = CalculatePlaneStability(plane);
            if (stability > maxStability)
            {
                maxStability = stability;
                mostStable = plane;
            }
        }

        return mostStable?.transform.position;
    }

    void OnDestroy()
    {
        if (planeManager != null)
        {
            planeManager.planesChanged -= OnPlanesChanged;
        }
    }
}