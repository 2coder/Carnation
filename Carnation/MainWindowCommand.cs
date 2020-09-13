﻿using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Carnation
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class MainWindowCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a432b46a-d8e0-4439-bd9e-58e40c02453c");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private MainWindowCommand(AsyncPackage package, OleMenuCommandService commandService, IComponentModel componentModel)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            ComponentModel = componentModel;
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static MainWindowCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => package;

        /// <summary>
        /// </summary>
        private IComponentModel ComponentModel { get; }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in MainWindowCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var componentModel = (IComponentModel)(await package.GetServiceAsync(typeof(SComponentModel)));
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new MainWindowCommand(package, commandService, componentModel);
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = package.FindToolWindow(typeof(MainWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            var classificationItems = GetClassificationNames()
                .Select(FontsAndColorsHelper.TryGetItemForClassification)
                .OfType<ClassificationGridItem>()
                .ToImmutableArray();
            
            var model = (MainWindowControlViewModel)((MainWindowControl)window.Content).DataContext;
            model.ClassificationGridItems.Clear();

            foreach (var classificationItem in classificationItems)
            {
                model.ClassificationGridItems.Add(classificationItem);
            }

            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private ImmutableArray<string> GetClassificationNames()
        {
            var editorFormatDefinitions = ComponentModel.DefaultExportProvider.GetExportedValues<EditorFormatDefinition>();
            return editorFormatDefinitions.Select(definition => definition.GetType())
                .Where(type => type.GetCustomAttribute<UserVisibleAttribute>()?.UserVisible == true)
                .Select(type => type.GetCustomAttribute<ClassificationTypeAttribute>()?.ClassificationTypeNames)
                .OfType<string>()
                .Distinct()
                .ToImmutableArray();
        }
    }
}
