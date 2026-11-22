using Enums;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UserInterface
{
    /// <summary>Shows the clear / game-over panel when the GameManager reports the end of the game.</summary>
    public class GameResultDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private float delayBeforeShowing = 1.5f;

        [SerializeField] private string clearTitle = "GAME CLEAR";
        [SerializeField] private string clearDescription = "언데드 기사를 쓰러뜨렸습니다.";
        [SerializeField] private Color clearColor = new Color(1f, 0.82f, 0.3f);
        [SerializeField] private string overTitle = "GAME OVER";
        [SerializeField] private string overDescription = "쓰러졌습니다. 다시 도전해 보세요.";
        [SerializeField] private Color overColor = new Color(0.85f, 0.15f, 0.12f);

        private void Awake()
        {
            panel.SetActive(false);
            retryButton.onClick.AddListener(PlayClick);
            quitButton.onClick.AddListener(PlayClick);
            retryButton.onClick.AddListener(Retry);
            quitButton.onClick.AddListener(Quit);
        }

        private void Start()
        {
            GameManager.Instance.GameEnded += OnGameEnded;
        }

        private void OnDestroy()
        {
            var gameManager = GameManager.ExistingInstance;
            if (gameManager)
                gameManager.GameEnded -= OnGameEnded;
        }

        private void OnGameEnded(GameState result)
        {
            var cleared = result == GameState.Cleared;
            titleLabel.text = cleared ? clearTitle : overTitle;
            titleLabel.color = cleared ? clearColor : overColor;
            descriptionLabel.text = cleared ? clearDescription : overDescription;
            Invoke(nameof(ShowPanel), delayBeforeShowing);
        }

        private void ShowPanel()
        {
            panel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static void PlayClick() => AudioManager.Instance.Play(Sfx.UiClick);

        private static void Retry()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
