﻿using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Carnation
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("c1c74746-265a-4718-a5ab-63963d38e8bb")]
    public class MainWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow() : base(null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            VSServiceHelpers.GlobalServiceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.OLE.Interop.IServiceProvider));

            Caption = "Carnation";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            Content = new MainWindowControl();
        }
    }
}
