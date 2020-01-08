using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace ParsleyDevstudio
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("CountLines", "Generate XML with line count", "1.0")]
    [Guid("B1D25618-1FCF-42FB-B785-F097A8EF5DB6")]
    [ComVisible(true)]
    [ProvideObject(typeof(Gplex))]
    [CodeGeneratorRegistration(typeof(Gplex), "Gplex", "{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}", GeneratesDesignTimeSource = true)]
    public sealed class Gplex : IVsSingleFileGenerator
    {

        #region IVsSingleFileGenerator Members

        public int DefaultExtension(out string pbstrDefaultExtension)
        {
            pbstrDefaultExtension = ".log";
            return pbstrDefaultExtension.Length;
        }

        public int Generate(string wszInputFilePath, string bstrInputFileContents,
          string wszDefaultNamespace, IntPtr[] rgbOutputFileContents,
          out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
        {
            try
            {
                var dir = Path.GetDirectoryName(wszInputFilePath);
                var scannerFile = Path.GetFileNameWithoutExtension(wszInputFilePath)+".cs";
                
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "gplex";
                psi.CreateNoWindow = true;
                psi.Arguments = "\"" + wszInputFilePath.Replace("\"", "\"\"") + "\"";
                psi.Arguments += " /out:\""+Path.Combine(dir,scannerFile).Replace("\"","\"\"")+"\"";
                psi.RedirectStandardOutput = true;
                string text;
                using(var proc = new Process())
                {
                    proc.StartInfo = psi;
                    proc.Start();
                    text = proc.StandardOutput.ReadToEnd();
                }

                byte[] bytes = Encoding.UTF8.GetBytes(text);
                int length = bytes.Length;
                rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(length);
                Marshal.Copy(bytes, 0, rgbOutputFileContents[0], length);
                pcbOutput = (uint)length;
            }
            catch (Exception ex)
            {
                pcbOutput = 0;
            }
            return VSConstants.S_OK;
        }

        #endregion
    }
}