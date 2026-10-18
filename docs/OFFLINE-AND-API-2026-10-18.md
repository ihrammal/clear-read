# Offline OCR and optional API implementation — 18 October 2026

Clear Read's primary recognition is free local Arabic, English and French OCR. AppSettings defaults to offline-only mode, provider None, and all API document permissions false. Recognize Document and batch OCR use LocalOcrService; they do not call any API provider or need a key. Startup/manual update checks are blocked in offline-only mode.

Optional online processing is text correction after local OCR. Supported providers: OpenAI Responses, Gemini generateContent and Groq chat completions. Each provider has a separate Windows-DPAPI-protected key and persisted model ID. Official HTTPS endpoints are fixed in code, redirects are disabled, and keys are request headers rather than URL/body fields. No shared application key is shipped. No automatic provider fallback or paid retry is implemented.

The provider settings show key-creation and Test API key buttons for the selected provider. Key tests require offline-only mode to be disabled and send only the fixed synthetic string `Clear Read API connection test.` using that provider/model. This validates an actual completed generation, including response permission/quota availability at that moment. Tests can consume free quota or incur charges on a paid plan. They do not send editor text and do not require document-sharing permission. Changing the key/model clears the prior success status.

Document correction additionally requires document-sharing permission, correction permission and explicit per-request confirmation. The user selects text, or explicitly confirms all editor text, up to 12,000 characters. The proposed result is reviewed separately, with Apply/Cancel and Undo. Empty, blocked, malformed, refused or incomplete results cannot replace local text. Failure/cancellation retains local text and offline OCR. Provider quotas can require shorter selections. There are no background API calls triggered by saving a key or running local OCR.

Defaults checked against provider documentation on 18 October 2026: OpenAI gpt-5.6-terra; Gemini gemini-3.5-flash-lite; Groq openai/gpt-oss-20b. Account access and availability must be tested with the user's own key. API adapters were tested with dummy credentials and simulated responses, not with live paid accounts. No actual provider key was printed, bundled or used for these tests.

OpenAI usage is generally paid. Gemini and Groq offer limited free tiers/plans for eligible accounts/models/regions; no unlimited-free promise is made. Gemini's free-tier data-use notice is shown before document transmission. Offline OCR remains free regardless of API account availability.

References:

- OpenAI text/Responses: https://developers.openai.com/api/docs/guides/text
- OpenAI model: https://developers.openai.com/api/docs/models/gpt-5.6-terra
- OpenAI pricing: https://developers.openai.com/api/docs/pricing
- OpenAI data controls: https://developers.openai.com/api/docs/guides/your-data
- Gemini generation: https://ai.google.dev/api/generate-content
- Gemini keys: https://ai.google.dev/gemini-api/docs/api-key
- Gemini plans/data use: https://ai.google.dev/gemini-api/docs/pricing
- Groq API: https://console.groq.com/docs/api-reference
- Groq models: https://console.groq.com/docs/models
- Groq free-plan limits: https://console.groq.com/docs/rate-limits

OpenAI requests set store:false; this is not a promise of zero provider retention. Other providers use their own account/plan data terms. Provider error response bodies are never shown, because they can echo sensitive input. Image description, table reconstruction and custom providers remain explicitly disabled.

Validation commands: scripts/test-windows.ps1 runs the full local OCR and API suite. For only the simulated API checks, run .tools/dotnet/dotnet.exe run --project tests/AccessibleOCRStudio.SmokeTests -c Release -- --api-checks-only. No provider credentials are needed. The offline regression run uses unreachable HTTP_PROXY/HTTPS_PROXY/ALL_PROXY addresses, skips NuGet restore, and invokes local native engines. This is not a packet-capture or physically disconnected-machine test.

Before publication: complete a live test using each desired provider's own key and validate the new settings, status and review workflow with assistive technology. Native redistribution, clean-machine and GitHub-access gates in RELEASE-PUBLISH-CHECKLIST-1.0.0.md remain open.


API setup follow-up: the user reached Settings saved without a correction review. Saving settings alone does not correct editor text. The correction command now offers setup for incomplete configuration, reloads the selected protected key after Save, revalidates permissions, and continues to the explicit send-confirmation prompt. Separate settings saves explain that no correction has run and name the OCR menu action. The updated build passed all 94 focused API checks; manual end-to-end confirmation remains pending.


User confirmation: Gemini successfully corrected the sample to "The quick brown fox." after explicit sharing permission. NVDA keyboard navigation to the proposed correction and Apply worked. This confirms that sample and workflow; it does not establish Groq/OpenAI live correction, Cancel or universal recognition accuracy. Authenticated GitHub REST access now confirms push/admin permissions on the private ihrammal/clear-read repository.
