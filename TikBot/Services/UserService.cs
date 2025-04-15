using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TikBot.Models;

namespace TikBot.Services
{
    public class UserService
    {
        private readonly List<Mentor> _mentors = new List<Mentor>
        {
            new Mentor { FullName = "شکوفه آریائی", ChatId = 1831938157, UpgradeGroupId = -1002387367673 },
            new Mentor { FullName = "زری پیوند", ChatId = 7789439996, UpgradeGroupId = -1002265372057 },
            new Mentor { FullName = "مریم ریاحی", ChatId = 6860721539, UpgradeGroupId = -1002425604571 },
            new Mentor { FullName = "متین عربیان", ChatId = 1711179858, UpgradeGroupId = -1002265542165 },
            new Mentor { FullName = "مریم فرهادی", ChatId = 2033476190, UpgradeGroupId = -1002442281434 },
            new Mentor { FullName = "نرگس اجلی", ChatId = 1473088759, UpgradeGroupId = -1002323122323 },
            new Mentor { FullName = "محمدمهدی اسفندیاری", ChatId = 5600405688, UpgradeGroupId = -1002399312916 },
            new Mentor { FullName = "نیلوفر توان", ChatId = 1311532257, UpgradeGroupId = -1002494878701 },
            new Mentor { FullName = "فاطمه مرادی", ChatId = 735414053, UpgradeGroupId = -1002359151658 },
            new Mentor { FullName = "صبا بذرافشان", ChatId = 99942457, UpgradeGroupId = -1002312083613 },
            new Mentor { FullName = "مهسا بزرگی", ChatId = 7910843553, UpgradeGroupId = -1002336420670 },
            new Mentor { FullName = "ریحانه رئیسی", ChatId = 6105977474, UpgradeGroupId = -1002252209057 },
            new Mentor { FullName = "نسرین معدلتی", ChatId = 171263213, UpgradeGroupId = -1002288537568 },
            new Mentor { FullName = "محترم عبدی", ChatId = 1647661529, UpgradeGroupId = -1002147598381 },
            new Mentor { FullName = "محمدخادم امیری", ChatId = 93158340, UpgradeGroupId = -1002432438472 },
            new Mentor { FullName = "احسان قربانی", ChatId = 169171953, UpgradeGroupId = -1002348615709 },
            new Mentor { FullName = "مریم صلابتی", ChatId = 5734058197, UpgradeGroupId = -1002386104492 },
            new Mentor { FullName = "شنو وهابی", ChatId = 819671186, UpgradeGroupId = -1002261612336 },
            new Mentor { FullName = "خاطره فتحی", ChatId = 1509665818, UpgradeGroupId = -1002354809400 },
            new Mentor { FullName = "مهدیه مقدم", ChatId = 6212483631, UpgradeGroupId = -1002462078310 },
            new Mentor { FullName = "حسنی رضازاده", ChatId = 133588124, UpgradeGroupId = -1002296791437 },
            new Mentor { FullName = "سحر مورکیان", ChatId = 169815067, UpgradeGroupId = -1002485281665 },
            new Mentor { FullName = "نسیم محتشم", ChatId = 795714487, UpgradeGroupId = -1002229520287 },
            new Mentor { FullName = "محدثه یاسینی", ChatId = 1294241730, UpgradeGroupId = -1002219380740 },
            new Mentor { FullName = "زهرا خزایی", ChatId = 5781498206, UpgradeGroupId = -1002493313580 },
            new Mentor { FullName = "پونه علی‌محمدی", ChatId = 1512095976, UpgradeGroupId = -1002305135501 },
            new Mentor { FullName = "ریبین یارویسی", ChatId = 5753140597, UpgradeGroupId = -1002226487468 },
            new Mentor { FullName = "ثنا عبداللهی", ChatId = 1355170189, UpgradeGroupId = -1002250077408 },
            new Mentor { FullName = "کاوه پارسا", ChatId = 7572739007, UpgradeGroupId = -1002377606568 },
            new Mentor { FullName = "رعنا سبحانی", ChatId = 2127690656, UpgradeGroupId = -1002283627498 },
            new Mentor { FullName = "ساینا بناکار", ChatId = 136037587, UpgradeGroupId = -1002369680862 },
            new Mentor { FullName = "مهسا مومیوند", ChatId = 951230299, UpgradeGroupId = -1002366401983 },
            new Mentor { FullName = "الهام طلوعی‌فر", ChatId = 1300610466, UpgradeGroupId = -1002340389556 },
            new Mentor { FullName = "زهرا زمانی", ChatId = 2058590103, UpgradeGroupId = -1002457681170 },
            new Mentor { FullName = "آیدا نجفی", ChatId = 2107367296, UpgradeGroupId = -1002282958575 },
            new Mentor { FullName = "شهیره قادری", ChatId = 367051858, UpgradeGroupId = -1002345691106 },
            new Mentor { FullName = "نازنین امیری", ChatId = 517642118, UpgradeGroupId = -1002263464228 },
            new Mentor { FullName = "فاطمه مرادپور", ChatId = 1982125823, UpgradeGroupId = -1002342706025 },
            new Mentor { FullName = "الهه محمودی‌نسب", ChatId = 958527214, UpgradeGroupId = -1002289369260 },
            new Mentor { FullName = "عارف مرادی", ChatId = 952210110, UpgradeGroupId = -1002461696025 },
            new Mentor { FullName = "نگین عاشوری", ChatId = 1296928244, UpgradeGroupId = -1002499493489 },
            new Mentor { FullName = "مهسا بزی", ChatId = 7257047570, UpgradeGroupId = -1002414588780 },
            new Mentor { FullName = "حامد", ChatId = 5900030627, UpgradeGroupId = -1002361581752 },
            new Mentor { FullName = "سینا", ChatId = 5317814624, UpgradeGroupId = -1002361581752 },
            new Mentor { FullName = "سارا", ChatId = 7652580327, UpgradeGroupId = -1002361581752 },
        };

