using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TikBot.Models;
using System.Collections.Generic;

namespace TikBot.Services
{
    public class GroupService
    {
        private readonly Dictionary<long, GroupType> _groups = new Dictionary<long, GroupType>
        {
            { -4652850967, GroupType.Complaint },
            { -4786584682, GroupType.Feedback },
            { -4678544514, GroupType.Appreciation }
        };

        private readonly List<long> _upgradeGroupIds = new List<long>
        {
            -1002387367673, -1002265372057, -1002425604571, -1002265542165, -1002442281434,
            -1002323122323, -1002399312916, -1002494878701, -1002359151658, -1002312083613,
            -1002336420670, -1002252209057, -1002288537568, -1002147598381, -1002432438472,
            -1002348615709, -1002386104492, -1002261612336, -1002354809400, -1002462078310,
            -1002296791437, -1002485281665, -1002229520287, -1002219380740, -1002493313580,
            -1002305135501, -1002226487468, -1002250077408, -1002377606568, -1002283627498,
            -1002369680862, -1002366401983, -1002340389556, -1002457681170, -1002282958575,
            -1002345691106, -1002263464228, -1002342706025, -1002289369260, -1002461696025,
            -1002499493489, -1002414588780, -1002361581752
        };

        public GroupType GetGroupType(long groupId)
        {
            if (_groups.TryGetValue(groupId, out var type))
                return type;

            return _upgradeGroupIds.Contains(groupId) ? GroupType.Upgrade : GroupType.Progress;
        }
    }
}
