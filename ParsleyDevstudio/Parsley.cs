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
    [InstalledProductRegistration("Parsley", "Generate Composable Recursive Descent Parsers", "0.1.5.0")]
    [Guid("1A9FE6C2-6287-49DE-A277-F8BA92959492")]
    [ComVisible(true)]
    [ProvideObject(typeof(Parsley))]
    [CodeGeneratorRegistration(typeof(Parsley), "Parsley", "{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}", GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(typeof(Parsley),"Parsley", "{164b10b9-b200-11d0-8c61-00a0c91e29d5}",GeneratesDesignTimeSource =true)]
    public sealed class Parsley : IVsSingleFileGenerator, IObjectWithSite
    {
        object _site;
        Array _projects;
        ServiceProvider _serviceProvider;

        public Parsley()
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
            string outputfile = null;
            string gplexfile = null;
            string gplexshared = null;
            string gplexcode = null;
            string rolexfile = null;
            string log="";
            try
            {
                if (null == _site)
                    throw new InvalidOperationException("The Parsley custom tool can only be used in a design time environment. Consider using Parsley as a pre-build step instead.");
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
                var parserFile = Path.GetFileNameWithoutExtension(wszInputFilePath) +"."+lang;
                // we don't add it to our generated files if it already exists
                var addShared = null==_FindItem(Path.Combine(dir, "GplexShared.cs"));
                var args = new List<string>();
                args.Add(wszInputFilePath);
                args.Add("/output");
                args.Add(Path.Combine(dir, parserFile));
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
                if (!addShared)
                    args.Add("/noshared");
                var sw = new StringWriter();
                var ec = global::Parsley.Program.Run(args.ToArray(), TextReader.Null, TextWriter.Null, sw);
                log = sw.ToString();
                var isSuccess = 0 == ec;
                var outputPath = Path.Combine(dir, parserFile);
                if (isSuccess)
                {
                    var files = new List<string>();
                    var sr = new StringReader(log);
                    string line = sr.ReadLine();
                    while (null != (line = sr.ReadLine()))
                    {
                        var idx = line.IndexOf("file: ", StringComparison.InvariantCulture);
                        if (0 > idx)
                            break;
                        var key = line.Substring(0, idx - 1);
                        var file = Path.GetFullPath(line.Substring(idx + 6));
                        if (0 == string.Compare("Output", key, StringComparison.InvariantCulture))
                            outputfile = file;
                        if (0 == string.Compare("Rolex", key, StringComparison.InvariantCulture))
                            rolexfile = file;
                        if (0 == string.Compare("Gplex", key, StringComparison.InvariantCulture))
                            gplexfile = file;
                        if (0 == string.Compare("Gplex shared code", key, StringComparison.InvariantCulture))
                            gplexshared = file;
                        if (0 == string.Compare("Gplex tokenizer code", key, StringComparison.InvariantCulture))
                            gplexcode = file;
                        files.Add(line.Substring(idx + 6));
                    }
                    if (addShared)
                    {
                        if (null == gplexshared)
                            addShared = false;
                    }
                    for (int ic = files.Count, i = 0; i < ic; ++i)
                    {
                        var file = Path.GetFullPath(files[i]);
                        EnvDTE.ProjectItem outitm = item.ProjectItems.AddFromFile(file);
                    }
                    // attempt to set the custom tool for the gplex lexer file
                    if (null != gplexfile)
                    {
                        var itm = _FindItem(gplexfile);
                        if (null != itm)
                        {
                            EnvDTE.Property prop = itm.Properties.Item("CustomTool");
                            prop.Value = typeof(Gplex).Name;
                        }
                    }
                    // now set the tool for the rolex file
                    if (null != rolexfile)
                    {
                        var itm = _FindItem(rolexfile);
                        if (null != itm)
                        {
                            EnvDTE.Property prop = itm.Properties.Item("CustomTool");
                            prop.Value = typeof(Rolex).Name;
                        }
                    }
                } else
                {
                   pGenerateProgress.GeneratorError(0,0,"Parsley returned error code: " + ec.ToString(), unchecked((uint)-1), unchecked((uint)-1));
                }

            }
            catch (Exception ex)
            {
                pGenerateProgress.GeneratorError(0, 0, "Parsley custom tool failed with: " + ex.Message, unchecked((uint)-1), unchecked((uint)-1));
                log += "Parsley custom tool failed with: " + ex.Message;
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