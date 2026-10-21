namespace AccessibleOCRStudio.Services;

public static class ApiProviderCatalog
{
    public static bool IsSupported(string provider) => provider is "OpenAI" or "Gemini" or "Groq";
    public static string KeyUrl(string provider) => provider switch
    {
        "OpenAI" => OpenAiCorrectionService.ApiKeysUrl,
        "Gemini" => "https://aistudio.google.com/apikey",
        "Groq" => "https://console.groq.com/keys",
        _ => string.Empty
    };
    public static string Model(AppSettings settings, string provider) => provider switch
    {
        "OpenAI" => settings.OpenAiModel,
        "Gemini" => settings.GeminiModel,
        "Groq" => settings.GroqModel,
        _ => string.Empty
    };
    public static void SetModel(AppSettings settings, string provider, string model)
    {
        switch(provider) { case "OpenAI": settings.OpenAiModel=model; break; case "Gemini": settings.GeminiModel=model; break; case "Groq": settings.GroqModel=model; break; }
    }
    public static string Notice(string provider) => provider switch
    {
        "OpenAI" => "OpenAI API usage is generally paid. Use your own account/key and check your billing limits.",
        "Gemini" => "Gemini offers limited free-tier usage for eligible accounts/models/regions. Free-tier content may be used to improve Google products. Paid billing can apply; check your Google account and terms.",
        "Groq" => "Groq offers a limited free plan for eligible accounts/models. Quotas apply; paid plan charges may apply. Check your Groq account and terms.",
        _ => "Offline OCR is free and needs no account or key. Select a provider only if you want optional online text correction."
    };
}
