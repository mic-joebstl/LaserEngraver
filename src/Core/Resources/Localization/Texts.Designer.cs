﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LaserEngraver.Core.Resources.Localization {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Texts {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Texts() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("LaserEngraver.Core.Resources.Localization.Texts", typeof(Texts).Assembly);
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
        ///   Looks up a localized string similar to Engraving.
        /// </summary>
        internal static string EngraveJobTitle {
            get {
                return ResourceManager.GetString("EngraveJobTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Framing.
        /// </summary>
        internal static string FramingJobTitle {
            get {
                return ResourceManager.GetString("FramingJobTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Homing.
        /// </summary>
        internal static string HomingJobTitle {
            get {
                return ResourceManager.GetString("HomingJobTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to More than one device found. Please configure &quot;PortName&quot; from the list:
        ///{0}.
        /// </summary>
        internal static string MoreThanOneDeviceFoundExceptionFormat {
            get {
                return ResourceManager.GetString("MoreThanOneDeviceFoundExceptionFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Positioning.
        /// </summary>
        internal static string MoveAbsoluteJobTitle {
            get {
                return ResourceManager.GetString("MoveAbsoluteJobTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No device found.
        /// </summary>
        internal static string NoDeviceFoundException {
            get {
                return ResourceManager.GetString("NoDeviceFoundException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Device disconnected unexpectedly.
        /// </summary>
        internal static string UnexpectedDeviceDisconnectedExceptionMessage {
            get {
                return ResourceManager.GetString("UnexpectedDeviceDisconnectedExceptionMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unexpected response &quot;{0}&quot;.
        /// </summary>
        internal static string UnexpectedResponseExceptionFormat {
            get {
                return ResourceManager.GetString("UnexpectedResponseExceptionFormat", resourceCulture);
            }
        }
    }
}
