using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace PacScripts
{
    /// <summary>
    /// TutorialSlides — 教学场景图片翻页
    /// 点击按钮切下一张图 / 开始游戏；可切换自动翻页
    /// </summary>
    public class TutorialSlides : MonoBehaviour
    {
        // ==================== Inspector ====================

        [Header("【幻灯片图片】")]
        [SerializeField] private Image[] slides;

        [Header("【按钮】")]
        [SerializeField] private Button mainButton;
        [SerializeField] private Button autoButton;

        [Header("【设置】")]
        [SerializeField] private string gameSceneName = "Pacman";
        [SerializeField] private float autoInterval = 2.5f;

        // ==================== 内部状态 ====================

        private int      currentIndex = -1;
        private bool     isAuto = false;
        private float    autoTimer;
        private TMP_Text mainLabel;
        private TMP_Text autoLabel;

        // ==================== 生命周期 ====================

        private void Start()
        {
            foreach (var img in slides)
                if (img != null) img.gameObject.SetActive(false);

            if (mainButton != null)
            {
                mainButton.onClick.AddListener(OnMainClick);
                mainLabel = mainButton.GetComponentInChildren<TMP_Text>();
            }
            if (autoButton != null)
            {
                autoButton.onClick.AddListener(ToggleAuto);
                autoLabel = autoButton.GetComponentInChildren<TMP_Text>();
            }

            // 初始化文字
            RefreshAutoLabel();
            ShowSlide(0);
        }

        private void Update()
        {
            if (!isAuto) return;

            // 任意键翻页（但不触发开始游戏）
            if (Input.anyKeyDown) { ShowSlide(currentIndex + 1); autoTimer = 0f; return; }

            autoTimer += Time.deltaTime;
            if (autoTimer >= autoInterval)
            {
                autoTimer = 0f;
                TryAutoAdvance();
            }
        }

        // ==================== 按钮回调 ====================

        /// <summary>手动点击"下一页/开始游戏"</summary>
        public void OnMainClick()
        {
            autoTimer = 0f;

            if (currentIndex >= slides.Length - 1)
            {
                if (mainLabel != null) mainLabel.text = "加载中...";
                SceneManager.LoadScene(gameSceneName);
                return;
            }

            ShowSlide(currentIndex + 1);
        }

        /// <summary>自动翻页：到最后一张时停止自动并隐藏自动按钮</summary>
        private void TryAutoAdvance()
        {
            if (currentIndex >= slides.Length - 1)
            {
                isAuto = false;
                RefreshAutoLabel();
                if (autoButton != null) autoButton.gameObject.SetActive(false);
                return;
            }
            ShowSlide(currentIndex + 1);
        }

        public void ToggleAuto()
        {
            isAuto = !isAuto;
            autoTimer = 0f;
            RefreshAutoLabel();
        }

        // ==================== 翻页 ====================

        private void ShowSlide(int index)
        {
            if (currentIndex >= 0 && currentIndex < slides.Length && slides[currentIndex] != null)
                slides[currentIndex].gameObject.SetActive(false);

            currentIndex = index;

            if (currentIndex < slides.Length && slides[currentIndex] != null)
                slides[currentIndex].gameObject.SetActive(true);

            // 到最后一张：立刻隐藏自动按钮
            if (currentIndex >= slides.Length - 1)
            {
                isAuto = false;
                if (autoButton != null) autoButton.gameObject.SetActive(false);
            }

            RefreshMainLabel();
        }

        private void RefreshMainLabel()
        {
            if (mainLabel == null) return;
            mainLabel.text = currentIndex >= slides.Length - 1 ? "开始游戏" : "下一页";
        }

        private void RefreshAutoLabel()
        {
            if (autoLabel == null) return;
            autoLabel.text = isAuto ? "自动: 开" : "自动: 关";
        }
    }
}
