using UnityEngine;
using UnityEngine.UI;
using TMPro; // Cần thư viện TextMeshPro

/// <summary>
/// Gắn script này vào Prefab "GoalIcon"
/// </summary>
public class GoalIcon : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Gán Image của prefab này vào (cho màu jelly)")]
    [SerializeField] private Image iconImage;
    [Tooltip("Gán TextMeshPro (TMP) của prefab này vào (cho số lượng)")]
    [SerializeField] private TMP_Text amountText;
    [Tooltip("Gán Image dùng để hiển thị dấu tick khi hoàn thành. Để trống nếu không dùng.")]
    [SerializeField] private Image tickImage;

    // Lưu lại màu để UIManager có thể tìm
    public JellyColor ColorType { get; private set; }

    void Awake()
    {
        // (MỚI) Đảm bảo dấu tick ban đầu bị ẩn
        if (tickImage != null)
        {
            tickImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Được gọi bởi UIManager để cài đặt prefab
    /// </summary>
    public void SetGoal(JellyColor color, int amount)
    {
        ColorType = color;
        
        iconImage.color = JellyPiece.GetColorValue(color);
        
        UpdateAmount(amount);
    }

    /// <summary>
    /// Chỉ cập nhật số lượng
    /// </summary>
    public void UpdateAmount(int newAmount)
    {
        if (newAmount > 0)
        {
            // Vẫn còn mục tiêu
            amountText.text = newAmount.ToString();
            amountText.gameObject.SetActive(true); // Đảm bảo số lượng hiện ra
            if (tickImage != null) tickImage.gameObject.SetActive(false); // Ẩn dấu tick
            iconImage.color = JellyPiece.GetColorValue(ColorType); // Đảm bảo màu jelly là màu gốc
        }
        else // newAmount <= 0 (Đã hoàn thành mục tiêu)
        {
            amountText.text = ""; // Xóa số lượng
            amountText.gameObject.SetActive(false); // Tắt TextMeshPro

            if (tickImage != null)
            {
                tickImage.gameObject.SetActive(true); // Hiện dấu tick
            }
        }
    }
}