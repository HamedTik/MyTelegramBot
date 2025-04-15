using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TikBot.Models;

namespace TikBot.Services
{
    public class GroupInfoService
    {
        private readonly List<GroupInfo> _groupInfos = new List<GroupInfo>();
        private readonly string _groupInfoStoragePath = @"C:\TikBotData\GroupInfos.json";

        public GroupInfoService()
        {
            SaveGroupInfosAsync().GetAwaiter().GetResult();
        }

        public void RegisterGroupInfo(long groupId, string groupName, string groupLink)
        {
            if (!_groupInfos.Any(g => g.GroupId == groupId))
            {
                _groupInfos.Add(new GroupInfo
                {
                    GroupId = groupId,
                    GroupName = groupName,
                    GroupLink = groupLink
                });
            }
            else
            {
                var group = _groupInfos.First(g => g.GroupId == groupId);
                group.GroupName = groupName;
                group.GroupLink = groupLink;
            }
            SaveGroupInfosAsync().GetAwaiter().GetResult();
        }

        public async Task SaveGroupInfosAsync()
        {
            var json = JsonSerializer.Serialize(_groupInfos, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(_groupInfoStoragePath)!);
            await File.WriteAllTextAsync(_groupInfoStoragePath, json);
        }

        public GroupInfo GetGroupInfo(long groupId)
        {
            return _groupInfos.FirstOrDefault(g => g.GroupId == groupId);
        }

        public List<long> GetStudentGroups(long studentId)
        {
            // Placeholder: Ideally, link students to groups via StudentService
            return _groupInfos.Select(g => g.GroupId).ToList();
        }
    }
}
