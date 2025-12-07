<html>
<body>
    <!-- Puter AI-based Image Generation -->
    <script src="https://js.puter.com/v2/"></script>
    <script>
        async function generateImage(prompt, width, height) {
            const imageElement = await puter.ai.txt2img(prompt, {
                model: "google/gemini-3-pro-image", provider: "together-ai", disable_safety_checker: true, width: width, height: height
            });
            // Return the base64 string of the generated image
            return imageElement.src.split(',')[1];
        }
    </script>
</body>
</html>
