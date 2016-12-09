﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Drcom_Dialer.Model;
using Drcom_Dialer.Model.Utils;
using Drcom_Dialer.Properties;

namespace Drcom_Dialer.ViewModel
{
    public class ViewModel : NotifyProperty
    {
        public ViewModel()
        {
            View = this;

            //初始化

            NewStatusPresenterModel();

            InitTrayIcon();

            InitializeFieldFormDialerConfig();

            //DialOrHangup = true;
            DialStatus = DialHangupStatus.Disconnect;
        }

        public string Password
        {
            set
            {
                _password = value;
                OnPropertyChanged();
            }
            get
            {
                return _password;
            }
        }

        public string UserName
        {
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
            get
            {
                return _userName;
            }
        }

        public bool IsRememberPassword
        {
            set
            {
                _isRememberPassword = value;
                OnPropertyChanged();
            }
            get
            {
                return _isRememberPassword;
            }
        }

        public bool IsAutoLogin
        {
            set
            {
                _isAutoLogin = value;
                OnPropertyChanged();
            }
            get
            {
                return _isAutoLogin;
            }
        }

        public StatusPresenterModel StatusPresenterModel
        {
            set
            {
                _statusPresenterModel = value;
                OnPropertyChanged();
            }
            get
            {
                return _statusPresenterModel;
            }
        }

        /// <summary>
        ///     抽象
        /// </summary>
        public static ViewModel View
        {
            set;
            get;
        }

        //public bool Enable
        //{
        //    set
        //    {
        //        _enable = value;
        //        OnPropertyChanged();
        //    }
        //    get
        //    {
        //        return _enable;
        //    }
        //}

        /// <summary>
        /// 按钮是否可以按下
        /// true 可以按下
        /// false 不可以按下
        /// </summary>
        public bool DialBtnEnable
        {
            set
            {
                _dialBtnEnable = value;
                OnPropertyChanged();
            }
            get
            {
                return _dialBtnEnable;
            }
        }

        /// <summary>
        /// 按钮内容
        /// </summary>
        public string DialBtnContent => _dialOrHangup ? "拨号" : "断开";

        ///// <summary>
        ///// false 拨号
        ///// true 断开 
        ///// 为true 就可拨号
        ///// </summary>
        //public bool DialOrHangup
        //{
        //    set
        //    {
        //        _dialOrHangup = value;
        //        OnPropertyChanged(nameof(DialBtnContent));
        //        OnPropertyChanged();
        //    }
        //    get
        //    {
        //        return _dialOrHangup;
        //    }
        //}

        public void DialOrHangup()
        {
            if (DialStatus == DialHangupStatus.Disconnect)
            {
                Dial();
            }
            else if (DialStatus == DialHangupStatus.Connect)
            {
                Hangup();
            }
        }

        private DialHangupStatus DialStatus
        {
            set
            {
                _dialStatus = value;
                OnPropertyChanged(nameof(DialBtnContent));
                OnPropertyChanged();
            }
            get
            {
                return _dialStatus;
            }
        }



        private DialHangupStatus _dialStatus;

        private enum DialHangupStatus
        {
            //断开
            Disconnect,
            //连接
            Connect
        }

        public NotifyIcon TrayIcon
        {
            set;
            get;
        }

        /// <summary>
        ///     显示气泡
        /// </summary>
        /// <param name="timeout">消失时间（毫秒）</param>
        /// <param name="title">标题</param>
        /// <param name="text">内容</param>
        /// <param name="icon">图标</param>
        public void ShowBalloonTip(int timeout, string title,
            string text, ToolTipIcon icon = ToolTipIcon.Info)
        {
            TrayIcon.ShowBalloonTip(timeout, title, text, icon);
        }

