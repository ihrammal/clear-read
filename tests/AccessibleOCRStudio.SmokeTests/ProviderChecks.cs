using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Controls;
using AccessibleOCRStudio;
using AccessibleOCRStudio.Services;

internal static class ProviderChecks
{
    public static void Run(string temp,Action<bool,string> check)
    {
        const string text="مرحبا بالعالم\nBonjour le monde.\nThe quick fox.";
        var handler=new FakeHandler(); using var client=new HttpClient(handler); var service=new ApiCorrectionService(client);
        AppSettings Settings(string provider) => new() { ApiProvider=provider,OfflineOnlyMode=false,AllowApiDocumentContent=true,AllowApiOcrCorrection=true };
        foreach(var provider in new[] { "OpenAI","Gemini","Groq" })
        {
            var settings=Settings(provider); settings.OfflineOnlyMode=true;
            Refuses(()=>service.TestConnectionAsync(settings,"dummy-key").GetAwaiter().GetResult(),check,provider+" key test makes no request in offline-only mode");
            Refuses(()=>service.CorrectAsync(text,settings,"dummy-key",true).GetAwaiter().GetResult(),check,provider+" document correction makes no request in offline-only mode");
        }
        check(handler.Calls==0,"All three providers keep offline mode free of API requests");
        foreach(var provider in new[] { "Gemini","Groq" })
        {
            var settings=Settings(provider); var key="dummy-"+provider;
            handler.Status=HttpStatusCode.OK;
            handler.Body=provider=="Gemini" ? JsonSerializer.Serialize(new { candidates=new[] { new { finishReason="STOP",content=new { parts=new object[] { new { thought=true,text="hidden reasoning" },new { text } } } } } }) : JsonSerializer.Serialize(new { choices=new[] { new { finish_reason="stop",message=new { role="assistant",content=text } } } });
            var result=service.CorrectAsync(text,settings,key,true).GetAwaiter().GetResult();
            check(result==text,provider+" parses Arabic/English/French output and excludes reasoning");
            check(!handler.Url!.Contains(key) && !handler.BodySent!.Contains(key),provider+" key is absent from URL and request body");
            if(provider=="Gemini")
            {
                using var sent=JsonDocument.Parse(handler.BodySent!);
                check(handler.Url=="https://generativelanguage.googleapis.com/v1beta/models/"+settings.GeminiModel+":generateContent" && handler.GoogleKey==key && handler.Authorization==null,"Gemini uses its official endpoint and key header without forwarding an OpenAI key");
                check(sent.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString()==text,"Gemini sends only the selected document text");
            }
            else
            {
                using var sent=JsonDocument.Parse(handler.BodySent!);
                check(handler.Url=="https://api.groq.com/openai/v1/chat/completions" && handler.Authorization=="Bearer "+key && handler.GoogleKey==null,"Groq uses its official endpoint and its own bearer key");
                check(sent.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()==text,"Groq sends only the selected document text");
            }
            service.TestConnectionAsync(settings,key).GetAwaiter().GetResult();
            using(var probe=JsonDocument.Parse(handler.BodySent!))
            {
                var sample=provider=="Gemini"?probe.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString():probe.RootElement.GetProperty("messages")[1].GetProperty("content").GetString();
                check(sample=="TextBridge API connection test.",provider+" Test API key makes an actual generation request using only a fixed sample");
            }
            var count=handler.Calls;
            settings.AllowApiDocumentContent=false;
            Refuses(()=>service.CorrectAsync(text,settings,key,true).GetAwaiter().GetResult(),check,provider+" document-sharing permission is enforced");
            settings.AllowApiDocumentContent=true; settings.AllowApiOcrCorrection=false;
            Refuses(()=>service.CorrectAsync(text,settings,key,true).GetAwaiter().GetResult(),check,provider+" correction permission is enforced");
            settings.AllowApiOcrCorrection=true;
            Refuses(()=>service.CorrectAsync(text,settings,key,false).GetAwaiter().GetResult(),check,provider+" sending requires per-request confirmation");
            check(count==handler.Calls,provider+" refused requests never reach the network");
            foreach(var status in new[] { HttpStatusCode.BadRequest,HttpStatusCode.Unauthorized,HttpStatusCode.Forbidden,HttpStatusCode.NotFound,HttpStatusCode.TooManyRequests,HttpStatusCode.InternalServerError,HttpStatusCode.Redirect })
            {
                handler.Status=status; handler.Body="private-response-must-not-be-shown"; count=handler.Calls; string? error=null;
                try { service.TestConnectionAsync(settings,key).GetAwaiter().GetResult(); } catch(InvalidOperationException ex) { error=ex.Message; }
                check(error!=null && !error.Contains(handler.Body) && handler.Calls==count+1,provider+$" key test safely reports HTTP {(int)status} with no provider fallback");
            }
            handler.Status=HttpStatusCode.OK;
            handler.Body=provider=="Gemini"?"{\"candidates\":[{\"finishReason\":\"MAX_TOKENS\"}]}":"{\"choices\":[{\"finish_reason\":\"length\"}]}";
            Refuses(()=>service.CorrectAsync(text,settings,key,true).GetAwaiter().GetResult(),check,provider+" truncated results never replace local text");
            handler.Body="{}";
            Refuses(()=>service.CorrectAsync(text,settings,key,true).GetAwaiter().GetResult(),check,provider+" empty or blocked results never replace local text");
            handler.Disconnected=true;
            Refuses(()=>service.TestConnectionAsync(settings,key).GetAwaiter().GetResult(),check,provider+" connection failure keeps offline OCR available");
            handler.Disconnected=false;
        }
        var folder=Path.Combine(temp,"provider-keys"); var credentials=new CredentialService(folder);
        foreach(var provider in new[] { "OpenAI","Gemini","Groq" }) credentials.Save(provider,"dummy-"+provider);
        var window=new ApiSettingsWindow(Settings("OpenAI"),credentials); var combo=(ComboBox)window.FindName("Provider");
        foreach(var provider in new[] { "OpenAI","Gemini","Groq" })
        {
            combo.SelectedItem=combo.Items.Cast<ComboBoxItem>().Single(i=>i.Content.ToString()==provider);
            check(((TextBlock)window.FindName("ProviderSelectionStatus")).Text==provider+" selected." && System.Windows.Automation.AutomationProperties.GetName(combo)=="API provider: "+provider,provider+" selection updates its own accessible announcement without refocusing");
            check(((PasswordBox)window.FindName("ApiKey")).Password=="dummy-"+provider,provider+" displays only its own saved Windows-protected key");
            check(((Button)window.FindName("TestApiKey")).Content.ToString()!.Contains(provider),provider+" has a clearly labeled Test API key action");
            check(((Button)window.FindName("CreateApiKey")).Content.ToString()!.Contains(provider) && ApiProviderCatalog.KeyUrl(provider).StartsWith("https://"),provider+" has a key-creation link to its own official website");
        }
        window.Close();
        var models=Settings("Groq"); ApiProviderCatalog.SetModel(models,"Groq","test/groq-model"); ApiProviderCatalog.SetModel(models,"Gemini","test-gemini-model");
        var path=Path.Combine(temp,"provider-settings.json"); models.Save(path); var loaded=AppSettings.Load(path);
        check(loaded.GroqModel=="test/groq-model" && loaded.GeminiModel=="test-gemini-model" && loaded.OpenAiModel==OpenAiCorrectionService.DefaultModel,"Provider model choices persist separately without overwriting other providers");
        check(!File.ReadAllText(path).Contains("dummy-"),"Settings JSON contains no provider API keys");
    }
    private static void Refuses(Action action,Action<bool,string> check,string label)
    { var failed=false; try { action(); } catch(InvalidOperationException) { failed=true; } check(failed,label); }
    private sealed class FakeHandler:HttpMessageHandler
    {
        public int Calls; public string? Url,BodySent,Authorization,GoogleKey; public string Body="{}"; public HttpStatusCode Status=HttpStatusCode.OK; public bool Disconnected;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); if(Disconnected) throw new HttpRequestException("private transport details");
            Calls++; Url=request.RequestUri!.AbsoluteUri; Authorization=request.Headers.Authorization?.ToString(); GoogleKey=request.Headers.TryGetValues("x-goog-api-key",out var values)?values.Single():null;
            BodySent=request.Content==null?null:await request.Content.ReadAsStringAsync(token);
            return new HttpResponseMessage(Status) { Content=new StringContent(Body,Encoding.UTF8,"application/json") };
        }
    }
}

