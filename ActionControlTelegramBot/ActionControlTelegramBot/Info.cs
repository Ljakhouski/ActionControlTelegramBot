using System;
using System.Collections.Generic;
using System.Text;

namespace ActionControlTelegramBot
{
    class UserInfo
    {
        public long ChatId;
        public string UzerName;
        public string LastName;
        public bool RecieveLogs = true;
    }
    class Info
    {
        public string Token;
        public List<UserInfo> Users = new List<UserInfo>();
        public bool ShowWindow = true;
        public bool IgnoreAll = false;
        public bool ContainsUser(long id)
        {
            foreach (UserInfo i in Users)
            {
                if (i.ChatId == id)
                    return true;
            }
            return false;
        }
    }
}
