﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ReferenceGenerator.Engine.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ReferenceGenerator.Engine.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot;?&gt;
        ///&lt;Project ToolsVersion=&quot;12.0&quot; DefaultTargets=&quot;Build&quot; xmlns=&quot;http://schemas.microsoft.com/developer/msbuild/2003&quot;&gt;
        ///&lt;!-- From https://github.com/dotnet/buildtools/blob/master/src/Microsoft.DotNet.Build.Tasks.Packaging/src/PackageFiles/baseline.packages.targets --&gt;
        ///  &lt;ItemGroup&gt;
        ///    &lt;BaseLinePackage Include=&quot;Microsoft.CSharp&quot;&gt;
        ///      &lt;Version&gt;4.0.1&lt;/Version&gt;
        ///    &lt;/BaseLinePackage&gt;
        ///    &lt;BaseLinePackage Include=&quot;Microsoft.VisualBasic&quot;&gt;
        ///      &lt;Version&gt;10.0.1&lt;/Version&gt;        /// [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string baseline_packages {
            get {
                return ResourceManager.GetString("baseline_packages", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot;?&gt;
        ///&lt;FileList&gt;
        ///  &lt;!-- this lists the highest stable version of a contract at the point
        ///       in which we switched from contract versioning (eg: 4.0.API.0) to
        ///       semantic versioning (eg: 4.API.Bugfix.servicing).  --&gt;
        ///
        ///  &lt;File AssemblyName=&quot;Microsoft.CSharp&quot; Version=&quot;4.0.0.0&quot; /&gt;
        ///  &lt;File AssemblyName=&quot;Microsoft.VisualBasic&quot; Version=&quot;10.0.0.0&quot; /&gt;
        ///  &lt;File AssemblyName=&quot;Microsoft.Win32.Primitives&quot; Version=&quot;4.0.0.0&quot; /&gt;
        ///  &lt;File AssemblyName=&quot;System.AppContext&quot; Versio [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string LastNonSemanticVersions {
            get {
                return ResourceManager.GetString("LastNonSemanticVersions", resourceCulture);
            }
        }
    }
}
