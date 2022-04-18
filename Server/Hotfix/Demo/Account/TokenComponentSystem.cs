using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public static class TokenComponentSystem
    {
        public static void Add(this TokenComponent self,long key, string token)
        {
            self.TokenDictionary.Add(key, token);
            self.TimeOutRemoveKey(key, token).Coroutine();
        }
        public static string Get(this TokenComponent self,long key)
        {
            string value = null;
            self.TokenDictionary.TryGetValue(key, out value);
            return value;
        }
        public static void Remove(this TokenComponent self,long key)
        {
            if (self.TokenDictionary.ContainsKey(key))
            {
                self.TokenDictionary.Remove(key);
            }
        }
        private static async ETTask TimeOutRemoveKey(this TokenComponent self,long key,string tokenKey)
        {
            await TimerComponent.Instance.WaitAsync(36000000);//等待10分钟
            string onlineToken = self.Get(key);
            if (!string.IsNullOrEmpty(onlineToken) && onlineToken == tokenKey)//不为空并且相等
            {
                self.Remove(key);
            }
        }
    }
}
