using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;
using System.Text;

namespace _222303026_proje3
{
    public static class PuterJsWrapper
    {
        private static WebView2 _webView;

        public static void Initialize(WebView2 webView)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        }

        public static async Task<Image> GenerateImageAsync(string prompt, int width, int height, CancellationToken cancellationToken = default)
        {
            if (_webView == null)
            {
                throw new InvalidOperationException("PuterJsWrapper is not initialized. Call PuterJsWrapper.Initialize first.");
            }

            if (_webView.CoreWebView2 == null)
            {
                throw new InvalidOperationException("WebView2 is not initialized. Call EnsureCoreWebView2Async first.");
            }

            string script = $"generateImage('{System.Web.HttpUtility.JavaScriptStringEncode(prompt)}', {width}, {height});";

            // JavaScript yürütme görevini baþlatýn
            var scriptTask = _webView.CoreWebView2.ExecuteScriptAsync(script);

            // Ýptal için bekleyecek bir görev oluþturun
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => tcs.TrySetResult(true)))
            {
                // JavaScript görevi veya iptal görevi tamamlanana kadar bekleyin
                var completedTask = await Task.WhenAny(scriptTask, tcs.Task);

                // Eðer iptal görevi önce tamamlandýysa, bir istisna fýrlatýn
                if (completedTask == tcs.Task)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            // JavaScript görevi baþarýyla tamamlandý, sonucu alýn
            string base64Image = await scriptTask;

            // JSON dizesinden (ör. "\"base64...\"") týrnaklarý temizleyin
            if (!string.IsNullOrEmpty(base64Image) && base64Image.Length > 1 && base64Image.StartsWith("\"") && base64Image.EndsWith("\""))
            {
                base64Image = base64Image.Substring(1, base64Image.Length - 2);
            }

            if (string.IsNullOrEmpty(base64Image) || base64Image == "null")
            {
                return null;
            }

            // Base64 dizesini Görüntü'ye dönüþtürün
            byte[] imageBytes = Convert.FromBase64String(base64Image);
            using (var ms = new MemoryStream(imageBytes))
            {
                return Image.FromStream(ms);
            }
        }
    }
}