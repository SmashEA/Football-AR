using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Добавляем для работы со сценами

public class UIManager : MonoBehaviour
{
    [Header("Основная кнопка")]
    public Button toggleMenuButton;

    [Header("Панель с кнопками")]
    public GameObject buttonsPanel;

    [Header("Кнопки на панели")]
    public Button placeGoalButton;
    public Button resetButton;
    public Button exitButton;

    [Header("Настройки")]
    public string mainMenuSceneName = "MainMenu"; // Имя сцены главного меню

    void Start()
    {
        // Настройка основной кнопки
        if (toggleMenuButton != null)
        {
            toggleMenuButton.onClick.AddListener(ToggleMenu);
        }

        // Настройка кнопок на панели
        if (placeGoalButton != null)
        {
            placeGoalButton.onClick.AddListener(PlaceGoal);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetGame);
        }

        // Настройка кнопки выхода
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitToMainMenu);
        }

        // Скрываем панель при старте
        if (buttonsPanel != null)
        {
            buttonsPanel.SetActive(false);
        }
    }

    public void ToggleMenu()
    {
        if (buttonsPanel != null)
        {
            bool isActive = buttonsPanel.activeSelf;
            buttonsPanel.SetActive(!isActive);
            Debug.Log(isActive ? "Меню закрыто" : "Меню открыто");
        }
    }

    public void PlaceGoal()
    {
        Debug.Log("Кнопка: Создать ворота");
        if (GameManager.instance != null)
        {
            GameManager.instance.SpawnGoal();
        }

        // Закрываем меню после действия (опционально)
        CloseMenuIfNeeded();
    }

    public void ResetGame()
    {
        Debug.Log("Кнопка: Перезапуск");

        if (GameManager.instance != null)
        {
            // Вызываем ResetGame из GameManager (он сбросит и счет)
            GameManager.instance.ResetGame();
        }
        else
        {
            Debug.LogWarning("GameManager не найден!");
        }

        // Закрываем меню после действия
        CloseMenuIfNeeded();
    }

    public void ExitToMainMenu()
    {
        Debug.Log("Выход в главное меню");

        CloseMenuIfNeeded();
    }
    void CloseMenuIfNeeded()
    {
        if (buttonsPanel != null && buttonsPanel.activeSelf)
        {
            buttonsPanel.SetActive(false);
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (buttonsPanel != null && buttonsPanel.activeSelf)
            {
                buttonsPanel.SetActive(false);
            }
            else if (toggleMenuButton != null)
            {
                ToggleMenu();
            }
        }
    }

    public void UpdateScoreDisplay(int score)
    {
        Debug.Log($"Текущий счет: {score}");
    }
    void OnDestroy()
    {
        if (toggleMenuButton != null) toggleMenuButton.onClick.RemoveAllListeners();
        if (placeGoalButton != null) placeGoalButton.onClick.RemoveAllListeners();
        if (resetButton != null) resetButton.onClick.RemoveAllListeners();
        if (exitButton != null) exitButton.onClick.RemoveAllListeners();
    }
}