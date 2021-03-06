﻿using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using EnvDTE;
using Microsoft.VisualStudio.OLE.Interop;
#pragma warning disable VSTHRD010
namespace ParsleyDevstudio
{
    using Process = System.Diagnostics.Process;

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("Gplex", "Generate C# lexers", "1.2")]
    [Guid("B1D25618-1FCF-42FB-B785-F097A8EF5DB6")]
    [ComVisible(true)]
    [ProvideObject(typeof(Gplex))]
    [CodeGeneratorRegistration(typeof(Gplex), "Gplex", "{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}", GeneratesDesignTimeSource = true)]
    // if we supported VB for this we'd add the following, but Gplex only emits C#
    //[CodeGeneratorRegistration(typeof(Gplex),"Gplex", "{164b10b9-b200-11d0-8c61-00a0c91e29d5}",GeneratesDesignTimeSource =true)]
    public sealed class Gplex : IVsSingleFileGenerator, IObjectWithSite
    {
        object _site;
        Array _projects;
        ServiceProvider _serviceProvider;

        public Gplex()
        {
            EnvDTE.DTE dte;
            try
            {
                dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
                _projects = (Array)dte.ActiveSolutionProjects;
            }
            catch
            {
                dte = null;
                _projects = null;
            }

        }
        ProjectItem _FindItem(string path)
        {
            int iFound = 0;
            uint itemId = 0;
            EnvDTE.ProjectItem item;
            Microsoft.VisualStudio.Shell.Interop.VSDOCUMENTPRIORITY[] pdwPriority = new Microsoft.VisualStudio.Shell.Interop.VSDOCUMENTPRIORITY[1];
            for(var i = 0;i<_projects.Length;i++)
            {
                Microsoft.VisualStudio.Shell.Interop.IVsProject vsProject = VSUtility.ToVsProject(_projects.GetValue(i) as EnvDTE.Project);
                vsProject.IsDocumentInProject(path, out iFound, pdwPriority, out itemId);
                if (iFound != 0 && itemId != 0)
                {
                    Microsoft.VisualStudio.OLE.Interop.IServiceProvider oleSp = null;
                    vsProject.GetItemContext(itemId, out oleSp);
                    if (null!= oleSp)
                    {
                        ServiceProvider sp = new ServiceProvider(oleSp);
                        // convert our handle to a ProjectItem
                        item = sp.GetService(typeof(EnvDTE.ProjectItem)) as EnvDTE.ProjectItem;
                        return item;
                    }
                    
                }
            }
            return null;


        }
        #region IVsSingleFileGenerator Members

        public int DefaultExtension(out string pbstrDefaultExtension)
        {
            pbstrDefaultExtension = ".log";
            return pbstrDefaultExtension.Length;
        }
        ServiceProvider SiteServiceProvider {
            get {
                if (null == _site)
                    return null;
                if (null==_serviceProvider)
                {
                    var oleServiceProvider = _site as Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
                    _serviceProvider = new ServiceProvider(oleServiceProvider);
                }
                return _serviceProvider;
            }
        }

        public int Generate(string wszInputFilePath, string bstrInputFileContents,
          string wszDefaultNamespace, IntPtr[] rgbOutputFileContents,
          out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
        {
            string log="";
            try
            {
                if (null == _site)
                    throw new InvalidOperationException("The Gplex custom tool can only be used in a design time environment. Consider using Gplex as a pre-build step instead.");
                wszInputFilePath = Path.GetFullPath(wszInputFilePath);
                var item = _FindItem(wszInputFilePath);
                if (null == item)
                    throw new ApplicationException("Design time environment project item fetch failed.");
                if (0 != string.Compare("cs", VSUtility.GetProjectLanguageFromItem(item), StringComparison.InvariantCultureIgnoreCase))
                    throw new NotSupportedException("The Gplex generator only supports C# projects");
                var dir = Path.GetDirectoryName(wszInputFilePath);
                var scannerFile = Path.GetFileNameWithoutExtension(wszInputFilePath) + ".cs";
                foreach (EnvDTE.ProjectItem childItem in item.ProjectItems)
                    childItem.Delete();
                // we don't add it to our generated files if it already exists
                // TODO: see if we can check if it's part of the project instead (more robust)
                var addGplexBuffers = !File.Exists(Path.Combine(dir, "GplexBuffers.cs"));
                pGenerateProgress.Progress(0, 2);
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "gplex";
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.Arguments = "/out:\"" + Path.Combine(dir, scannerFile).Replace("\"", "\"\"") + "\"";
                psi.Arguments += " \"" + wszInputFilePath.Replace("\"", "\"\"") + "\"";
                // HACK: Have to do this so Gplex spits out GplexBuffers.cs in the correct place:
                psi.WorkingDirectory = dir;
                psi.RedirectStandardOutput = true;

                using (var proc = new Process())
                {
                    proc.StartInfo = psi;
                    proc.Start();
                    log = proc.StandardOutput.ReadToEnd().TrimEnd();
                }
                var isSuccess = log.EndsWith("Completed successfully", StringComparison.InvariantCulture);
                var outputPath = Path.Combine(dir, scannerFile);
                pGenerateProgress.Progress(1, 2);
                if (isSuccess)
                {
                    var idx = log.IndexOf("GPLEX: opened output file <");
                    if (-1 < idx)
                    {
                        idx += 27; // len of above str
                        var len = log.IndexOfAny(new char[] { '\r', '\n' }, idx);
                        var p = log.Substring(idx, len - idx - 1);
                        outputPath = Path.GetFullPath(p);
                    }
                    if (addGplexBuffers)
                    {
                        idx = log.IndexOf("GPLEX: created file <GplexBuffers.cs>");
                        if (0 > idx)
                            addGplexBuffers = false;
                    }
                    EnvDTE.ProjectItem outitm = item.ProjectItems.AddFromFile(outputPath);
                    EnvDTE.ProjectItem gpbufitm = null;
                    if (addGplexBuffers)
                        gpbufitm = item.ProjectItems.AddFromFile(Path.Combine(dir,"GplexBuffers.cs"));

                }
                else
                    pGenerateProgress.GeneratorError(0, 0, "Gplex failed. See log for details", unchecked((uint)-1), unchecked((uint)-1));


            }
            catch (Exception ex)
            {
                pGenerateProgress.GeneratorError(0, 0, "Gplex custom tool failed with: " + ex.Message, unchecked((uint)-1), unchecked((uint)-1));
                log += "Gplex custom tool failed with: " + ex.Message;
            }
            finally
            {
                try
                {
                    pGenerateProgress.Progress(2, 2);
                }
                catch { }
            }
            // have used streams here in the past to scale, but even for huge items, this is faster!
            // most likely due to the lack of extra copies (memory/array resizing)
            byte[] bytes = Encoding.UTF8.GetBytes(log);
            int length = bytes.Length;
            rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(length);
            Marshal.Copy(bytes, 0, rgbOutputFileContents[0], length);
            pcbOutput = (uint)length;
            return VSConstants.S_OK;
        }

        #endregion

        #region IObjectWithSite Members
        
        public void GetSite(ref Guid riid, out IntPtr ppvSite)
        {
            if (null == this._site)
            {
                throw new Win32Exception(-2147467259);
            }

            IntPtr objectPointer = Marshal.GetIUnknownForObject(this._site);

            try
            {
                Marshal.QueryInterface(objectPointer, ref riid, out ppvSite);
                if (ppvSite == IntPtr.Zero)
                {
                    throw new Win32Exception(-2147467262);
                }
            }
            finally
            {
                if (objectPointer != IntPtr.Zero)
                {
                    Marshal.Release(objectPointer);
                    objectPointer = IntPtr.Zero;
                }
            }
        }

        public void SetSite(object pUnkSite)
        {
            this._site = pUnkSite;
        }

        #endregion

    }
}
#pragma warning restore VSTHRD010