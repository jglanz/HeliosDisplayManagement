using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WindowsDisplayAPI.DisplayConfig;
using HeliosDisplayManagement.Shared.Resources;
using Newtonsoft.Json;
using NvAPIWrapper.GPU;
using NvAPIWrapper.Mosaic;
using NvAPIWrapper.Native.Mosaic;
using Console = System.Console;
using Path = HeliosDisplayManagement.Shared.Topology.Path;

namespace HeliosDisplayManagement.Shared {
    public class Profile: IEquatable<Profile> {
        private static Profile _currentProfile;

        public static Version Version = new Version(2, 0);

        static Profile() {
            try {
                NvAPIWrapper.NVIDIA.Initialize();
            } catch {
                // ignored
            }
        }

        public string Id { get; set; } = Guid.NewGuid().ToString("B");

        [JsonIgnore]
        public bool IsActive {
            get {
                if (_currentProfile == null) {
                    _currentProfile = GetCurrent(string.Empty);
                }

                return _currentProfile.Equals(this);
            }
        }

        [JsonIgnore]
        public bool IsPossible {
            get {
                var surroundTopologies =
                    Paths.SelectMany(path => path.Targets)
                        .Select(target => target.SurroundTopology)
                        .Where(topology => topology != null).ToArray();

                if (surroundTopologies.Length > 0) {
                    try {
                        // Not working quite well yet
                        //var status =
                        //    GridTopology.ValidateGridTopologies(
                        //        SurroundTopologies.Select(topology => topology.ToGridTopology()).ToArray(),
                        //        SetDisplayTopologyFlag.MaximizePerformance);
                        //return status.All(topologyStatus => topologyStatus.Errors == DisplayCapacityProblem.NoProblem);

                        // Least we can do is to check for the availability of all display devices
                        var displayDevices =
                            PhysicalGPU.GetPhysicalGPUs()
                                .SelectMany(gpu => gpu.GetDisplayDevices())
                                .Select(device => device.DisplayId);

                        if (!
                            surroundTopologies.All(
                                topology =>
                                    topology.Displays.All(display => displayDevices.Contains(display.DisplayId)))) {
                            return false;
                        }

                        // And to see if one path have two surround targets
                        if (Paths.Any(path => path.Targets.Count(target => target.SurroundTopology != null) > 1)) {
                            return false;
                        }

                        return true;
                    } catch {
                        // ignore
                    }

                    return false;
                }

                return true;
                //return PathInfo.ValidatePathInfos(Paths.Select(path => path.ToPathInfo()));
            }
        }

        public string Name { get; set; }

        public Path[] Paths { get; set; } = new Path[0];

        public static string ProfilesPathOverride { get; set; } = null;

        public static string ProfilesPath {
            get {
                if (ProfilesPathOverride != null) {
                    return ProfilesPathOverride;
                }

                return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Assembly.GetExecutingAssembly().GetName().Name, $"DisplayProfiles_{Version.ToString(2)}.json");
            }
        }

        /// <inheritdoc />
        public bool Equals(Profile other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            var otherPaths = other.Paths;

            return Paths.All(path => other.Paths.Contains(path)) &&
                   otherPaths.All(path => Paths.Contains(path));
        }

        public static IEnumerable<Profile> GetAllProfiles(string profilesFilePath = null) {
            try {
                if (profilesFilePath == null) {
                    profilesFilePath = ProfilesPath;
                }

                if (File.Exists(profilesFilePath)) {
                    var json = File.ReadAllText(profilesFilePath, Encoding.ASCII);

                    if (!string.IsNullOrWhiteSpace(json)) {
                        var profiles = JsonConvert.DeserializeObject<Profile[]>(json,
                            new JsonSerializerSettings {
                                MissingMemberHandling = MissingMemberHandling.Ignore,
                                NullValueHandling = NullValueHandling.Ignore,
                                DefaultValueHandling = DefaultValueHandling.Include,
                                TypeNameHandling = TypeNameHandling.Auto
                            });

                        // if (profiles.Any()) {
                        //     SetAllProfiles(profiles);
                        // }

                        return profiles;
                    }
                }
            } catch {
                // ignored
            }

            return Array.Empty<Profile>();
        }

