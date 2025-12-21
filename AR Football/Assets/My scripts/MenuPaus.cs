using UnityEngine;
using UnityEngine.UI;

public class ToggleButtonsUI : MonoBehaviour
{
    [Header("Основные элементы")]
    public Button toggleButton;          // Кнопка для показа/скрытия
    public GameObject buttonsPanel;      // Панель с кнопками Reset и Spawn

    [Header("Текст кнопки")]
    public Text toggleButtonText;
    public string showText = "Показать меню";
    public string hideText = "Скрыть меню";

    [Header("Анимация (опционально)")]
    public bool useAnimation = true;
    public float animationSpeed = 0.2f;

    private bool buttonsVisible = false;
    private RectTransform panelTransform;
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;

    void Start()
    {
        // Проверяем ссылки
        if (toggleButton == null)
        {
            Debug.LogError("ToggleButton не назначена!");
            return;
        }

        if (buttonsPanel == null)
        {
            Debug.LogError("ButtonsPanel не назначена!");
            return;
        }

        // Назначаем обработчик кнопки
        toggleButton.onClick.AddListener(ToggleButtons);

        // Изначально скрываем панель
        if (useAnimation && buttonsPanel.TryGetComponent<RectTransform>(out panelTransform))
        {
            // Сохраняем позиции для анимации
            visiblePosition = panelTransform.anchoredPosition;
            hiddenPosition = visiblePosition + new Vector2(0, 150f); // Сдвигаем вниз

            // Устанавливаем начальную позицию
            panelTransform.anchoredPosition = hiddenPosition;
            buttonsPanel.SetActive(true); // Включаем для анимации
        }
        else
        {
            // Без анимации - просто выключаем
            buttonsPanel.SetActive(false);
        }

        // Обновляем текст кнопки
        UpdateButtonText();

        Debug.Log("ToggleButtonsUI инициализирован");
    }

    void ToggleButtons()
    {
        Debug.Log("Нажата кнопка переключения");

        // Переключаем состояние
        buttonsVisible = !buttonsVisible;

        if (useAnimation && panelTransform != null)
        {
            // Анимированное переключение
            StartCoroutine(AnimatePanel(buttonsVisible));
        }
        else
        {
            // Простое переключение
            buttonsPanel.SetActive(buttonsVisible);
        }

        // Обновляем текст кнопки
        UpdateButtonText();
    }

    System.Collections.IEnumerator AnimatePanel(bool show)
    {
        Vector2 startPos = panelTransform.anchoredPosition;
        Vector2 targetPos = show ? visiblePosition : hiddenPosition;

        float elapsed = 0f;

        while (elapsed < animationSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationSpeed;

            // Плавная анимация
            panelTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t));

            yield return null;
        }

        panelTransform.anchoredPosition = targetPos;
    }

    void UpdateButtonText()
    {
        if (toggleButtonText != null)
        {
            toggleButtonText.text = buttonsVisible ? hideText : showText;
        }
    }

    // Методы для внешнего управления
    public void ShowButtons()
    {
        if (!buttonsVisible)
        {
            ToggleButtons();
        }
    }

    public void HideButtons()
    {
        if (buttonsVisible)
        {
            ToggleButtons();
        }
    }

    public bool AreButtonsVisible()
    {
        return buttonsVisible;
    }
}