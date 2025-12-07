<html>
<body>
    <h1>Nano Banana Pro Image Generation</h1>
    <script src="https://js.puter.com/v2/"></script>
    <script>
        puter.ai.txt2img("A vintage movie poster for 'The Last Voyage', featuring bold art deco typography with the tagline 'An Adventure Beyond Time' and 'Coming Soon 2024' at the bottom", { 
            model: "google/gemini-3-pro-image", provider: "together-ai", disable_safety_checker: true
        })
        .then(imageElement => {
            document.body.appendChild(imageElement);
        });
    </script>
</body>
</html>