        /// <summary>
        ///     拨号
        /// </summary>
        private void Dial()
        {
            // 不想写Command

            if (string.IsNullOrEmpty(UserName))
            {
                Notify("请输入账户");
                return;
            }

            if (string.IsNullOrEmpty(Password))
            {
                Notify("请输入密码");
                return;
            }

            DialerConfig.password = Password;
            DialerConfig.username = UserName;

            //开始拨号
            Notify("开始拨号");

            //Enable = false;
            DialBtnEnable = false; //拿出，不应该在Task 

            new Task(() =>
            {
                try
                {
                    //后台保存
                    DialerConfig.SaveConfig();
                    Model.Dial.Auth();
                }
                catch (Exception e)
                {
                    Notify(e.Message);
                    Log4Net.WriteLog(e.Message, e);
                }
                //Enable = true;
            }).Start();
        }

        public void Hangup()
        {
            try
            {
                DialBtnEnable = false;
                NetworkCheck.StopCheck();
                PPPoE.Hangup();
                //DialStatus=DialHangupStatus.Disconnect;
            }
            catch (Exception e)
            {
                Log4Net.WriteLog(e.Message, e);
            }
        }

        private bool _dialBtnEnable = true;

        private bool _dialOrHangup = true;

        //private bool _enable;

        private bool _isAutoLogin;

        private bool _isRememberPassword;

        private string _password;

        private StatusPresenterModel _statusPresenterModel;

        private string _userName;

        /// <summary>
        ///     从配置获字段
        /// </summary>
        private void InitializeFieldFormDialerConfig()
        {
            if (!string.IsNullOrEmpty(DialerConfig.password))
            {
                Password = DialerConfig.password;
            }

            if (!string.IsNullOrEmpty(DialerConfig.username))
            {
                UserName = DialerConfig.username;
            }

            IsRememberPassword = DialerConfig.isRememberPassword;

            IsAutoLogin = DialerConfig.isAutoLogin;
        }


        /// <summary>
        ///     初始化托盘图标
        /// </summary>
        private void InitTrayIcon()
        {
            TrayIcon = new NotifyIcon
            {
                Text = Resources.ProgramTitle,
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                Visible = true
            };
        }

        /// <summary>
        ///     通知
        /// </summary>
        /// <param name="str"></param>
        private void Notify(string str)
        {
            StatusPresenterModel.Status = str;
        }

        /// <summary>
        /// 重新Dial
        /// </summary>
        private void Redial()
        {
            if (DialStatus == DialHangupStatus.Connect)
            {
                Hangup();
                Dial();
            }
        }

        private void NewStatusPresenterModel()
        {
            StatusPresenterModel = new StatusPresenterModel();
            PPPoE.PPPoEDialFailEvent += (s, e) =>
            {
                StatusPresenterModel.Status = e.Message;
                DialBtnEnable = true;
            };
            PPPoE.PPPoEDialSuccessEvent += (s, e) =>
            {
                StatusPresenterModel.Status = "拨号成功，IP: " + e.Message;

                DialBtnEnable = true;
                //DialOrHangup = false;
                DialStatus = DialHangupStatus.Connect;

                if (DialerConfig.isReDialOnFail)
                {
                    NetworkCheck.LoopCheck();
                }
            };
            PPPoE.PPPoEHangupSuccessEvent += (s, e) =>
            {
                StatusPresenterModel.Status = "拨号已断开";
                DialBtnEnable = true;
                //DialOrHangup = true;
                DialStatus = DialHangupStatus.Disconnect;
            };
            PPPoE.PPPoEHangupFailEvent += (s, e) =>
            {
                StatusPresenterModel.Status = e.Message;
                DialBtnEnable = true;
                //DialOrHangup = true;
                DialStatus = DialHangupStatus.Disconnect;
            };
            HeartBeatProxy.HeartbeatExited += (s, code) =>
            {
                StatusPresenterModel.Status = $"心跳终止({code})";
            };
            NetworkCheck.InnerNetworkCheckFailed += (s, e) =>
            {
                Redial();
            };
            NetworkCheck.OuterNetworkCheckFailed += (s, e) =>
            {
                StatusPresenterModel.Status = "似乎无法连接到外网";
                Redial();
            };
            NetworkCheck.OuterNetworkCheckSuccessed += (s, e) =>
            {
                StatusPresenterModel.Status = "拨号成功";
            };
        }
    }
}