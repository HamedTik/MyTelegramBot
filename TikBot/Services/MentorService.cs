using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TikBot.Models;

namespace TikBot.Services
{
    public class MentorService
    {
        private readonly List<LeaveRecord> _leaveRecords = new List<LeaveRecord>();
        private readonly List<CapacityRecord> _capacityRecords = new List<CapacityRecord>();
        private readonly string _leaveStoragePath = @"C:\TikBotData\LeaveRecords.json";
        private readonly string _capacityStoragePath = @"C:\TikBotData\CapacityRecords.json";

        public MentorService()
        {
            Console.WriteLine("MentorService constructor started");
            try
            {
                LoadDataAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading MentorService data: {ex.Message}");
            }
            Console.WriteLine("MentorService constructor completed");
        }

        private async Task LoadDataAsync()
        {
            try
            {
                if (File.Exists(_leaveStoragePath))
                {
                    var json = await File.ReadAllTextAsync(_leaveStoragePath);
                    var leaveRecords = JsonSerializer.Deserialize<List<LeaveRecord>>(json);
                    if (leaveRecords != null) _leaveRecords.AddRange(leaveRecords);
                }
                if (File.Exists(_capacityStoragePath))
                {
                    var json = await File.ReadAllTextAsync(_capacityStoragePath);
                    var capacityRecords = JsonSerializer.Deserialize<List<CapacityRecord>>(json);
                    if (capacityRecords != null) _capacityRecords.AddRange(capacityRecords);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LoadDataAsync: {ex.Message}");
                throw;
            }
        }

        public async Task SaveLeaveRecordsAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_leaveRecords, new JsonSerializerOptions { WriteIndented = true });
                Directory.CreateDirectory(Path.GetDirectoryName(_leaveStoragePath)!);
                await File.WriteAllTextAsync(_leaveStoragePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving leave records: {ex.Message}");
                throw;
            }
        }

        public async Task SaveCapacityRecordsAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_capacityRecords, new JsonSerializerOptions { WriteIndented = true });
                Directory.CreateDirectory(Path.GetDirectoryName(_capacityStoragePath)!);
                await File.WriteAllTextAsync(_capacityStoragePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving capacity records: {ex.Message}");
                throw;
            }
        }

        public bool CanTakeLeave(long mentorId, out string errorMessage)
        {
            errorMessage = string.Empty;
            var weekStart = GetWeekStart(DateTime.UtcNow);
            var weekEnd = weekStart.AddDays(7);

            var leaveThisWeek = _leaveRecords
                .Where(r => r.MentorId == mentorId && r.LeaveDate >= weekStart && r.LeaveDate < weekEnd)
                .Any();

            if (leaveThisWeek)
            {
                var daysUntilSaturday = (weekEnd - DateTime.UtcNow).Days;
                errorMessage = $"شما از مرخصی هفتگیت استفاده کردی تا روز شنبه صبر کن";
                return false;
            }

            return true;
        }

        public void RecordLeave(long mentorId)
        {
            _leaveRecords.Add(new LeaveRecord
            {
                MentorId = mentorId,
                LeaveDate = DateTime.UtcNow
            });
        }

        public bool IsOnLeaveTomorrow(long mentorId)
        {
            var today = DateTime.UtcNow.Date;
            return _leaveRecords.Any(r => r.MentorId == mentorId && r.LeaveDate.Date == today);
        }

        public void RecordMentorCapacity(long mentorId, int proposedCapacity)
        {
            var record = _capacityRecords.FirstOrDefault(r => r.MentorId == mentorId);
            if (record == null)
            {
                record = new CapacityRecord { MentorId = mentorId };
                _capacityRecords.Add(record);
            }
            record.MentorProposedCapacity = proposedCapacity;
        }

        public void RecordSupervisorCapacity(long mentorId, int approvedCapacity)
        {
            var record = _capacityRecords.FirstOrDefault(r => r.MentorId == mentorId);
            if (record == null)
            {
                record = new CapacityRecord { MentorId = mentorId };
                _capacityRecords.Add(record);
            }
            record.SupervisorApprovedCapacity = approvedCapacity;
        }

        public void AddStudentToMentor(long mentorId)
        {
            var record = _capacityRecords.FirstOrDefault(r => r.MentorId == mentorId);
            if (record != null)
            {
                record.CurrentStudents++;
            }
        }

        public int GetRemainingCapacity(long mentorId)
        {
            var record = _capacityRecords.FirstOrDefault(r => r.MentorId == mentorId);
            return record != null ? Math.Max(0, record.SupervisorApprovedCapacity - record.CurrentStudents) : 0;
        }

        public int GetCurrentStudentCount(long mentorId)
        {
            var record = _capacityRecords.FirstOrDefault(r => r.MentorId == mentorId);
            return record?.CurrentStudents ?? 0;
        }

        public List<CapacityRecord> GetCapacityRecords()
        {
            return _capacityRecords.Where(r => r.SupervisorApprovedCapacity > r.CurrentStudents).ToList();
        }

        private DateTime GetWeekStart(DateTime date)
        {
            var daysSinceSaturday = (int)date.DayOfWeek - (int)DayOfWeek.Saturday;
            if (daysSinceSaturday < 0) daysSinceSaturday += 7;
            return date.Date.AddDays(-daysSinceSaturday);
        }
    }
}
