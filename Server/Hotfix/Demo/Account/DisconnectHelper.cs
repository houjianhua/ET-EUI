using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ET
{
    public static class DisconnectHelper
    {
        /**延迟释放*/
        public static async ETTask Disconnect(this Session self)
        {
            if(self == null || self.IsDisposed)
            {
                return;
            }
            long instanceId = self.InstanceId;//session 释放还是1秒前的状态
            await TimerComponent.Instance.WaitAsync(1000);
            if (self.InstanceId != instanceId)
            {
                return ;
            }
            self.Dispose();
        }
    }
}
