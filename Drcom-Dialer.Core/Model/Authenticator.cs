﻿using Drcom_Dialer.Model.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drcom_Dialer.Model
{
    static class Authenticator
    {
        public static void Init()
        {
            PPPoE.PPPoEDialSuccessEvent += OnPPPoESuccess;
            PPPoE.PPPoEDialFailEvent += OnPPPoEFail;
            PPPoE.PPPoEHangupSuccessEvent += OnPPPoEHangup;

            PPPoE.PPPoEDialSuccessEvent += ExecutePlugin;
        }

        private static void ExecutePlugin(object sender, Msg e)
        {
            try
            {
                Process.Start(@".\auto.exe");
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// 认证
        /// </summary>
        public static void Authenticate()
        {
            string username = "\r\n" + DialerConfig.username;
            string password = DialerConfig.password;
            PPPoE.Dial(username, password);
        }

        /// <summary>
        /// 断开
        /// </summary>
        public static async Task Deauthenticate()
        {
            NetworkCheck.StopCheck();
            await HeartBeatProxy.Kill();
            PPPoE.Hangup();
        }

        /// <summary>
        /// 拨号成功
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="e"></param>
        private static void OnPPPoESuccess(object obj, Msg e)
        {
            switch (HeartBeatUpdate.TryUpdate())
            {
                case Updater.UpdateState.Failed:
                    Log4Net.WriteLog("更新DLL失败");
                    break;
                default:
                    Log4Net.WriteLog("更新DLL成功");
                    break;
            }

            if (HeartBeatUpdate.CheckDLL())
            {
                //获取账户信息
                //除非不知道校区
                //否则延迟获取
                bool delay = DialerConfig.zone != DialerConfig.Campus.Unknown;
                if (!delay)
                    AccountStatus.AccountInfo();

                //开始心跳
                if (!MakeHeartbeat(e.Message))
                {
                    return;
                }

                //获取账户信息
                if (delay && DialerConfig.isNotifyWhenExpire)
                    AccountStatus.AccountInfo();

                //断网检查
                NetworkCheck.LoopCheck();

                //修复VPN
                if (DialerConfig.isFixVPN)
                {
                    VPNFixer.Fix();
                }

                //发送反馈
                BmobAnalyze.SendAnalyze();
            }
            else
            {
                Log4Net.WriteLog("心跳DLL缺失且更新失败");
                ViewModel.Binder.BaseBinder.ShowStatus("心跳DLL缺失且更新失败");
            }
        }

        private static bool MakeHeartbeat(string ipmsg)
        {
            if (HeartBeatProxy.Init() != HeartBeatProxy.HeadBeatStatus.Success)
            {
                Log4Net.WriteLog("初始化心跳失败");
                ViewModel.Binder.BaseBinder.ShowStatus("初始化心跳失败");
            }
            else
            {
                HeartBeatProxy.HeadBeatStatus stat = HeartBeatProxy.Heartbeat();
                if (stat == HeartBeatProxy.HeadBeatStatus.Success)
                {
                    ViewModel.Binder.BaseBinder.ShowStatus($"认证成功,IP: {ipmsg}");
                    return true;
                }
                else
                {
                    Log4Net.WriteLog("心跳失败");
                    ViewModel.Binder.BaseBinder.ShowStatus("心跳失败");
                }
            }
            return false;
        }

        /// <summary>
        /// 拨号错误
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="e"></param>
        private static void OnPPPoEFail(object obj, Msg e)
        {
            Log4Net.WriteLog(nameof(OnPPPoEFail));
        }

        /// <summary>
        /// 注销
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="e"></param>
        private static void OnPPPoEHangup(object obj, EventArgs e)
        {
            Log4Net.WriteLog(nameof(OnPPPoEHangup));
        }
    }
}
