
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaWheel : MonoBehaviour
{
    [Header("Wheel")]
    [SerializeField] private RectTransform wheel;
    [SerializeField] private float spinDuration = 4f;
    [SerializeField] private int minFullRotations = 4;
    [SerializeField] private int maxFullRotations = 7;

    [Header("Buttons")]
    [SerializeField] private Button x1Button;
    [SerializeField] private Button x5Button;
    [SerializeField] private Button okButton;

    [Header("Notice Panel")]
    [SerializeField] private GameObject noticePanel;
    [SerializeField] private TMP_Text noticeText;

    private bool isSpinning;
    private float currentAngle;

    private void Start()
    {
        // Đăng ký sự kiện nút bấm
        x1Button.onClick.AddListener(SpinX1);
        x5Button.onClick.AddListener(SpinX5);
        okButton.onClick.AddListener(CloseNotice);

        // Ẩn thông báo lúc bắt đầu
        noticePanel.SetActive(false);

        // Lấy góc hiện tại của vòng quay
        currentAngle = wheel.localEulerAngles.z;
    }

    public void SpinX1()
    {
        StartSpin();
    }

    public void SpinX5()
    {
        StartSpin();
    }

    private void StartSpin()
    {
        // Không cho quay khi đang quay
        // hoặc thông báo vẫn đang mở
        if (isSpinning || noticePanel.activeSelf)
            return;

        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        isSpinning = true;

        x1Button.interactable = false;
        x5Button.interactable = false;

        // Chọn số vòng quay ngẫu nhiên
        int fullRotations = Random.Range(
            minFullRotations,
            maxFullRotations + 1
        );

        // Góc dừng ngẫu nhiên
        float finalAngle = Random.Range(0f, 360f);

        float startAngle = currentAngle;
        float totalRotation =
            fullRotations * 360f + finalAngle;

        float elapsed = 0f;

        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / spinDuration
            );

            // Ease-out: quay nhanh rồi chậm dần
            float easedT = 1f - Mathf.Pow(1f - t, 4f);

            currentAngle =
                startAngle + totalRotation * easedT;

            wheel.localRotation =
                Quaternion.Euler(0f, 0f, currentAngle);

            yield return null;
        }

        currentAngle = startAngle + totalRotation;

        wheel.localRotation =
            Quaternion.Euler(0f, 0f, currentAngle);

        // Chỉ hiện thông báo khi vòng quay đã dừng
        noticeText.text = "You received ...";
        noticePanel.SetActive(true);

        isSpinning = false;
    }

    public void CloseNotice()
    {
        if (isSpinning)
            return;

        noticePanel.SetActive(false);

        x1Button.interactable = true;
        x5Button.interactable = true;
    }
}