        public static Profile GetCurrent(string name = null) {
            _currentProfile = new Profile {
                Name = name, Paths = PathInfo.GetActivePaths().Select(info => new Path(info)).ToArray()
            };

            return _currentProfile;
        }

        public static bool operator ==(Profile left, Profile right) {
            return Equals(left, right) || left?.Equals(right) == true;
        }

        public static bool operator !=(Profile left, Profile right) {
            return !(left == right);
        }

        public static void ClearCurrentProfileCache() {
            _currentProfile = null;
        }

        public static bool SetAllProfiles(IEnumerable<Profile> array, string profilesFilePath = null) {
            try {
                if (profilesFilePath == null) {
                    profilesFilePath = ProfilesPath;
                }

                var json = JsonConvert.SerializeObject(array.ToArray(), Formatting.Indented,
                    new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Include,
                        DefaultValueHandling = DefaultValueHandling.Populate,
                        TypeNameHandling = TypeNameHandling.Auto
                    });

                if (!string.IsNullOrWhiteSpace(json)) {
                    var dir = System.IO.Path.GetDirectoryName(profilesFilePath);

                    if (dir != null) {
                        Directory.CreateDirectory(dir);
                        File.WriteAllText(profilesFilePath, json, Encoding.Unicode);

                        return true;
                    }
                }
            } catch {
                // ignored
            }

            return false;
        }

        /// <inheritdoc />
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj.GetType() != GetType()) {
                return false;
            }

            return Equals((Profile)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode() {
            unchecked {
                return (Paths?.GetHashCode() ?? 0) * 397;
            }
        }

        /// <inheritdoc />
        public override string ToString() {
            return (Name ?? Language.UN_TITLED_PROFILE) + (IsActive ? " " + Language._Active_ : "");
        }

        public bool Apply() {
            const int MaxTryCount = 15;
            try {
                // Thread.Sleep(2000);

                try {
                    var surroundTopologies =
                        Paths.SelectMany(path => path.Targets)
                            .Select(target => target.SurroundTopology)
                            .Where(topology => topology != null)
                            .Select(topology => topology.ToGridTopology())
                            .ToArray();

                    if (surroundTopologies.Length == 0) {
                        var currentTopologies = GridTopology.GetGridTopologies();

                        if (currentTopologies.Any(topology => topology.Rows * topology.Columns > 1)) {
                            surroundTopologies =
                                GridTopology.GetGridTopologies()
                                    .SelectMany(topology => topology.Displays)
                                    .Select(displays => new GridTopology(1, 1, new[] { displays }))
                                    .ToArray();
                        }
                    }

                    if (surroundTopologies.Length > 0) {
                        GridTopology.SetGridTopologies(surroundTopologies, SetDisplayTopologyFlag.MaximizePerformance);
                    }

                    Thread.Sleep(18000);
                } catch {
                    // ignored
                }

                var pathInfos = Paths.Select(path => path.ToPathInfo()).Where(info => info != null).ToArray();

                if (!pathInfos.Any()) {
                    throw new InvalidOperationException(
                        @"Display configuration changed since this profile is created. Please re-create this profile.");
                }

                PathInfo.ApplyPathInfos(pathInfos, true, true, true);
                bool failed = true;
                for (var tryCount = 0; tryCount < MaxTryCount; tryCount++) {
                    ClearCurrentProfileCache();
                    if (IsActive) {
                        failed = false;
                        Console.WriteLine($@"Activated profile: {Name}");
                        break;
                    }
                    Console.WriteLine($@"Profile switching pending ({tryCount + 1} of {MaxTryCount} tries)");
                    Thread.Sleep(1000);
                }

                Console.WriteLine($@"Completed success = {!failed}");

                // var toast = new ToastContentBuilder();
                //
                // if (!failed) {
                //     toast.AddText($@"Successfully activated profile ""{Name}"" ");
                // } else {
                //     toast.AddText($@"Failed to activate profile ""{Name}"" ");
                // }
                // toast.Show();

                return true;
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, @"Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            } finally {
                ClearCurrentProfileCache();
            }
        }

        public Profile Clone() {
            try {
                var serialized = JsonConvert.SerializeObject(this);

                var cloned = JsonConvert.DeserializeObject<Profile>(serialized);
                cloned.Id = Guid.NewGuid().ToString("B");

                return cloned;
            } catch {
                return null;
            }
        }
    }
}