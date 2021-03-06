﻿using System;
using System.Xml.XPath;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using NuGet.Frameworks;
using ReferenceGenerator.Engine;

namespace ReferenceGenerator
{
    class Program
    {
        static XElement CreateDependencyElement(NuGetFramework tfm, IEnumerable<Package> refs, XNamespace nuspecNs)
        {
            var ele = new XElement(nuspecNs + "group", new XAttribute("targetFramework", tfm.GetShortFolderName()),
                                   refs.Select(r =>
                                               new XElement(nuspecNs + "dependency",
                                                            new XAttribute("id", r.Id),
                                                            new XAttribute("version", $"{r.VersionString}")
                                                   )));

            return ele;
        }

        static XElement GetOrCreateDependenciesNode(XDocument doc, XNamespace nuspecNs)
        {
            var mde = doc.Root.Element(nuspecNs + "metadata");
            if (mde == null)
                throw new ArgumentException("NuSpec XML namespaces are not correctly formed. Ensure the xmlns is on the root package element", nameof(doc));

            var deps = mde.Element(nuspecNs + "dependencies");

            if (deps == null)
            {
                deps = new XElement(nuspecNs + "dependencies");
                mde.Add(deps);
            }

            return deps;
        }

        enum Command 
        {
           Single,
           Cross
        }

        static int Main(string[] args)
        {
            
            
            try
            {
                if (args.Length > 0 && (args[0].StartsWith("\"") || args[0].StartsWith(".")))
                {
                    // HACK: detect param 1 based on starting with a " or a .
                    GenerateV1(args);
                }
                else
                {

                    var command = Command.Single;
                    string project = null;
                    string baseDirectory = null;
                    string libraryName = null;
                    string nuSpec = null;
                    string moniker = null;
                    string tfms = null;
                    string files = null;

                    ArgumentSyntax.Parse(args, syntax =>
                    {
                        // args 0: NuGetTargetMonikers -- .NETStandard,Version=v1.4  
                        // args 1: TFM's to generate, semi-colon joined. E.g.: auto;uap10.0 
                        // args 2: nuspec file
                        // args 3: project file (csproj/vbproj, etc). Used to look for packages.config/project.json and references. should match order of target files
                        // args 4: target files, semi-colon joined
                        syntax.DefineCommand("generate-single", ref command, Command.Single, "Single platform");
                        syntax.DefineOption("m|moniker", ref moniker, "NuGetTargetMonikers -- .NETStandard,Version=v1.4");
                        syntax.DefineOption("t|tfm", ref tfms, "TFM\'s to generate, semi-colon joined. E.g.: auto;uap10.0");
                        syntax.DefineOption("n|nuspec", ref nuSpec, "Full path to NuSpec");
                        syntax.DefineOption("p|project", ref project, "Path to project file");
                        syntax.DefineOption("f|file", ref files, "target files, semi-colon joined");

                        
                        syntax.DefineCommand("generate-cross", ref command, Command.Cross, "Cross Platform");
                        syntax.DefineOption("p|project", ref project, "Path to project file");
                        syntax.DefineOption("d|directory", ref baseDirectory, "Base directory where output folders are created");
                        syntax.DefineOption("n|nuspec", ref nuSpec, "Full path to NuSpec");
                        syntax.DefineOption("l|library", ref libraryName, "Library name, including .dll");

                        
                        // common validation
                        if (string.IsNullOrWhiteSpace(project))
                            syntax.ReportError("project is required");
                        if (string.IsNullOrWhiteSpace(nuSpec))
                            syntax.ReportError("nuspec is required");
                        

                        if (command == Command.Single)
                        {
                            if (string.IsNullOrWhiteSpace(moniker))
                                syntax.ReportError("moniker is required");

                            if (string.IsNullOrWhiteSpace(tfms))
                                syntax.ReportError("tfm is required");

                            if (string.IsNullOrWhiteSpace(files))
                                syntax.ReportError("file is required");
                        }

                        if (command == Command.Cross)
                        {
                            if (string.IsNullOrWhiteSpace(baseDirectory))
                                syntax.ReportError("directory is required");
                            if (string.IsNullOrWhiteSpace(libraryName))
                                syntax.ReportError("library is required");
                        }
                        
                    });

                    if (command == Command.Single)
                    {
                        GenerateV1(new [] {moniker, tfms, nuSpec, project, files});
                    }
                    else if (command == Command.Cross)
                    {
                        GenerateV2(project, baseDirectory, libraryName, nuSpec);
                    }

                }
                
                return 0;
            }
            catch (UnixNotSupportedException)
            {
                // If we're in a place where we cannot check reference assemblies on Unix, issue a 
                // warning and return a non-error code 
                Console.Error.WriteLine(WarningWithMessage.ClassicPclUnix);
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(new ErrorWithMessage(e));

                return -1;
            }
        }

