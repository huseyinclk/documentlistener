using HidromasOzel;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileManager
{
    public class ReflectionHelper
    {
        public static object FunctionExec(String classCode, String mainClass, Object[] requiredAssemblies)
        {
            CSharpCodeProvider provider = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });

            CompilerParameters parameters = new CompilerParameters
            {
                GenerateExecutable = false,       // Create a dll
                GenerateInMemory = true,          // Create it in memory
                WarningLevel = 0,                 // Default warning level
                CompilerOptions = "/optimize",    // Optimize code
                TreatWarningsAsErrors = false     // Better be false to avoid break in warnings
            };

            //----------------
            // Add basic referenced assemblies
            parameters.ReferencedAssemblies.Add("system.dll");
            parameters.ReferencedAssemblies.Add("System.Data.dll");
            parameters.ReferencedAssemblies.Add("System.Data.OracleClient.dll");
            parameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");


            //----------------
            // Add all extra assemblies required
            foreach (var extraAsm in requiredAssemblies)
            {
                parameters.ReferencedAssemblies.Add(extraAsm as string);
            }

            //--------------------
            // Try to compile the code received
            CompilerResults results = provider.CompileAssemblyFromSource(parameters, classCode);

            //--------------------
            // If the compilation returned error, then return the CompilerErrorCollection class with the errors to the caller
            if (results.Errors.Count != 0)
            {
                return results.Errors;
            }

            //--------------------
            // Return the created class instance to caller
            return results.CompiledAssembly.CreateInstance(mainClass); ;
        }

        public static List<object> FunctionExec(string filename)
        {
            List<object> arguman = new List<object>();
            string koddosyasi = Application.StartupPath + "\\FileParser.xcs";
            if (File.Exists(koddosyasi))
            {
                string code = "";
                using (StreamReader reader = new StreamReader(new FileStream(koddosyasi, FileMode.Open, FileAccess.Read, FileShare.Read), Encoding.GetEncoding("windows-1254")))
                {
                    code = reader.ReadToEnd().Trim();
                }
                Object[] requiredAssemblies = new Object[] { };
                dynamic classRef;
                try
                {
                    classRef = ReflectionHelper.FunctionExec(code, "HidromasOzel.FileParser", requiredAssemblies);

                    //-------------------
                    // If the compilation process returned an error, then show to the user all errors
                    if (classRef is CompilerErrorCollection)
                    {
                        StringBuilder sberror = new StringBuilder();

                        foreach (CompilerError error in (CompilerErrorCollection)classRef)
                        {
                            sberror.AppendLine(string.Format("{0}:{1} {2} {3}", error.Line, error.Column, error.ErrorNumber, error.ErrorText));
                        }

                        Logger.V(sberror.ToString());

                        return arguman;
                    }

                    arguman = classRef.DosyaTuru(Path.GetFileNameWithoutExtension(filename), AppSettingHelper.GetConnectionString());
                }
                catch (Exception ex)
                {
                    // If something very bad happened then throw it
                    //MessageBox.Show(ex.Message);
                    Logger.E(string.Concat("Prosedür yürütme hatasi:", ex.Message, ",Detay:", ex.StackTrace));
                    return arguman;
                }
            }
            else
            {

                using (StreamWriter wr = new StreamWriter(new FileStream(koddosyasi, FileMode.Create, FileAccess.Write, FileShare.Write), Encoding.GetEncoding("windows-1254")))
                {
                    wr.Write(ReflectionHelper.DosyaIcerik("FileManager.FileParser.xcs"));
                    wr.Flush();
                    wr.Close();
                }

                //FileParser ozel = new FileParser();
                //ozel.DosyaTuru(Path.GetFileNameWithoutExtension(filename), AppSettingHelper.GetConnectionString());
            }

            return arguman;
        }

        public static string[] Dosyalar()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceNames();
        }

        public static string DosyaIcerik(string resname)
        {
            using (StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(resname)))//"FileManager.HidromasOzel.txt")))
            {
                string datas = reader.ReadToEnd();
                reader.Close();
                return datas;
            }
        }
    }
}
