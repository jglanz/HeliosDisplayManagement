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
using HeliosDisplayManagement.Actions;
namespace HeliosDisplayManagement {
    static class Program {

        private static void CreateShortcut(IReadOnlyList<Profile> profiles, int profileIndex) {
            if (profileIndex < 0) {
                throw new Exception(Language.Selected_profile_is_invalid_or_not_found);
            }

            var opts = CommandLineOptions.Default;
            IPCService.GetInstance().Status = InstanceStatus.User;
            new ShortcutForm(profiles[profileIndex]) {
                FileName = opts.ExecuteFilename,
                SteamAppId = opts.ExecuteSteamApp,
                Arguments = opts.ExecuteArguments,
                ProcessName = opts.ExecuteProcessName,
                Timeout = opts.ExecuteProcessTimeout
            }.ShowDialog();
        }

        private static void EditProfile(IList<Profile> profiles, int profileIndex) {
            if (profileIndex < 0) {
                throw new Exception(Language.Selected_profile_is_invalid_or_not_found);
            }

            IPCService.GetInstance().Status = InstanceStatus.User;
            var editForm = new EditForm(profiles[profileIndex]);

            if (editForm.ShowDialog() == DialogResult.OK) {
                profiles[profileIndex] = editForm.Profile;
            }

            if (!Profile.SetAllProfiles(profiles)) {
                throw new Exception(Language.Failed_to_save_profile);
            }
        }

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var opts = CommandLineOptions.Default;
            try {
                if (!IPCService.StartService()) {
                    throw new Exception(Language.Can_not_open_a_named_pipe_for_Inter_process_communication);
                }

                var configFileOverride = opts.ConfigFilename;
                if (configFileOverride != null && File.Exists(configFileOverride)) {
                    Profile.ProfilesPathOverride = configFileOverride;
                }

                Profile[] profiles = Profile.GetAllProfiles(configFileOverride).ToArray();
                string profileId = opts.ProfileId;
                int profileIndex = !string.IsNullOrWhiteSpace(profileId) &&
                                   profiles.Length > 0
                    ? Array.FindIndex(profiles,
                        p =>
                            p.Id.Equals(profileId,
                                StringComparison.InvariantCultureIgnoreCase) ||
                            p.Name.Equals(profileId,
                                StringComparison.InvariantCultureIgnoreCase))
                    : -1;

                switch (opts.Action) {
                    case HeliosStartupAction.SwitchProfile:
                        SwitchProfile(profiles, profileIndex);

                        break;
                    case HeliosStartupAction.EditProfile:
                        EditProfile(profiles, profileIndex);

                        break;
                    case HeliosStartupAction.CreateShortcut:
                        CreateShortcut(profiles, profileIndex);

                        break;
                    default:
                        IPCService.GetInstance().Status = InstanceStatus.User;
                        Application.Run(new MainForm());

                        break;
                }
            } catch (Exception e) {
                MessageBox.Show(
                    string.Format(Language.Operation_Failed, e.Message),
                    Language.Fatal_Error,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void SwitchProfile(IReadOnlyList<Profile> profiles, int profileIndex) {
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


            GoProfile.execute(profiles[profileIndex]);

            // if (!string.IsNullOrWhiteSpace(opts.ExecuteFilename)) {
            //     if (!File.Exists(opts.ExecuteFilename)) {
            //         throw new Exception(Language.Executable_file_not_found);
            //     }
            //
            //     if (!GoProfile.execute(profiles[profileIndex])) {
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
            //         if (!GoProfile.execute(rollbackProfile)) {
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
            //     if (!GoProfile.execute(profiles[profileIndex])) {
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
            //         if (!GoProfile.execute(rollbackProfile)) {
            //             throw new Exception(Language.Can_not_change_active_profile);
            //         }
            //     }
            // } else {
            //     if (!GoProfile.execute(profiles[profileIndex])) {
            //         throw new Exception(Language.Can_not_change_active_profile);
            //     }
            // }
        }
    }
}