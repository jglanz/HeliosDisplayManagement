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
                        SwitchProfile.Execute(profiles, profileIndex);

                        break;
                    case HeliosStartupAction.EditProfile:
                        EditProfile.Execute(profiles, profileIndex);

                        break;
                    case HeliosStartupAction.CreateShortcut:
                        CreateShortcut.Execute(profiles, profileIndex);

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

        
    }
}