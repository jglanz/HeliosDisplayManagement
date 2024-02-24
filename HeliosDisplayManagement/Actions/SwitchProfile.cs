using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HeliosDisplayManagement.InterProcess;
using HeliosDisplayManagement.Resources;
using HeliosDisplayManagement.Shared;
using HeliosDisplayManagement.Steam;
using HeliosDisplayManagement.UIForms;

namespace HeliosDisplayManagement.Actions {
    internal static class SwitchProfile {
        public static bool Execute(IReadOnlyList<Profile> profiles, int profileIndex) {
            var opts = CommandLineOptions.Default;
            var rollbackProfile = Profile.GetCurrent(string.Empty);

            if (profileIndex < 0) {
                throw new Exception(Language.Selected_profile_is_invalid_or_not_found);
            }

            if (!profiles[profileIndex].IsPossible) {
                throw new Exception(Language.Selected_profile_is_not_possible);
            }

            if (
                IPCClient.QueryAll()
                    .Any(
                        client =>
                            client.Status == InstanceStatus.Busy ||
                            client.Status == InstanceStatus.OnHold)) {
                throw new Exception(
                    Language
                        .Another_instance_of_this_program_is_in_working_state_Please_close_other_instances_before_trying_to_switch_profile);
            }

            return GoProfile.Execute(profiles[profileIndex]);

            // if (!string.IsNullOrWhiteSpace(opts.ExecuteFilename)) {
            //     if (!File.Exists(opts.ExecuteFilename)) {
            //         throw new Exception(Language.Executable_file_not_found);
            //     }
            //
            //     if (!GoProfile.Execute(profiles[profileIndex])) {
            //         throw new Exception(Language.Can_not_change_active_profile);
            //     }
            //
            //     var process = Process.Start(opts.ExecuteFilename,
            //         opts.ExecuteArguments);
            //     var processes = new Process[0];
            //
            //     if (!string.IsNullOrWhiteSpace(opts.ExecuteProcessName)) {
            //         var ticks = 0;
            //
            //         while (ticks < opts.ExecuteProcessTimeout * 1000) {
            //             processes = Process.GetProcessesByName(opts.ExecuteProcessName);
            //
            //             if (processes.Length > 0) {
            //                 break;
            //             }
            //
            //             Thread.Sleep(300);
            //             ticks += 300;
            //         }
            //     }
            //
            //     if (processes.Length == 0) {
            //         processes = new[] { process };
            //     }
            //
            //     IPCService.GetInstance().HoldProcessId = processes.FirstOrDefault()?.Id ?? 0;
            //     IPCService.GetInstance().Status = InstanceStatus.OnHold;
            //     NotifyIcon notify = null;
            //
            //     try {
            //         notify = new NotifyIcon {
            //             Icon = Properties.Resources.Icon,
            //             Text = string.Format(
            //                 Language.Waiting_for_the_0_to_terminate,
            //                 processes[0].ProcessName),
            //             Visible = true
            //         };
            //         Application.DoEvents();
            //     } catch {
            //         // ignored
            //     }
            //
            //     foreach (var p in processes) {
            //         try {
            //             p.WaitForExit();
            //         } catch {
            //             // ignored
            //         }
            //     }
            //
            //     if (notify != null) {
            //         notify.Visible = false;
            //         notify.Dispose();
            //         Application.DoEvents();
            //     }
            //
            //     IPCService.GetInstance().Status = InstanceStatus.Busy;
            //
            //     if (!rollbackProfile.IsActive) {
            //         if (!GoProfile.Execute(rollbackProfile)) {
            //             throw new Exception(Language.Can_not_change_active_profile);
            //         }
            //     }
            // } else if (opts.ExecuteSteamApp > 0) {
            //     var steamGame = new SteamGame(opts.ExecuteSteamApp);
            //
            //     if (!SteamGame.SteamInstalled) {
            //         throw new Exception(Language.Steam_is_not_installed);
            //     }
            //
            //     if (!File.Exists(SteamGame.SteamAddress)) {
            //         throw new Exception(Language.Steam_executable_file_not_found);
            //     }
            //
            //     if (!steamGame.IsInstalled) {
            //         throw new Exception(Language.Steam_game_is_not_installed);
            //     }
            //
            //     if (!steamGame.IsOwned) {
            //         throw new Exception(Language.Steam_game_is_not_owned);
            //     }
            //
            //     if (!GoProfile.Execute(profiles[profileIndex])) {
            //         throw new Exception(Language.Can_not_change_active_profile);
            //     }
            //
            //     var address = $"steam://rungameid/{steamGame.AppId}";
            //
            //     if (!string.IsNullOrWhiteSpace(opts.ExecuteArguments)) {
            //         address += "/" + opts.ExecuteArguments;
            //     }
            //
            //     var steamProcess = Process.Start(address);
            //     // Wait for steam game to update and then run
            //     var ticks = 0;
            //
            //     while (ticks < opts.ExecuteProcessTimeout * 1000) {
            //         if (steamGame.IsRunning) {
            //             break;
            //         }
            //
            //         Thread.Sleep(300);
            //
            //         if (!steamGame.IsUpdating) {
            //             ticks += 300;
            //         }
            //     }
            //
            //     IPCService.GetInstance().HoldProcessId = steamProcess?.Id ?? 0;
            //     IPCService.GetInstance().Status = InstanceStatus.OnHold;
            //     NotifyIcon notify = null;
            //
            //     try {
            //         notify = new NotifyIcon {
            //             Icon = Properties.Resources.Icon,
            //             Text = string.Format(
            //                 Language.Waiting_for_the_0_to_terminate,
            //                 steamGame.Name),
            //             Visible = true
            //         };
            //         Application.DoEvents();
            //     } catch {
            //         // ignored
            //     }
            //
            //     // Wait for the game to exit
            //     if (steamGame.IsRunning) {
            //         while (true) {
            //             if (!steamGame.IsRunning) {
            //                 break;
            //             }
            //
            //             Thread.Sleep(300);
            //         }
            //     }
            //
            //     if (notify != null) {
            //         notify.Visible = false;
            //         notify.Dispose();
            //         Application.DoEvents();
            //     }
            //
            //     IPCService.GetInstance().Status = InstanceStatus.Busy;
            //
            //     if (!rollbackProfile.IsActive) {
            //         if (!GoProfile.Execute(rollbackProfile)) {
            //             throw new Exception(Language.Can_not_change_active_profile);
            //         }
            //     }
            // } else {
            //     if (!GoProfile.Execute(profiles[profileIndex])) {
            //         throw new Exception(Language.Can_not_change_active_profile);
            //     }
            // }
        }
    }
}