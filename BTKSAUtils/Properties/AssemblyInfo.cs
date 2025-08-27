﻿using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("BTKSAUtils")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("BTK-Development")]
[assembly: AssemblyProduct("BTKSAUtils")]
[assembly: AssemblyCopyright("Copyright © 2025")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a48b4245-1bb9-4fba-9ff3-43927bb769f1")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.1.6")]
[assembly: AssemblyFileVersion("1.1.6")]
[assembly: MelonInfo(typeof(BTKSAUtils.BTKSAUtils), BTKSAUtils.BuildInfo.Name, BTKSAUtils.BuildInfo.Version, BTKSAUtils.BuildInfo.Author, BTKSAUtils.BuildInfo.DownloadLink)]
[assembly: MelonGame("ChilloutVR", "ChilloutVR")]
[assembly: MelonColor(255, 139,0,139)]
[assembly: MelonAuthorColor(255, 139,0,139)]
[assembly: HarmonyDontPatchAll]
[assembly: MelonOptionalDependencies("TotallyWholesome")]