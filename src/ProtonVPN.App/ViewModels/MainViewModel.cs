﻿/*
 * Copyright (c) 2020 Proton Technologies AG
 *
 * This file is part of ProtonVPN.
 *
 * ProtonVPN is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ProtonVPN is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ProtonVPN.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Threading.Tasks;
using System.Windows.Input;
using Caliburn.Micro;
using GalaSoft.MvvmLight.CommandWpf;
using ProtonVPN.About;
using ProtonVPN.Account;
using ProtonVPN.BugReporting;
using ProtonVPN.Common.Vpn;
using ProtonVPN.Config.Url;
using ProtonVPN.ConnectingScreen;
using ProtonVPN.Core;
using ProtonVPN.Core.Auth;
using ProtonVPN.Core.Events;
using ProtonVPN.Core.Modals;
using ProtonVPN.Core.Service.Vpn;
using ProtonVPN.Core.Vpn;
using ProtonVPN.FlashNotifications;
using ProtonVPN.Map.ViewModels;
using ProtonVPN.Onboarding;
using ProtonVPN.Profiles;
using ProtonVPN.Resources;
using ProtonVPN.Settings;

namespace ProtonVPN.ViewModels
{
    internal class MainViewModel :
        LanguageAwareViewModel,
        IVpnStateAware,
        IOnboardingStepAware
    {
        private readonly UserAuth _userAuth;
        private readonly VpnManager _vpnManager;
        private readonly IActiveUrls _urls;
        private readonly IEventAggregator _eventAggregator;
        private readonly AppExitHandler _appExitHandler;
        private readonly IModals _modals;
        private readonly IDialogs _dialogs;

        private bool _connecting;

        public MainViewModel(
            UserAuth userAuth,
            VpnManager vpnManager,
            IActiveUrls urls,
            IEventAggregator eventAggregator,
            AppExitHandler appExitHandler,
            IModals modals,
            IDialogs dialogs,
            MapViewModel mapViewModel,
            ConnectingViewModel connectingViewModel,
            OnboardingViewModel onboardingViewModel,
            FlashNotificationViewModel flashNotificationViewModel,
            TrayNotificationViewModel trayNotificationViewModel)
        {
            _eventAggregator = eventAggregator;
            _vpnManager = vpnManager;
            _urls = urls;
            _userAuth = userAuth;
            _appExitHandler = appExitHandler;
            _modals = modals;
            _dialogs = dialogs;

            Map = mapViewModel;
            Connection = connectingViewModel;
            Onboarding = onboardingViewModel;
            TrayNotification = trayNotificationViewModel;
            FlashNotification = flashNotificationViewModel;

            eventAggregator.Subscribe(this);

            SettingsCommand = new RelayCommand(SettingsAction, CanClick);
            AccountCommand = new RelayCommand(AccountAction, CanClick);
            ProfilesCommand = new RelayCommand(ProfilesAction, CanClick);
            LogoutCommand = new RelayCommand(LogoutAction);
            ExitCommand = new RelayCommand(ExitAction);
            HelpCommand = new RelayCommand(HelpAction);
            ReportBugCommand = new RelayCommand(ReportBugAction, CanClick);
            AboutCommand = new RelayCommand(AboutAction, CanClick);
        }

        public MapViewModel Map { get; }
        public ConnectingViewModel Connection { get; }
        public OnboardingViewModel Onboarding { get; }
        public TrayNotificationViewModel TrayNotification { get; }
        public FlashNotificationViewModel FlashNotification { get; }


        public ICommand SettingsCommand { get; }
        public ICommand AccountCommand { get; }
        public ICommand ProfilesCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand AboutCommand { get; }
        public ICommand HelpCommand { get; }
        public ICommand ReportBugCommand { get; }
        public ICommand ExitCommand { get; }

        public bool Connecting
        {
            get => _connecting;
            set
            {
                if (_connecting != value)
                {
                    _eventAggregator.PublishOnUIThread(new ToggleOverlay(value));
                }
                Set(ref _connecting, value);
                CommandManager.InvalidateRequerySuggested();
                SetupMenuItems();
            }
        }

        private bool _blockMenu;
        public bool BlockMenu
        {
            get => _blockMenu;
            set => Set(ref _blockMenu, value);
        }
        
        private bool _showOnboarding;
        public bool ShowOnboarding
        {
            get => _showOnboarding;
            set => Set(ref _showOnboarding, value);
        }

        private VpnStatus _vpnStatus;
        public VpnStatus VpnStatus
        {
            get => _vpnStatus;
            set => Set(ref _vpnStatus, value);
        }

        public void Load()
        {
            SetupMenuItems();
        }

        public void ProfilesAction()
        {
            _modals.Show<ProfileListModalViewModel>();
        }

        public Task OnVpnStateChanged(VpnStateChangedEventArgs e)
        {
            VpnStatus = e.State.Status;
            switch (e.State.Status)
            {
                case VpnStatus.Connecting:
                case VpnStatus.Reconnecting:
                case VpnStatus.AssigningIp:
                case VpnStatus.Authenticating:
                case VpnStatus.RetrievingConfiguration:
                case VpnStatus.Waiting:
                    Connecting = true;
                    break;
                case VpnStatus.Connected:
                    Connecting = false;
                    break;
                case VpnStatus.Disconnecting:
                case VpnStatus.Disconnected:
                    Connecting = false;
                    SetupMenuItems();
                    break;
            }

            return Task.CompletedTask;
        }

        public void OnStepChanged(int step)
        {
            ShowOnboarding = step > 0;
        }

        private void SetupMenuItems()
        {
            BlockMenu = Connecting;
        }

        private bool CanClick()
        {
            return !Connecting;
        }

        private void ReportBugAction()
        {
            _modals.Show<ReportBugModalViewModel>();
        }

        private void AboutAction()
        {
            _modals.Show<AboutModalViewModel>();
        }

        private void HelpAction()
        {
            _urls.HelpUrl.Open();
        }

        private async void LogoutAction()
        {
            if (_vpnManager.Status == VpnStatus.Connected)
            {
                var result = _dialogs.ShowQuestion(StringResources.Get("App_msg_LogoutConnectedConfirm"));
                if (!result.HasValue || !result.Value)
                {
                    return;
                }
            }

            _userAuth.Logout();
            await _vpnManager.Disconnect();
        }

        private void ExitAction()
        {
            _appExitHandler.RequestExit();
        }

        private void SettingsAction()
        {
            _modals.Show<SettingsModalViewModel>();
        }

        private void AccountAction()
        {
            _modals.Show<AccountModalViewModel>();
        }
    }
}
