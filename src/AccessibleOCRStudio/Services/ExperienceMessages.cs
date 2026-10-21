using System.Globalization;
namespace AccessibleOCRStudio.Services;

// Keep document recognition language independent from message language.
public static class ExperienceMessages
{
    public static string Language(string? language) => language switch
    {
        "ar" or "en" or "fr" => language,
        _ => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName is "ar" ? "ar"
            : CultureInfo.CurrentUICulture.TwoLetterISOLanguageName is "fr" ? "fr" : "en"
    };
    public static string Get(string key, string? language) => (key, Language(language)) switch
    {
        ("welcome", "ar") => "مرحباً بك في TextBridge من إيمان رمال",
        ("welcome", "fr") => "Bienvenue dans TextBridge, par Iman Rammal",
        ("welcome", _) => "Welcome to TextBridge by Iman Rammal",
        ("goodbye", "ar") => "شكراً لاستخدامك البرنامج. إلى اللقاء.",
        ("goodbye", "fr") => "Merci d’avoir utilisé mon programme. Au revoir.",
        ("goodbye", _) => "Thank you for using my program. Goodbye.",
        ("started", "ar") => "بدأ التعرف على النص.",
        ("started", "fr") => "Reconnaissance démarrée.",
        ("started", _) => "Recognition started.",
        ("complete", "ar") => "اكتمل التعرف على النص.",
        ("complete", "fr") => "Reconnaissance terminée.",
        ("complete", _) => "Recognition complete.",
        ("failed", "ar") => "تعذر التعرف على النص.",
        ("failed", "fr") => "Échec de la reconnaissance.",
        ("failed", _) => "Recognition failed.",
        ("openFirst", "ar") => "افتح ملف PDF أو صورة أولاً.",
        ("openFirst", "fr") => "Ouvrez d’abord un PDF ou une image.",
        ("openFirst", _) => "Open a PDF or image file first.",
        ("unsaved", "ar") => "هل تريد حفظ النص قبل المتابعة؟ اختر لا لتجاهل التغييرات، أو إلغاء لمواصلة العمل.",
        ("unsaved", "fr") => "Enregistrer le texte avant de continuer ? Choisissez Non pour abandonner les modifications, ou Annuler pour continuer à travailler.",
        ("unsaved", _) => "Save the recognized text before continuing? Choose No to discard unsaved text, or Cancel to keep working.",
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown experience message")
    };
}
