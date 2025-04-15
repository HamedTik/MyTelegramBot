using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Telegram.Bot;
using TikBot.Models;

namespace TikBot.Services
{
    public class InviteService
    {
        private readonly List<InviteRecord> _inviteRecords = new List<InviteRecord>();
        private readonly string _storagePath = @"C:\TikBotData\Tickets_Invites.json";

        public InviteService()
        {
            SaveInvitesAsync().GetAwaiter().GetResult();
        }
        public bool CanInvite(long mentorId, long groupId, out string errorMessage)
        {
            errorMessage = string.Empty;
            var lastInvite = _inviteRecords
                .Where(r => r.MentorId == mentorId && r.GroupId == groupId)
                .OrderByDescending(r => r.InvitedAt)
                .FirstOrDefault();

            if (lastInvite != null)
            {
                var timeSinceLastInvite = DateTime.UtcNow - lastInvite.InvitedAt;
                if (timeSinceLastInvite < TimeSpan.FromDays(7))
                {
                    var remainingTime = TimeSpan.FromDays(7) - timeSinceLastInvite;
                    var days = remainingTime.Days;
                    var hours = remainingTime.Hours;
                    var minutes = remainingTime.Minutes;
                    errorMessage = $"شما هر ۷ روز یک‌بار می‌توانید از این دکمه استفاده کنید\nزمان باقی‌مانده: {days} روز {hours}:{minutes:D2}";
                    return false;
                }
            }

            return true;
        }

        public void RecordInvite(long mentorId, long groupId)
        {
            _inviteRecords.Add(new InviteRecord
            {
                MentorId = mentorId,
                GroupId = groupId,
                InvitedAt = DateTime.UtcNow
            });
            SaveInvitesAsync().GetAwaiter().GetResult();
        }

        public async Task SaveInvitesAsync()
        {
            var existingData = File.Exists(_storagePath)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(await File.ReadAllTextAsync(_storagePath))
                : new Dictionary<string, object>();
            existingData["Invites"] = _inviteRecords;
            var json = JsonSerializer.Serialize(existingData, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(_storagePath)!);
            await File.WriteAllTextAsync(_storagePath, json);
        }

        public async Task<string> GetGroupInviteLinkAsync(ITelegramBotClient botClient, long groupId, CancellationToken cancellationToken)
        {
            try
            {
                // Try to get existing invite link
                var chat = await botClient.GetChatAsync(groupId, cancellationToken);
                if (!string.IsNullOrEmpty(chat.InviteLink))
                {
                    return chat.InviteLink;
                }

                // If no link, create a new one
                var inviteLink = await botClient.ExportChatInviteLinkAsync(groupId, cancellationToken);
                return inviteLink;
            }
            catch
            {
                // Fallback: return null if bot lacks permissions
                return null;
            }
        }
    }
}
