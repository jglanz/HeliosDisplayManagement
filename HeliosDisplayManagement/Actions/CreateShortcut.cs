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
    internal static class CreateShortcut {
        public static bool Execute(IReadOnlyList<Profile> profiles, int profileIndex) {
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

            return true;
        }
    }
}