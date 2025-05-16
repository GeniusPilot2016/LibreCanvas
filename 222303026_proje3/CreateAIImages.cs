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

namespace _222303026_proje3
{
    public static class CreateAIImages
    {
        public static Image CreateImage(string prompt)
        {
            try
            {
                Application.DoEvents();
                Runtime.PythonDLL = Application.ExecutablePath + @"\Python\python310.dll";
                PythonEngine.Initialize();
                using (Py.GIL())
                {
                    var script = Py.Import("AIImageCreator");
                    var outputImagePyObject = script.InvokeMethod("create", new PyObject[] { new PyString(prompt) });
                    var outputImage = outputImagePyObject.As<Image>();
                    return outputImage;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public static Image EditImage(Image inputImage, string prompt)
        {
            try
            {
                Application.DoEvents();
                Runtime.PythonDLL = Application.ExecutablePath + @"\Python\python310.dll";
                PythonEngine.Initialize();
                using (Py.GIL())
                {
                    var script = Py.Import("AIImageEditor");
                    var inputImagePyObject = PyObject.FromManagedObject(inputImage);
                    var promptPyObject = new PyString(prompt);
                    var outputImagePyObject = script.InvokeMethod("edit", new PyObject[] { promptPyObject, inputImagePyObject });
                    var outputImage = outputImagePyObject.As<Image>();
                    return outputImage;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured: " + ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
    }
}
