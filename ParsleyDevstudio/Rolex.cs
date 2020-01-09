using System;
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
using System.Collections.Generic;
#pragma warning disable VSTHRD010
namespace ParsleyDevstudio
{
    using Process = System.Diagnostics.Process;

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("Rolex", "Generate simple DFA lexers", "0.2.0.0")]
    [Guid("E9F32CD2-8AD3-4B48-BECC-DD323F632592")]
    [ComVisible(true)]
    [ProvideObject(typeof(Rolex))]
    [CodeGeneratorRegistration(typeof(Rolex), "Rolex", "{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}", GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(typeof(Rolex), "Rolex", "{164b10b9-b200-11d0-8c61-00a0c91e29d5}", GeneratesDesignTimeSource = true)]
    public sealed class Rolex : IVsSingleFileGenerator, IObjectWithSite
    {
        object _site;
        Array _projects;
        ServiceProvider _serviceProvider;

        public Rolex()
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
            for (var i = 0; i < _projects.Length; i++)
            {
                Microsoft.VisualStudio.Shell.Interop.IVsProject vsProject = VSUtility.ToVsProject(_projects.GetValue(i) as EnvDTE.Project);
                vsProject.IsDocumentInProject(path, out iFound, pdwPriority, out itemId);
                if (iFound != 0 && itemId != 0)
                {
                    Microsoft.VisualStudio.OLE.Interop.IServiceProvider oleSp = null;
                    vsProject.GetItemContext(itemId, out oleSp);
                    if (null != oleSp)
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
                if (null == _serviceProvider)
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
            string outputfile = null;
            string log = "";
            try
            {
                if (null == _site)
                    throw new InvalidOperationException("The Rolex custom tool can only be used in a design time environment. Consider using Rolex as a pre-build step instead.");
                wszInputFilePath = Path.GetFullPath(wszInputFilePath);
                var item = _FindItem(wszInputFilePath);
                if (null == item)
                    throw new ApplicationException("Design time environment project item fetch failed.");
                foreach (EnvDTE.ProjectItem childItem in item.ProjectItems)
                    childItem.Delete();

                var dir = Path.GetDirectoryName(wszInputFilePath);
                var lang = VSUtility.GetProjectLanguageFromItem(item);
                if (null == lang)
                    lang = "cs";
                outputfile = Path.Combine(dir, Path.GetFileNameWithoutExtension(wszInputFilePath) + "." + lang);
                var args = new List<string>();
                args.Add(wszInputFilePath);
                args.Add("/output");
                args.Add(outputfile);
                if (0 != string.Compare("cs", lang, StringComparison.InvariantCultureIgnoreCase))
                {
                    args.Add("/language");
                    args.Add(lang);
                }
                if (!string.IsNullOrWhiteSpace(wszDefaultNamespace))
                {
                    args.Add("/namespace");
                    args.Add(wszDefaultNamespace);
                }

                var sw = new StringWriter();
                var ec = global::Rolex.Program.Run(args.ToArray(), TextReader.Null, TextWriter.Null, sw);
                log = sw.ToString();

                var isSuccess = 0 == ec;
                if (isSuccess)
                {
                    
                    EnvDTE.ProjectItem outitm = item.ProjectItems.AddFromFile(outputfile);
                    
                }
                else
                {
                    pGenerateProgress.GeneratorError(0, 0, "Rolex returned error code: " + ec.ToString(), unchecked((uint)-1), unchecked((uint)-1));
                }

            }
            catch (Exception ex)
            {
                pGenerateProgress.GeneratorError(0, 0, "Rolex custom tool failed with: " + ex.Message, unchecked((uint)-1), unchecked((uint)-1));
                log += "Rolex custom tool failed with: " + ex.Message;
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