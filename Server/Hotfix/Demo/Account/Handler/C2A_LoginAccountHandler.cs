using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ET
{
    public class C2A_LoginAccountHandler : AMRpcHandler<C2A_LoginAccount, A2C_LoginAccount>
    {
        protected override async ETTask Run(Session session, C2A_LoginAccount request, A2C_LoginAccount response, Action reply)
        {
            if (session.DomainScene().SceneType != SceneType.Account)
            {
                Log.Error($"请求的Scene错误，当前Scene为：{session.DomainScene().SceneType}");
                session.Dispose();
                return;
            }
            session.RemoveComponent<SessionAcceptTimeoutComponent>();
            //防止多次点击登录
            if(session.GetComponent<SessionLockingComponent>() != null)
            {
                response.Error = ErrorCode.ERR_RequestRepeatedly;
                reply();
                session.Disconnect().Coroutine();
            }
            if(string.IsNullOrEmpty(request.AccountName)|| string.IsNullOrEmpty(request.Password))
            {
                response.Error = ErrorCode.ERR_LoginInfoError;
                reply();//服务器向客户端发消息
                session.Disconnect().Coroutine();//立即调用Dispose 消息还没发送到客户端 这样是错误的
                return;
            }
            if (!Regex.IsMatch(request.AccountName.Trim(), @"'(?=.*[0-9].*)(?=.*[A-Z].*)(?=.*[a-z].*).{6,15}$'")){
                response.Error = ErrorCode.ERR_LoginInfoError;
                reply();
                session.Disconnect().Coroutine();
                return;
            }
            if (!Regex.IsMatch(request.Password.Trim(), @"'(?=.*[0-9].*)(?=.*[A-Z].*)(?=.*[a-z].*).{6,15}$'")){
                response.Error = ErrorCode.ERR_LoginInfoError;
                reply();
                session.Disconnect().Coroutine();
                return;
            }
            //增加组件 SessionLockingComponent 保证是玩家唯一一次登录
            //using 关键字 在逻辑执行完成后会自动调用组件的Dispose自动释放
            //下方逻辑大部分都是异步的
            using (session.AddComponent<SessionLockingComponent>())
            {
                //用协程锁锁住这段代码 防止多个玩家同时注册或登录
                //避免造成两个玩家同时注册同一个账号
                //协程锁使用hash值 不理解？？
                using(await CoroutineLockComponent.Instance.Wait(CoroutineLockType.LoginAccount,request.AccountName.Trim().GetHashCode()))
                {
                    var accountInfoList = await DBManagerComponent.Instance.GetZoneDB(session.DomainZone()).Query<Account>(d => d.AccountName.Equals(request.AccountName.Trim()));
                    Account account = null;
                    if (accountInfoList !=null && accountInfoList.Count > 0)
                    {
                        account = accountInfoList[0];
                        session.AddChild(account);
                        if (account.AccountType == (int)AccountType.BlackList)
                        {
                            response.Error = ErrorCode.ERR_LoginInfoError;
                            reply();
                            session.Disconnect().Coroutine();
                            account?.Dispose();
                            return;
                        }
                        if (!account.Password.Equals(request.Password))
                        {
                            response.Error = ErrorCode.ERR_LoginInfoError;
                            reply();
                            session.Disconnect().Coroutine();
                            account?.Dispose();
                            return;
                        }
                    }
                    else
                    {
                        account = session.AddChild<Account>();
                        account.AccountName = request.AccountName.Trim();
                        account.Password = request.Password;
                        account.CreateTime = TimeHelper.ServerNow();
                        account.AccountType = (int)AccountType.General;
                        await DBManagerComponent.Instance.GetZoneDB(session.DomainZone()).Save<Account>(account);
                    }
                    string Token = TimeHelper.ServerNow().ToString() + RandomHelper.RandomNumber(int.MinValue, int.MaxValue).ToString();
                    session.DomainScene().GetComponent<TokenComponent>().Remove(account.Id);
                    session.DomainScene().GetComponent<TokenComponent>().Add(account.Id, Token);
                    response.AccountId = account.Id;
                    response.Token = Token;
                    reply();
                    account?.Dispose();
                }
            }
        }
    }
}