        static void GenerateV2(string projectFile, string baseDirectory, string dllFile, string nuspecFile)
        {

            var projDir = Path.GetDirectoryName(projectFile);
            var lockFile = Path.Combine(projDir, "project.lock.json");


            var targets = ProjectEngine.GetTargetFrameworksFromProjectJson(lockFile);

            foreach (var target in targets)
            {
                var lib = Path.Combine(baseDirectory, target.GetShortFolderName(), dllFile);
                var assm = AssemblyInfo.GetAssemblyInfo(lib);
                var pkgs = ProjectEngine.GetProjectJsonPackages(lockFile, assm.References, new [] {target});

                var platforms = ProjectEngine.GetNonReferencePackages(projectFile, target);
                pkgs = pkgs.Except(platforms);

                // Now squash all but most recent
                var groups = ProjectEngine.GetSortedMostRecentVersions(pkgs);

                // make sure there is no mscorlib
                if (groups.Any(g => string.Equals(g.Id, "mscorlib", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("mscorlib-based projects are not supported");


                UpdateNuspecFile(nuspecFile, groups, new[] { target });
            }
        }

        static void GenerateV1(string[] args)
        {
            // args 0: NuGetTargetMonikers -- .NETStandard,Version=v1.4  
            // args 1: TFM's to generate, semi-colon joined. E.g.: auto;uap10.0 
            // args 2: nuspec file
            // args 3: project file (csproj/vbproj, etc). Used to look for packages.config/project.json and references. should match order of target files
            // args 4: target files, semi-colon joined

            var nugetTargetMonikers = args[0].Split(';')
                                             .Where(s => !string.IsNullOrWhiteSpace(s))
                                             .Select(NuGetFramework.Parse)
                                             .Where(tfm => tfm.Framework != ".NETPlatform")
                                             .ToArray();
            var tfms = args[1].Split(';')
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .Select(NuGetFramework.Parse)
                              .ToArray();

            var nuspecFile = args[2];

            var projectFiles = args[3].Split(';')
                                      .Where(s => !string.IsNullOrWhiteSpace(s))
                                      .ToArray();
            var files = args[4].Split(';')
                               .Where(s => !string.IsNullOrWhiteSpace(s))
                               .ToArray();


            // calc target for PCL profiles
            var firstTfm = nugetTargetMonikers.FirstOrDefault();
            if (firstTfm != null)
            {
                for (var i = 0; i < tfms.Length; i++)
                {
                    // look for an unsupported TFM and calc the result
                    if (!tfms[i].IsUnsupported)
                        continue;

                    if (firstTfm.IsPCL)
                    {
                        var profileVer = int.Parse(nugetTargetMonikers[0].Profile.Substring(7), CultureInfo.InvariantCulture);
                        // map the PCL profile to a netstandard target
                        tfms[i] = DefaultPortableFrameworkMappings.Instance.CompatibilityMappings.First(t => t.Key == profileVer)
                                                                  .Value.Max;
                    }
                    else if (firstTfm.IsPackageBased)
                    {
                        tfms[i] = firstTfm;
                    }
                    else
                    {
                        Console.Error.WriteLine(ErrorWithMessage.TargetFrameworkNotFound);
                    }
                }
            }


            var packages = new List<PackageWithReference>();

            for (var i = 0; i < projectFiles.Length; i++)
            {
                var assm = AssemblyInfo.GetAssemblyInfo(files[i]);
                var projectFileName = Path.GetFileNameWithoutExtension(projectFiles[i]);

                var projDir = Path.GetDirectoryName(projectFiles[i]);
                if (File.Exists(Path.Combine(projDir, $"{projectFileName}.project.json")))
                {
                    // ProjectName.Project.json
                    var lockFile = Path.Combine(projDir, $"{projectFileName}.project.lock.json");

                    var pkgs = ProjectEngine.GetProjectJsonPackages(lockFile, assm.References, nugetTargetMonikers);
                    packages.AddRange(pkgs);
                }
                else if (File.Exists(Path.Combine(projDir, "project.json")))
                {
                    // Project.json
                    var lockFile = Path.Combine(projDir, "project.lock.json");
                    var pkgs = ProjectEngine.GetProjectJsonPackages(lockFile, assm.References, nugetTargetMonikers);
                    packages.AddRange(pkgs);
                }
                else if (File.Exists(Path.Combine(projDir, $"packages.{projectFileName}.config")))
                {
                    var pkgs = ProjectEngine.GetPackagesConfigPackages(projectFiles[i], $"packages.{projectFileName}.config", assm.References);
                    packages.AddRange(pkgs);
                }
                else if (File.Exists(Path.Combine(projDir, "packages.config")))
                {
                    var pkgs = ProjectEngine.GetPackagesConfigPackages(projectFiles[i], "packages.config", assm.References);
                    packages.AddRange(pkgs);
                }
                else
                {
                    // Must be an "old" PCL without any refs. Best we can do is read the refs
                    var pkgs = ProjectEngine.GetPackagesConfigPackages(projectFiles[i], null, assm.References);
                    packages.AddRange(pkgs);
                }
            }


            // Now squash all but most recent
            var groups = ProjectEngine.GetSortedMostRecentVersions(packages);

            // make sure there is no mscorlib
            if (groups.Any(g => string.Equals(g.Id, "mscorlib", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("mscorlib-based projects are not supported");


            UpdateNuspecFile(nuspecFile, groups, tfms);
        }

        static void UpdateNuspecFile(string nuspecFile, IReadOnlyList<PackageWithReference> packages, IEnumerable<NuGetFramework> tfms)
        {
            // Takes the input TFMs that the user specified and writes. For portable, we squash "inbox" references and then apply baseline updates
            foreach (var tfm in tfms)
            {
#if false
                foreach (var tuple in ProjectEngine.SquashBuiltInPackages(packages, tfm).OrderBy(t => t.Item1.GetShortFolderName()))
                {
                    var baselined = ProjectEngine.ApplyBaselinePackageVersions(tuple.Item2)
                                                 .ToList();
                    UpdateNuSpecFileForTfm(nuspecFile, baselined, tuple.Item1);
                }
#endif

                var baselined = ProjectEngine.ApplyBaselinePackageVersions(packages)
                                                 .ToList();

                UpdateNuSpecFileForTfm(nuspecFile, baselined, tfm);
            }
        }

        /// <summary>
        ///     Writes a specific TFM and its packages into the nuspec file
        /// </summary>
        /// <param name="nuspecFile"></param>
        /// <param name="framework"></param>
        /// <param name="packages"></param>
        static void UpdateNuSpecFileForTfm(string nuspecFile, IReadOnlyList<Package> packages, NuGetFramework framework)
        {
            var refNames = new HashSet<string>(packages.Select(g => g.Id), StringComparer.OrdinalIgnoreCase);


            XmlNamespaceManager nsm;
            // Read nuspec as xml
            using (var reader = XmlReader.Create(nuspecFile))
            {
                nsm = new XmlNamespaceManager(reader.NameTable);
            }

            var xdoc = XDocument.Load(nuspecFile);

            // get the default namespace
            var name = xdoc.Root.Attribute("xmlns")
                          ?.Value ?? string.Empty;
            nsm.AddNamespace("ns", name);

            XNamespace nuspecNs = name;


            var deps = GetOrCreateDependenciesNode(xdoc, nuspecNs);


            var ele = CreateDependencyElement(framework, packages, nuspecNs);

            // see if we have a node with this tfm
            var grp = deps.XPathSelectElement($"./ns:group[@targetFramework='{framework.GetShortFolderName()}']", nsm);
            if (grp != null)
            {
                // Need to merge
                // find nodes that match by name, remove and then readd them
                var existing = grp.Elements(nuspecNs + "dependency")
                                  .Where(e => refNames.Contains(e.Attribute("id")
                                                                 .Value))
                                  .ToList();

                foreach (var xe in existing)
                {
                    xe.Remove();
                }

                // Add the new ones back in 
                grp.Add(ele.Elements());
            }
            else
            {
                deps.Add(ele);
            }


            xdoc.Save(nuspecFile, SaveOptions.OmitDuplicateNamespaces); // TODO: handle read-only files and return error
        }
    }
}