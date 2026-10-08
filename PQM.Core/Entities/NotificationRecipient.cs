using PQM.Core.Entities;

public class NotificationRecipient
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime? ReadAt { get; set; }

    public User User { get; set; } = null!;
}