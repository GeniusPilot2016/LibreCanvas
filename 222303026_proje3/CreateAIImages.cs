using GenerativeAI;
using GenerativeAI.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;
using Python.Runtime;
using System.Drawing;
using System.IO;

namespace _222303026_proje3
{
    public static class CreateAIImages
    {
        public static async Task<Image> CreateImage(string prompt)
        {
            try
            {
                return await Task.Run(() =>
                {
                    Application.DoEvents();
                    Runtime.PythonDLL = Application.StartupPath + @"\Python\python313.dll";
                    PythonEngine.Initialize();
                    using (Py.GIL())
                    {
                        var script = Py.Import("AIImageCreator");
                        var inputText = new PyString(prompt);
                        var apiKey = new PyString(EncryptionHelper.DecryptString1(Settings1.Default.HashedGeminiAIAPIKey));
                        var outputImagePyObject = script.InvokeMethod("create", new PyObject[] { inputText, apiKey });
                        var outputImageBytes = outputImagePyObject.As<byte[]>();
                        PythonEngine.Shutdown();
                        using (var ms = new MemoryStream(outputImageBytes))
                        {
                            Image img = Image.FromStream(ms);
                            return img; 
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public static async Task<Image> EditImage(Image inputImage, string prompt)
        {
            try
            {
                return await Task.Run(() =>
                {
                    Application.DoEvents();
                    Runtime.PythonDLL = Application.ExecutablePath + @"\Python\python310.dll";
                    PythonEngine.Initialize();
                    using (Py.GIL())
                    {
                        var script = Py.Import("AIImageEditor");
                        var apiKey = new PyString(EncryptionHelper.DecryptString1(Settings1.Default.HashedGeminiAIAPIKey));
                        var inputText = new PyString(prompt);
                        var inputImagePyObject = PyObject.FromManagedObject(inputText);
                        var promptPyObject = new PyString(prompt);
                        var outputImagePyObject = script.InvokeMethod("edit", new PyObject[] { promptPyObject, inputImagePyObject, apiKey });
                        var outputImageBytes = outputImagePyObject.As<byte[]>();
                        PythonEngine.Shutdown();
                        using (var ms = new MemoryStream(outputImageBytes))
                        {
                            Image img = Image.FromStream(ms);
                            return img;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
    }
}
