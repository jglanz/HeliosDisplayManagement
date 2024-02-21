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

    internal static class GoProfile {
        public static bool execute(Profile profile) {
            if (profile.IsActive) {
                return true;
            }

            var instanceStatus = IPCService.GetInstance().Status;

            try {
                IPCService.GetInstance().Status = InstanceStatus.Busy;
                var failed = false;

                if (new SplashForm(() => {
                        Task.Factory.StartNew(() => {
                            if (!profile.Apply()) {
                                failed = true;
                            }
                        }, TaskCreationOptions.LongRunning);
                    }, 3, 30).ShowDialog() !=
                    DialogResult.Cancel) {
                    if (failed) {
                        throw new Exception(Language.Profile_is_invalid_or_not_possible_to_apply);
                    }

                    return true;
                }

                return false;
            } finally {
                IPCService.GetInstance().Status = instanceStatus;
            }
        }
    }
}