        private readonly List<long> _supervisors = new List<long> { 6423450550, 5317814624 };
        private readonly string _mentorsStoragePath = @"C:\TikBotData\Mentors.json";

        public UserService()
        {
            // دیباگ
            Console.WriteLine("UserService constructor started");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_mentorsStoragePath)!);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating directory: {ex.Message}");
            }
            Console.WriteLine("UserService constructor completed");
        }

        public IEnumerable<Mentor> GetAllMentors()
        {
            return _mentors.AsReadOnly();
        }

        public async Task SaveMentorsAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_mentors, new JsonSerializerOptions { WriteIndented = true });
                Directory.CreateDirectory(Path.GetDirectoryName(_mentorsStoragePath)!);
                await File.WriteAllTextAsync(_mentorsStoragePath, json);
                Console.WriteLine("Mentors saved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving mentors: {ex.Message}");
                throw;
            }
        }

        public UserRole GetUserRole(long chatId, long groupId)
        {
            if (_supervisors.Contains(chatId))
                return UserRole.Supervisor;
            var mentor = _mentors.FirstOrDefault(m => m.ChatId == chatId);
            if (mentor != null)
                return mentor.UpgradeGroupId == groupId ? UserRole.Mentor : UserRole.Student;
            return UserRole.Student;
        }

        public bool CanAddBot(long chatId)
        {
            return _supervisors.Contains(chatId);
        }

        public bool IsMentorInGroup(long chatId, long groupId)
        {
            return _mentors.Any(m => m.ChatId == chatId && m.UpgradeGroupId == groupId);
        }

        public Mentor GetMentor(long chatId)
        {
            return _mentors.FirstOrDefault(m => m.ChatId == chatId);
        }

        public IEnumerable<long> GetMentorsInGroup(long groupId)
        {
            return _mentors.Where(m => m.UpgradeGroupId == groupId).Select(m => m.ChatId);
        }
    }
}
